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
        public static IAppBuilder UseAppServiceAuthentication(this IAppBuilder appBuilder, HttpConfiguration config, AppServiceAuthenticationMode appServiceAuthMode, AuthenticationMode mode = AuthenticationMode.Active)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException("appBuilder");
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            MobileAppAuthenticationOptions serviceOptions = GetMobileAppAuthenticationOptions(config, mode);
            IMobileAppTokenHandler tokenHandler = config.GetMobileAppTokenHandler();
            appBuilder.UseAppServiceAuthentication(config, appServiceAuthMode, serviceOptions, tokenHandler);

            return appBuilder;
        }

        public static IAppBuilder UseAppServiceAuthentication(this IAppBuilder appBuilder, HttpConfiguration config, AppServiceAuthenticationMode appServiceAuthMode, MobileAppAuthenticationOptions options, IMobileAppTokenHandler tokenHandler)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException("appBuilder");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            MobileAppSettingsDictionary settings = config.GetMobileAppSettingsProvider().GetMobileAppSettings();
            bool runningInAzure = !string.IsNullOrEmpty(settings.HostName);

            if ((appServiceAuthMode == AppServiceAuthenticationMode.LocalOnly && !runningInAzure)
                            || appServiceAuthMode == AppServiceAuthenticationMode.Always)
            {
                appBuilder.Use(typeof(MobileAppAuthenticationMiddleware), new object[]
                {
                    appBuilder,
                    options,
                    tokenHandler
                });
            }
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
                SigningKey = settings.SigningKey,
            };

            return serviceOptions;
        }
    }
}
