using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MG2Connector
{
    public class ProductApi
    {
        public string Sku { get; set; }
        public int? ProviderId { get; set; }
        public int? ProviderCode { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public int? Stock { get; set; }
        public decimal? Price { get; set; }
        public decimal? ListPrice { get; set; }
        public decimal? NetPrice { get; set; }
        public decimal? Taxes { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Depth { get; set; }
        public bool? Active { get; set; }
        public string Ean { get; set; }
        public string Brand { get; set; }
        public List<CategoryBranch> CategoryBranch { get; set; }
        public List<ProductAttribute> ProductAttributes { get; set; }
        public string TyC { get; set; }
        public List<Images> Images { get; set; }
        public string CategoryPath { get; set; }
        public int? ProductType { get; set; }
        public decimal? Cost { get; set; }
        public string Model { get; set; }
        public string Warranty { get; set; }
        public string shippingWarehouse { get; set; }
        public List<Packages> Packages { get; set; }
        public List<Children> Children { get; set; }


    }
    public class ProductApiVendorByFJ
    {
        public string Sku { get; set; }
        public int? ProviderId { get; set; }
        public string ProviderCode { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public int? Stock { get; set; }
        public decimal? Price { get; set; }
        public decimal? ListPrice { get; set; }
        public decimal? NetPrice { get; set; }
        public decimal? Taxes { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Depth { get; set; }
        public bool? Active { get; set; }
        public string Ean { get; set; }
        public string Brand { get; set; }
        public List<CategoryBranch> CategoryBranch { get; set; }
        public List<ProductAttribute> ProductAttributes { get; set; }
        public string TyC { get; set; }
        public List<Images> Images { get; set; }
        public string CategoryPath { get; set; }
        public int? ProductType { get; set; }
        public decimal? Cost { get; set; }
        public string Model { get; set; }
        public string Warranty { get; set; }
        public string shippingWarehouse { get; set; }
        public List<Packages> Packages { get; set; }
        public List<Children> Children { get; set; }


    }
    public class Packages
    {
        public string Name { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Depth { get; set; }
        public decimal? Measured_Weight{get; set;}
    }
    public class Children
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Stock { get; set; }
        public decimal? Price { get; set; }

    }
    public class CategoryBranch
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
    public class ProductImage
    {
        public string Base64 { get; set; }
    }
    public class ProductAttribute
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }
    public class Images
    {
        public string Url { get; set; }
    }
}