// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;
using Microsoft.Owin.Security;

namespace Owin
{
    /// <summary>
    /// Extension methods for <see cref="IAppBuilder"/>.
    /// </summary>
    public static class MobileAppAuthenticationAppBuilderExtensions
    {
        public static IAppBuilder UseMobileAppAuthentication(this IAppBuilder appBuilder, HttpConfiguration config, AuthenticationMode mode = AuthenticationMode.Active)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException("appBuilder");
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            // Add the service authentication middleware
            MobileAppAuthenticationOptions serviceOptions = GetMobileAppAuthenticationOptions(config, mode);
            IMobileAppTokenHandler tokenHandler = config.GetMobileAppTokenHandler();
            appBuilder.UseMobileAppAuthentication(serviceOptions, tokenHandler);

            return appBuilder;
        }

        /// <summary>
        /// Adds authentication using the built-in <see cref="MobileAppAuthenticationMiddleware"/> authentication model.
        /// </summary>
        /// <param name="appBuilder">The <see cref="IAppBuilder"/> passed to the configuration method.</param>
        /// <param name="options">Middleware configuration options.</param>
        /// <param name="tokenHandler">An <see cref="MobileAppTokenHandler"/> instance.</param>
        /// <returns>The updated <see cref="IAppBuilder"/>.</returns>
        public static IAppBuilder UseMobileAppAuthentication(this IAppBuilder appBuilder, MobileAppAuthenticationOptions options, IMobileAppTokenHandler tokenHandler)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException("appBuilder");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            appBuilder.Use(typeof(MobileAppAuthenticationMiddleware), new object[]
            {
                appBuilder,
                options,
                tokenHandler
            });

            return appBuilder;
        }

        /// <summary>
        /// Gets the <see cref="MobileAppAuthenticationOptions" /> that will be used by the <see cref="MobileAppAuthenticationHandler"/>./>
        /// </summary>
        /// <returns>The <see cref="MobileAppAuthenticationOptions" /> to use.</returns>
        private static MobileAppAuthenticationOptions GetMobileAppAuthenticationOptions(HttpConfiguration config, AuthenticationMode mode)
        {
            IMobileAppSettingsProvider settingsProvider = config.GetMobileAppSettingsProvider();
            MobileAppSettingsDictionary settings = settingsProvider.GetMobileAppSettings();

            MobileAppAuthenticationOptions serviceOptions = new MobileAppAuthenticationOptions
            {
                AuthenticationMode = mode,
                SigningKey = settings.SigningKey
            };

            return serviceOptions;
        }
    }
}
