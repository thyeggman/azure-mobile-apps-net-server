// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;
using Microsoft.Azure.Mobile.Server.Tables.Config;
using Microsoft.Owin.Testing;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;

namespace Microsoft.Azure.Mobile.Server
{
    public class SecuredControllerTests
    {
        private const string TestLocalhostName = "http://localhost/";

        [Fact]
        public async Task AnonymousAction_AnonymousRequest_ReturnsOk()
        {
            TestContext context = TestContext.Create();

            HttpResponseMessage response = await context.Client.GetAsync("api/secured/anonymous");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            JObject actual = await response.Content.ReadAsAsync<JObject>();
            Assert.Equal(JTokenType.Null, actual["id"].Type);
        }

        [Fact]
        public async Task AnonymousAction_AuthTokenInRequest_ReturnsOk()
        {
            TestContext context = TestContext.Create();
            string audience = TestLocalhostName;
            string issuer = TestLocalhostName;

            JwtSecurityToken token = context.GetTestToken(context.Settings.SigningKey, audience, issuer);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/anonymous");
            request.Headers.Add(MobileAppAuthenticationHandler.AuthenticationHeaderName, token.RawData);
            HttpResponseMessage response = await context.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            JObject result = await response.Content.ReadAsAsync<JObject>();

            Assert.Equal("Facebook:1234", result["id"]);
        }

        [Fact]
        public async Task InvalidAuthToken_WrongSigningKey_ReturnsUnauthorized()
        {
            TestContext context = TestContext.Create();
            string audience = TestLocalhostName;
            string issuer = TestLocalhostName;

            JwtSecurityToken token = context.GetTestToken("wrongkey", audience, issuer);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/application");
            request.Headers.Add(MobileAppAuthenticationHandler.AuthenticationHeaderName, token.RawData);
            HttpResponseMessage response = await context.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task InvalidAuthToken_ToAnonymousAction_ReturnsOk()
        {
            TestContext context = TestContext.Create();

            string malformedToken = "no way is this a jwt";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/anonymous");
            request.Headers.Add(MobileAppAuthenticationHandler.AuthenticationHeaderName, malformedToken);
            HttpResponseMessage response = await context.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task InvalidAuthToken_ToAuthorizedAction_ReturnsUnauthorized()
        {
            TestContext context = TestContext.Create();

            string malformedToken = "no way is this a jwt";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/application");
            request.Headers.Add(MobileAppAuthenticationHandler.AuthenticationHeaderName, malformedToken);
            HttpResponseMessage response = await context.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ApplicationAction_AppKeyNotInRequest_ReturnsForbidden()
        {
            TestContext context = TestContext.Create();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/application");
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UserAction_TokenNotInRequest_ReturnsForbidden()
        {
            TestContext context = TestContext.Create();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/user");
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UserAction_TokenInRequest_ReturnsOk()
        {
            TestContext context = TestContext.Create();
            string audience = TestLocalhostName;
            string issuer = TestLocalhostName;

            JwtSecurityToken token = context.GetTestToken(context.Settings.SigningKey, audience, issuer);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/user");
            request.Headers.Add(MobileAppAuthenticationHandler.AuthenticationHeaderName, token.RawData);
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("x-zumo-auth")]
        [InlineData("X-ZUMO-AUTH")]
        [InlineData("X-ZuMo-AuTh")]
        public async Task UserAction_TokenInRequest_CaseInsensitiveHeader_ReturnsOk(string authHeaderName)
        {
            TestContext context = TestContext.Create();
            string audience = TestLocalhostName;
            string issuer = TestLocalhostName;

            JwtSecurityToken token = context.GetTestToken(context.Settings.SigningKey, audience, issuer);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/user");
            request.Headers.Add(authHeaderName, token.RawData);
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AdminAction_MasterKeyNotInRequest_ReturnsForbidden()
        {
            TestContext context = TestContext.Create();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/secured/admin");
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("api/secured/anonymous")]
        [InlineData("api/secured/application")]
        [InlineData("api/secured/user")]
        public async Task TokenInRequest_CanInvokeUserLevelAndBelow(string action)
        {
            TestContext context = TestContext.Create();
            string audience = TestLocalhostName;
            string issuer = TestLocalhostName;

            JwtSecurityToken token = context.GetTestToken(context.Settings.SigningKey, audience, issuer);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, action);
            request.Headers.Add(MobileAppAuthenticationHandler.AuthenticationHeaderName, token.RawData);
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("api/secured/application")]
        [InlineData("api/secured/user")]
        [InlineData("api/secured/admin")]
        public async Task AnonymousRequest_CannotInvokeSecuredActions(string action)
        {
            TestContext context = TestContext.Create();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, action);
            HttpResponseMessage response = await context.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private class TestContext
        {
            public HttpConfiguration Config { get; private set; }

            public HttpClient Client { get; private set; }

            public MobileAppSettingsDictionary Settings { get; private set; }

            public static TestContext Create()
            {
                TestContext context = new TestContext();
                context.Config = new HttpConfiguration();
                TestServer server = context.CreateTestServer(context.Config);
                context.Client = server.HttpClient;
                context.Settings = context.Config.GetMobileAppSettingsProvider().GetMobileAppSettings();
                return context;
            }

            private TestServer CreateTestServer(HttpConfiguration config)
            {
                config.MapHttpAttributeRoutes();

                new MobileAppConfiguration()
                    .MapApiControllers()
                    .AddTables(
                        new MobileAppTableConfiguration()
                        .MapTableControllers())
                    .ApplyTo(config);

                // setup test authorization config values
                IMobileAppSettingsProvider settingsProvider = config.GetMobileAppSettingsProvider();
                var settings = settingsProvider.GetMobileAppSettings();
                settings.SigningKey = "signing_key";

                return TestServer.Create((appBuilder) =>
                {
                    MobileAppAuthenticationOptions options = new MobileAppAuthenticationOptions()
                    {
                        SigningKey = settings.SigningKey,
                    };

                    appBuilder.UseMobileAppAuthentication(options, config.GetMobileAppTokenHandler());
                    appBuilder.UseWebApi(config);
                });
            }

            public JwtSecurityToken GetTestToken(string secretKey, string audience, string issuer)
            {
                Claim[] claims = new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "Facebook:1234"),
                    new Claim("custom_claim_1", "CustomClaimValue1"),
                    new Claim("custom_claim_2", "CustomClaimValue2"),
                    new Claim("aud", audience),
                    new Claim("iss", issuer),
                };

                TokenInfo info = this.Config.GetMobileAppTokenHandler().CreateTokenInfo(claims, TimeSpan.FromDays(30), secretKey);

                JwtSecurityToken token = info.Token;
                Assert.Equal(8, token.Claims.Count());

                return token;
            }
        }
    }
}