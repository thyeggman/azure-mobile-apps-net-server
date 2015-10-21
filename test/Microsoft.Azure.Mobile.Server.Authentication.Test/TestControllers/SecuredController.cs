// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace Microsoft.Azure.Mobile.Server.TestControllers
{
    public class SecuredController : ApiController
    {
        [Route("api/secured/anonymous")]
        public IHttpActionResult GetAnonymous()
        {
            return this.GetUserDetails();
        }

        [Authorize]
        [Route("api/secured/application")]
        public HttpResponseMessage GetApplication()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [Authorize]
        [Route("api/secured/user")]
        public HttpResponseMessage GetUser()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [Authorize]
        [Route("api/secured/admin")]
        public HttpResponseMessage GetAdmin()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [Route("api/secured/nothing")]
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private IHttpActionResult GetUserDetails()
        {
            ClaimsPrincipal user = this.User as ClaimsPrincipal;
            JObject details = null;
            ClaimsIdentity identity = this.User.Identity as ClaimsIdentity;
            string userId = identity.GetClaimValueOrNull("uid");

            if (user != null)
            {
                details = new JObject
                {
                    { "id", userId },
                };
            }
            return this.Json(details);
        }
    }
}
