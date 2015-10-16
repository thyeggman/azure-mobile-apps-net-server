// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.Collections.Generic;
using Xunit;

namespace System.Security.Claims
{
    public class ClaimsIdentityExtensionsTests
    {
        private ClaimsIdentity claimsIdentity;

        public ClaimsIdentityExtensionsTests()
        {
            List<Claim> claims = new List<Claim> { new Claim("type", "value") };
            this.claimsIdentity = new ClaimsIdentity(claims);
        }

        [Theory]
        [InlineData("type", "value")]
        [InlineData("unknown", null)]
        public void GetClaimValueOrNull_ReturnsFoundValue(string type, string expectedValue)
        {
            string result = this.claimsIdentity.GetClaimValueOrNull(type);
            Assert.Equal(expectedValue, result);
        }
    }
}
