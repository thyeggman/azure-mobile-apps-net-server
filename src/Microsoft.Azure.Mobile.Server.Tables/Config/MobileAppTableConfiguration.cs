// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using Microsoft.Azure.Mobile.Server.Config;

namespace Microsoft.Azure.Mobile.Server.Tables.Config
{
    /// <summary>
    /// </summary>
    public class MobileAppTableConfiguration : AppConfiguration
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public MobileAppTableConfiguration MapTableControllers()
        {
            this.RegisterConfigProvider(new MapTableControllersExtensionConfigProvider());
            return this;
        }
    }
}