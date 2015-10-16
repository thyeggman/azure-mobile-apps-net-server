// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Http.Dispatcher;
using Microsoft.Azure.Mobile.Server.Cache;
using Microsoft.Azure.Mobile.Server.Config;

namespace System.Web.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpConfiguration"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        private const string AllowedMediaTypesKey = "MS_AllowedMediaTypes";
        private const string ConfigOptionsKey = "MS_ServiceConfigOptions";
        private const string MobileAppOptionsKey = "MS_MobileAppOptions";
        private const string IsSingletonKey = "MS_IsSingleton";
        private const string MobileAppSettingsProviderKey = "MS_MobileAppSettingsProvider";
        private const string CachePolicyProviderKey = "MS_CachePolicyProvider";

        /// <summary>
        /// Gets the set of allowed media types to be served by the ContentController./>.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>.</param>
        /// <returns>The set of allowed media types.</returns>
        public static ISet<string> GetAllowedMediaTypes(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            ISet<string> allowedMediaTypes;
            if (config.Properties.TryGetValue(AllowedMediaTypesKey, out allowedMediaTypes))
            {
                return allowedMediaTypes;
            }

            HashSet<string> mediaTypes = new HashSet<string>();
            config.Properties[AllowedMediaTypesKey] = mediaTypes;
            return mediaTypes;
        }

        /// <summary>
        /// Sets the set of allowed media types to be served by the ContentController"/>.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>.</param>
        /// <param name="allowedMediaTypes">The set of allowed media types.</param>
        public static void SetAllowedMediaTypes(this HttpConfiguration config, ISet<string> allowedMediaTypes)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Properties[AllowedMediaTypesKey] = allowedMediaTypes;
        }

        /// <summary>
        /// Gets a value indicating whether the service is guaranteed to run as a singleton service, i.e. only using one instance, or
        /// whether it is running in an environment with potentially multiple instances.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>.</param>
        /// <returns><c>true</c> is this service runs as a singleton instance; false if it potentially can run as multiple instances.</returns>
        public static bool GetIsSingletonInstance(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            bool isSingleton;
            if (!config.Properties.TryGetValue(IsSingletonKey, out isSingleton))
            {
                isSingleton = true;
                config.Properties[IsSingletonKey] = isSingleton;
            }

            return isSingleton;
        }

        /// <summary>
        /// Sets a value indicating whether the service is guaranteed to run as a singleton service, i.e. only using one instance, or
        /// whether it is running in an environment with potentially multiple instances.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>.</param>
        /// <param name="isSingleton">The value indicating whether the service is guaranteed to run as a single instance or not.</param>
        public static void SetIsSingletonInstance(this HttpConfiguration config, bool isSingleton)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Properties[IsSingletonKey] = isSingleton;
        }

        public static MobileAppConfiguration GetMobileAppConfiguration(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            MobileAppConfiguration options;
            config.Properties.TryGetValue(MobileAppOptionsKey, out options);
            return options;
        }

        public static void SetMobileAppConfiguration(this HttpConfiguration config, MobileAppConfiguration options)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Properties[MobileAppOptionsKey] = options;
        }

        public static IMobileAppSettingsProvider GetMobileAppSettingsProvider(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            IMobileAppSettingsProvider provider = null;

            if (provider == null)
            {
                if (!config.Properties.TryGetValue(MobileAppSettingsProviderKey, out provider))
                {
                    provider = new MobileAppSettingsProvider();
                    config.Properties[MobileAppSettingsProviderKey] = provider;
                }
            }

            return provider;
        }

        public static void SetMobileAppSettingsProvider(this HttpConfiguration config, IMobileAppSettingsProvider provider)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Properties[MobileAppSettingsProviderKey] = provider;
        }

        public static ICachePolicyProvider GetCachePolicyProvider(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            ICachePolicyProvider provider = config.Properties.GetValueOrDefault<ICachePolicyProvider>(CachePolicyProviderKey);
            return provider;
        }

        public static void SetCachePolicyProvider(this HttpConfiguration config, ICachePolicyProvider provider)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Properties[CachePolicyProviderKey] = provider;
        }

        public static HashSet<string> GetMobileAppControllerNames(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            IAssembliesResolver assemblyResolver = config.Services.GetAssembliesResolver();
            IHttpControllerTypeResolver controllerTypeResolver = config.Services.GetHttpControllerTypeResolver();
            Type[] controllerTypes = controllerTypeResolver.GetControllerTypes(assemblyResolver).ToArray();

            // Add controllers that have the MobileAppController attribute
            IEnumerable<string> matches = controllerTypes
                .Where(t => t.GetCustomAttributes(typeof(MobileAppControllerAttribute), true).Any())
                .Select(t => t.Name.Substring(0, t.Name.Length - DefaultHttpControllerSelector.ControllerSuffix.Length));

            HashSet<string> result = new HashSet<string>(matches, StringComparer.OrdinalIgnoreCase);
            return result;
        }
    }
}