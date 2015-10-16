using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using Owin;
using $safeprojectname$.DataObjects;
using $safeprojectname$.Models;

namespace $safeprojectname$
{
    public partial class Startup
    {
        public static void ConfigureMobileApp(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            new MobileAppConfiguration()
                .UseDefaultConfiguration()
                .ApplyTo(config);
            
            Database.SetInitializer(new $safeinitializerclassname$());

            app.UseMobileAppAuthentication(config);
            app.UseWebApi(config);
        }
    }

    public class $safeinitializerclassname$ : CreateDatabaseIfNotExists<$safecontextclassname$>
    {
        protected override void Seed($safecontextclassname$ context)
        {
            List<TodoItem> todoItems = new List<TodoItem>
            {
                new TodoItem { Id = Guid.NewGuid().ToString(), Text = "First item", Complete = false },
                new TodoItem { Id = Guid.NewGuid().ToString(), Text = "Second item", Complete = false }
            };

            foreach (TodoItem todoItem in todoItems)
            {
                context.Set<TodoItem>().Add(todoItem);
            }

            base.Seed(context);
        }
    }
}
