// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    /// Provides a default implementation of the <see cref="IMobileAppTokenHandler"/> interface.
    /// </summary>
    public class MobileAppTokenHandler : IMobileAppTokenHandler
    {
        internal const string ZumoIssuerValue = "urn:microsoft:windows-azure:zumo";
        internal const string ZumoAudienceValue = ZumoIssuerValue;

        private readonly JsonSerializerSettings tokenSerializerSettings = GetTokenSerializerSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="MobileAppTokenHandler"/> class.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> for this instance.</param>
        public MobileAppTokenHandler(HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.tokenSerializerSettings = GetTokenSerializerSettings();
        }

        /// <inheritdoc />
        public virtual TokenInfo CreateTokenInfo(IEnumerable<Claim> claims, TimeSpan? lifetime, string secretKey)
        {
            if (claims == null)
            {
                throw new ArgumentNullException("claims");
            }

            if (lifetime != null && lifetime < TimeSpan.Zero)
            {
                string msg = CommonResources.ArgMustBeGreaterThanOrEqualTo.FormatForUser(TimeSpan.Zero);
                throw new ArgumentOutOfRangeException("lifetime", lifetime, msg);
            }

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException("secretKey");
            }

            // add the claims passed in
            Collection<Claim> finalClaims = new Collection<Claim>();
            foreach (Claim claim in claims)
            {
                finalClaims.Add(claim);
            }

            // add our standard claims
            finalClaims.Add(new Claim("ver", "3"));

            Claim uidClaim = finalClaims.SingleOrDefault(p => p.Type == ClaimTypes.NameIdentifier);
            if (uidClaim != null)
            {
                finalClaims.Remove(uidClaim);
                finalClaims.Add(new Claim("uid", uidClaim.Value));
            }

            return CreateTokenFromClaims(finalClaims, secretKey, ZumoAudienceValue, ZumoIssuerValue, lifetime);
        }

        /// <inheritdoc />
        public virtual bool TryValidateLoginToken(string token, string secretKey, out ClaimsPrincipal claimsPrincipal)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            if (secretKey == null)
            {
                throw new ArgumentNullException("secretKey");
            }

            JwtSecurityToken parsedToken = null;
            try
            {
                parsedToken = new JwtSecurityToken(token);
            }
            catch (ArgumentException)
            {
                // happens if the token cannot even be read
                // i.e. it is malformed
                claimsPrincipal = null;
                return false;
            }

            TokenValidationParameters validationParams = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = ZumoAudienceValue,
                ValidateIssuer = true,
                ValidIssuer = ZumoIssuerValue,
                ValidateLifetime = parsedToken.Payload.Exp.HasValue  // support tokens with no expiry
            };

            return TryValidateToken(validationParams, token, secretKey, out claimsPrincipal);
        }

        /// <inheritdoc />
        public virtual MobileAppUser CreateServiceUser(ClaimsIdentity claimsIdentity, string authToken)
        {
            MobileAppUser user = new MobileAppUser(claimsIdentity);

            if (user.Identity.IsAuthenticated)
            {
                // Determine the user ID based on either the uid or NameIdentifier claims.
                string userIdValue = claimsIdentity.GetClaimValueOrNull(ClaimTypes.NameIdentifier) ?? claimsIdentity.GetClaimValueOrNull("uid");
                string prefix;
                string userId;
                if (!string.IsNullOrEmpty(userIdValue) &&
                    this.TryParseUserId(userIdValue, out prefix, out userId))
                {
                    user.Id = userIdValue;
                    user.MobileAppAuthenticationToken = authToken;
                }
                else
                {
                    // if no user name specified or the format is invalid,
                    // set to anonymous
                    SetAnonymousUser(user);
                }
            }

            return user;
        }

        /// <inheritdoc />
        public virtual string CreateUserId(string providerName, string providerUserId)
        {
            if (providerName == null)
            {
                throw new ArgumentNullException("providerName");
            }

            return "{0}:{1}".FormatInvariant(providerName, providerUserId);
        }

        /// <inheritdoc />
        public virtual bool TryParseUserId(string userId, out string providerName, out string providerUserId)
        {
            if (userId == null)
            {
                providerName = null;
                providerUserId = null;
                return false;
            }

            string[] parts = userId.Split(new char[] { ':' }, 2);
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
            {
                providerName = parts[0];
                providerUserId = parts[1];
                return true;
            }
            else
            {
                providerName = null;
                providerUserId = null;
                return false;
            }
        }

        internal static MobileAppUser SetAnonymousUser(MobileAppUser user)
        {
            if (user != null)
            {
                user.Id = null;
                user.MobileAppAuthenticationToken = null;
            }

            return user;
        }

        [CLSCompliant(false)]
        public static bool TryValidateToken(TokenValidationParameters validationParameters, string tokenValue, string secretKey, out ClaimsPrincipal claimsPrincipal)
        {
            if (validationParameters == null)
            {
                throw new ArgumentNullException("validationParameters");
            }

            claimsPrincipal = null;

            try
            {
                claimsPrincipal = ValidateToken(validationParameters, tokenValue, secretKey);
            }
            catch (SecurityTokenException)
            {
                // can happen if the token fails validation for any reason,
                // e.g. wrong signature, etc.
                return false;
            }
            catch (ArgumentException)
            {
                // happens if the token cannot even be read
                // i.e. it is malformed
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the specified JWT token string against the specified secret key. Exceptions thrown by the validation method
        /// are not caught.
        /// </summary>
        /// <param name="token">The JWT token string to validate.</param>
        /// <param name="secretKey">The key to use in the validation.</param>
        /// <exception cref="System.ArgumentException">Thrown if the JWT token is malformed</exception>
        /// <exception cref="System.IdentityModel.Tokens.SecurityTokenValidationException">Thrown if the JWT token fails validation.</exception>
        /// <exception cref="System.IdentityModel.Tokens.SecurityTokenExpiredException">Thrown if the JWT token is expired.</exception>
        /// <exception cref="System.IdentityModel.Tokens.SecurityTokenNotYetValidException">Thrown if the JWT token is not yet valid.</exception>
        public static void ValidateToken(string token, string secretKey)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("token");
            }

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException("secretKey");
            }

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidAudience = ZumoAudienceValue,
                ValidateIssuer = true,
                ValidIssuer = ZumoIssuerValue
            };

            ValidateToken(validationParameters, token, secretKey);
        }

        internal static ClaimsPrincipal ValidateToken(TokenValidationParameters validationParams, string tokenString, string secretKey)
        {
            List<BinarySecretSecurityToken> signingTokens = new List<BinarySecretSecurityToken>();
            signingTokens.Add(new BinarySecretSecurityToken(GetSigningKey(secretKey)));
            validationParams.IssuerSigningTokens = signingTokens;

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken = null;

            return tokenHandler.ValidateToken(tokenString, validationParams, out validatedToken);
        }

        public static TokenInfo CreateTokenFromClaims(IEnumerable<Claim> claims, string secretKey, string audience, string issuer, TimeSpan? lifetime)
        {
            byte[] signingKey = GetSigningKey(secretKey);
            BinarySecretSecurityToken signingToken = new BinarySecretSecurityToken(signingKey);
            SigningCredentials signingCredentials = new SigningCredentials(new InMemorySymmetricSecurityKey(signingToken.GetKeyBytes()), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", "http://www.w3.org/2001/04/xmlenc#sha256");
            DateTime created = DateTime.UtcNow;

            // we allow for no expiry (if lifetime is null)
            DateTime? expiry = (lifetime != null) ? created + lifetime : null;

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                AppliesToAddress = audience,
                TokenIssuerName = issuer,
                SigningCredentials = signingCredentials,
                Lifetime = new Lifetime(created, expiry),
                Subject = new ClaimsIdentity(claims),
            };

            JwtSecurityTokenHandler securityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = securityTokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;

            return new TokenInfo { Token = token };
        }

        internal static byte[] GetSigningKey(string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException("secretKey");
            }

            UTF8Encoding encoder = new UTF8Encoding(true, true);
            byte[] computeHashInput = encoder.GetBytes(secretKey);
            byte[] signingKey = null;

            using (var sha256Provider = new SHA256Managed())
            {
                signingKey = sha256Provider.ComputeHash(computeHashInput);
            }

            return signingKey;
        }

        /// <summary>
        /// This method is ONLY to be used in cases where the SkipTokenSignatureValidation option is turned on
        /// and we can safely assume that any incoming tokens are valid and their claims can be trusted.
        /// </summary>
        /// <param name="tokenValue">The token to be parsed.</param>
        /// <param name="claimsPrincipal">The resulting claims principal.</param>
        /// <returns>True if the token can be parsed successfully.</returns>
        internal static bool GetClaimsPrincipalForPrevalidatedToken(string tokenValue, out ClaimsPrincipal claimsPrincipal)
        {
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = ZumoAudienceValue,
                ValidateIssuer = true,
                ValidIssuer = ZumoIssuerValue,
                ValidateLifetime = false,
            };

            SkipSignatureJwtSecurityTokenHandler tokenHandler = new SkipSignatureJwtSecurityTokenHandler();
            claimsPrincipal = null;
            try
            {
                SecurityToken validatedToken;
                claimsPrincipal = tokenHandler.ValidateToken(tokenValue, validationParameters, out validatedToken);
            }
            catch (SecurityTokenException)
            {
                // can happen if the token fails validation for any reason,
                // e.g. wrong signature, etc.
                return false;
            }
            catch (ArgumentException)
            {
                // happens if the token cannot even be read
                // i.e. it is malformed
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the serialized JSON for this credentials object that should
        /// be returned in the claims of Mobile Service JWT tokens.
        /// </summary>
        /// <returns>The claim value</returns>
        internal string ToClaimValue(ProviderCredentials credentials)
        {
            return JsonConvert.SerializeObject(credentials, Formatting.None, this.tokenSerializerSettings);
        }

        internal static JsonSerializerSettings GetTokenSerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),

                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
                TypeNameHandling = TypeNameHandling.None
            };
        }

        /// <summary>
        /// This token handler is only used in scenarios where signature verification
        /// does NOT need to be performed on tokens (e.g. when the runtime is running behind
        /// a gateway that is already doing token validation, and the runtime is not publically
        /// exposed).
        /// <remarks>
        /// We use this mechanism of skipping signature validation because we want to preserve
        /// all the other side effects that the base token handler causes. For example, it does
        /// claim name mapping, sets the resulting principal to authenticated, etc. We don't
        /// want to try to simulate all of those.
        /// </remarks>
        /// </summary>
        private class SkipSignatureJwtSecurityTokenHandler : JwtSecurityTokenHandler
        {
            protected override JwtSecurityToken ValidateSignature(string token, TokenValidationParameters validationParameters)
            {
                return new JwtSecurityToken(token);
            }
        }
    }
}