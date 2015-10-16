// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.AppService;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Authentication.AppService;
using Moq;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Security
{
    public class ServiceUserTests
    {
        private string key = "lk;jadlfkjla;kjljlk";
        private FacebookCredentials facebookCredentials;
        private Mock<MobileAppTokenHandler> tokenHandlerMock;
        private IMobileAppTokenHandler tokenHandler;

        public ServiceUserTests()
        {
            this.facebookCredentials = new FacebookCredentials() { UserId = "Facebook:FBUserId", AccessToken = "ABCDEF" };

            HttpConfiguration config = new HttpConfiguration();
            this.tokenHandlerMock = new Mock<MobileAppTokenHandler>(config) { CallBase = true };
            this.tokenHandler = this.tokenHandlerMock.Object;
        }

        [Fact]
        public void UserPropertiesAreValid()
        {
            // Arrange

            // Act
            MobileAppUser user = this.CreateTestUser();

            // Assert
            this.tokenHandlerMock.Verify();
            Assert.Equal(this.facebookCredentials.UserId, user.Id);
            Assert.True(user.Identity.IsAuthenticated);
        }

        [Fact]
        public async Task GetIdentityAsync_Calls_GetRawTokenAsync()
        {
            // Arrange
            MockServiceUser mockServiceUser = new MockServiceUser();
            mockServiceUser.MobileAppAuthenticationToken = "123456";

            // Act
            await mockServiceUser.GetIdentityAsync<FacebookCredentials>();

            // Assert
            mockServiceUser.AppServiceClientMock
                .Verify(a => a.GetRawTokenAsync(mockServiceUser.MobileAppAuthenticationToken, "Facebook"), Times.Once);
        }

        [Fact]
        public void PopulateProviderCredentials_Facebook_CreatesExpectedCredentials()
        {
            const string UserIdClaimValue = "FacebookId";

            FacebookCredentials credentials = new FacebookCredentials();

            TokenResult tokenResult = new TokenResult();
            tokenResult.Properties.Add(TokenResult.Authentication.AccessTokenName, "TestAccessToken");
            Dictionary<string, string> claims = new Dictionary<string, string>
            {
                { "Claim1", "Value1" },
                { "Claim2", "Value1" },
                { "Claim3", "Value1" },
                { ClaimTypes.NameIdentifier, UserIdClaimValue }
            };
            tokenResult.Claims = claims;

            MobileAppUser.PopulateProviderCredentials(tokenResult, credentials);

            Assert.Equal("TestAccessToken", credentials.AccessToken);
            Assert.Equal(UserIdClaimValue, credentials.UserId);
            Assert.Equal(claims.Count, credentials.Claims.Count);
        }

        [Fact]
        public void PopulateProviderCredentials_Google_CreatesExpectedCredentials()
        {
            const string UserIdClaimValue = "GoogleId";

            GoogleCredentials credentials = new GoogleCredentials();

            TokenResult tokenResult = new TokenResult();
            tokenResult.Properties.Add(TokenResult.Authentication.AccessTokenName, "TestAccessToken");
            tokenResult.Properties.Add(TokenResult.Authentication.RefreshTokenName, "TestRefreshToken");
            tokenResult.Properties.Add("AccessTokenExpiration", "2015-03-12T16:49:28.504Z");
            Dictionary<string, string> claims = new Dictionary<string, string>
            {
                { "Claim1", "Value1" },
                { "Claim2", "Value1" },
                { "Claim3", "Value1" },
                { ClaimTypes.NameIdentifier, UserIdClaimValue }
            };
            tokenResult.Claims = claims;

            MobileAppUser.PopulateProviderCredentials(tokenResult, credentials);

            Assert.Equal("TestAccessToken", credentials.AccessToken);
            Assert.Equal("TestRefreshToken", credentials.RefreshToken);
            Assert.Equal(DateTimeOffset.Parse("2015-03-12T16:49:28.504Z"), credentials.AccessTokenExpiration);
            Assert.Equal(UserIdClaimValue, credentials.UserId);
            Assert.Equal(claims.Count, credentials.Claims.Count);
        }

        [Fact]
        public void PopulateProviderCredentials_MicrosoftAccount_CreatesExpectedCredentials()
        {
            const string UserIdClaimValue = "MicrosoftId";

            MicrosoftAccountCredentials credentials = new MicrosoftAccountCredentials();

            TokenResult tokenResult = new TokenResult();
            tokenResult.Properties.Add(TokenResult.Authentication.AccessTokenName, "TestAccessToken");
            tokenResult.Properties.Add(TokenResult.Authentication.RefreshTokenName, "TestRefreshToken");
            tokenResult.Properties.Add("AccessTokenExpiration", "2015-03-12T16:49:28.504Z");
            Dictionary<string, string> claims = new Dictionary<string, string>
            {
                { "Claim1", "Value1" },
                { "Claim2", "Value1" },
                { "Claim3", "Value1" },
                { ClaimTypes.NameIdentifier, UserIdClaimValue }
            };
            tokenResult.Claims = claims;

            MobileAppUser.PopulateProviderCredentials(tokenResult, credentials);

            Assert.Equal("TestAccessToken", credentials.AccessToken);
            Assert.Equal("TestRefreshToken", credentials.RefreshToken);
            Assert.Equal(DateTimeOffset.Parse("2015-03-12T16:49:28.504Z"), credentials.AccessTokenExpiration);
            Assert.Equal(UserIdClaimValue, credentials.UserId);
            Assert.Equal(claims.Count, credentials.Claims.Count);
        }

        [Fact]
        public void PopulateProviderCredentials_AzureActiveDirectory_CreatesExpectedCredentials()
        {
            const string UserIdClaimValue = "AadId";

            AzureActiveDirectoryCredentials credentials = new AzureActiveDirectoryCredentials();

            TokenResult tokenResult = new TokenResult();
            tokenResult.Properties.Add(TokenResult.Authentication.AccessTokenName, "TestAccessToken");
            tokenResult.Properties.Add("TenantId", "TestTenantId");
            tokenResult.Properties.Add("ObjectId", "TestObjectId");
            Dictionary<string, string> claims = new Dictionary<string, string>
            {
                { "Claim1", "Value1" },
                { "Claim2", "Value1" },
                { "Claim3", "Value1" },
                { ClaimTypes.NameIdentifier, UserIdClaimValue }
            };
            tokenResult.Claims = claims;

            MobileAppUser.PopulateProviderCredentials(tokenResult, credentials);

            Assert.Equal("TestAccessToken", credentials.AccessToken);
            Assert.Equal("TestTenantId", credentials.TenantId);
            Assert.Equal("TestObjectId", credentials.ObjectId);
            Assert.Equal(UserIdClaimValue, credentials.UserId);
            Assert.Equal(claims.Count, credentials.Claims.Count);
        }

        [Fact]
        public void PopulateProviderCredentials_Twitter_CreatesExpectedCredentials()
        {
            TwitterCredentials credentials = new TwitterCredentials();

            TokenResult tokenResult = new TokenResult();
            tokenResult.Properties.Add(TokenResult.Authentication.AccessTokenName, "TestAccessToken");
            tokenResult.Properties.Add("AccessTokenSecret", "TestAccessTokenSecret");
            Dictionary<string, string> claims = new Dictionary<string, string>
            {
                { "Claim1", "Value1" },
                { "Claim2", "Value1" },
                { "Claim3", "Value1" }
            };
            tokenResult.Claims = claims;

            MobileAppUser.PopulateProviderCredentials(tokenResult, credentials);

            Assert.Equal("TestAccessToken", credentials.AccessToken);
            Assert.Equal("TestAccessTokenSecret", credentials.AccessTokenSecret);
            Assert.Equal(claims.Count, credentials.Claims.Count);
        }

        [Fact]
        public void IsTokenValid_ReturnsFalse_WhenTokenIsInvalid()
        {
            // Arrange
            // This is what is returned when a token is not found.
            TokenResult tokenResult = new TokenResult()
            {
                Diagnostics = "Token not found in store. id=sid:90BF712CA4464DDCADED130D8E5D1D8E, name=Twitter"
            };

            // Act
            bool result = MobileAppUser.IsTokenValid(tokenResult);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTokenValid_ReturnsTrue_WhenTokenIsValid()
        {
            // Arrange
            TokenResult tokenResult = new TokenResult();

            // Act
            bool result = MobileAppUser.IsTokenValid(tokenResult);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetIdentitiesAsync_Throws_IfGatewayUrlNotInAppSettings()
        {
            // Arrange
            ConfigurationManager.AppSettings["EMA_RuntimeUrl"] = null;
            // ServiceUser should be authenticated to hit the exception
            MobileAppUser user = new MobileAppUser(CreateMockClaimsIdentity(Enumerable.Empty<Claim>(), true));
            user.MobileAppAuthenticationToken = "1234567890";
            NullReferenceException ex = null;

            // Act
            try
            {
                ex = await Assert.ThrowsAsync<NullReferenceException>(() => user.GetIdentityAsync<FacebookCredentials>());
            }
            finally
            {
                // reset the config for future tests
                ConfigurationManager.RefreshSection("appSettings");
            }

            // Assert
            Assert.NotNull(ex);
            Assert.Equal("The 'EMA_RuntimeUrl' app setting is missing from the configuration.", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetIdentitiesAsync_ReturnsNull_IfMobileAppAuthenticationTokenIsNullOrEmpty(string token)
        {
            // Arrange
            MobileAppUser user = new MobileAppUser(CreateMockClaimsIdentity(Enumerable.Empty<Claim>(), true));
            user.MobileAppAuthenticationToken = token;

            // Act
            var tokenResult = await user.GetIdentityAsync<FacebookCredentials>();

            // Assert
            Assert.Null(tokenResult);
        }

        [Fact]
        public async Task GetIdentitiesAsync_ReturnsNull_IfUserNotAuthenticated()
        {
            // Arrange
            MobileAppUser user = new MobileAppUser(CreateMockClaimsIdentity(Enumerable.Empty<Claim>(), false));

            // Act
            var tokenResult = await user.GetIdentityAsync<FacebookCredentials>();

            // Assert
            Assert.Null(tokenResult);
        }

        /// <summary>
        /// Create a test user
        /// </summary>
        private MobileAppUser CreateTestUser()
        {
            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, this.facebookCredentials.UserId)
            };
            TokenInfo info = this.tokenHandler.CreateTokenInfo(claims, TimeSpan.FromDays(10), this.key);
            JwtSecurityToken token = info.Token;

            ClaimsPrincipal claimsPrincipal = null;
            this.tokenHandler.TryValidateLoginToken(token.RawData, this.key, out claimsPrincipal);

            MobileAppUser user = this.tokenHandler.CreateServiceUser((ClaimsIdentity)claimsPrincipal.Identity, null);

            return user;
        }

        private static ClaimsIdentity CreateMockClaimsIdentity(IEnumerable<Claim> claims, bool isAuthenticated)
        {
            Mock<ClaimsIdentity> claimsIdentityMock = new Mock<ClaimsIdentity>(claims);
            claimsIdentityMock.CallBase = true;
            claimsIdentityMock.SetupGet(c => c.IsAuthenticated).Returns(isAuthenticated);
            return claimsIdentityMock.Object;
        }

        private class MockServiceUser : MobileAppUser
        {
            public MockServiceUser()
                : base(ServiceUserTests.CreateMockClaimsIdentity(Enumerable.Empty<Claim>(), true))
            {
            }

            public Mock<AppServiceHttpClient> AppServiceClientMock { get; private set; }

            internal override AppServiceHttpClient CreateAppServiceHttpClient(Uri appServiceGatewayUrl)
            {
                Mock<AppServiceHttpClient> appServiceClientMock = new Mock<AppServiceHttpClient>(appServiceGatewayUrl);
                appServiceClientMock.Setup(c => c.GetRawTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(new TokenResult()));
                this.AppServiceClientMock = appServiceClientMock;
                return appServiceClientMock.Object;
            }
        }
    }
}