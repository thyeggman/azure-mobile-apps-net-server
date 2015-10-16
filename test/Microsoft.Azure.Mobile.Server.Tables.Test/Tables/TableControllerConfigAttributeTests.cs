// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.Azure.Mobile.Mocks;
using Moq;
using Xunit;

namespace Microsoft.Azure.Mobile.Server.Tables
{
    public class TableControllerConfigAttributeTests
    {
        private TableControllerConfigAttribute tableControllerConfig;

        public TableControllerConfigAttributeTests()
        {
            this.tableControllerConfig = new TableControllerConfigAttribute();
        }

        [Fact]
        public void Initialize_Calls_TableControllerConfigProvider()
        {
            // Arrange
            Mock<ITableControllerConfigProvider> configProviderMock = new Mock<ITableControllerConfigProvider>();
            HttpConfiguration config = new HttpConfiguration();
            config.SetTableControllerConfigProvider(configProviderMock.Object);

            HttpControllerSettings settings = new HttpControllerSettings(config);
            HttpControllerDescriptor descriptor = new HttpControllerDescriptor()
            {
                Configuration = config
            };

            // Act
            this.tableControllerConfig.Initialize(settings, descriptor);

            // Assert
            configProviderMock.Verify(p => p.Configure(settings, descriptor), Times.Once());
        }
    }
}
