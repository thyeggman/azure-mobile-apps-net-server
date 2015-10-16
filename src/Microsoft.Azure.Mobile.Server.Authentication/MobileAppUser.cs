// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile.Server.AppService;
using Microsoft.Azure.Mobile.Server.Authentication.AppService;
using Microsoft.Azure.Mobile.Server.Properties;

namespace Microsoft.Azure.Mobile.Server.Authentication
{
    /// <summary>
    /// The <see cref="MobileAppUser"/> class is an <see cref="IPrincipal"/> implementation which provides information about how
    /// the user is authenticated using any of the supported authentication mechanisms.
    /// </summary>
    public class MobileAppUser : ClaimsPrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MobileAppUser"/> class from the specified <paramref name="identity"/>.
        /// </summary>
        /// <param name="identity">The identity from which to initialize the new claims principal.</param>
        public MobileAppUser(IIdentity identity)
            : base(identity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MobileAppUser"/> class from the specified <paramref name="identities"/>.
        /// </summary>
        /// <param name="identities">The identities from which to initialize the new claims principal.</param>
        public MobileAppUser(IEnumerable<ClaimsIdentity> identities)
            : base(identities)
        {
        }

        /// <summary>
        /// Gets or sets the user ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the token used to authenticate this User.
        /// </summary>
        public string MobileAppAuthenticationToken { get; set; }

        /// <summary>
        /// Gets the provider specific identity details for the ServiceUser
        /// </summary>
        /// <typeparam name="T">The provider type</typeparam>
        /// <returns>The provider credentials if found, otherwise null</returns>
        public async Task<T> GetIdentityAsync<T>() where T : ProviderCredentials, new()
        {
            if (this.Identity == null || !this.Identity.IsAuthenticated || string.IsNullOrEmpty(this.MobileAppAuthenticationToken))
            {
                return null;
            }

            string gatewayUrl = ConfigurationManager.AppSettings["EMA_RuntimeUrl"];
            if (gatewayUrl == null)
            {
                throw new NullReferenceException(RResources.Missing_EmaRuntimeUrl);
            }

            AppServiceHttpClient client = this.CreateAppServiceHttpClient(new Uri(gatewayUrl));

            ProviderCredentials credentials = (ProviderCredentials)new T();
            TokenResult tokenResult = await client.GetRawTokenAsync(this.MobileAppAuthenticationToken, credentials.Provider);

            if (!IsTokenValid(tokenResult))
            {
                return null;
            }

            PopulateProviderCredentials(tokenResult, credentials);

            return (T)credentials;
        }

        internal virtual AppServiceHttpClient CreateAppServiceHttpClient(Uri appServiceGatewayUrl)
        {
            return new AppServiceHttpClient(appServiceGatewayUrl);
        }

        internal static bool IsTokenValid(TokenResult tokenResult)
        {
            // TODO: Improve this when the AppService SDK gives us a way to differentiate between
            //       'token not found' and other errors.
            if (tokenResult == null || !string.IsNullOrWhiteSpace(tokenResult.Diagnostics))
            {
                return false;
            }

            return true;
        }

        internal static void PopulateProviderCredentials(TokenResult tokenResult, ProviderCredentials credentials)
        {
            if (tokenResult.Claims != null)
            {
                credentials.Claims = new Dictionary<string, string>(tokenResult.Claims);
            }

            FacebookCredentials facebookCredentials = credentials as FacebookCredentials;
            if (facebookCredentials != null)
            {
                facebookCredentials.AccessToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.AccessTokenName);
                facebookCredentials.UserId = tokenResult.Claims.GetValueOrDefault(ClaimTypes.NameIdentifier);
                return;
            }

            GoogleCredentials googleCredentials = credentials as GoogleCredentials;
            if (googleCredentials != null)
            {
                googleCredentials.AccessToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.AccessTokenName);
                googleCredentials.RefreshToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.RefreshTokenName);
                googleCredentials.UserId = tokenResult.Claims.GetValueOrDefault(ClaimTypes.NameIdentifier);

                string expiresOn = tokenResult.Properties.GetValueOrDefault("AccessTokenExpiration");
                if (!string.IsNullOrEmpty(expiresOn))
                {
                    googleCredentials.AccessTokenExpiration = DateTimeOffset.Parse(expiresOn, CultureInfo.InvariantCulture);
                }

                return;
            }

            AzureActiveDirectoryCredentials aadCredentials = credentials as AzureActiveDirectoryCredentials;
            if (aadCredentials != null)
            {
                aadCredentials.AccessToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.AccessTokenName);
                aadCredentials.ObjectId = tokenResult.Properties.GetValueOrDefault("ObjectId");
                aadCredentials.TenantId = tokenResult.Properties.GetValueOrDefault("TenantId");
                aadCredentials.UserId = tokenResult.Claims.GetValueOrDefault(ClaimTypes.NameIdentifier);
                return;
            }

            MicrosoftAccountCredentials microsoftAccountCredentials = credentials as MicrosoftAccountCredentials;
            if (microsoftAccountCredentials != null)
            {
                microsoftAccountCredentials.AccessToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.AccessTokenName);
                microsoftAccountCredentials.RefreshToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.RefreshTokenName);
                microsoftAccountCredentials.UserId = tokenResult.Claims.GetValueOrDefault(ClaimTypes.NameIdentifier);

                string expiresOn = tokenResult.Properties.GetValueOrDefault("AccessTokenExpiration");
                if (!string.IsNullOrEmpty(expiresOn))
                {
                    microsoftAccountCredentials.AccessTokenExpiration = DateTimeOffset.Parse(expiresOn, CultureInfo.InvariantCulture);
                }

                return;
            }

            TwitterCredentials twitterCredentials = credentials as TwitterCredentials;
            if (twitterCredentials != null)
            {
                twitterCredentials.AccessToken = tokenResult.Properties.GetValueOrDefault(TokenResult.Authentication.AccessTokenName);
                twitterCredentials.AccessTokenSecret = tokenResult.Properties.GetValueOrDefault("AccessTokenSecret");
                twitterCredentials.UserId = tokenResult.Claims.GetValueOrDefault(ClaimTypes.NameIdentifier);
                return;
            }
        }
    }
}