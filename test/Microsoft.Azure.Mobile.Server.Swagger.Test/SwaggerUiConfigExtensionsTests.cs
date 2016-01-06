// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Swashbuckle.Application;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Swagger.Test
{
    public class SwaggerUiConfigExtensionsTests
    {
        [Fact]
        public async Task SwaggerUiDocs()
        {
            // Arrange
            TestServer server = SwashbuckleHelper.CreateSwaggerServer(null, c =>
            {
                c.MobileAppUi();
            });

            string o2cExpected = GetResourceString("Microsoft.Azure.Mobile.Server.Swagger.o2c.html");
            string oauthExpected = GetResourceString("Microsoft.Azure.Mobile.Server.Swagger.swagger-oauth.js");

            // Act
            var o2cResponse = await server.HttpClient.GetAsync("http://localhost/swagger/ui/o2c-html");
            var oauthResponse = await server.HttpClient.GetAsync("http://localhost/swagger/ui/lib/swagger-oauth-js");

            string o2c = await o2cResponse.Content.ReadAsStringAsync();
            string oauth = await oauthResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(o2cExpected, o2c);
            Assert.Equal(oauthExpected, oauth);
        }

        private static string GetResourceString(string resourceName)
        {
            string resourceText;

            using (Stream stream = typeof(SwaggerUiConfigExtensions).Assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    resourceText = reader.ReadToEnd();
                }
            }

            return resourceText;
        }
    }
}