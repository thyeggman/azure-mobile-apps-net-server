using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Swagger;
using Swashbuckle.Application;

namespace Local
{
    public partial class Startup
    {
        public static void ConfigureSwagger(HttpConfiguration config)
        {
            config.Services.Replace(typeof(IApiExplorer), new MobileAppApiExplorer(config));
            config
               .EnableSwagger(c =>
               {
                   c.SingleApiVersion("v1", "brettsam1201Service");
                   c.OperationFilter<MobileAppHeaderFilter>();
                   c.SchemaFilter<MobileAppSchemaFilter>();
                   c.AppServiceAuthentication("https://brettsam1201.azurewebsites.net/", "aad");
               })
               .EnableSwaggerUi(c =>
               {
                   c.EnableOAuth2Support("test-client-id", "test-realm", "Swagger UI");
                   c.MobileAppUi();
               });
        }
    }
}