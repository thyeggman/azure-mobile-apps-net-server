// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Properties;

namespace Microsoft.Azure.Mobile.Server.Config
{
    /// <summary>
    /// Configures the specified <see cref="System.Web.Http.HttpConfiguration" /> with settings used by
    /// Azure Mobile Apps.
    /// </summary>
    public class MobileAppConfiguration : AppConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.Azure.Mobile.Server.Config.MobileAppConfiguration" /> class.
        /// </summary>
        public MobileAppConfiguration()
        {
            this.EnableApiControllers = false;
        }

        private bool EnableApiControllers { get; set; }

        /// <inheritdoc />
        public override void ApplyTo(HttpConfiguration config)
        {
            if (config.GetMobileAppConfiguration() != null)
            {
                throw new InvalidOperationException(RResources.ApplyTo_CalledTwice);
            }

            config.SetMobileAppConfiguration(this);

            base.ApplyTo(config);

            if (this.EnableApiControllers)
            {
                MapApiControllers(config);
            }
        }

        /// <summary>
        /// Maps all controllers with the <see cref="Microsoft.Azure.Mobile.Server.Config.MobileAppControllerAttribute"/> to the route
        /// template "api/{controller}/{id}".
        /// </summary>
        /// <returns>The current <see cref="Microsoft.Azure.Mobile.Server.Config.MobileAppConfiguration"/>.</returns>
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