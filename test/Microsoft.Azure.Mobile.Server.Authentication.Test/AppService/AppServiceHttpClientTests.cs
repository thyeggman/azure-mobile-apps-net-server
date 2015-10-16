// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.AppService;
using Microsoft.Azure.Mobile.Server.Authentication.AppService;
using Microsoft.Azure.Mobile.Server.Config;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Authentication.Test.AppService
{
    public class AppServiceHttpClientTests
    {
        [Fact]
        public async Task GetRawTokenAsync_SendsCorrectRequest()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.SetMobileAppSettingsProvider(new MobileAppSettingsProvider());

            string accessToken = "11111";
            string facebookId = "22222";
            TokenResult tokenResult = CreateTokenResult(facebookId, accessToken);

            MockHttpMessageHandler handlerMock = new MockHttpMessageHandler(CreateResponse(tokenResult));

            var gatewayUri = "http://test";
            Mock<AppServiceHttpClient> appServiceClientMock = new Mock<AppServiceHttpClient>(new Uri(gatewayUri));
            appServiceClientMock.CallBase = true;
            appServiceClientMock.Setup(c => c.CreateHttpClient())
                .Returns(new HttpClient(handlerMock));

            // Act
            TokenResult result = await appServiceClientMock.Object.GetRawTokenAsync(accessToken, "Facebook");

            // Assert
            Assert.Equal(accessToken, result.Properties[TokenResult.Authentication.AccessTokenName]);
            Assert.Equal(gatewayUri + "/api/tokens?tokenName=Facebook&api-version=2015-01-14", handlerMock.ActualRequest.RequestUri.ToString());
            Assert.Equal(accessToken, handlerMock.ActualRequest.Headers.GetValues("X-ZUMO-AUTH").Single());
            Assert.Equal("MobileAppNetServerSdk", handlerMock.ActualRequest.Headers.GetValues("User-Agent").Single());
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task GetRawTokenAsync_Throws_IfResponseIsNotSuccess(HttpStatusCode status)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.SetMobileAppSettingsProvider(new MobileAppSettingsProvider());

            var response = new HttpResponseMessage(status);
            MockHttpMessageHandler handlerMock = new MockHttpMessageHandler(response);
            var gatewayUri = "http://test";

            Mock<AppServiceHttpClient> appServiceClientMock = new Mock<AppServiceHttpClient>(new Uri(gatewayUri));
            appServiceClientMock.CallBase = true;
            appServiceClientMock.Setup(c => c.CreateHttpClient())
                .Returns(new HttpClient(handlerMock));

            // Act
            var ex = await Assert.ThrowsAsync<HttpResponseException>(() => appServiceClientMock.Object.GetRawTokenAsync("123456", "Facebook"));

            // Assert
            Assert.NotNull(ex);
            Assert.Same(response, ex.Response);
        }

        [Theory]
        [InlineData(null, "Facebook", "authToken")]
        [InlineData("123456", null, "tokenName")]
        [InlineData(null, null, "authToken")]
        public async Task GetRawTokenAsync_Throws_IfParametersAreNull(string first, string second, string parameterThatThrows)
        {
            AppServiceHttpClient appServiceClient = new AppServiceHttpClient(new Uri("http://testuri"));

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => appServiceClient.GetRawTokenAsync(first, second));

            // Assert
            Assert.NotNull(ex);
            Assert.Equal(parameterThatThrows, ex.ParamName);
        }

        private static HttpResponseMessage CreateResponse(TokenResult tokenResult)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent(JsonConvert.SerializeObject(tokenResult));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return response;
        }

        private static TokenResult CreateTokenResult(string nameIdentifier, string accessToken)
        {
            var claims = new Dictionary<string, string>
            {
                { ClaimTypes.NameIdentifier, nameIdentifier }
            };
            var properties = new Dictionary<string, string>
            {
                { TokenResult.Authentication.AccessTokenName, accessToken },
            };

            return new TokenResult
            {
                Claims = claims,
                Properties = properties
            };
        }

        public class MockHttpMessageHandler : HttpMessageHandler
        {
            private HttpResponseMessage response;

            public MockHttpMessageHandler(HttpResponseMessage response)
            {
                this.response = response;
            }

            public HttpRequestMessage ActualRequest { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.ActualRequest = request;
                return Task.FromResult(this.response);
            }
        }
    }
}