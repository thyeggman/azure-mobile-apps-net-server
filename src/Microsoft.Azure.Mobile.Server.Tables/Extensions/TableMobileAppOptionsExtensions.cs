// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Mobile.Server.Tables;
using Microsoft.Azure.Mobile.Server.Tables.Config;

namespace Microsoft.Azure.Mobile.Server.Config
{
    /// <summary>
    /// </summary>
    public static class TableMobileAppOptionsExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tableConfigProvider"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We only want this extension to apply to MobileAppConfiguration, not just any AppConfiguration")]
        public static MobileAppConfiguration WithTableControllerConfigProvider(this MobileAppConfiguration options, ITableControllerConfigProvider tableConfigProvider)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (tableConfigProvider == null)
            {
                throw new ArgumentNullException("tableConfigProvider");
            }

            options.RegisterConfigProvider(new TableMobileAppExtensionConfig(tableConfigProvider));
            return options;
        }

        /// <summary>
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static MobileAppConfiguration AddTables(this MobileAppConfiguration config)
        {
            MobileAppTableConfiguration tableConfig = new MobileAppTableConfiguration().MapTableControllers();
            AddTables(config, tableConfig);
            return config;
        }

        /// <summary>
        /// </summary>
        /// <param name="config"></param>
        /// <param name="tableConfig"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We only want this extension to apply to MobileAppConfiguration, not just any AppConfiguration")]
        public static MobileAppConfiguration AddTables(this MobileAppConfiguration config, MobileAppTableConfiguration tableConfig)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.RegisterConfigProvider(new TableMobileAppConfigProvider(tableConfig));
            return config;
        }
    }
}