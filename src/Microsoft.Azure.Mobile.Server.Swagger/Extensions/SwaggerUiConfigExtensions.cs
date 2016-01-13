// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Swashbuckle.Application
{
    public static class SwaggerUiConfigExtensions
    {
        [CLSCompliant(false)]
        public static SwaggerUiConfig MobileAppUi(this SwaggerUiConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Assembly thisAssembly = typeof(SwaggerUiConfigExtensions).Assembly;
            config.CustomAsset("o2c-html", thisAssembly, "Microsoft.Azure.Mobile.Server.Swagger.o2c.html");
            config.CustomAsset("lib/swagger-oauth-js", thisAssembly, "Microsoft.Azure.Mobile.Server.Swagger.swagger-oauth.js");

            return config;
        }
    }
}