// ---------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Web.Http;

namespace Microsoft.Azure.Mobile.Server.Config
{
    /// <summary>
    /// </summary>
    public interface IMobileAppExtensionConfigProvider
    {
        /// <summary>
        /// Initializes the extension.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/>.</param>        
        void Initialize(HttpConfiguration config);
    }
}
