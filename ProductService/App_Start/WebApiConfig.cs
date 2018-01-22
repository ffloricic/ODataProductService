using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Builder;
using ProductService.Models;

namespace ProductService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Supplier>("Suppliers");
            builder.EntitySet<ProductRating>("Ratings");

            //new code: Add an action to the EDM, and define the parameter and return type
            ActionConfiguration rateProduct = builder.Entity<Product>().Action("RateProduct");
            rateProduct.Parameter<int>("Rating");
            rateProduct.Returns<double>();

            var rateAllProducts = builder.Entity<Product>().Collection.Action("RateAllProducts");
            rateAllProducts.CollectionParameter<int>("Ratings");

            config.Routes.MapODataRoute("odata", "odata", builder.GetEdmModel());

        }
    }
}
