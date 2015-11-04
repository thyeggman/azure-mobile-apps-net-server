// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.Linq;
using System.Security.Claims;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Mobile.Server.TestControllers
{
    [Authorize]
    public class SecuredController : ApiController
    {
        [AllowAnonymous]
        [Route("api/secured/anonymous")]
        public IHttpActionResult GetAnonymous()
        {
            return this.GetUserDetails();
        }

        [Route("api/secured/authorize")]
        public string GetApplication()
        {
            return this.Request.Headers.GetValues("x-zumo-auth").FirstOrDefault<string>();
        }

        private IHttpActionResult GetUserDetails()
        {
            ClaimsPrincipal user = this.User as ClaimsPrincipal;
            JObject details = null;
            if (user != null)
            {
                ClaimsIdentity identity = user.Identity as ClaimsIdentity;
                string userId = identity.GetClaimValueOrNull("uid");
                details = new JObject
                {
                    { "id", userId }               
                };
            }

            return this.Json(details);
        }
    }
}
