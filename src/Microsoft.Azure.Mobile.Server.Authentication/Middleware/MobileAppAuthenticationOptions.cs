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
        private string realm = "Service";

        public MobileAppAuthenticationOptions()
            : base(AuthenticationName)
        {
            this.AuthenticationMode = AuthenticationMode.Active;
        }

        /// <summary>
        /// The realm to use for HTTP Basic Authentication which allows browsers to authenticate against 
        /// resources that require <see cref="System.Web.Http.AuthorizeAttribute"/> level access.
        /// </summary>        
        public string Realm
        {
            get
            {
                return this.realm;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                HttpHeaderUtils.ValidateToken(value);
                this.realm = value;
            }
        }

        /// <summary>
        /// Gets or sets the application signing key.
        /// </summary>
        public string SigningKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether signature validation should be performed on incoming authentication tokens. The default is false.
        /// </summary>
        /// <remarks>
        /// Only in advanced scenarios should this be set to true. For example if the application is behind another gateway component that is validating 
        /// the token signatures.
        /// </remarks>
        public bool SkipTokenSignatureValidation { get; set; }
    }
}
