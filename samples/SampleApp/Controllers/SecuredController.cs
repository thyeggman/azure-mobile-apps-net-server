// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.ComponentModel;
using System.Web.Http;
using System.Security.Claims;
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
            ClaimsPrincipal user = this.User as ClaimsPrincipal;
            ClaimsIdentity identity = user.Identity as ClaimsIdentity;
            Claim userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                return "Hello from secured controller! UserId: " + userIdClaim.Value;
            }
            else
            {
                return "Hello from secured controller! UserId: null";
            }
        }
    }
}