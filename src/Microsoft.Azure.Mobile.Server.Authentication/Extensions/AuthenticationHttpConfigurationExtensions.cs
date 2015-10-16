// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Azure.Mobile.Server.Authentication;

namespace System.Web.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpConfiguration"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AuthenticationHttpConfigurationExtensions
    {
        private const string ServiceTokenHandlerKey = "MS_ServiceTokenHandler";

        public static IMobileAppTokenHandler GetMobileAppTokenHandler(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            IMobileAppTokenHandler handler;
            if (!config.Properties.TryGetValue(ServiceTokenHandlerKey, out handler))
            {
                handler = new MobileAppTokenHandler(config);
                config.Properties[ServiceTokenHandlerKey] = handler;
            }

            return handler;
        }

        public static void SetMobileAppTokenHandler(this HttpConfiguration config, IMobileAppTokenHandler handler)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Properties[ServiceTokenHandlerKey] = handler;
        }
    }
}