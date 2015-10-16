// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

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
            var user = this.User as MobileAppUser;
            return user.MobileAppAuthenticationToken;
        }

        private IHttpActionResult GetUserDetails()
        {
            MobileAppUser user = this.User as MobileAppUser;
            JObject details = null;
            if (user != null)
            {
                details = new JObject
                {
                    { "id", user.Id }               
                };
            }

            return this.Json(details);
        }
    }
}
