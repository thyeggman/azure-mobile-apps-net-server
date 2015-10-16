// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Properties;

namespace Microsoft.Azure.Mobile.Server.Config
{
    public abstract class AppConfiguration : IAppConfiguration
    {
        protected AppConfiguration()
        {
            this.ConfigProviders = new Dictionary<Type, IMobileAppExtensionConfigProvider>();
        }

        protected IDictionary<Type, IMobileAppExtensionConfigProvider> ConfigProviders { get; private set; }

        public virtual void RegisterConfigProvider(IMobileAppExtensionConfigProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            Type providerType = provider.GetType();
            if (this.ConfigProviders.ContainsKey(providerType))
            {
                throw new ArgumentException(RResources.ExtensionProvider_AlreadyExists.FormatInvariant(providerType));
            }

            this.ConfigProviders.Add(providerType, provider);
        }

        public virtual void ApplyTo(HttpConfiguration config)
        {
            foreach (IMobileAppExtensionConfigProvider provider in this.ConfigProviders.Values)
            {
                provider.Initialize(config);
            }
        }
    }
}
