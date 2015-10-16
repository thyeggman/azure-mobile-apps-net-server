// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
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
        private MobileAppAuthenticationHandlerMock handlerMock;
        private Mock<ILogger> loggerMock;

        public MobileAppAuthenticationHandlerTests()
        {
            this.config = new HttpConfiguration();
            this.tokenHandler = new MobileAppTokenHandler(this.config);
            this.loggerMock = new Mock<ILogger>();
            this.handlerMock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, this.tokenHandler);
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

        public static TheoryDataCollection<MobileAppAuthenticationOptions, bool> MobileAppAuthenticationOptionsData
        {
            get
            {
                string realKey = "signing_key";
                return new TheoryDataCollection<MobileAppAuthenticationOptions, bool>
                {
                    { CreateOptions(false, realKey), true },
                    { CreateOptions(false, "wrong_key"), false },
                    { CreateOptions(true, realKey), true },
                    { CreateOptions(true, "wrong_key"), true },
                    { CreateOptions(false, null), false },
                    { CreateOptions(true, null), true },
                    { CreateOptions(false, string.Empty), false },
                    { CreateOptions(true, string.Empty), true }
                };
            }
        }

        [Fact]
        public void TryParseLoginToken_ReturnsExpectedClaims()
        {
            // Arrange
            MobileAppAuthenticationOptions options = new MobileAppAuthenticationOptions();
            options.SigningKey = "SOME_SIGNING_KEY";
            // SkipTokenSignatureValidation defaults to false
            JwtSecurityToken token = GetTestToken(options.SigningKey);

            // Act
            ClaimsPrincipal claimsPrincipal;
            bool result = this.handlerMock.TryParseLoginToken(token.RawData, options, out claimsPrincipal);

            // Assert
            Assert.True(result);
            MobileAppUser user = this.tokenHandler.CreateServiceUser((ClaimsIdentity)claimsPrincipal.Identity, token.RawData);
            Assert.Equal("Facebook:1234", user.Id);
            Assert.True(user.Identity.IsAuthenticated);

            Claim[] claims = user.Claims.ToArray();
            Assert.Equal(8, claims.Length);
            Assert.Equal("Frank", claims.Single(p => p.Type == ClaimTypes.GivenName).Value);
            Assert.Equal("Miller", claims.Single(p => p.Type == ClaimTypes.Surname).Value);
            Assert.Equal("Admin", claims.Single(p => p.Type == ClaimTypes.Role).Value);
            Assert.Equal("Facebook:1234", claims.Single(p => p.Type == "uid").Value);
            Assert.Equal("MyClaimValue", claims.Single(p => p.Type == "my_custom_claim").Value);
        }

        [Fact]
        public void TryParseLoginToken_NoTokenValidation_ReturnsExpectedClaims()
        {
            // Arrange
            Mock<MobileAppTokenHandler> tokenHandlerMock = new Mock<MobileAppTokenHandler>(this.config);
            MobileAppAuthenticationHandlerMock authHandlerMock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, tokenHandlerMock.Object);
            MobileAppAuthenticationOptions skipOptions = new MobileAppAuthenticationOptions();
            skipOptions.SigningKey = "SOME_SIGNING_KEY";
            skipOptions.SkipTokenSignatureValidation = true;

            JwtSecurityToken skipToken = GetTestToken("SOME_OTHER_KEY");

            // Act
            ClaimsPrincipal skipClaimsPrincipal;
            bool skipResult = authHandlerMock.TryParseLoginToken(skipToken.RawData, skipOptions, out skipClaimsPrincipal);

            // Assert
            tokenHandlerMock.Verify(h => h.TryValidateLoginToken(It.IsAny<string>(), It.IsAny<string>(), out skipClaimsPrincipal), Times.Never);
            Assert.True(skipResult);
            MobileAppUser user = this.tokenHandler.CreateServiceUser((ClaimsIdentity)skipClaimsPrincipal.Identity, skipToken.RawData);

            Assert.Equal("Facebook:1234", user.Id);
            Assert.True(user.Identity.IsAuthenticated);

            Claim[] claims = user.Claims.ToArray();
            Assert.Equal(8, claims.Length);
            Assert.Equal("Frank", claims.Single(p => p.Type == ClaimTypes.GivenName).Value);
            Assert.Equal("Miller", claims.Single(p => p.Type == ClaimTypes.Surname).Value);
            Assert.Equal("Admin", claims.Single(p => p.Type == ClaimTypes.Role).Value);
            Assert.Equal("Facebook:1234", claims.Single(p => p.Type == "uid").Value);
            Assert.Equal("MyClaimValue", claims.Single(p => p.Type == "my_custom_claim").Value);
        }

        [Fact]
        public void TryParseLoginToken_ReturnsSameClaimsIdentity_WhetherValidatingTokensOrNot()
        {
            // Arrange
            MobileAppAuthenticationOptions options = new MobileAppAuthenticationOptions();
            options.SigningKey = "SOME_SIGNING_KEY";
            // SkipTokenSignatureValidation defaults to false
            JwtSecurityToken token = GetTestToken(options.SigningKey);

            MobileAppAuthenticationOptions skipOptions = new MobileAppAuthenticationOptions();
            skipOptions.SigningKey = "SOME_SIGNING_KEY";
            skipOptions.SkipTokenSignatureValidation = true;
            JwtSecurityToken skipToken = GetTestToken("SOME_OTHER_KEY");

            // Act
            ClaimsPrincipal claimsPrincipal;
            this.handlerMock.TryParseLoginToken(token.RawData, options, out claimsPrincipal);
            Assert.True(claimsPrincipal.Identity.IsAuthenticated);

            ClaimsPrincipal skipClaimsPrincipal;
            this.handlerMock.TryParseLoginToken(skipToken.RawData, skipOptions, out skipClaimsPrincipal);
            Assert.True(claimsPrincipal.Identity.IsAuthenticated);

            // Assert
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)claimsPrincipal.Identity;
            ClaimsIdentity skipClaimsIdentity = (ClaimsIdentity)skipClaimsPrincipal.Identity;

            Assert.Equal(claimsIdentity.Actor, skipClaimsIdentity.Actor);
            Assert.Equal(claimsIdentity.AuthenticationType, skipClaimsIdentity.AuthenticationType);
            Assert.Equal(claimsIdentity.BootstrapContext, skipClaimsIdentity.BootstrapContext);
            Assert.Equal(claimsIdentity.IsAuthenticated, skipClaimsIdentity.IsAuthenticated);
            Assert.Equal(claimsIdentity.Label, skipClaimsIdentity.Label);
            Assert.Equal(claimsIdentity.Name, skipClaimsIdentity.Name);
            Assert.Equal(claimsIdentity.NameClaimType, skipClaimsIdentity.NameClaimType);
            Assert.Equal(claimsIdentity.RoleClaimType, skipClaimsIdentity.RoleClaimType);
            Assert.Equal(claimsIdentity.Claims.Count(), skipClaimsIdentity.Claims.Count());
            Claim[] claims = claimsIdentity.Claims.OrderBy(c => c.Type).ToArray();
            Claim[] skipClaims = skipClaimsIdentity.Claims.OrderBy(c => c.Type).ToArray();
            for (int i = 0; i < claims.Length; i++)
            {
                Claim claim = claims[i];
                Claim skipClaim = skipClaims[i];
                Assert.Equal(claim.Type, skipClaim.Type);
                Assert.Equal(claim.Issuer, skipClaim.Issuer);
                Assert.Equal(claim.OriginalIssuer, skipClaim.OriginalIssuer);
                Assert.Equal(claim.Subject, claimsIdentity);
                Assert.Equal(skipClaim.Subject, skipClaimsIdentity);
                Assert.Equal(claim.ValueType, skipClaim.ValueType);
                Assert.Equal(claim.Properties.Count, skipClaim.Properties.Count);
                if (claim.Type == "nbf")
                {
                    // nbf can be slightly off
                    int claimNbf = Int32.Parse(claim.Value);
                    int skipClaimNbf = Int32.Parse(skipClaim.Value);
                    Assert.True(Math.Abs(claimNbf - skipClaimNbf) < 10);
                }
                else
                {
                    Assert.Equal(claim.Value, skipClaim.Value);
                }
            }
        }

        [Theory]
        [MemberData("MobileAppAuthenticationOptionsData")]
        public void Authenticate_CorrectlyAuthenticates(MobileAppAuthenticationOptions options, bool expectAuthenticated)
        {
            // Arrange
            var mock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, this.tokenHandler);
            var request = CreateAuthRequest("signing_key");
            request.User = new ClaimsPrincipal();

            // Act
            mock.Authenticate(request, options);

            // Assert            
            if (expectAuthenticated)
            {
                Assert.NotNull(request.User.Identity);
                Assert.True(request.User.Identity.IsAuthenticated);
                Assert.IsType(typeof(MobileAppUser), request.User);
            }
            else
            {
                Assert.Null(request.User);
            }
        }

        [Fact]
        public void Authenticate_LeavesUserNull_IfException()
        {
            // Arrange
            var mockTokenHandler = new Mock<MobileAppTokenHandler>(this.config);
            mockTokenHandler.CallBase = true;
            mockTokenHandler
                .Setup(t => t.CreateServiceUser(It.IsAny<ClaimsIdentity>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException())
                .Verifiable();
            var mock = new MobileAppAuthenticationHandlerMock(this.loggerMock.Object, mockTokenHandler.Object);
            var request = CreateAuthRequest("signing_key");
            request.User = new ClaimsPrincipal();

            // Act
            mock.Authenticate(request, CreateOptions(false, "signing_key"));

            // Assert            
            mockTokenHandler.VerifyAll();
            Assert.Null(request.User);
        }

        private static JwtSecurityToken GetTestToken(string secretKey)
        {
            Claim[] claims = new Claim[]
            {
                new Claim("uid", "Facebook:1234"),
                new Claim(ClaimTypes.GivenName, "Frank"),
                new Claim(ClaimTypes.Surname, "Miller"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("my_custom_claim", "MyClaimValue")
            };

            string zumoIssuerValue = "urn:microsoft:windows-azure:zumo";
            TokenInfo info = MobileAppTokenHandler.CreateTokenFromClaims(claims, secretKey, zumoIssuerValue, zumoIssuerValue, null);

            return info.Token;
        }

        private static IOwinRequest CreateAuthRequest(string signingKey)
        {
            var token = GetTestToken(signingKey);
            IOwinRequest request = new OwinRequest();
            request.Headers.Append(MobileAppAuthenticationHandler.AuthenticationHeaderName, token.RawData);
            return request;
        }

        private static MobileAppAuthenticationOptions CreateOptions(bool skipTokenValidation, string signingKey)
        {
            return new MobileAppAuthenticationOptions
                {
                    SigningKey = signingKey,
                    SkipTokenSignatureValidation = skipTokenValidation
                };
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

            public new bool TryParseLoginToken(string token, MobileAppAuthenticationOptions options, out ClaimsPrincipal claimsPrincipal)
            {
                return base.TryParseLoginToken(token, options, out claimsPrincipal);
            }
        }
    }
}