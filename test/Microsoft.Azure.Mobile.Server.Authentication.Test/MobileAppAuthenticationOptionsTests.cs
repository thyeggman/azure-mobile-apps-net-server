// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Owin.Security;
using TestUtilities;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Security
{
    public class MobileAppAuthenticationOptionsTests
    {
        private const string MasterKey = "$$MasterKey";
        private const string SigningKey = "$$SigningKey";
        private const string ApplicationKey = "$$ApplicationKey";

        private MobileAppAuthenticationOptions options;

        public MobileAppAuthenticationOptionsTests()
        {
            this.options = new MobileAppAuthenticationOptions();
            this.options.SigningKey = SigningKey;
        }

        [Fact]
        public void AuthenticationMode_IsActive()
        {
            Assert.Equal(AuthenticationMode.Active, this.options.AuthenticationMode);
        }

        [Fact]
        public void Realm_Roundtrips()
        {
            PropertyAssert.Roundtrips(this.options, o => o.Realm, PropertySetter.NullThrows, defaultValue: "Service", roundtripValue: "Hello");
        }

        [Fact]
        public void Realm_ThrowsOnInvalidCharacter()
        {
            // Act
            FormatException ex = Assert.Throws<FormatException>(() => this.options.Realm = "你好世界");

            // Assert
            Assert.Equal("The format of value '你好世界' is invalid. The character '你' is not a valid HTTP header token character.", ex.Message);
        }

        [Fact]
        public void SigningKey_Roundtrips()
        {
            PropertyAssert.Roundtrips(this.options, o => o.SigningKey, PropertySetter.NullRoundtrips, defaultValue: SigningKey, roundtripValue: "roundtrips");
        }

        [Fact]
        public void SkipTokenSignatureValidation_Roundtrips()
        {
            this.options.SkipTokenSignatureValidation = true;
            Assert.True(this.options.SkipTokenSignatureValidation);

            this.options.SkipTokenSignatureValidation = false;
            Assert.False(this.options.SkipTokenSignatureValidation);
        }

        [Fact]
        public void SkipTokenSignatureValidation_DefaultsFalse()
        {
            var opt = new MobileAppAuthenticationOptions();
            Assert.False(opt.SkipTokenSignatureValidation);
        }
    }
}
