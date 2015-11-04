// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    /// Provides an abstraction for handling security tokens. This abstraction can be used for validating security
    /// tokens and creating <see cref="ClaimsPrincipal"/> instances.
    /// </summary>
    public interface IMobileAppTokenHandler
    {
        /// <summary>
        /// Creates a <see cref="TokenInfo"/> containing a security token to be used as part of the Mobile Service authentication process.
        /// </summary>
        /// <param name="claims">The set of claims to include in the token.</param>
        /// <param name="lifetime">A <see cref="TimeSpan"/> indicating how long the token is valid for. To create a token with no expiry, use null.</param>
        /// <param name="secretKey">The secret key to sign the token with.</param>
        /// <returns>A <see cref="TokenInfo"/> containing a security token.</returns>
        TokenInfo CreateTokenInfo(IEnumerable<Claim> claims, TimeSpan? lifetime, string secretKey);

        /// <summary>
        /// Validates a string representation of a mobile service authentication token used to authenticate a user request.
        /// </summary>
        /// <param name="token">A <see cref="string"/> representation of the authentication token to validate.</param>
        /// <param name="audience">The valid audience to accept in token validation.</param>
        /// <param name="issuer">The valid issuer to accept in token validation.</param>
        /// <param name="options">The <see cref="MobileAppAuthenticationOptions"/> object that has the signing key,
        /// issuer, and audience.</param>
        /// <param name="claimsPrincipal">The resulting <see cref="ClaimsPrincipal"/> if the token is valid; null otherwise.</param>
        /// <returns><c>true</c> if <paramref name="token"/> is valid; otherwise <c>false</c>/</returns>
        bool TryValidateLoginToken(string token, string audience, string issuer, MobileAppAuthenticationOptions options, out ClaimsPrincipal claimsPrincipal);

        /// <summary>
        /// Creates a user id value contained within a <see cref="ProviderCredentials"/>. The user id is of the form
        /// <c>ProviderName:ProviderId</c> where the <c>ProviderName</c> is the unique identifier for the login provider
        /// and the <c>ProviderId</c> is the provider specific id for a given user.
        /// </summary>
        /// <param name="providerName">The login provider name.</param>
        /// <param name="providerUserId">The provider specific user id.</param>
        /// <returns>A formatted <see cref="string"/> representing the resulting value.</returns>
        string CreateUserId(string providerName, string providerUserId);

        /// <summary>
        /// Parses a user id into its two components: a <c>ProviderName</c> which uniquely identifies the login provider
        /// and the <c>ProviderId</c> which identifies the provider specific id for a given user.
        /// </summary>
        /// <param name="userId">The input value to parse.</param>
        /// <param name="providerName">The login provider name; or <c>null</c> if the <paramref name="userId"/> is not valid.</param>
        /// <param name="providerUserId">The provider specific user id; or <c>null</c> is the <paramref name="userId"/> is not valid.</param>
        /// <returns><c>true</c> if <paramref name="userId"/> is valid; otherwise <c>false</c>/</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "This is not unreasonable for this API.")]
        bool TryParseUserId(string userId, out string providerName, out string providerUserId);
    }
}