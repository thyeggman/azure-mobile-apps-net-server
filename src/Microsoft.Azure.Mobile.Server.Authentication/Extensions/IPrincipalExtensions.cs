// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Authentication.AppService;

namespace System.Security.Principal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IPrincipalExtensions
    {
        private const string ObjectIdentifierClaimType = @"http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string TenantIdClaimType = @"http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// Gets the identity provider specific identity details for the <see cref="IPrincipal"/> making the request. 
        /// </summary>
        /// <param name="principal">The <see cref="IPrincipal"/> object.</param>
        /// <param name="request">The request context.</param>
        /// <typeparam name="T">The provider type.</typeparam>
        /// <returns>The identity provider credentials if found, otherwise null.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "Extension method")]
        public static async Task<T> GetAppServiceIdentityAsync<T>(this IPrincipal principal, HttpRequestMessage request) where T : ProviderCredentials, new()
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            // Get the token from the request
            string zumoAuthToken = request.GetHeaderOrDefault("x-zumo-auth");
            if (string.IsNullOrEmpty(zumoAuthToken))
            {
                return null;
            }

            string webappUrl = request.GetDomainName();

            AppServiceHttpClient client = CreateAppServiceHttpClient(new Uri(webappUrl));

            ProviderCredentials credentials = (ProviderCredentials)new T();
            TokenEntry tokenEntry = await client.GetRawTokenAsync(zumoAuthToken, credentials.Provider);

            if (!IsTokenValid(tokenEntry))
            {
                return null;
            }

            PopulateProviderCredentials(tokenEntry, credentials);

            return (T)credentials;
        }

        internal static AppServiceHttpClient CreateAppServiceHttpClient(Uri webAppUrl)
        {
            return new AppServiceHttpClient(webAppUrl);
        }

        internal static bool IsTokenValid(TokenEntry tokenEntry)
        {
            if (tokenEntry == null)
            {
                return false;
            }

            return true;
        }

        internal static void PopulateProviderCredentials(TokenEntry tokenEntry, ProviderCredentials credentials)
        {
            if (tokenEntry.UserClaims != null)
            {
                credentials.Claims = new Dictionary<string, string>();
                foreach (ClaimSlim claim in tokenEntry.UserClaims)
                {
                    credentials.Claims[claim.Type] = claim.Value;
                }
            }

            FacebookCredentials facebookCredentials = credentials as FacebookCredentials;
            if (facebookCredentials != null)
            {
                facebookCredentials.AccessToken = tokenEntry.AccessToken;
                facebookCredentials.UserId = tokenEntry.UserId;
                return;
            }

            GoogleCredentials googleCredentials = credentials as GoogleCredentials;
            if (googleCredentials != null)
            {
                googleCredentials.AccessToken = tokenEntry.AccessToken;
                googleCredentials.RefreshToken = tokenEntry.RefreshToken;
                googleCredentials.UserId = tokenEntry.UserId;
                googleCredentials.AccessTokenExpiration = tokenEntry.ExpiresOn;

                return;
            }

            AzureActiveDirectoryCredentials aadCredentials = credentials as AzureActiveDirectoryCredentials;
            if (aadCredentials != null)
            {
                aadCredentials.AccessToken = tokenEntry.AccessToken;
                aadCredentials.ObjectId = credentials.Claims.GetValueOrDefault(ObjectIdentifierClaimType);
                aadCredentials.TenantId = credentials.Claims.GetValueOrDefault(TenantIdClaimType);
                aadCredentials.UserId = tokenEntry.UserId;
                return;
            }

            MicrosoftAccountCredentials microsoftAccountCredentials = credentials as MicrosoftAccountCredentials;
            if (microsoftAccountCredentials != null)
            {
                microsoftAccountCredentials.AccessToken = tokenEntry.AccessToken;
                microsoftAccountCredentials.RefreshToken = tokenEntry.RefreshToken;
                microsoftAccountCredentials.UserId = tokenEntry.UserId;
                microsoftAccountCredentials.AccessTokenExpiration = tokenEntry.ExpiresOn;

                return;
            }

            TwitterCredentials twitterCredentials = credentials as TwitterCredentials;
            if (twitterCredentials != null)
            {
                twitterCredentials.AccessToken = tokenEntry.AccessToken;
                twitterCredentials.AccessTokenSecret = tokenEntry.AccessTokenSecret;
                twitterCredentials.UserId = tokenEntry.UserId;

                return;
            }
        }
    }
}
