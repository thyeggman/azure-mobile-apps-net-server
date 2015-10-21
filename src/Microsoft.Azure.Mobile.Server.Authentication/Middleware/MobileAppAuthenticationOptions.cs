// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using Microsoft.Owin.Security;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    /// The <see cref="MobileAppAuthenticationOptions"/> provides options for the OWIN <see cref="MobileAppAuthenticationMiddleware"/> class.
    /// </summary>
    public class MobileAppAuthenticationOptions : AuthenticationOptions
    {
        public const string AuthenticationName = "MobileApp";

        public MobileAppAuthenticationOptions()
            : base(AuthenticationName)
        {
            this.AuthenticationMode = AuthenticationMode.Active;
        }

        /// <summary>
        /// Gets or sets the application signing key.
        /// </summary>
        public string SigningKey { get; set; }
    }
}
