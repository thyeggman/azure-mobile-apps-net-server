// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.ServiceModel.Security.Tokens;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Moq;
using TestUtilities;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Security
{
    public class MobileAppTokenHandlerTests
    {
        private const string TestSecretKey = "l9dsa634ksfdlds;lkw43-psdfd";
        private const string TestAudience = "http://www.testaudience.com";
        private const string TestIssuer = "test-issuer";

        private static readonly TimeSpan Lifetime = TimeSpan.FromDays(10);

        private HttpConfiguration config;
        private Mock<MobileAppTokenHandler> tokenHandlerMock;
        private MobileAppTokenHandler tokenHandler;
        private FacebookCredentials credentials;

        public MobileAppTokenHandlerTests()
        {
            this.config = new HttpConfiguration();
            this.tokenHandlerMock = new Mock<MobileAppTokenHandler>(this.config) { CallBase = true };
            this.tokenHandler = this.tokenHandlerMock.Object;
            this.credentials = new FacebookCredentials
            {
                UserId = "Facebook:1234",
                AccessToken = "abc123"
            };
        }

        public static TheoryDataCollection<string, string> CreateUserIdData
        {
            get
            {
                return new TheoryDataCollection<string, string>
                {
                    { "你好", "世界" },
                    { "Hello", "World" },
                    { "Hello", null },
                    { "Hello", string.Empty },
                    { "Hello", "   " },
                };
            }
        }

        public static TheoryDataCollection<string, bool, string, string> ParseUserIdData
        {
            get
            {
                return new TheoryDataCollection<string, bool, string, string>
                {
                    { null, false, null, null },
                    { string.Empty, false, null, null },
                    { ":", false, null, null },
                    { ":::::", false, null, null },
                    { "invalid", false, null, null },
                    { ":id", false, null, null },
                    { "name:", false, null, null },
                    { "你好:世界", true, "你好", "世界" },
                    { "你好:世:界", true, "你好", "世:界" },
                    { "你好:::::", true, "你好", "::::" },
                };
            }
        }

        public static TheoryDataCollection<string> InvalidUserIdData
        {
            get
            {
                return new TheoryDataCollection<string>
                {
                    null,
                    string.Empty,
                    "   ",
                    "你好世界",
                };
            }
        }

        public static TheoryDataCollection<string> TokenData
        {
            get
            {
                return new TheoryDataCollection<string>
                {
                    // Our token format as of 11/5/2014
                    "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1cm46bWljcm9zb2Z0OmNyZWRlbnRpYWxzIjoie1wiYWNjZXNzVG9rZW5cIjpcImFiYzEyM1wifSIsInVpZCI6IkZhY2Vib29rOjEyMzQiLCJ2ZXIiOiIyIiwiaXNzIjoidXJuOm1pY3Jvc29mdDp3aW5kb3dzLWF6dXJlOnp1bW8iLCJhdWQiOiJ1cm46bWljcm9zb2Z0OndpbmRvd3MtYXp1cmU6enVtbyIsImV4cCI6MTczMTYyNTAyNCwibmJmIjoxNDE2MjY1MDI0fQ.xo5QJctzTzbHuFKaeI-bKqLDSL3O0-kfTG0A2TyKVmo",
                };
            }
        }

        [Fact]
        public void CreateTokenInfo_Throws_IfNegativeLifetime()
        {
            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, this.credentials.UserId)
            };

            // Act
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => this.tokenHandler.CreateTokenInfo(claims, TimeSpan.FromDays(-10), TestSecretKey));

            // Assert
            Assert.Contains("Argument must be greater than or equal to 00:00:00.", ex.Message);
        }

        [Fact]
        public void CreateTokenInfo_CreatesTokenWithNoExpiry_WhenLifetimeIsNull()
        {
            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, this.credentials.UserId)
            };
            TokenInfo tokenInfo = this.tokenHandler.CreateTokenInfo(claims, null, TestSecretKey);
            JwtSecurityToken token = tokenInfo.Token;

            // no exp claim
            Assert.Null(token.Payload.Exp);
            Assert.Equal(5, token.Claims.Count());

            Assert.Equal(MobileAppTokenHandler.ZumoAudienceValue, token.Audiences.Single());
            Assert.Equal(MobileAppTokenHandler.ZumoIssuerValue, token.Issuer);
            Assert.Equal(default(DateTime), token.ValidTo);
            Assert.Null(token.Payload.Exp);
            Assert.Equal("3", token.Claims.Single(p => p.Type == "ver").Value);
            Assert.Equal("Facebook:1234", token.Claims.Single(p => p.Type == "uid").Value);

            ClaimsPrincipal claimsPrincipal = null;
            bool isValid = this.tokenHandler.TryValidateLoginToken(token.RawData, TestSecretKey, out claimsPrincipal);
            Assert.True(isValid);
        }

        [Fact]
        public void CreateTokenInfo_CreatesExpectedToken()
        {
            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, this.credentials.UserId),
                new Claim("custom_claim_1", "CustomClaimValue1"),
                new Claim("custom_claim_2", "CustomClaimValue2")
            };
            TokenInfo tokenInfo = this.tokenHandler.CreateTokenInfo(claims, TimeSpan.FromDays(10), TestSecretKey);
            JwtSecurityToken token = tokenInfo.Token;

            Assert.Equal(8, token.Claims.Count());

            Assert.Equal(MobileAppTokenHandler.ZumoAudienceValue, token.Audiences.Single());
            Assert.Equal(MobileAppTokenHandler.ZumoIssuerValue, token.Issuer);
            Assert.Equal(10, (token.ValidTo - DateTime.Now).Days);
            Assert.NotNull(token.Payload.Exp);
            Assert.Equal("3", token.Claims.Single(p => p.Type == "ver").Value);
            Assert.Equal("Facebook:1234", token.Claims.Single(p => p.Type == "uid").Value);
            Assert.Equal("CustomClaimValue1", token.Claims.Single(p => p.Type == "custom_claim_1").Value);
            Assert.Equal("CustomClaimValue2", token.Claims.Single(p => p.Type == "custom_claim_2").Value);
        }

        [Theory]
        [MemberData("InvalidUserIdData")]
        public void CreateUser_ReturnsAnonymous_IfInvalidUserId(string invalidUserId)
        {
            // Arrange
            List<Claim> claims = new List<Claim>();
            if (invalidUserId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, invalidUserId));
            }
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims);

            // Act
            MobileAppUser user = this.tokenHandler.CreateServiceUser(claimsIdentity, null);

            // Assert
            Assert.Null(user.Id);
            Assert.False(user.Identity.IsAuthenticated);
        }

        [Fact]
        public void CreateUser_DoesNotSetUserId_WhenSpecifiedLevelIsNotUser()
        {
            // Arrange
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "provider:providerId"),
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims);

            // Act
            MobileAppUser user = this.tokenHandler.CreateServiceUser(claimsIdentity, null);

            // Assert
            Assert.Null(user.Id);
            Assert.False(user.Identity.IsAuthenticated);
        }

        [Fact]
        public void CreateUser_ReturnsUser_IfUnknownProviderName()
        {
            // Arrange
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "unknown:providerId"),
            };

            // Fake that we've been authenticated
            Mock<ClaimsIdentity> mockClaimsIdentity = new Mock<ClaimsIdentity>(claims);
            mockClaimsIdentity.CallBase = true;
            mockClaimsIdentity.SetupGet(c => c.IsAuthenticated).Returns(true);

            // Act
            MobileAppUser user = this.tokenHandler.CreateServiceUser(mockClaimsIdentity.Object, null);

            // Assert
            Assert.Equal("unknown:providerId", user.Id);
            Assert.True(user.Identity.IsAuthenticated);
        }

        [Fact]
        public void CreateUser_CreatesExpectedUser_FromLoginToken()
        {
            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, this.credentials.UserId)
            };

            // Create a login token for the provider
            TokenInfo info = this.tokenHandler.CreateTokenInfo(claims, Lifetime, TestSecretKey);
            JwtSecurityToken token = info.Token;

            this.ValidateLoginToken(token.RawData, this.credentials);
        }

        [Fact]
        public void CreateUser_DeterminesUserIdFromClaims()
        {
            // single uid claim
            Claim[] claims = new Claim[]
            {
                new Claim("uid", "Facebook:1234")
            };
            ClaimsIdentity claimsIdentity = CreateMockClaimsIdentity(claims, true);
            MobileAppUser user = this.tokenHandler.CreateServiceUser(claimsIdentity, null);
            Assert.Equal("Facebook:1234", user.Id);

            // single NameIdentifier claim
            claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Facebook:1234")
            };
            claimsIdentity = CreateMockClaimsIdentity(claims, true);
            user = this.tokenHandler.CreateServiceUser(claimsIdentity, null);
            Assert.Equal("Facebook:1234", user.Id);

            // BOTH uid and NameIdentifier claims
            // expect the NameIdentifier claim to take precedence
            claims = new Claim[]
            {
                new Claim("uid", "Facebook:1234"),
                new Claim(ClaimTypes.NameIdentifier, "Google:5678")
            };
            claimsIdentity = CreateMockClaimsIdentity(claims, true);
            user = this.tokenHandler.CreateServiceUser(claimsIdentity, null);
            Assert.Equal("Google:5678", user.Id);

            // if there are no claims, the user id will be null
            claimsIdentity = CreateMockClaimsIdentity(Enumerable.Empty<Claim>(), true);
            user = this.tokenHandler.CreateServiceUser(claimsIdentity, null);
            Assert.Equal(null, user.Id);
        }

        private static ClaimsIdentity CreateMockClaimsIdentity(IEnumerable<Claim> claims, bool isAuthenticated)
        {
            Mock<ClaimsIdentity> claimsIdentityMock = new Mock<ClaimsIdentity>(claims);
            claimsIdentityMock.CallBase = true;
            claimsIdentityMock.SetupGet(c => c.IsAuthenticated).Returns(isAuthenticated);
            return claimsIdentityMock.Object;
        }

        /// <summary>
        /// This test verifies that token formats that we've previously issued continue to validate.
        /// To produce these token strings, use the runtime code to create a token, ensuring that you
        /// set the lifetime to 100 years so the tests continue to run. Then add that raw token value
        /// to this test. Be sure to use the above tests to create the token, to ensure the claim values
        /// and test key match. E.g., you can use the below CreateTokenInfo_CreatesExpectedToken test code
        /// to generate the token, substituting a far out expiry.
        /// </summary>
        [Theory]
        [MemberData("TokenData")]
        public void TryValidateLoginToken_AcceptsPreviousTokenVersions(string tokenValue)
        {
            this.ValidateLoginToken(tokenValue, this.credentials);
        }

        private void ValidateLoginToken(string token, FacebookCredentials expectedCredentials)
        {
            // validate the token and get the claims principal
            ClaimsPrincipal claimsPrincipal = null;
            Assert.True(this.tokenHandler.TryValidateLoginToken(token, TestSecretKey, out claimsPrincipal));

            // create a user from the token and validate properties
            MobileAppUser user = this.tokenHandler.CreateServiceUser((ClaimsIdentity)claimsPrincipal.Identity, token);
            Assert.Equal(expectedCredentials.UserId, user.Id);
            Assert.Equal(token, user.MobileAppAuthenticationToken);
        }

        [Fact]
        public void TryValidateLoginToken_RejectsMalformedTokens()
        {
            ClaimsPrincipal claimsPrincipal = null;
            bool result = this.tokenHandler.TryValidateLoginToken("this is not a valid jwt", TestSecretKey, out claimsPrincipal);
            Assert.False(result);
            Assert.Null(claimsPrincipal);
        }

        [Fact]
        public void TryValidateLoginToken_RejectsTokensSignedWithWrongKey()
        {
            TokenInfo tokenInfo = this.tokenHandler.CreateTokenInfo(new Claim[] { }, null, TestSecretKey);
            JwtSecurityToken token = tokenInfo.Token;

            ClaimsPrincipal claimsPrincipal = null;

            bool isValid = this.tokenHandler.TryValidateLoginToken(token.RawData, "SOME_OTHER_KEY", out claimsPrincipal);
            Assert.False(isValid);
            Assert.Null(claimsPrincipal);
        }

        [Fact]
        public void ToClaimValue_ProducesCorrectValue()
        {
            // Act
            string actual = this.tokenHandler.ToClaimValue(this.credentials);

            // Assert
            Assert.Equal("{\"accessToken\":\"abc123\"}", actual);
            ValidateTestCredentials(this.credentials);
        }

        [Theory]
        [MemberData("CreateUserIdData")]
        public void CreateUserId_FormatsCorrectly(string providerName, string providerId)
        {
            // Arrange
            string expected = string.Format("{0}:{1}", providerName, providerId);

            // Act
            string actual = this.tokenHandler.CreateUserId(providerName, providerId);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CreateUserId_ThrowsIfNullProviderName()
        {
            Assert.Throws<ArgumentNullException>(() => this.tokenHandler.CreateUserId(null, "value"));
        }

        [Theory]
        [MemberData("ParseUserIdData")]
        public void TryParseUserId(string userId, bool expected, string providerName, string providerId)
        {
            // Arrange
            string actualProviderName;
            string actualProviderId;

            // Act
            bool actual = this.tokenHandler.TryParseUserId(userId, out actualProviderName, out actualProviderId);

            // Assert
            Assert.Equal(expected, actual);
            Assert.Equal(providerName, actualProviderName);
            Assert.Equal(providerId, actualProviderId);
        }

        [Fact]
        public void ValidateToken_ThrowsSecurityTokenValidationException_WhenValidFromIsAfterCurrentTime()
        {
            // Arrange
            TimeSpan lifetimeFiveMinute = new TimeSpan(0, 5, 0);
            DateTime tokenCreationDateInFuture = DateTime.UtcNow + new TimeSpan(1, 0, 0);
            DateTime tokenExpiryDate = tokenCreationDateInFuture + lifetimeFiveMinute;

            SecurityTokenDescriptor tokenDescriptor = this.GetTestSecurityTokenDescriptor(tokenCreationDateInFuture, tokenExpiryDate);

            JwtSecurityTokenHandler securityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = securityTokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;

            // Act
            // Assert
            SecurityTokenNotYetValidException ex = Assert.Throws<SecurityTokenNotYetValidException>(() =>
                MobileAppTokenHandler.ValidateToken(token.RawData, TestSecretKey));
            Assert.Contains("IDX10222: Lifetime validation failed. The token is not yet valid", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateToken_ThrowsSecurityTokenValidationException_WhenTokenExpired()
        {
            // Arrange
            TimeSpan lifetime = new TimeSpan(0, 0, 1);
            DateTime tokenCreationDate = DateTime.UtcNow + new TimeSpan(-1, 0, 0);
            DateTime tokenExpiryDate = tokenCreationDate + lifetime;

            SecurityTokenDescriptor tokenDescriptor = this.GetTestSecurityTokenDescriptor(tokenCreationDate, tokenExpiryDate);

            JwtSecurityTokenHandler securityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = securityTokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;

            // Act
            System.Threading.Thread.Sleep(1000);
            SecurityTokenExpiredException ex = Assert.Throws<SecurityTokenExpiredException>(() =>
                MobileAppTokenHandler.ValidateToken(token.RawData, TestSecretKey));

            // Assert
            Assert.Contains("IDX10223: Lifetime validation failed. The token is expired", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateToken_ThrowsSecurityTokenValidationException_WhenIssuerIsBlank()
        {
            // Arrange
            TimeSpan lifetime = new TimeSpan(24, 0, 0);
            DateTime tokenCreationDate = DateTime.UtcNow;
            DateTime tokenExpiryDate = tokenCreationDate + lifetime;

            SecurityTokenDescriptor tokenDescriptor = this.GetTestSecurityTokenDescriptor(tokenCreationDate, tokenExpiryDate);
            tokenDescriptor.TokenIssuerName = string.Empty;

            JwtSecurityTokenHandler securityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = securityTokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;

            // Act
            SecurityTokenInvalidIssuerException ex = Assert.Throws<SecurityTokenInvalidIssuerException>(() => MobileAppTokenHandler.ValidateToken(token.RawData, TestSecretKey));

            // Assert
            Assert.Contains("IDX10211: Unable to validate issuer. The 'issuer' parameter is null or whitespace", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateToken_PassesWithValidToken()
        {
            // Arrange
            TimeSpan lifetime = new TimeSpan(24, 0, 0);
            DateTime tokenCreationDate = DateTime.UtcNow;
            DateTime tokenExpiryDate = tokenCreationDate + lifetime;

            SecurityTokenDescriptor tokenDescriptor = this.GetTestSecurityTokenDescriptor(tokenCreationDate, tokenExpiryDate);

            JwtSecurityTokenHandler securityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = securityTokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;

            // Act
            // Assert
            MobileAppTokenHandler.ValidateToken(token.RawData, TestSecretKey);
        }

        [Fact]
        public void ValidateToken_ThrowsArgumentException_WithMalformedToken()
        {
            // Arrange
            TimeSpan lifetime = new TimeSpan(24, 0, 0);
            DateTime tokenCreationDate = DateTime.UtcNow;
            DateTime tokenExpiryDate = tokenCreationDate + lifetime;

            SecurityTokenDescriptor tokenDescriptor = this.GetTestSecurityTokenDescriptor(tokenCreationDate, tokenExpiryDate);

            JwtSecurityTokenHandler securityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = securityTokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;

            // Act
            ArgumentException ex = Assert.Throws<ArgumentException>(() => MobileAppTokenHandler.ValidateToken(token.RawData + ".malformedbits.!.2.", TestSecretKey));

            // Assert
            Assert.Contains("IDX10708: 'System.IdentityModel.Tokens.JwtSecurityTokenHandler' cannot read this string", ex.Message, StringComparison.Ordinal);
        }

        private SecurityTokenDescriptor GetTestSecurityTokenDescriptor(DateTime tokenLifetimeStart, DateTime tokenLifetimeEnd)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("uid", this.credentials.UserId),
                new Claim("ver", "2"),
            };

            byte[] signingKey = MobileAppTokenHandler.GetSigningKey(TestSecretKey);
            BinarySecretSecurityToken signingToken = new BinarySecretSecurityToken(signingKey);
            SigningCredentials signingCredentials = new SigningCredentials(new InMemorySymmetricSecurityKey(signingToken.GetKeyBytes()), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", "http://www.w3.org/2001/04/xmlenc#sha256");

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                AppliesToAddress = MobileAppTokenHandler.ZumoAudienceValue,
                TokenIssuerName = MobileAppTokenHandler.ZumoIssuerValue,
                SigningCredentials = signingCredentials,
                Lifetime = new Lifetime(tokenLifetimeStart, tokenLifetimeEnd),
                Subject = new ClaimsIdentity(claims),
            };
            return tokenDescriptor;
        }

        private static void ValidateTestCredentials(FacebookCredentials credentials)
        {
            Assert.Equal("Facebook", credentials.Provider);
            Assert.Equal("Facebook:1234", credentials.UserId);
            Assert.Equal("abc123", credentials.AccessToken);
        }
    }
}