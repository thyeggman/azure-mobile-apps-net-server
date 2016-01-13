// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Newtonsoft.Json.Linq;
using Swashbuckle.Application;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Swagger.Test
{
    public class SwaggerDocsConfigExtensionsTests
    {
        [Fact]
        public async Task AppServiceAuthentication_AddsSecurityDefinition()
        {
            // Arrange
            TestServer server = SwashbuckleHelper.CreateSwaggerServer(c =>
            {
                c.AppServiceAuthentication("http://mysite", "google");
            }, null);

            // Act
            HttpResponseMessage swaggerResponse = await server.HttpClient.GetAsync("http://localhost/swagger/docs/v1");
            var swagger = await swaggerResponse.Content.ReadAsAsync<JObject>();
            var googleDef = swagger["securityDefinitions"]["google"];

            // Assert
            Assert.Equal("oauth2", googleDef["type"]);
            Assert.Equal("OAuth2 Implicit Grant", googleDef["description"]);
            Assert.Equal("implicit", googleDef["flow"]);
            Assert.Equal("http://mysite/.auth/login/google", googleDef["authorizationUrl"]);
            Assert.Equal("{}", googleDef["scopes"].ToString());
        }

        [Fact]
        public async Task AppServiceAuthentication_AddsAuthToAuthenticatedControllers()
        {
            // Arrange
            TestServer server = SwashbuckleHelper.CreateSwaggerServer(c =>
            {
                c.AppServiceAuthentication("http://mysite", "google");
            }, null);

            // Act
            HttpResponseMessage swaggerResponse = await server.HttpClient.GetAsync("http://localhost/swagger/docs/v1");
            var swagger = await swaggerResponse.Content.ReadAsAsync<JObject>();

            // Assert
            ValidateSecurity(swagger, "/api/Anonymous", "get", false);
            ValidateSecurity(swagger, "/api/Anonymous", "post", false);
            ValidateSecurity(swagger, "/api/Authenticated", "get", true);
            ValidateSecurity(swagger, "/api/Authenticated", "post", true);
            ValidateSecurity(swagger, "/api/MixedAuth", "get", false);
            ValidateSecurity(swagger, "/api/MixedAuth", "post", true);
        }

        private void ValidateSecurity(JObject swagger, string route, string action, bool expectSecurity)
        {
            var security = swagger["paths"][route][action]["security"];

            if (!expectSecurity)
            {
                Assert.Null(security);
                return;
            }

            Assert.NotNull(security);
            security = security as JArray;
            Assert.Equal(1, security.Count());
            var securityDef = security[0];
            Assert.Equal(1, securityDef.Count());
            Assert.Equal("google", ((JProperty)securityDef.First).Name);
            Assert.Empty(securityDef["google"] as JArray);
        }
    }
}