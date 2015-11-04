// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Moq;
using TestUtilities;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Security
{
    public class MobileAppAuthenticationHandlerTests
    {
        private HttpConfiguration config;
        private MobileAppTokenHandler tokenHandler;
        private Mock<ILogger> loggerMock;

        private const string TestWebsiteUrl = @"https://faketestapp.faketestazurewebsites.net/";
        private const string TestSigningKey = "signing_key";
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public MobileAppAuthenticationHandlerTests()
        {
            this.config = new HttpConfiguration();
            this.tokenHandler = new MobileAppTokenHandler(this.config);
            this.loggerMock = new Mock<ILogger>();
        }

        [Flags]
        private enum AuthOptions
        {
            None = 0x00,
            UserAuthKey = 0x01
        }

        public static TheoryDataCollection<string, string, bool> KeysMatchingData
        {
            get
            {
                return new TheoryDataCollection<string, string, bool>
                {
                    { null, null, false },
                    { string.Empty, string.Empty, false },
                    { "你好世界", null, false },
                    { "你好世界", string.Empty, false },
                    { null, "你好世界", false },
                    { string.Empty, "你好世界", false },
                    { "hello", "Hello", false },
                    { "HELLO", "Hello", false },
                    { "hello", "hello", true },
                    { "你好世界", "你好世界", true },
                };
            }
        }

        public static TheoryDataCollection<string, string> AuthorizationData
        {
            get
            {
                return new TheoryDataCollection<string, string>
                {
                    { null, null },
                    { string.Empty, null },
                    { "Unknown OlBhc3N3b3Jk", null },
                    { "Basic Unknown", null },
                    { "Basic VXNlck5hbWU6", string.Empty },
                    { "Basic OlBhc3N3b3Jk", "Password" },
                    { "Basic VXNlck5hbWU6UGFzc3dvcmQ=", "Password" },
                    { "Basic OuS9oOWlveS4lueVjA==", "你好世界" },
                    { "Basic 5L2g5aW9OuS4lueVjA==", "世界" },
                };
            }
        }

        [Theory]
        [InlineData(TestSigningKey, true)]
        [InlineData("wrong_key", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void Authenticate_CorrectlyAuthenticates(string otherSigningKey, bool expectAuthenticated)
        {
            // Arrange
            MobileAppAuthenticationOptions optionsDefault = CreateTestOptions();
            optionsDefault.SigningKey = TestSigningKey;

            MobileAppAuthenticationOptions optionsOtherSigningKey = CreateTestOptions();
            optionsOtherSigningKey.SigningKey = otherSigningKey;

            var mock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, this.tokenHandler);
            var request = CreateAuthRequest(optionsDefault, new Uri(TestWebsiteUrl));

            // Act
            AuthenticationTicket authTicket = mock.Authenticate(request, optionsOtherSigningKey);

            // Assert            
            if (expectAuthenticated)
            {
                // ensure the AuthenticationTicket is set correctly
                Assert.NotNull(authTicket);
                Assert.NotNull(authTicket.Identity);
                Assert.True(authTicket.Identity.IsAuthenticated);
            }
            else
            {
                Assert.NotNull(authTicket);
                Assert.NotNull(authTicket.Identity);
                Assert.False(authTicket.Identity.IsAuthenticated);
            }
        }

        [Fact]
        public void Authenticate_FailsToAuthenticate_ValidIdentity_WithoutSigningKey()
        {
            // Arrange
            MobileAppAuthenticationOptions options = CreateTestOptions(TestSigningKey);
           
            var mock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, this.tokenHandler);
            var request = CreateAuthRequest(options, new Uri(TestWebsiteUrl), CreateTestIdentity());

            options.SigningKey = null;
                
            // Act
            AuthenticationTicket authticket = mock.Authenticate(request, options);

            // Assert            
            Assert.NotNull(authticket);
            Assert.NotNull(authticket.Identity);
            Assert.False(authticket.Identity.IsAuthenticated, "Expected Authenticate to fail without signing key specified in MobileAppAuthenticationOptions");
        }

        [Fact]
        public void Authenticate_FailsToAuthenticate_InvalidIdentity_WithValidSigningKey()
        {
            // Arrange
            MobileAppAuthenticationOptions options = CreateTestOptions();
            var mock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, this.tokenHandler);
            ClaimsIdentity badIdentity = CreateTestIdentity(issuer: TestWebsiteUrl, audience: "https://invalidAudience/");
            var request = CreateAuthRequest(options, new Uri(TestWebsiteUrl), badIdentity);

            // Act
            AuthenticationTicket authticket = mock.Authenticate(request, options);

            // Assert            
            Assert.NotNull(authticket);
            Assert.NotNull(authticket.Identity);
            Assert.False(authticket.Identity.IsAuthenticated, "Expected Authenticate to fail without signing key specified in MobileAppAuthenticationOptions");
        }

        private static ClaimsIdentity CreateTestIdentity(string audience = null, string issuer = null, bool validNotBefore = true, bool validExpiration = true)
        {
            ClaimsIdentity myIdentity = new ClaimsIdentity();
            if (!string.IsNullOrEmpty(issuer))
            {
                myIdentity.AddClaim(new Claim("iss", issuer));
            }

            if (!string.IsNullOrEmpty(audience))
            {
                myIdentity.AddClaim(new Claim("aud", audience));
            }

            DateTime now = DateTime.UtcNow;
            if (validNotBefore)
            {
                DateTime nbf = now.Subtract(TimeSpan.FromHours(1));
                string nbfAsString = Convert.ToInt64(nbf.Subtract(Epoch).TotalSeconds).ToString();
                myIdentity.AddClaim(new Claim("nbf", nbfAsString));
            }

            if (validExpiration)
            {
                DateTime exp = now.Add(TimeSpan.FromHours(1));
                string expAsString = Convert.ToInt64(exp.Subtract(Epoch).TotalSeconds).ToString();
                myIdentity.AddClaim(new Claim("exp", expAsString));
            }

            return myIdentity;
        }

        /// <summary>
        /// Makes a test token out of the specified claims, or a set of default claims if claims is unspecified.
        /// </summary>
        /// <param name="claims">The claims identity to make a token. Issuer and Audience in the claims will not be changed.</param>
        /// <param name="options">The <see cref="MobileAppAuthenticationOptions"/> object that wraps the signing key.</param>
        /// <param name="audience">The accepted valid audience used if claims is unspecified.</param>
        /// <param name="issuer">The accepted valid issuer used if claims is unspecified.</param>
        /// <returns></returns>
        private static JwtSecurityToken GetTestToken(MobileAppAuthenticationOptions options, string audience, string issuer, List<Claim> claims = null)
        {
            TokenInfo generatedToken;

            if (claims.Count == 0)
            {
                claims = new List<Claim>();
                claims.Add(new Claim("uid", "Facebook:1234"));
                claims.Add(new Claim(ClaimTypes.GivenName, "Frank"));
                claims.Add(new Claim(ClaimTypes.Surname, "Miller"));
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new Claim("my_custom_claim", "MyClaimValue"));
                claims.Add(new Claim("iss", issuer));
                claims.Add(new Claim("aud", audience));
                generatedToken = MobileAppTokenHandler.CreateTokenFromClaims(claims, options.SigningKey, null, audience, issuer);
            }
            else
            {
                string issFromClaims = string.Empty;
                string audFromClaims = string.Empty;
                Claim issClaim = claims.FirstOrDefault<Claim>(p => p.Type == "iss");
                if (issClaim != null)
                {
                    issFromClaims = issClaim.Value;
                }
                Claim audClaim = claims.FirstOrDefault<Claim>(p => p.Type == "aud");
                if (audClaim != null)
                {
                    audFromClaims = audClaim.Value;
                }

                generatedToken = MobileAppTokenHandler.CreateTokenFromClaims(claims, options.SigningKey, null, audFromClaims, issFromClaims);
            }

            return generatedToken.Token;
        }

        private static IOwinRequest CreateAuthRequest(MobileAppAuthenticationOptions options, Uri webappUri, ClaimsIdentity identity = null)
        {
            string webappBaseUrl = webappUri.GetLeftPart(UriPartial.Authority) + "/";

            if (identity == null)
            {
                identity = new ClaimsIdentity();
            }

            OwinContext context = new OwinContext();
            IOwinRequest request = context.Request;
            request.Host = HostString.FromUriComponent(webappUri.Host);
            request.Path = PathString.FromUriComponent(webappUri);
            request.Protocol = "HTTP/1.1";
            request.Method = "GET";
            request.Scheme = "https";
            request.PathBase = PathString.Empty;
            request.QueryString = QueryString.FromUriComponent(webappUri);
            request.Body = new System.IO.MemoryStream();

            var token = GetTestToken(options, webappBaseUrl, webappBaseUrl, identity.Claims.ToList<Claim>());
            request.Headers.Append(MobileAppAuthenticationHandler.AuthenticationHeaderName, token.RawData);
            return request;
        }

        internal class MobileAppAuthenticationHandlerMock : MobileAppAuthenticationHandler
        {
            public MobileAppAuthenticationHandlerMock(ILogger logger, IMobileAppTokenHandler tokenHandler)
                : base(logger, tokenHandler)
            {
            }

            public new AuthenticationTicket Authenticate(IOwinRequest request, MobileAppAuthenticationOptions options)
            {
                return base.Authenticate(request, options);
            }
        }

        private static MobileAppAuthenticationOptions CreateTestOptions(string signingKey = null)
        {
            MobileAppAuthenticationOptions options = new MobileAppAuthenticationOptions
            {
                SigningKey = signingKey,
            };

            if (string.IsNullOrEmpty(signingKey))
            {
                options.SigningKey = TestSigningKey;
            }
            return options;
        }
    }
}