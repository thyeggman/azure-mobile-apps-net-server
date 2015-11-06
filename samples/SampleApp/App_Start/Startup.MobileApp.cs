// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Text;
using System.Web.Http;
using AutoMapper;
using Local.DataObjects;
using Local.Models;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;
using Owin;

namespace Local
{
    public partial class Startup
    {
        public static void ConfigureMobileApp(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();
            config.EnableSystemDiagnosticsTracing();

            new MobileAppConfiguration()
                .UseDefaultConfiguration()
                .ApplyTo(config);

            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Order, BrownOnline>()
                    .ForMember(dst => dst.Id, map => map.MapFrom(src => SqlFuncs.StringConvert((double)src.OrderId).Trim()));

                cfg.CreateMap<BrownOnline, Order>();

                cfg.CreateMap<PersonEntity, Person>();

                cfg.CreateMap<Person, PersonEntity>();
            });

            Database.SetInitializer(new GreenInitializer());
            Database.SetInitializer(new BrownInitializer());

            app.UseAppServiceAuthentication(config, AppServiceAuthenticationMode.LocalOnly);
            app.UseWebApi(config);
        }
    }

    public class GreenInitializer : CreateDatabaseIfNotExists<GreenContext>
    {
        protected override void Seed(GreenContext context)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            byte[] version = Encoding.UTF8.GetBytes("version");
            List<Green> items = new List<Green>
            {
                new Green { Id = "1", CreatedAt = now, UpdatedAt = now, Version = version, Text = "Henrik" },
                new Green { Id = "2", CreatedAt = now, UpdatedAt = now, Version = version, Text = "Nora" },
                new Green { Id = "3", CreatedAt = now, UpdatedAt = now, Version = version, Text = "Benjamin" },
            };

            foreach (Green item in items)
            {
                context.Greens.Add(item);
            }

            base.Seed(context);
        }
    }

    public class BrownInitializer : CreateDatabaseIfNotExists<BrownContext>
    {
        protected override void Seed(BrownContext context)
        {
            List<Customer> customers = new List<Customer>
            {
                new Customer { CustomerId = 1, Name = "Henrik", Orders = new Collection<Order> { new Order { OrderId = 10, Item = "Shoes", Quantity = 2 }}},
                new Customer { CustomerId = 2, Name = "Scott", Orders = new Collection<Order> { new Order { OrderId = 20, Item = "Polos", Quantity = 10 }}},
                new Customer { CustomerId = 3, Name = "Benjamin", Orders = new Collection<Order> { new Order { OrderId = 30, Item = "S'mores", Quantity = 20 }}},
            };

            foreach (Customer customer in customers)
            {
                context.Customers.Add(customer);
            }

            base.Seed(context);
        }
    }
}
