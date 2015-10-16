// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.ComponentModel;

namespace System.Security.Claims
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ClaimsIdentityExtensions
    {
        /// <summary>
        /// Returns the first claim value if found, or null.
        /// </summary>
        public static string GetClaimValueOrNull(this ClaimsIdentity claimsIdentity, string claimType)
        {
            if (claimsIdentity == null)
            {
                throw new ArgumentNullException("claimsIdentity");
            }

            if (string.IsNullOrEmpty(claimType))
            {
                throw new ArgumentNullException("claimType");
            }

            Claim claim = claimsIdentity.FindFirst(claimType);
            if (claim != null)
            {
                return claim.Value;
            }

            return null;
        }
    }
}
