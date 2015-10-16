// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication.AppService;

namespace Microsoft.Azure.Mobile.Server.AppService
{
    internal class AppServiceHttpClient : IDisposable
    {
        private const string ApiVersionValue = "2015-01-14";
        private const string XZumoAuthHeader = "x-zumo-auth";
        private const string RuntimeUserAgent = "MobileAppNetServerSdk";

        private HttpClient client;
        private Uri gatewayUri;
        private bool isDisposed;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "We do this to allow this class to be mockable")]
        internal AppServiceHttpClient(Uri gatewayUri)
        {
            this.gatewayUri = gatewayUri;
            this.client = this.CreateHttpClient();
        }

        internal virtual HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Calls the App Service Gateway to retrieve the token for the specified user and token name.
        /// </summary>
        /// <param name="authToken">The auth token that was issued for the current user. Used for authentication and identification.</param>
        /// <param name="tokenName">The name of the token to retrieve. 'Facebook', 'Google', or 'Twitter', for example.</param>
        /// <returns>A <see cref="TokenResult"/> with user details.</returns>
        internal virtual async Task<TokenResult> GetRawTokenAsync(string authToken, string tokenName)
        {
            if (authToken == null)
            {
                throw new ArgumentNullException("authToken");
            }

            if (tokenName == null)
            {
                throw new ArgumentNullException("tokenName");
            }

            Uri requestUri = new Uri(this.gatewayUri, "/api/tokens?tokenName={0}&api-version={1}".FormatInvariant(tokenName, ApiVersionValue));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AddHeaders(request, authToken);
            HttpResponseMessage response = await this.client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpResponseException(response);
            }

            return await response.Content.ReadAsAsync<TokenResult>();
        }

        private static void AddHeaders(HttpRequestMessage request, string authToken)
        {
            request.Headers.Add(XZumoAuthHeader, authToken);
            request.Headers.UserAgent.ParseAdd(RuntimeUserAgent);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    this.client.Dispose();
                }
            }
        }
    }
}