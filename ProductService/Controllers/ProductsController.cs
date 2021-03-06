﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Routing;
using System.Web.Http.Routing;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using ProductService.Models;

namespace ProductService.Controllers
{
    /*
    The WebApiConfig class may require additional changes to add a route for this controller. Merge these statements into the Register method of the WebApiConfig class as applicable. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using ProductService.Models;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Product>("Products");
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
    public class ProductsController : ODataController
    {
        private ProductServiceContext db = new ProductServiceContext();

        // GET: odata/Products
        [EnableQuery]
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: odata/Products(5)
        [EnableQuery]
        public SingleResult<Product> GetProduct([FromODataUri] int key)
        {
            return SingleResult.Create(db.Products.Where(product => product.ID == key));
        }

        // PUT: odata/Products(5)
        public IHttpActionResult Put([FromODataUri] int key, Delta<Product> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Product product = db.Products.Find(key);
            if (product == null)
            {
                return NotFound();
            }

            patch.Put(product);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(product);
        }

        // POST: odata/Products
        public IHttpActionResult Post(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Products.Add(product);
            db.SaveChanges();

            return Created(product);
        }

        // PATCH: odata/Products(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Product> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Product product = db.Products.Find(key);
            if (product == null)
            {
                return NotFound();
            }

            patch.Patch(product);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(product);
        }

        // DELETE: odata/Products(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            Product product = db.Products.Find(key);
            if (product == null)
            {
                return NotFound();
            }

            db.Products.Remove(product);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        //GET /products(1)/Supplier
        public Supplier GetSupplier([FromODataUri] int key)
        {
            Product product = db.Products.FirstOrDefault(p => p.ID == key);
            if (product == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return product.Supplier;
        }

        //POST http://localhost/odata/Products(1)/$links/Supplier
        //Content-Type: application/json
        //Content-Length: 50

        //{"url":"http://localhost/odata/Suppliers('CTSO')"}
        [AcceptVerbs("POST", "PUT")]
        public async Task<IHttpActionResult> CreateLink([FromODataUri] int key, string navigationProperty, [FromBody] Uri link)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            switch (navigationProperty)
            {
                case "Supplier":
                    string supplierKey = GetKeyFromLinkUri<string>(link);
                    Supplier supplier = await db.Suppliers.FindAsync(supplierKey);
                    if (supplier == null)
                    {
                        return NotFound();
                    }

                    product.Supplier = supplier;
                    await db.SaveChangesAsync();
                    return StatusCode(HttpStatusCode.NoContent);
                default:
                    return NotFound();
            }

        }

        //helper method to extract the key from OData URI
        private TKey GetKeyFromLinkUri<TKey>(Uri link)
        {
            TKey key = default(TKey);

            ////get the route that was used for this request
            IHttpRoute route = Request.GetRouteData().Route;

            //creates an equivalent self-hosted route
            IHttpRoute newRoute = new HttpRoute(route.RouteTemplate
                , new HttpRouteValueDictionary(route.Defaults)
                , new HttpRouteValueDictionary(route.Constraints)
                , new HttpRouteValueDictionary(route.DataTokens)
                , route.Handler);

            //create a fake GET request for the link URi
            var tmpRequest = new HttpRequestMessage(HttpMethod.Get, link);

            //senf this request through routing process
            var routeData = newRoute.GetRouteData(Request.GetConfiguration().VirtualPathRoot, tmpRequest);

            //it the get request matches the route, use the path segments to find the key
            if (routeData != null)
            {
                ODataPath path = tmpRequest.GetODataPath();
                var segment = path.Segments.OfType<KeyValuePathSegment>().FirstOrDefault();
                if (segment != null)
                {
                    //convert the segment into the key type
                    key = (TKey)ODataUriUtils.ConvertFromUriLiteral(segment.Value, ODataVersion.V3);
                }
            }

            return key;
        }

        public async Task<IHttpActionResult> DeleteLink([FromODataUri] int key, string navigationProperty)
        {
            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            switch (navigationProperty)
            {
                case "Supplier":
                    product.Supplier = null;
                    await db.SaveChangesAsync();
                    return StatusCode(HttpStatusCode.NoContent);

                default:
                    return NotFound();
            }
        }

        //http://localhost/odata/Products(1)/RateProduct
        //{"Rating":2}
        [HttpPost]
        public async Task<IHttpActionResult> RateProduct([FromODataUri] int key, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            int rating = (int)parameters["Rating"];
            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                NotFound();
            }

            product.Ratings.Add(new ProductRating() { Rating = rating });
            db.SaveChanges();

            double average = product.Ratings.Average(r => r.Rating);

            return Ok(average);
        }

        [HttpPost]
        public int RateAllProducts(ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var ratings = parameters["Ratings"] as ICollection<int>;

            //ovo je ovdje stavljeno samo kako bi se moglo compilirati. Inače, nije jasno iz objašenjenja, šta bi trebala raditi ova funkcija
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ProductExists(int key)
        {
            return db.Products.Count(e => e.ID == key) > 0;
        }
    }
}
