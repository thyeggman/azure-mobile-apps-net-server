// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.Collections.Generic;
using Microsoft.Azure.Mobile.Server.Config;
using Xunit;

namespace System.Web.Http
{
    public class HttpConfigurationExtensionsTests
    {
        private HttpConfiguration config = new HttpConfiguration();

        [Fact]
        public void GetAllowedMediaTypes_CreatesEmptyListIfNotSet()
        {
            // Arrange
            ISet<string> result = this.config.GetAllowedMediaTypes();

            // Assert
            Assert.Empty(result);
            Assert.Same(this.config.Properties["MS_AllowedMediaTypes"], result);
        }

        [Fact]
        public void GetAllowedMediaTypes_ReturnsSameEmptyList()
        {
            // Arrange
            ISet<string> result1 = this.config.GetAllowedMediaTypes();
            ISet<string> result2 = this.config.GetAllowedMediaTypes();

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void SetAndGetAllowedMediaTypes_Roundtrips()
        {
            // Arrange
            HashSet<string> expected = new HashSet<string>();

            // Act
            this.config.SetAllowedMediaTypes(expected);
            ISet<string> result = this.config.GetAllowedMediaTypes();

            // Assert
            Assert.Same(expected, result);
        }

        [Fact]
        public void SetAllowedMediaTypes_AllowsNull()
        {
            // Act
            this.config.SetAllowedMediaTypes(null);

            // Assert
            Assert.Null(this.config.Properties["MS_AllowedMediaTypes"]);
        }

        [Fact]
        public void GetAllowedMediaTypes_ReturnsEmptyListAfterSetToNull()
        {
            // Arrange
            this.config.SetAllowedMediaTypes(null);

            // Act
            ISet<string> result = this.config.GetAllowedMediaTypes();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetIsSingletonInstance_Returns_TrueAsDefault()
        {
            // Act
            bool actual = this.config.GetIsSingletonInstance();

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void SetIsSingletonInstance_Roundtrips()
        {
            // Act
            this.config.SetIsSingletonInstance(isSingleton: false);
            bool actual = this.config.GetIsSingletonInstance();

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public void GetMobileAppConfigOptions_ReturnsNullByDefault()
        {
            // Act
            MobileAppConfiguration actual = this.config.GetMobileAppConfiguration();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void SetMobileAppConfigOptions_Roundtrips()
        {
            // Arrange
            MobileAppConfiguration options = new MobileAppConfiguration();

            // Act
            this.config.SetMobileAppConfiguration(options);
            MobileAppConfiguration actual = this.config.GetMobileAppConfiguration();

            // Assert
            Assert.Same(options, actual);
        }
    }
}
