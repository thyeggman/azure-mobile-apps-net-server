// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Web.Http;

namespace Microsoft.Azure.Mobile.Server.Config
{
    public interface IAppConfiguration
    {
        void RegisterConfigProvider(IMobileAppExtensionConfigProvider provider);

        void ApplyTo(HttpConfiguration config);
    }
}
