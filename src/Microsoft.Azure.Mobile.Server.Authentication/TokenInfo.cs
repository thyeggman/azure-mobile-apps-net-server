// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using System.IdentityModel.Tokens;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    ///  Contains user login information such as a security token. Used by <see cref="IMobileAppTokenHandler"/> as part of the mobile service authentication process.
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// A JSON Web Token (JWT).
        /// </summary>
        [CLSCompliant(false)]
        public JwtSecurityToken Token { get; set; }
    }
}
