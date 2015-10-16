// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile.Server.Properties;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    /// The <see cref="MobileAppAuthenticationHandler"/> authenticates a caller who has already authenticated using the Login controller,
    /// or has provided HTTP basic authentication credentials matching either the application key or the master key (for admin access).
    /// </summary>
    public class MobileAppAuthenticationHandler : AuthenticationHandler<MobileAppAuthenticationOptions>
    {
        public const string AuthenticationHeaderName = "x-zumo-auth";

        private readonly ILogger logger;
        private readonly IMobileAppTokenHandler tokenUtility;

        /// <summary>
        /// Initializes a new instance of the <see cref="MobileAppAuthenticationHandler"/> class with the given <paramref name="logger"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use for logging.</param>
        /// <param name="tokenHandler">The <see cref="IMobileAppTokenHandler"/> to use.</param>
        public MobileAppAuthenticationHandler(ILogger logger, IMobileAppTokenHandler tokenHandler)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            if (tokenHandler == null)
            {
                throw new ArgumentNullException("tokenHandler");
            }

            this.logger = logger;
            this.tokenUtility = tokenHandler;
        }

        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            return Task.FromResult(this.Authenticate(this.Request, this.Options));
        }

        protected virtual AuthenticationTicket Authenticate(IOwinRequest request, MobileAppAuthenticationOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            string token;
            try
            {
                ClaimsIdentity claimsIdentity = null;

                // We do not want any existing User to flow through.
                request.User = null;

                // If there is an auth token specified then validate it.
                token = request.Headers.Get(AuthenticationHeaderName);
                if (!string.IsNullOrEmpty(token))
                {
                    ClaimsPrincipal claimsPrincipal;
                    bool tokenIsValid = this.TryParseLoginToken(token, options, out claimsPrincipal);

                    if (!tokenIsValid)
                    {
                        this.logger.WriteInformation(RResources.Authentication_InvalidToken);
                        return null;
                    }

                    claimsIdentity = claimsPrincipal.Identity as ClaimsIdentity;
                    if (claimsIdentity == null)
                    {
                        this.logger.WriteError(RResources.Authentication_InvalidIdentity
                            .FormatForUser(typeof(IIdentity).Name, typeof(ClaimsIdentity).Name,
                            claimsPrincipal.Identity != null ? claimsPrincipal.Identity.GetType().Name : "unknown"));
                        return null;
                    }
                }
                else
                {
                    claimsIdentity = new ClaimsIdentity();
                }

                // Set the user for the current request                
                request.User = this.tokenUtility.CreateServiceUser(claimsIdentity, token);
            }
            catch (Exception ex)
            {
                this.logger.WriteError(RResources.Authentication_Error.FormatForUser(ex.Message), ex);
            }

            // If you return an actual AuthenticationTicket, Katana will create a new ClaimsIdentity and set 
            // that to Request.User, which overwrites our MobileAppUser. By setting Request.User ourselves and
            // returning null, that step is skipped and the current user remains a MobileAppUser.
            return null;
        }

        protected virtual bool TryParseLoginToken(string token, MobileAppAuthenticationOptions options, out ClaimsPrincipal claimsPrincipal)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (!options.SkipTokenSignatureValidation)
            {
                return this.tokenUtility.TryValidateLoginToken(token, options.SigningKey, out claimsPrincipal);
            }
            else
            {
                // With token signature validation turned off, we assume validation
                // has been done externally, and we trust all the claims.
                return MobileAppTokenHandler.GetClaimsPrincipalForPrevalidatedToken(token, out claimsPrincipal);
            }
        }
    }
}
