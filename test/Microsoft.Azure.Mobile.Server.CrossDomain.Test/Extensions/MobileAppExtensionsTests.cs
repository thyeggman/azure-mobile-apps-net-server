// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.CrossDomain.Test.Extensions
{
    public class MobileAppExtensionsTests
    {
        [Fact]
        public void MapLegacyCrossDomainController_SetsCrossDomainOrigins()
        {
            // Arrange
            var origins = new[] { "a", "b" };
            HttpConfiguration config = new HttpConfiguration();

            // Act
            new MobileAppConfiguration()
                .MapLegacyCrossDomainController(origins)
                .ApplyTo(config);

            // Assert
            var actual = config.GetCrossDomainOrigins();
            Assert.Same(origins, actual);
        }
    }
}
