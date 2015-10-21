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

            return CreateTokenFromClaims(finalClaims, secretKey, lifetime);
        }

        /// <inheritdoc />
        public virtual bool TryValidateLoginToken(string token, string audience, string issuer, MobileAppAuthenticationOptions options, out ClaimsPrincipal claimsPrincipal)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
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
                ValidAudience = audience,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateLifetime = parsedToken.Payload.Exp.HasValue  // support tokens with no expiry
            };

            return TryValidateToken(validationParams, token, options.SigningKey, out claimsPrincipal);
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

        internal static ClaimsPrincipal SetAnonymousUser(ClaimsPrincipal user)
        {
            ClaimsIdentity identity = user.Identity as ClaimsIdentity;
            foreach (Claim claim in identity.Claims)
            {
                if (claim.Type == ClaimTypes.NameIdentifier || claim.Type == "uid")
                {
                    identity.TryRemoveClaim(claim);
                }
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
        /// <param name="audience">The audience to use in validation.</param>
        /// <param name="issuer">The issuer to use in validation.</param>
        /// <exception cref="System.ArgumentException">Thrown if the JWT token is malformed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if one of the method parameters are null or empty.</exception>
        /// <exception cref="System.IdentityModel.Tokens.SecurityTokenValidationException">Thrown if the JWT token fails validation.</exception>
        /// <exception cref="System.IdentityModel.Tokens.SecurityTokenExpiredException">Thrown if the JWT token is expired.</exception>
        /// <exception cref="System.IdentityModel.Tokens.SecurityTokenNotYetValidException">Thrown if the JWT token is not yet valid.</exception>
        public static void ValidateToken(string token, string secretKey, string audience, string issuer)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            if (secretKey == null)
            {
                throw new ArgumentNullException("secretKey");
            }

            if (audience == null)
            {
                throw new ArgumentNullException("audience");
            }

            if (issuer == null)
            {
                throw new ArgumentNullException("issuer");
            }

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuer = true,
                ValidIssuer = issuer,
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

        public static TokenInfo CreateTokenFromClaims(IEnumerable<Claim> claims, string secretKey, TimeSpan? lifetime, string audience = null, string issuer = null)
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
    }
}