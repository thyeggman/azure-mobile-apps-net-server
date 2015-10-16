// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;

namespace Local.Controllers
{
    /// <summary>
    /// The endpoints of this controller are secured
    /// </summary>

    [MobileAppController]
    public class CustomSecuredController : ApiController
    {
        [Authorize]
        public string Get()
        {
            MobileAppUser user = this.User as MobileAppUser;
            return "Hello from secured controller! UserId: " + user.Id;
        }
    }
}