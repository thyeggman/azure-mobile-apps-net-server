// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;

namespace Microsoft.Azure.Mobile.Server.CrossDomain.Config
{
    public class CrossDomainExtensionConfigProvider : IMobileAppExtensionConfigProvider
    {
        private IEnumerable<string> domains;

        public const string CrossDomainBridgeRouteName = "CrossDomain";
        public const string CrossDomainLoginReceiverRouteName = "CrossDomainLoginReceiver";

        public CrossDomainExtensionConfigProvider()
        {
        }

        public CrossDomainExtensionConfigProvider(IEnumerable<string> domains)
        {
            this.domains = domains;
        }

        public void Initialize(HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (this.domains != null)
            {
                config.SetCrossDomainOrigins(this.domains);
            }

            HttpRouteCollectionExtensions.MapHttpRoute(
                config.Routes,
                name: CrossDomainBridgeRouteName,
                routeTemplate: "crossdomain/bridge",
                defaults: new { controller = "crossdomain" });

            HttpRouteCollectionExtensions.MapHttpRoute(
                config.Routes,
                name: CrossDomainLoginReceiverRouteName,
                routeTemplate: "crossdomain/loginreceiver",
                defaults: new { controller = "crossdomain" });
        }
    }
}
