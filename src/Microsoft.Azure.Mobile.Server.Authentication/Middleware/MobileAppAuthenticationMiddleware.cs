// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    /// The <see cref="MobileAppAuthenticationMiddleware"/> provides the OWIN middleware for authenticating a caller who has already authenticated using the Login controller, 
    /// or has provided HTTP basic authentication credentials matching either the application key or the master key (for admin access).
    /// </summary>
    public class MobileAppAuthenticationMiddleware : AuthenticationMiddleware<MobileAppAuthenticationOptions>
    {
        private readonly ILogger logger;
        private readonly IMobileAppTokenHandler tokenHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MobileAppAuthenticationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next <see cref="OwinMiddleware"/>.</param>
        /// <param name="appBuilder">The <see cref="IAppBuilder"/> to configure.</param>
        /// <param name="options">The options for this middleware.</param>
        /// <param name="tokenHandler">The <see cref="IMobileAppTokenHandler"/> to use for processing tokens.</param>
        public MobileAppAuthenticationMiddleware(OwinMiddleware next, IAppBuilder appBuilder, MobileAppAuthenticationOptions options, IMobileAppTokenHandler tokenHandler)
            : base(next, options)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException("appBuilder");
            }

            if (tokenHandler == null)
            {
                throw new ArgumentNullException("tokenHandler");
            }

            this.logger = appBuilder.CreateLogger<MobileAppAuthenticationMiddleware>();
            this.tokenHandler = tokenHandler;
        }

        /// <inheritdoc />
        protected override AuthenticationHandler<MobileAppAuthenticationOptions> CreateHandler()
        {
            return new MobileAppAuthenticationHandler(this.logger, this.tokenHandler);
        }
    }
}
