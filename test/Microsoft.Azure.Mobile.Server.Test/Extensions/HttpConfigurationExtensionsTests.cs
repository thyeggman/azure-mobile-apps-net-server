// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using Microsoft.Azure.Mobile.Server.Cache;
using Microsoft.Azure.Mobile.Server.Config;
using Xunit;

namespace System.Web.Http
{
    public class HttpConfigurationExtensionsTests
    {
        [Fact]
        public void GetMobileAppConfiguration_ReturnsNullByDefault()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            MobileAppConfiguration actual = config.GetMobileAppConfiguration();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void SetMobileAppConfiguration_Roundtrips()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            MobileAppConfiguration options = new MobileAppConfiguration();

            // Act
            config.SetMobileAppConfiguration(options);
            MobileAppConfiguration actual = config.GetMobileAppConfiguration();

            // Assert
            Assert.Same(options, actual);
        }

        [Fact]
        public void SetMobileAppConfiguration_ReturnsNull_IfSetToNull()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            config.SetMobileAppConfiguration(null);
            MobileAppConfiguration actual = config.GetMobileAppConfiguration();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void GetMobileAppSettingsProvider_ReturnsDefaultInstance()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            IMobileAppSettingsProvider actual = config.GetMobileAppSettingsProvider();

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<MobileAppSettingsProvider>(actual);
        }

        [Fact]
        public void SetMobileAppSettingsProvider_Roundtrips()
        {
            // Arrange
            MobileAppSettingsProvider provider = new MobileAppSettingsProvider();
            HttpConfiguration config = new HttpConfiguration();

            // Act
            config.SetMobileAppSettingsProvider(provider);
            IMobileAppSettingsProvider actual = config.GetMobileAppSettingsProvider();

            // Assert
            Assert.Same(provider, actual);
        }

        [Fact]
        public void SetMobileAppSettingsProvider_ReturnsDefault_IfSetToNull()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            config.SetMobileAppSettingsProvider(null);
            IMobileAppSettingsProvider actual = config.GetMobileAppSettingsProvider();

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<MobileAppSettingsProvider>(actual);
        }

        [Fact]
        public void GetCachePolicyProvider_ReturnsDefaultInstance()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            ICachePolicyProvider actual = config.GetCachePolicyProvider();

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<CachePolicyProvider>(actual);
        }

        [Fact]
        public void SetCachePolicyProvider_Roundtrips()
        {
            // Arrange
            CachePolicyProvider provider = new CachePolicyProvider();
            HttpConfiguration config = new HttpConfiguration();

            // Act
            config.SetCachePolicyProvider(provider);
            ICachePolicyProvider actual = config.GetCachePolicyProvider();

            // Assert
            Assert.Same(provider, actual);
        }

        [Fact]
        public void SetCachePolicyProvider_ReturnsDefault_IfSetToNull()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            config.SetCachePolicyProvider(null);
            ICachePolicyProvider actual = config.GetCachePolicyProvider();

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<CachePolicyProvider>(actual);
        }
    }
}