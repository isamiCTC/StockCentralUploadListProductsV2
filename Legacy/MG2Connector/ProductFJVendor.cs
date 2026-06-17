using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MG2Connector
{
    public class ProductApiVendorByFJv2
    {
        public string sku { get; set; }
        public string providerCode { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string shortDescription { get; set; }
        public int? stock { get; set; }
        public decimal? price { get; set; }
        public decimal? listPrice { get; set; }
        public decimal? taxes { get; set; }
        public decimal? weight { get; set; }
        public decimal? height { get; set; }
        public decimal? width { get; set; }
        public decimal? depth { get; set; }
        public bool? active { get; set; }
        public string ean { get; set; }
        public string brand { get; set; }
        public List<categoryBranch> categoryBranch { get; set; }
 //       public List<ProductAttribute> productAttributes { get; set; }
 //       public string TyC { get; set; }
 //       public List<Images> Images { get; set; }
 //       public string CategoryPath { get; set; }
 //       public int? ProductType { get; set; }
        public decimal? cost { get; set; }
        public string model { get; set; }
        public string warranty { get; set; }
        public string warrantyType { get; set; }
        public string shippingWarehouse { get; set; }
        public List<packages> packages { get; set; }
        public List<children> children { get; set; }


    }
    public class packages
    {
        public string name { get; set; }
        public decimal? weight { get; set; }
        public decimal? height { get; set; }
        public decimal? width { get; set; }
        public decimal? depth { get; set; }
        public decimal? measured_weight{get; set;}
    }
    public class children
    {
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int? stock { get; set; }
        public decimal? price { get; set; }

    }
    public class categoryBranch
    {
        public string code { get; set; }
        public string name { get; set; }
    }
    public class productImage
    {
        public string Base64 { get; set; }
    }

}