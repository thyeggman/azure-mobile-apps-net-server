// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Cache;
using Microsoft.Azure.Mobile.Server.Properties;

namespace Microsoft.Azure.Mobile.Server.Config
{
    public class MobileAppConfiguration : AppConfiguration
    {
        public MobileAppConfiguration()
        {
            this.EnableApiControllers = false;
        }

        private bool EnableApiControllers { get; set; }

        public IMobileAppSettingsProvider MobileAppSettingsProvider { get; set; }

        public override void ApplyTo(HttpConfiguration config)
        {
            if (config.GetMobileAppConfiguration() != null)
            {
                throw new InvalidOperationException(RResources.ApplyTo_CalledTwice);
            }

            config.SetMobileAppConfiguration(this);
            config.SetMobileAppSettingsProvider(this.MobileAppSettingsProvider);
            config.SetCachePolicyProvider(new CachePolicyProvider());

            base.ApplyTo(config);

            if (this.EnableApiControllers)
            {
                MapApiControllers(config);
            }
        }

        public MobileAppConfiguration MapApiControllers()
        {
            this.EnableApiControllers = true;
            return this;
        }

        private static void MapApiControllers(HttpConfiguration config)
        {
            HashSet<string> tableControllerNames = config.GetMobileAppControllerNames();
            SetRouteConstraint<string> apiControllerConstraint = new SetRouteConstraint<string>(tableControllerNames, matchOnExcluded: false);

            HttpRouteCollectionExtensions.MapHttpRoute(
                config.Routes,
                name: RouteNames.Apis,
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional },
                constraints: new { controller = apiControllerConstraint });
        }
    }
}