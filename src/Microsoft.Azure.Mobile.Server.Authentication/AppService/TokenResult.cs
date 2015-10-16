// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Azure.Mobile.Server.Authentication.AppService
{
    internal class TokenResult
    {
        public TokenResult()
        {
            this.Properties = new Dictionary<string, string>();
            this.Claims = new Dictionary<string, string>();
        }

        public IDictionary<string, string> Claims { get; set; }

        public string Diagnostics { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public static class Authentication
        {
            public const string AccessTokenName = "AccessToken";
            public const string RefreshTokenName = "RefreshToken";
        }
    }
}