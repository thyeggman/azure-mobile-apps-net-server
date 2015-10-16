// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.Azure.Mobile.Server.Config;

namespace Microsoft.Azure.Mobile.Server.Tables
{
    /// <summary>
    /// Performs configuration customizations for <see cref="TableController{TData}"/> derived controllers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TableControllerConfigAttribute : MobileAppControllerAttribute, IControllerConfiguration
    {
        /// <inheritdoc />
        public override void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            if (controllerDescriptor == null)
            {
                throw new ArgumentNullException("controllerDescriptor");
            }

            base.Initialize(controllerSettings, controllerDescriptor);

            ITableControllerConfigProvider configurationProvider = controllerDescriptor.Configuration.GetTableControllerConfigProvider();
            if (configurationProvider == null)
            {
                configurationProvider = new TableControllerConfigProvider();
            }

            configurationProvider.Configure(controllerSettings, controllerDescriptor);
        }
    }
}