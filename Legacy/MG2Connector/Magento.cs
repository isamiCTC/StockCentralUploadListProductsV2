using AWSSignatureV4.Signers;
using ExcelDataReader;
using Imazen.WebP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Cms;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;



namespace MG2Connector
{
    public class Magento
    {
        private RestClient Client { get; set; }
        private string Token { get; set; }
        public string Tid = "";
        public Magento(string magentoUrl)
        {
            Client = new RestClient(magentoUrl);
        }
        public Magento(string magentoUrl, string token)
        {
            Client = new RestClient(magentoUrl);
            Token = token;

        }

        public string GetAdminToken(string userName, string passWord)
        {
            var request = CreateRequest("/rest/V1/integration/admin/token", Method.POST);
            var user = new Credentials();
            user.username = userName;
            user.password = passWord;

 
            string json = JsonConvert.SerializeObject(user, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Token = response.Content.Trim('"');
                return response.Content.Trim('"');
            } else
            {
                return "";
            }
        }

        private RestRequest CreateRequest(string endPoint, Method method)
        {
            var request = new RestRequest(endPoint, method);
            request.RequestFormat = DataFormat.Json;
            return request;
        }


        #region Productos
        public M2Product GetProduct(string sku, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(sku), Method.GET, Token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                M2Product product = JsonConvert.DeserializeObject<M2Product>(response.Content);
                return product;
            }
            return null;
        }

        public string PutProductW(string Token, string P, string Sku, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(Sku) + "", Method.PUT, Token);


            //tring json = JsonConvert.SerializeObject(P, Formatting.Indented);

            request.AddParameter("application/json", "{\"product\":" + P + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);

            return response.StatusCode.ToString();
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return true;
            //}
            //return false;
        }


        public string PutProductPL(string Token, M2Product_PayLoad P, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(P.Sku) + "", Method.PUT, Token);


            string json = JsonConvert.SerializeObject(P, Formatting.Indented);

            request.AddParameter("application/json", "{\"product\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);

            return response.StatusCode.ToString();
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return true;
            //}
            //return false;
        }



        public string PutProduct(M2Product PSku, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(PSku.Sku) + "", Method.PUT, Token);


            string json = JsonConvert.SerializeObject(PSku, Formatting.Indented);

            request.AddParameter("application/json", "{\"product\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);

            return response.StatusCode.ToString();
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return true;
            //}
            //return false;
        }
        public string PutProduct(String Token, M2Product PSku, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(PSku.Sku) + "", Method.PUT, Token);


            string json = JsonConvert.SerializeObject(PSku, Formatting.None);

            request.AddParameter("application/json", "{\"product\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);

            return response.StatusCode.ToString();
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return true;
            //}
            //return false;
        }

        public string SetProductos(M2Product_PayLoad item, string StoreView)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/products/", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);

            request.AddParameter("application/json", "{\"product\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString();
        }

        public string UpdateProductoAttributeStoreView(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/products/", Method.POST, Token);
            //var request = CreateRequest("/rest/V1/products", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"sku\": \"" + item.Sku + "\", \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\" } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString();
        }
        public string UpdateProductoAttributeStoreViewNumber(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/products/" + HttpUtility.UrlEncode(item.Sku), Method.PUT, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"sku\": \"" + item.Sku + "\", \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": " + item.CustomAttributes[0].Value + "  } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString() + "|" + response.Content;
        }


        public string UpdateProductoAttributeStoreViewPut(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/products/" + HttpUtility.UrlEncode(item.Sku), Method.PUT, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\"  } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString();
        }
        public string UpdateProductoAttributePut(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(item.Sku), Method.PUT, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\"  } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString();
        }
        public string UpdateProductoTyC(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/V1/products", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"sku\": \"" + item.Sku + "\", \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\" } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString();
        }

        public string UpdateProductoGlobalAttribute(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/V1/products", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"sku\": \"" + item.Sku + "\", \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\" } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString() + "|" + response.Content;
        }
        public string UpdateProductoGlobalAttributePOST_Store(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/products", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"sku\": \"" + item.Sku + "\", \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\" } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString();
        }

        public string UpdateProductoDefaultAttribute(string Token, M2Product item, string StoreView)
        {
            var request = CreateRequest("/rest/default/V1/products", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            string msg = "{\"product\": { \"sku\": \"" + item.Sku + "\", \"custom_attributes\": [ { \"attribute_code\": \"" + item.CustomAttributes[0].AttributeCode + "\", \"value\": \"" + item.CustomAttributes[0].Value + "\" } ]}} ";

            request.AddParameter("application/json", msg, ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    response.StatusCode.ToString();
            //}
            return response.StatusCode.ToString() + "|" + response.Content;
        }
        public string SetImagen(Media_Gallery_Entries item, string SKU, string Store)
        {
            var request = CreateRequest("/rest/all/V1/products/" + HttpUtility.UrlEncode(SKU) + "/media/", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);

            request.AddParameter("application/json", "{\"entry\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return "0";
            //}
            return response.StatusCode.ToString();
        }

        public string PutImagen(Media_Gallery_Entries item, string SKU, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(SKU) + "/media/", Method.POST, Token);

            string json = JsonConvert.SerializeObject(item, Formatting.Indented);

            request.AddParameter("application/json", "{\"entry\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return "0";
            //}
            return response.StatusCode.ToString();
        }

        public string SetProductLink(string SKU, List<Product_Links> p, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(SKU) + "/links/", Method.POST, Token);

            string json = JsonConvert.SerializeObject(p, Formatting.Indented);

            request.AddParameter("application/json", "{\"items\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return "0";
            //}
            return response.StatusCode.ToString();
        }

        public string SetStockItemsSku(int Qty, string sku, string Store)
        {
            var request = CreateRequest("/rest/" + Store + "/V1/products/" + HttpUtility.UrlEncode(sku) + "/stockItems/1", Method.PUT, Token);
            var PSku = new StockItem();
            PSku.Qty = Qty;
            string json = JsonConvert.SerializeObject(PSku, Formatting.Indented);

            request.AddParameter("application/json", "{\"stockItem\":{\"qty\":" + Qty.ToString() + ",\"is_in_stock\":true}}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            return response.ToString();
            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return "0";
            //}
            return response.StatusCode.ToString();
        }

        public string SetStockItemsSku(string token, int Qty, string sku)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(sku) + "/stockItems/1", Method.PUT, token);
            var PSku = new StockItem();
            PSku.Qty = Qty;
            string json = JsonConvert.SerializeObject(PSku, Formatting.Indented);

            request.AddParameter("application/json", "{\"stockItem\":{\"qty\":" + Qty.ToString() + ", \"is_in_stock\":" + (Qty > 0 ? "true" : "false") + "}}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }

        public string SetEspecialPrice(List<Price> especial_price, string Store)
        {
            //          var request = CreateRequest("/rest/"+Store+"/V1/products/special-price", Method.POST, Token);
            var request = CreateRequest("/rest/V1/products/special-price", Method.POST, Token);


            string json = JsonConvert.SerializeObject(especial_price, Formatting.Indented);

            request.AddParameter("application/json", "{\"prices\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            //return response.StatusCode.ToString();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }

        public string SetEspecialPrice(List<Price> especial_price, string Store, string Token)
        {
            //          var request = CreateRequest("/rest/"+Store+"/V1/products/special-price", Method.POST, Token);
            var request = CreateRequest("/rest/V1/products/special-price", Method.POST, Token);


            string json = JsonConvert.SerializeObject(especial_price, Formatting.Indented);

            request.AddParameter("application/json", "{\"prices\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            //return response.StatusCode.ToString();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }

        public string SetEspecialPrice(string token, int Store, string sku, string Price)
        {
            var request = CreateRequest("/rest/V1/products/special-price", Method.POST, token);

            request.AddParameter("application/json", "{\"prices\":[{\"price\":" + Price + ", \"store_id\": " + Store.ToString() + ", \"sku\": \"" + sku + "\", \"price_from\": \"2021-11-01 00:00:00\", \"price_to\": \"2031-12-31 00:00:00\",  \"extension_attributes\": {} }]}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString() + "|" + response.Content.ToString() ;
        }
        public string SetEspecialPriceAlt(string token, int Store, string sku, string Price, string StoreCode)
        {
            var request = CreateRequest("/rest/" + StoreCode + "_store_view/V1/products/special-price", Method.POST, token);

            request.AddParameter("application/json", "{\"prices\":[{\"price\":" + Price + ", \"store_id\":0, \"sku\": \"" + sku + "\", \"price_from\": \"2022-07-22 00:00:00\", \"price_to\": \"2031-12-31 00:00:00\" }]}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString() + "|" + response.Content.ToString();
        }

        public string EspecialPriceDelete(string token, int Store, string sku, string Price, string StoreCode, string price_from, string price_to)
        {
            var request = CreateRequest("/rest/V1/products/special-price-delete", Method.POST, token);

            request.AddParameter("application/json", "{\"prices\":[{\"price\":" + Price + ", \"store_id\": " + Store.ToString() + ", \"sku\": \"" + sku + "\", \"price_from\": \""+ price_from +"\", \"price_to\": \"" +price_to+"\" }]}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString() + "|" + response.Content.ToString();
        }


        public string SetCostPrice(string token, string sku, string cost_price, string Store)
        {
            var request = CreateRequest("/rest/V1/products/cost", Method.POST, token);

            request.AddParameter("application/json", "{\"prices\":[{ \"cost\": " + cost_price.Replace(",", ".") + ", \"store_id\": " + Store + ", \"sku\": \"" + sku + "\" }]} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }

        public List<Price> getEspecialPrice(List<string> SKU, string token)
        {
            var request = CreateRequest("/rest/V1/products/special-price-information", Method.POST, token);


            string json = JsonConvert.SerializeObject(SKU, Formatting.Indented);

            request.AddParameter("application/json", "{\"skus\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode.ToString() == "OK")
            {
                List<Price> product = JsonConvert.DeserializeObject<List<Price>>(response.Content);
                return product;
            }
            else
                return null;

        }




        #endregion



        #region Categorias
        public Category CreateCategory(ProductCategory cat, string StoreView)
        {



            var request = CreateRequest("/rest/" + StoreView + "/V1/categories", Method.POST, Token);



            string json = JsonConvert.SerializeObject(cat, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<Category>(response.Content);
            }
            else
            {
                return null;
            }

        }

        public CategoryList GetCategories(string store)
        {
            var request = CreateRequest("/rest/" + store + "/V1/categories/", Method.GET, Token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                CategoryList categories = JsonConvert.DeserializeObject<CategoryList>(response.Content);
                return categories;
            }
            return null;
        }

        public Category getCategory(int Id, string StoreView)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/categories/" + Id.ToString(), Method.GET, Token);




            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<Category>(response.Content);
            }
            else
            {
                return null;
            }

            return null;

        }

        public string PutCategory(string StoreView, Category category)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/categories/" + category.Id, Method.PUT, Token);


            string json = JsonConvert.SerializeObject(category, Formatting.Indented);

            request.AddParameter("application/json", "{\"category\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            return response.ToString();

            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}

        }

        public List<M2Product> CategoryProducts(string StoreView, int id)
        {
            var request = CreateRequest("/rest/" + StoreView + "/V1/categories/" + id.ToString() + "/products/", Method.GET, Token);




            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<List<M2Product>>(response.Content);
            }
            else
            {
                return null;
            }

            return null;

        }


        #endregion
        public M2Product GetSku(string token, string sku)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(sku), Method.GET, token);
            LogSystem.WriteLogDebugDirect(" -----  Consulta :  /rest/V1/products/" + sku, "MagentoProductDetail");

            var response = Client.Execute(request);
            LogSystem.WriteLogDebugDirect(" -----  Producto Obtenido de Magento :" + response.Content, "MagentoProductDetail");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                M2Product product = JsonConvert.DeserializeObject<M2Product>(response.Content);
                return product;
            }
            return null;
        }

        public M2Product_PayLoad GetSku_PP(string token, string sku)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(sku), Method.GET, token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                M2Product_PayLoad product = JsonConvert.DeserializeObject<M2Product_PayLoad>(response.Content);
                return product;
            }
            return null;
        }

        public M2Product GetSku(string token, string sku, int StoreId)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(sku) + "?storeId=" + StoreId, Method.GET, token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                M2Product product = JsonConvert.DeserializeObject<M2Product>(response.Content);
                return product;
            }
            return null;
        }





        public ProductPriceSku[] GetPriceSku(string token, string sku)
        {
            var request = CreateRequest("/rest/V1/products/base-prices-information", Method.POST, token);
            var PSku = new ProductSku();
            PSku.Sku = new string[1];
            PSku.Sku[0] = sku;
            string json = JsonConvert.SerializeObject(PSku, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //prices = (response.Content);
                ProductPriceSku[] prices = JsonConvert.DeserializeObject<ProductPriceSku[]>(response.Content);
                return prices;
            }
            return null;
        }


        public string GetStockSku(string token, string sku)
        {
            var request = CreateRequest("/rest/V1/stockItems/" + HttpUtility.UrlEncode(sku), Method.GET, token);
            var PStk = new StockItem();

            string json = JsonConvert.SerializeObject(PStk, Formatting.Indented);

            // request.AddParameter("application/json", "{\"stockItem\":{\"qty\":0}}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            string prices = "";
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                PStk = JsonConvert.DeserializeObject<StockItem>(response.Content);

            }
            else
                return "ERROR";
            return PStk.Qty.ToString();
        }



        public string GetProductAttributeCode(string token, string AttributeCode)
        {
            var request = CreateRequest("/pub/rest/default/V1/products/attributes/" + AttributeCode + "/options" , Method.GET, token);
            var PStk = new StockItem();

            string json = JsonConvert.SerializeObject(PStk, Formatting.Indented);

            // request.AddParameter("application/json", "{\"stockItem\":{\"qty\":0}}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            string resp = "";
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                resp = (response.Content);

            }
            else
                return "ERROR";
            return resp;
        }


        public string SetProductAttributeCodeOption(string token, string AttributeCode, string Label, string Value)
        {
            var request = CreateRequest("/pub/rest/default/V1/products/attributes/" + AttributeCode + "/options", Method.GET, token);
            var PStk = new StockItem();

            //string json = JsonConvert.SerializeObject(PStk, Formatting.Indented);

            request.AddParameter("application/json", "{\"option\":{\"label\":\""+Label+"\",\"value\":\""+Value+"\" }}", ParameterType.RequestBody);
            
            var response = Client.Execute(request);
            string resp = "";
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                resp = (response.Content);

            }
            else
                return "ERROR";
            return resp;
        }




        public M2Product_PayLoad GetBy_SkuFull(string token, string sku)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(sku), Method.GET, token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                M2Product_PayLoad product = JsonConvert.DeserializeObject<M2Product_PayLoad>(response.Content);
                return product;
            }
            return null;
        }


        public string GetBy_SkuFJ(string sku, string ProviderId)
        {
            var request = CreateRequest("/query/publication?provider_id=" + ProviderId + "&sku=" + sku, Method.GET);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string product = response.Content;
                List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                return product;
            }
            return null;
        } 
        public string ImportProductfromFileLine(string Path, string ProviderID)
        {
            LogSystem.WriteLogDebugDirect(" -----  Iniciando Importación", "ImportFile");
            using (FileStream stream = File.Open(Path, FileMode.Open, FileAccess.Read))
            {
                var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                var result = reader.AsDataSet();
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];

                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
                
                Magento m_api = new Magento(URL_API_CTC);

                try
                {
                    for (int j = 1; j < result.Tables[0].Rows.Count; j++)
                    {
                        int Stock = 0;
                        string sku = result.Tables[0].Rows[j][0].ToString();
                        switch (result.Tables[0].Columns.Count)
                        {
                            case 2:
                                Stock = Convert.ToInt32(result.Tables[0].Rows[j][1].ToString());
                                var request2_1 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/", Method.GET, TokenAPI);

                                var response2_1 = m_api.Client.Execute(request2_1);

                                if (response2_1.StatusCode == HttpStatusCode.OK)
                                {
                                    JObject o = JObject.Parse(response2_1.Content);

                                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());


                                    //ProductApi product = JsonConvert.DeserializeObject<ProductApi>(response2_1.Content);

                                    product.Stock = Stock;
                                    string jsonStock = JsonConvert.SerializeObject(product, Formatting.None);

                                    /*
                                    Object objJSONStock = new ProductApi()
                                    {
                                        Sku = sku,
                                        ProviderId = Convert.ToInt32(ProviderID),
                                        Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                                        Stock = Stock,
                                        Name = Name,
                                        Description = Description,
                                        ShortDescription = ShortDescription,
                                        Price = Price,
                                        ListPrice = ListPrice,
                                        NetPrice = NetPrice,
                                        Taxes = Taxes,
                                        //Height = (decimal)o.SelectToken("result.product.height"),
                                        Height = Height,
                                        Width = Width,
                                        Depth = Depth,
                                        Weight = Weight,
                                        Active = true,
                                        Ean = "",
                                        Brand = Brand,
                                        CategoryBranch = LCB


                                    };
                                    string json = JsonConvert.SerializeObject(objJSONStock, Formatting.None);
                                    */

                                    var request2_2 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/", Method.PUT, TokenAPI);
                                    request2_2.AddParameter("application/json", jsonStock, ParameterType.RequestBody);

                                    var response2_2 = m_api.Client.Execute(request2_2);
                                    Console.WriteLine("Producto Nro " + j + ", SKU [" + sku + "] - Stock : " + Stock);
                                    LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - Stock: " + Stock + ". RES :" + response2_1.Content, "ImportFile");

                                }

                                break;
                            case 5:
                                break;

                            default:
                                // Formato Diggit / Samsung
                                //0 SKU	
                                //1 Nombre
                                //2 Marca
                                //3 Descripcion
                                //4 Alto
                                //5 Ancho
                                //6 Largo
                                //7 Peso miligramos
                                //8 URL Imagenes
                                //9 Precio PVP
                                // 10 IVA
                                // 11 Origen
                                // 12 Si aplica AHORA XX
                                // 13 Categoria
                                // 14 Subcategoria


                                string responseFile = "";
                                try
                                {

                                    // Validación de nombres.... CONTROL DE FORMATO

                                    if (result.Tables[0].Rows[0][0].ToString().ToUpper() != "SKU")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en SKU|";
                                    if (result.Tables[0].Rows[0][1].ToString().ToUpper() != "NOMBRE")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en NOMBRE|";
                                    if (result.Tables[0].Rows[0][2].ToString().ToUpper() != "MARCA")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en MARCA|";
                                    if (result.Tables[0].Rows[0][3].ToString().ToUpper() != "DESCRIPCION")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en DESCRIPCION|";
                                    if (result.Tables[0].Rows[0][4].ToString().ToUpper() != "ALTO")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en ALTO|";
                                    if (result.Tables[0].Rows[0][5].ToString().ToUpper() != "ANCHO")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en ANCHO|";
                                    if (result.Tables[0].Rows[0][6].ToString().ToUpper() != "LARGO")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en LARGO|";
                                    //if (result.Tables[0].Rows[0][7].ToString().ToUpper() != "PESO MILIGRAMOS")
                                    //    return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en PESO MILIGRAMOS|";
                                    if (result.Tables[0].Rows[0][8].ToString().ToUpper() != "URL IMAGENES")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en URL IMAGENES|";
                                    if (result.Tables[0].Rows[0][9].ToString().ToUpper() != "PRECIO")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en PRECIO|";
                                    if (result.Tables[0].Rows[0][10].ToString().ToUpper() != "IVA")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en IVA|";
                                    if (result.Tables[0].Rows[0][11].ToString().ToUpper() != "TIPO")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en TIPO|";
                                    if (result.Tables[0].Rows[0][12].ToString().ToUpper() != "AHORA")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en AHORA|";
                                    if (result.Tables[0].Rows[0][13].ToString().ToUpper() != "CATEGORIA")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en CATEGORIA|";
                                    if (result.Tables[0].Rows[0][14].ToString().ToUpper() != "SUB CATEGORIA")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en SUB CATEGORIA|";
                                    if (result.Tables[0].Rows[0][15].ToString().ToUpper() != "STOCK")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en STOCK|";
                                    if (result.Tables[0].Rows[0][16].ToString().ToUpper() != "OFERTA")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en OFERTA|";
                                    if (result.Tables[0].Rows[0][17].ToString().ToUpper() != "FECHA DE INICIO")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en FECHA DE INICIO|";
                                    if (result.Tables[0].Rows[0][18].ToString().ToUpper() != "FECHA DE FIN")
                                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error de formato en FECHA DE FIN|";




                                    // Console.WriteLine(result.Tables[0].Rows[1][0].ToString());
                                    // Console.WriteLine(result.Tables[0].Rows[1][1].ToString());
                                    CategoryBranch CB = new CategoryBranch();

                                    switch (result.Tables[0].Rows[j][14].ToString())
                                    {
                                        case "ACCESORIOS CEL":
                                            CB.Name = "ACCESORIOS CEL";
                                            CB.Code = "1217";
                                            break;
                                        case "AUDIO":
                                            CB.Name = "Audio y Video";
                                            CB.Code = "1211";
                                            break;
                                        case "CELULARES":
                                            CB.Name = "Telefonía y Accesorios";
                                            CB.Code = "1217";
                                            break;
                                        case "CLIMATIZACIÓN":
                                            CB.Name = "Línea Blanca y Climatización";
                                            CB.Code = "1215";
                                            break;
                                        case "COMPUTACIÓN":
                                            CB.Name = "Tecnología y Computación";
                                            CB.Code = "1213";
                                            break;
                                        case "GAMING":
                                            CB.Name = "GAMING";
                                            CB.Code = "1214";
                                            break;
                                        case "ILUMINACIÓN":
                                            CB.Name = "1249";
                                            CB.Code = "1249";
                                            break;
                                        case "LINEA BLANCA":
                                            CB.Name = "LINEA BLANCA";
                                            CB.Code = "1215";
                                            break;
                                        case "MOVILIDAD":
                                            CB.Name = "Outdoors";
                                            CB.Code = "1226";
                                            break;
                                        case "PEQUEÑOS ELECTRODOMESTICOS":
                                            CB.Name = "Pequeños Electro";
                                            CB.Code = "1212";
                                            break;
                                        case "SALUD":
                                            CB.Name = "Cuidado Personal";
                                            CB.Code = "1234";
                                            break;
                                        case "TV":
                                            CB.Name = "TV y Gaming";
                                            CB.Code = "1214";
                                            break;
                                        case "Maquillaje y Skincare":
                                            CB.Name = "Maquillaje y Skincare";
                                            CB.Code = "1235";
                                            break;
                                        case "Pequeños Electro":
                                            CB.Name = "Pequeños Electro";
                                            CB.Code = "1212";
                                            break;
                                       // case "Casa y Jardín":
                                       //     CB.Name = "Casa y Jardín";
                                       //     CB.Code = "1245";
                                      //     break;
                                        case "Cocina":
                                            CB.Name = "Cocina";
                                            CB.Code = "1246";
                                            break;
                                        case "Herramientas":
                                            CB.Name = "Herramientas";
                                            CB.Code = "1248";
                                            break;
                                        case "Accesorios Niños":
                                            CB.Name = "Accesorios Niños";
                                            CB.Code = "1218";
                                            break;
                                        case "Juegos y Juguetes":
                                            CB.Name = "Juegos y Juguetes";
                                            CB.Code = "1220";
                                            break;
                                        case "Outdoors":
                                            CB.Name = "Outdoors";
                                            CB.Code = "1226";
                                            break;
                                        case "Accesorios de Viajes":
                                            CB.Name = "Accesorios de Viajes";
                                            CB.Code = "1221";
                                            break;
                                        case "Accesorios Mascotas":
                                            CB.Name = "Accesorios Mascotas";
                                            CB.Code = "1237";
                                            break;
                                        default:
                                            CB.Name = "Varios";
                                            CB.Code = "1041";
                                            break;
                                    }

                                    if (CB.Code == "1041")
                                    {
                                        //ItemCompleteSubCategoryEntity rub = scdal.GetSubCategoryByProviderCategoryName(p.ID, catSearch);
                                        LogSystem.WriteLogDebugDirect(" ----- Intentando ubicar ID de Nombre de Categoria ["+ result.Tables[0].Rows[j][14].ToString()  + "] tabuladas en Base de datos para proveedor -  " + ProviderID, "ImportFile");
                                        try
                                        {
                                            var requ = m_api.CreateRequest("/Mp_ProductsAPI_CTC/subcategories/" + ProviderID + "/" + result.Tables[0].Rows[j][14].ToString(), Method.GET, TokenAPI);

                                            var respCN = m_api.Client.Execute(requ);

                                            if (respCN.StatusCode == HttpStatusCode.OK)
                                            {
                                                LogSystem.WriteLogDebugDirect(" ----- Respuesta consulta Categorias tabuladas en Base de datos para proveedor - Response : " + respCN.Content, "ImportFile");
                                                
                                                JArray pr = JArray.Parse(respCN.Content);

                                                if ((string)pr[0].SelectToken("ID") != null )
                                                {
                                                    CB.Name = (string)pr[0].SelectToken("Name");
                                                    CB.Code = (string)pr[0].SelectToken("ID");
                                                    LogSystem.WriteLogDebugDirect(" ----- Asignación de Categoria sin errores --> SC = " + CB.Code, "ImportFile");
                                                }

                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  Respuesta consulta Categorias tabuladas en Base de datos para proveedor - Response  : " + respCN.Content, "ImportFile");
                                            }

                                        }
                                        catch (Exception frt)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Error obteniendo categoria desde TAblas de parametrizacion para Producto Nro " + j + ", SKU[" + sku + "] - Exception : " + frt.Message + frt.InnerException , "ImportFile");

                                        }
                                    }


                                    List<CategoryBranch> LCB = new List<CategoryBranch>();
                                    LCB.Add(CB);

                                    decimal Height = Math.Round(Convert.ToDecimal(result.Tables[0].Rows[j][4].ToString()), 2, MidpointRounding.AwayFromZero);
                                    decimal Width = Math.Round(Convert.ToDecimal(result.Tables[0].Rows[j][5].ToString()), 2, MidpointRounding.AwayFromZero);
                                    decimal Depth = Math.Round(Convert.ToDecimal(result.Tables[0].Rows[j][6].ToString()), 2, MidpointRounding.AwayFromZero);
                                    decimal Weight = Math.Round(Convert.ToDecimal(result.Tables[0].Rows[j][7].ToString()), 2, MidpointRounding.AwayFromZero) / 1000;
                                    string Name = result.Tables[0].Rows[j][1].ToString();
                                    string Description = result.Tables[0].Rows[j][3].ToString();
                                    string ShortDescription = result.Tables[0].Rows[j][1].ToString();
                                    decimal Price = Convert.ToDecimal(result.Tables[0].Rows[j][9].ToString());
                                    decimal ListPrice = Convert.ToDecimal(result.Tables[0].Rows[j][9].ToString());
                                    decimal NetPrice = Convert.ToDecimal(result.Tables[0].Rows[j][9].ToString());
                                    decimal Taxes = Convert.ToDecimal(result.Tables[0].Rows[j][10].ToString());
                                    if (Taxes > 0 && Taxes < 1) Taxes = Taxes * 100;
                                    //Height = (decimal)o.SelectToken("result.product.height"),
                                    string Brand = result.Tables[0].Rows[j][2].ToString();

                                    // ********* Escritura en Console para ver avance
                                    Console.WriteLine("Producto Nro " + j + ", SKU [" + sku + "] - " + Name);
                                    // **********************************************
                                    LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name, "ImportFile");

                                    Stock = Convert.ToInt32(result.Tables[0].Rows[j][15].ToString());
                                    DateTime fechaInicio;
                                    DateTime fechaFin;
                                    decimal oferta;
                                    try
                                    {
                                        oferta = Convert.ToDecimal(result.Tables[0].Rows[j][16].ToString());
                                        if (oferta > 0)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name + " - CON OFERTA", "ImportFile");
                                            Price = oferta;
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name + " - aparentemente SIN OFERTA", "ImportFile");
                                        }

                                    }
                                    catch (Exception dff)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name + " - SIN OFERTA", "ImportFile");

                                    }

                                    Object objJSON = new ProductApi()
                                    {
                                        Sku = sku,
                                        ProviderId = Convert.ToInt32(ProviderID),
                                        Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                                        Stock = Stock,
                                        Name = Name,
                                        Description = Description,
                                        ShortDescription = ShortDescription,
                                        Price = Price,
                                        ListPrice = ListPrice,
                                        NetPrice = NetPrice,
                                        Taxes = Taxes,
                                        //Height = (decimal)o.SelectToken("result.product.height"),
                                        Height = Height,
                                        Width = Width,
                                        Depth = Depth,
                                        Weight = Weight,
                                        Active = true,
                                        Ean = "",
                                        Brand = Brand,
                                        CategoryBranch = LCB


                                    };
                                    string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                                    var request2 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/", Method.PUT, TokenAPI);
                                    request2.AddParameter("application/json", json, ParameterType.RequestBody);

                                    var response2 = m_api.Client.Execute(request2);

                                    if (response2.StatusCode == HttpStatusCode.BadRequest)
                                    {
                                        JObject pr = JObject.Parse(response2.Content);
                                        LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - Response : " + response2.Content, "ImportFile");


                                        if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                                        {
                                            var request22 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                            request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                            var response22 = m_api.Client.Execute(request22);
                                            LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - Response New Product : " + response22.Content, "ImportFile");
                                        }

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - Response : " + response2.Content, "ImportFile");
                                    }

                                    string IncludeImages = "0";
                                    try
                                    {
                                        IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                                    }
                                    catch (Exception ffg)
                                    {
                                        IncludeImages = "NO";
                                    }


                                    if (IncludeImages == "1")
                                    {
                                        string[] urlsImages = result.Tables[0].Rows[j][8].ToString().Split('&');
                                        int cantImages = urlsImages.Length;

                                        for (int i = 0; i < cantImages; i++)
                                        {
                                            string url = (string)urlsImages[i];
                                            LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name + ", subiendo imagen : " + url, "ImportFile");
                                            string encodedFileAsBase64 = "";
                                            try
                                            {
                                                using (var client = new WebClient())
                                                {
                                                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                                                    byte[] dataBytes = client.DownloadData(new Uri(url));
                                                    encodedFileAsBase64 = Convert.ToBase64String(dataBytes);


                                                    //Formato
                                                    MemoryStream ms = new MemoryStream(dataBytes);

                                                    try
                                                    {
                                                        Image image = Image.FromStream(ms);
                                                    }
                                                    catch (Exception eimg)
                                                    {
                                                        Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                                        var decoder = new SimpleDecoder();
                                                        var webpBytes = ms.ToArray();
                                                        var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                                        System.IO.MemoryStream ms1 = new MemoryStream();
                                                        reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                        encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                    }
                                                }

                                                Object objJSONImages = new ProductImage()
                                                {
                                                    Base64 = encodedFileAsBase64
                                                };
                                                string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);

                                                var request3_i = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.GET, TokenAPI);

                                                var response3_1 = m_api.Client.Execute(request3_i);

                                                bool ActualizarImages = true;
                                                if (response3_1.StatusCode == HttpStatusCode.OK)
                                                {
                                                    string image = response3_1.Content;
                                                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                                                    JObject o_i = JObject.Parse(image);
                                                    string Base64_PM = "";
                                                    try
                                                    {
                                                        Base64_PM = (string)o_i.SelectToken("Result.Base64");
                                                    }
                                                    catch (Exception err) { }
                                                    bool result_comp = encodedFileAsBase64.Equals(Base64_PM);
                                                    if (result_comp)
                                                    {
                                                        ActualizarImages = false;
                                                        //Req += "\n........................................   ****  Image " + i + " Response = NO SE ACTUALIZA PORQUE SON IGUALES ";
                                                        LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name + ", NO SE SUBE PORQUE ES IGUAL A LA DE STOCKCENTRAL ", "ImportFile");

                                                    }
                                                }

                                                if (ActualizarImages)
                                                {

                                                    var request3 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                                    request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                    var response3 = m_api.Client.Execute(request3);

                                                    if (response3.StatusCode == HttpStatusCode.BadRequest)
                                                    {
                                                        JObject im = JObject.Parse(response3.Content);

                                                        if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                                        {
                                                            var request4 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                                            request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                            var response4 = m_api.Client.Execute(request4);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception fl)
                                            {
                                                Console.WriteLine("********* Error : " + fl.Message + ". " + fl.ToString());
                                                LogSystem.WriteLogDebugDirect(" -----  Producto Nro " + j + ", SKU[" + sku + "] - " + Name + ", subiendo imagen ERROR xxxxxx : " + fl.Message + ". " + fl.ToString(), "ImportFile");

                                            }
                                        }

                                    }

                                    if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        responseFile += "0!Aprobada|" + sku + '\n';
                                    }
                                }
                                catch(Exception der)
                                {
                                    LogSystem.WriteLogDebugDirect(" ERROR XXXXXXXXXXXXXXXXXXXXXXX " + der.Message + der.StackTrace + der.Source + der.InnerException, "ImportFile");

                                }
                                break;
                        }
                    }
                }
                catch (Exception rr)
                {
                    LogSystem.WriteLogDebugDirect(" ERROR XXXXXXXXXXXXXXXXXXXXXXX " + rr.Message + rr.StackTrace + rr.Source + rr.InnerException, "ImportFile");
                    return "-1| EERROR GRAVE " + rr.Message + rr.StackTrace + rr.Source + rr.InnerException;
                }


                return "0| Done|";

            }
            return "0| Done ";
        }
        public string ImportGCfromFileLine(string Path, string ProviderID)
        {
            LogSystem.WriteLogDebugDirect(" -----  Iniciando Importación", "ImportFile");
            using (FileStream stream = File.Open(Path, FileMode.Open, FileAccess.Read))
            {
                var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                var result = reader.AsDataSet();
 
                try
                {
                    string lista = "";
                    for (int j = 2; j < result.Tables[0].Rows.Count; j++)
                    {
                        string line = result.Tables[0].Rows[j][7].ToString();

                        using (var client = new WebClient())
                        {
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                            byte[] dataBytes = client.DownloadData(new Uri(line));
                            string encodedFile = Encoding.UTF8.GetString(dataBytes);

                            int indice = encodedFile.IndexOf("<!-- Monto VARIABLE --><span style=\"font-family: 'VAGRundschrift bold', 'Helvetica Neue', Helvetica, Arial, 'sans-serif'; font-size: 30px;font-weight: bold\" class=\"outlook_font\">$");
                            string monto = encodedFile.Substring(indice + 180, encodedFile.Substring(indice + 180).IndexOf("</span><!-- End Monto VARIABLE -->"));

                            int indice1 = encodedFile.IndexOf("<!-- Número de tarjeta VARIABLE -->\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t<span class=\"table-responsive outlook_font\" style=\"font-family: 'VAGRundschrift bold', 'Helvetica Neue', Helvetica, Arial, 'sans-serif'; font-size: 24px;font-weight: bold; color: #58595B; text-decoration: none;\">");
                            string Card = encodedFile.Substring(indice1 + 262, encodedFile.Substring(indice1 + 262).IndexOf("</span>\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t<!-- End Número de tarjeta VARIABLE -->"));

                            int indice2 = encodedFile.IndexOf("<!-- Vencimiento VARIABLE -->\r\n\t\t\t                                 \t<span class=\"table-responsive outlook_font\" style=\"font-family: 'VAGRundschrift bold', 'Helvetica Neue', Helvetica, Arial, 'sans-serif'; font-size: 24px;font-weight: bold\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t");
                            string Venc = encodedFile.Substring(indice2 + 255, 10);

                            int indice3 = encodedFile.IndexOf("<!-- Código de seguridad VARIABLE -->\r\n\t\t\t                                 <span class=\"table-responsive outlook_font\" style=\"font-family: 'VAGRundschrift bold', 'Helvetica Neue', Helvetica, Arial, 'sans-serif'; font-size: 24px;font-weight: bold\">");
                            string Seg = encodedFile.Substring(indice3 + 247, 3);

                            //Formato
                            lista = monto + ";" + Card.Replace(" ","") + ";" + Venc + ";" + Seg ;
                            LogSystem.WriteDirect(lista, "ListaGC");

                        }




                    }
                   

                }
                catch (Exception rr)
                {
                    LogSystem.WriteLogDebugDirect(" ERROR XXXXXXXXXXXXXXXXXXXXXXX " + rr.Message + rr.StackTrace + rr.Source + rr.InnerException, "ImportFile");
                    return "-1| EERROR GRAVE " + rr.Message + rr.StackTrace + rr.Source + rr.InnerException;
                }


                return "0| Done|";

            }
            return "0| Done ";
        }
        public string GetBy_SkuFJ_CompleteList( string ProviderFjID, string ProviderCTC)
        {

            string Req = "";
            string Res = "";
            string Page = "1";
            string URL_API_FJ_BO = System.Configuration.ConfigurationManager.AppSettings["URL_API_FJ_BO"];
            LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "FullCatalogFJ_Provider");
            LogSystem.WriteLogDebugDirect(" -----  000000000000000000000000000   Provider FJ - "+ ProviderFjID + ", ProviderIDCTC - " + ProviderCTC + " 0000000000000000000000000000000  -----", "FullCatalogFJ_Provider");
            LogSystem.WriteLogDebugDirect(" -----  000000000000000000000000000   URL_API_FJ_BO - " + URL_API_FJ_BO + " 0000000000000000000000000000000  -----", "FullCatalogFJ_Provider");
            LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "FullCatalogFJ_Provider");

            string CantidadDeProductosaPedir = "200";
            try
            {
                CantidadDeProductosaPedir = System.Configuration.ConfigurationManager.AppSettings["Cantidad_De_Productos_a_Pedir_a_FJ"];
            }
            catch(Exception g)
            {
                CantidadDeProductosaPedir = "200";
            }

            Magento m_api1 = new Magento(URL_API_FJ_BO);
            var request122 = m_api1.CreateRequest("/api/publications?maxResults="+ CantidadDeProductosaPedir + "&superadmin=1&sort=publishedAt%3Adesc&marketplaceCode=patagonia&revisionStatus=APPROVED&page="+Page+"&seller=" + ProviderFjID, Method.GET);
            m_api1.Client.Timeout = 120000;
            request122.Timeout = 120000;
            LogSystem.WriteLogDebugDirect(" -----  Setting Timeout to 120 sg  -----", "FullCatalogFJ_Provider");
            LogSystem.WriteLogDebugDirect(" -----  /api/publications?maxResults="+ CantidadDeProductosaPedir + "&superadmin=1&sort=publishedAt%3Adesc&marketplaceCode=patagonia&revisionStatus=APPROVED&page="+Page+"&seller=" + ProviderFjID, "FullCatalogFJ_Provider");


            var response122 = m_api1.Client.Execute(request122);
 
            for (int p = 1; p <= Convert.ToInt32(Page); p++)
            {
                LogSystem.WriteLogDebugDirect(" -----------------------------------------------------------------------------------", "FullCatalogFJ_Provider");
                LogSystem.WriteLogDebugDirect(" ---------------------------------- Pagina " + p + "  ------------------------------", "FullCatalogFJ_Provider");
                LogSystem.WriteLogDebugDirect(" -----------------------------------------------------------------------------------", "FullCatalogFJ_Provider");
                try
                {

                    if (p > 1)
                    {
                        LogSystem.WriteLogDebugDirect(" ----- Iniciando busqueda de prorductos de Pagina " + p + "  -----", "FullCatalogFJ_Provider");
                        request122 = m_api1.CreateRequest("/api/publications?maxResults=" + CantidadDeProductosaPedir + "&superadmin=1&sort=publishedAt%3Adesc&marketplaceCode=patagonia&revisionStatus=APPROVED&page=" + p.ToString() + "&seller=" + ProviderFjID, Method.GET);
                        request122.Timeout = 120000;
                        response122 = m_api1.Client.Execute(request122);
                    }

                    if (response122.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string product = response122.Content;
                        LogSystem.WriteLogDebugDirect(" ----- Respuesta exitosa de productos de la Pagina " + p + "  -----", "FullCatalogFJ_Provider");

                        //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                        JObject o = JObject.Parse(product);
                        if ((Boolean)o.SelectToken("success"))
                        {
                            Page = (string)o.SelectToken("pages");
                            int countrows = o.SelectToken("result").Count();
                            LogSystem.WriteLogDebugDirect(" ----- Registros de la Pagina " + p + " = " + countrows, "FullCatalogFJ_Provider");

                            for (int row = 0; row < countrows; row++)

                            {
                                try
                                {
                                    string sku = (string)o.SelectToken("result[" + row + "].sku");
                                    if ((bool)o.SelectToken("result[" + row + "].toUpdate"))
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Error SE DESCARTA EL PRODUCTO sku=" + sku + ", por estar marcado en FJ para actualizar. Si se actualiza ahora genera un loop de precios.", "FullCatalogFJ_Provider");
                                        //return "-10| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error SE DESCARTA EL PRODUCTO DEL provider_id=" + ProviderID + "&sku=" + sku + ", por estar marcado en FJ para actualizar. Si se actualiza ahora genera un loop de precios.|Error";
                                        continue;
                                    }

                                    CategoryBranch CB = new CategoryBranch();
                                    CB.Name = (string)o.SelectToken("result[" + row + "].marketplaceCategory.name");
                                    CB.Code = (string)o.SelectToken("result[" + row + "].marketplaceCategory.code");

                                    if (CB.Code == null)
                                    {
                                        CB.Code = "1041";
                                        CB.Name = "Varios";
                                    }

                                    List<CategoryBranch> LCB = new List<CategoryBranch>();
                                    LCB.Add(CB);

                                    decimal Height = 0;
                                    decimal Width = 0;
                                    decimal Depth = 0;
                                    decimal Weight = 10;
                                    try
                                    {
                                        Height = Math.Round((decimal)o.SelectToken("result[" + row + "].product.height"), 2, MidpointRounding.AwayFromZero);
                                    }
                                    catch (Exception d)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Error Height", "FullCatalogFJ_Provider");
                                        try { Height = Math.Round((decimal)o.SelectToken("result[" + row + "].product.packages[0].height"), 2, MidpointRounding.AwayFromZero); }
                                        catch (Exception ds)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Error Height 2da vez", "FullCatalogFJ_Provider");
                                        }
                                    }
                                    try
                                    {
                                        Width = Math.Round((decimal)o.SelectToken("result[" + row + "].product.width"), 2, MidpointRounding.AwayFromZero);
                                    }
                                    catch (Exception d)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Error Width", "FullCatalogFJ_Provider");
                                        try { Width = Math.Round((decimal)o.SelectToken("result[" + row + "].product.packages[0].width"), 2, MidpointRounding.AwayFromZero); }
                                        catch (Exception ds)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Error Width 2da vez", "FullCatalogFJ_Provider");
                                        }
                                    }

                                    try
                                    {
                                        Depth = Math.Round((decimal)o.SelectToken("result[" + row + "].product.depth"), 2, MidpointRounding.AwayFromZero);
                                    }
                                    catch (Exception d)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Error Depth", "FullCatalogFJ_Provider");
                                        try { Depth = Math.Round((decimal)o.SelectToken("result[" + row + "].product.packages[0].depth"), 2, MidpointRounding.AwayFromZero); }
                                        catch (Exception dx)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Error Depth 2da vez", "FullCatalogFJ_Provider");
                                        }
                                    }
                                    try
                                    {
                                        Weight = Math.Round((decimal)o.SelectToken("result[" + row + "].product.weight"), 2, MidpointRounding.AwayFromZero);
                                        if (Weight == 0)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Weight en CERO. Intento levantar el PESO con nueva estructura para  provider_id=" + ProviderCTC + "&sku=" + sku, "FullCatalogFJ_Provider");
                                            try { Weight = Math.Round((decimal)o.SelectToken("result[" + row + "].product.packages[0].weight"), 2, MidpointRounding.AwayFromZero); }
                                            catch (Exception ds)
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  Error Weight 2da vez provider_id=" + ProviderCTC + "&sku=" + sku, "FullCatalogFJ_Provider");
                                            }

                                        }
                                    }
                                    catch (Exception d)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  Error Weight", "FullCatalogFJ_Provider");
                                        try { Weight = Math.Round((decimal)o.SelectToken("result[" + row + "].product.packages[0].weight"), 2, MidpointRounding.AwayFromZero); }
                                        catch (Exception dx)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  Error Weight 2da vez", "FullCatalogFJ_Provider");
                                        }
                                    }


                                    int ProviderID = Convert.ToInt32(ProviderCTC);
                                    LogSystem.WriteLogDebugDirect(" -----  Page = " + p + "/" + Page + ", Prod Nro= " + row + ", sku = " + sku, "FullCatalogFJ_Provider");

                                    int stockFisico = (int)o.SelectToken("result[" + row + "].product.stock");
                                    int stocktopublish = (int)o.SelectToken("result[" + row + "].stockToPublish");

                                    Object objJSON = new ProductApi()
                                    {
                                        Sku = sku,
                                        ProviderId = Convert.ToInt32(ProviderCTC),
                                        Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                                        Stock = (int)o.SelectToken("result[" + row + "].stockToPublish"),
                                        Name = (string)o.SelectToken("result[" + row + "].product.name"),
                                        Description = (string)o.SelectToken("result[" + row + "].product.description"),
                                        ShortDescription = (string)o.SelectToken("result[" + row + "].product.shortDescription"),
                                        Price = (decimal)o.SelectToken("result[" + row + "].priceToPublish.price"),
                                        ListPrice = (decimal)o.SelectToken("result[" + row + "].priceToPublish.listPrice"),
                                        NetPrice = (decimal)o.SelectToken("result[" + row + "].priceToPublish.price"),
                                        Taxes = (decimal)o.SelectToken("result[" + row + "].product.taxes"),
                                        //Height = (decimal)o.SelectToken("result["+row+"].product.height"),
                                        Height = Height,
                                        Width = Width,
                                        Depth = Depth,
                                        Weight = Weight,
                                        Active = (bool)o.SelectToken("result[" + row + "].product.active"),
                                        Ean = (string)o.SelectToken("result[" + row + "].product.ean"),
                                        Brand = (string)o.SelectToken("result[" + row + "].product.brand.brand"),
                                        CategoryBranch = LCB


                                    };

                                    if (stockFisico != stocktopublish)
                                    {
                                        LogSystem.WriteLogDebugDirect("", "FullCatalogFJ_Provider");
                                        LogSystem.WriteLogDebugDirect(" XXXXXXXXXXXXXXXXXXXXXXXXXXX STOCK FISICO = " + stockFisico + " vs STOCK To PUBLISH " + stocktopublish + " XXXXXXXXXXXXXXXXXXXXXXXX", "FullCatalogFJ_Provider");
                                        LogSystem.WriteLogDebugDirect(";00" + ProviderCTC + sku + ";" + stockFisico + ";" + stocktopublish, "Dif_STOCK_FJ");
                                        LogSystem.WriteLogDebugDirect("", "FullCatalogFJ_Provider");
                                    }


                                    string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                                    string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                                    Req += json;
                                    string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                                    Magento m_api = new Magento(URL_API_CTC);
                                    var request2 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                                    request2.AddParameter("application/json", json, ParameterType.RequestBody);
                                    LogSystem.WriteLogDebugDirect(" -----   request = " + json, "FullCatalogFJ_Provider");

                                    var response2 = m_api.Client.Execute(request2);

                                    if (response2.StatusCode == HttpStatusCode.BadRequest)
                                    {
                                        JObject pr = JObject.Parse(response2.Content);


                                        if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                                        {
                                            var request22 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                            request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                            var response22 = m_api.Client.Execute(request22);
                                            LogSystem.WriteLogDebugDirect(" -----   response = " + response22.Content, "FullCatalogFJ_Provider");
                                            //Req += "\n........................................RESPONSE = " + response22.Content;
                                        }
                                        else
                                            LogSystem.WriteLogDebugDirect(" -----   response = " + response2.Content, "FullCatalogFJ_Provider");
                                        //Req += "\n........................................RESPONSE = ERROR | " + response2.Content;

                                    }
                                    else
                                        LogSystem.WriteLogDebugDirect(" -----   response = " + response2.Content, "FullCatalogFJ_Provider");
                                    //Req += "\n........................................RESPONSE = " + response2.Content;



                                    string IncludeImages = "0";
                                    try
                                    {
                                        IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                                    }
                                    catch (Exception ffg)
                                    {
                                        IncludeImages = "NO";
                                    }


                                    if (IncludeImages == "1")
                                    {
                                        int cantImages = o.SelectToken("result[" + row + "].product.images").Count();

                                        for (int i = 0; i < cantImages; i++)
                                        {
                                            string url = (string)o.SelectToken("result[" + row + "].product.images[" + i + "].remote_url");
                                            string encodedFileAsBase64 = "";
                                            using (var client = new WebClient())
                                            {
                                                byte[] dataBytes = client.DownloadData(new Uri(url));
                                                encodedFileAsBase64 = Convert.ToBase64String(dataBytes);


                                                //Formato
                                                MemoryStream ms = new MemoryStream(dataBytes);

                                                try
                                                {
                                                    Image image = Image.FromStream(ms);
                                                }
                                                catch (Exception eimg)
                                                {
                                                    Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                                    var decoder = new SimpleDecoder();
                                                    var webpBytes = ms.ToArray();
                                                    try
                                                    {
                                                        var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                                        System.IO.MemoryStream ms1 = new MemoryStream();
                                                        reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                        encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                    }
                                                    catch (Exception img2)
                                                    {
                                                        LogSystem.WriteLogDebugDirect(" -----   ERROR AL INTERPRETAR IMAGEN " + img2.Message + "\n" + img2.InnerException, "FullCatalogFJ_Provider");
                                                        break;
                                                    }
                                                }
                                            }

                                            Object objJSONImages = new ProductImage()
                                            {
                                                Base64 = encodedFileAsBase64
                                            };
                                            string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);

                                            var request3_i = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.GET, TokenAPI);

                                            var response3_1 = m_api.Client.Execute(request3_i);

                                            bool ActualizarImages = true;
                                            if (response3_1.StatusCode == HttpStatusCode.OK)
                                            {
                                                string image = response3_1.Content;
                                                //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                                                JObject o_i = JObject.Parse(image);
                                                string Base64_PM = "";
                                                try
                                                {
                                                    Base64_PM = (string)o_i.SelectToken("Result.Base64");
                                                }
                                                catch (Exception err) { }
                                                bool result_comp = encodedFileAsBase64.Equals(Base64_PM);
                                                if (result_comp)
                                                {
                                                    ActualizarImages = false;
                                                    LogSystem.WriteLogDebugDirect(" -----  ........................................****Image " + i + " Response = NO SE ACTUALIZA PORQUE SON IGUALES ", "FullCatalogFJ_Provider");

                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebugDirect(" -----  ........................................   ****  Image " + i + " SE ACTUALIZA .... ", "FullCatalogFJ_Provider");
                                                }
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  ........................................   ****  Image " + i + " Fallo chequeo de imagen en CTC ... ", "FullCatalogFJ_Provider");

                                            }
                                            if (ActualizarImages)
                                            {
                                                var request3 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                                request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                var response3 = m_api.Client.Execute(request3);

                                                if (response3.StatusCode == HttpStatusCode.BadRequest)
                                                {
                                                    JObject im = JObject.Parse(response3.Content);

                                                    if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                                    {
                                                        var request4 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                                        request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                        var response4 = m_api.Client.Execute(request4);
                                                        LogSystem.WriteLogDebugDirect(" -----  ........................................   ****  Image " + i + " Response = " + response4.Content, "FullCatalogFJ_Provider");

                                                    }
                                                    else
                                                        LogSystem.WriteLogDebugDirect(" -----  ........................................   ****  Image " + i + " Response = " + response3.Content, "FullCatalogFJ_Provider");


                                                }
                                                else
                                                    LogSystem.WriteLogDebugDirect(" -----  ........................................   ****  Image " + i + " Response = " + response3.Content, "FullCatalogFJ_Provider");


                                            }
                                        }
                                    }

                                    if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        //        return "0!Request : " + Req;
                                    }

                                }
                                catch (Exception error_datos)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  ERROR EN DATOS PRODUCTO ----- row: " + row, "FullCatalogFJ_Provider");

                                }
                                //   return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error Actualizando producto en API CTC|" + json;
                            }
                        }
                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  ERROR EN FJ?? " + response122.StatusDescription + " - " + response122.ErrorMessage, "FullCatalogFJ_Provider");
                        return Res;
                    }
                }
                catch(Exception ErrProd)
                {
                    LogSystem.WriteLogDebugDirect(" -----  ERROR EN FJ?? " + ErrProd.Message + " - " + ErrProd.InnerException + " - " + ErrProd.StackTrace , "FullCatalogFJ_Provider");

                }
            }
            return Res;
        }
        public string GetBy_SkuFJ_CompleteListAPIOficial(string ProviderFjID, string ProviderCTC)
        {

            string Req = "";
            string Res = "";
            string Page = "1";
            string URL_API_FJ_BO = System.Configuration.ConfigurationManager.AppSettings["URL_API_FJ_BO"];
            LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "FullCatalogFJ_Provider");
            LogSystem.WriteLogDebugDirect(" -----  000000000000000000000000000   Provider FJ - " + ProviderFjID + ", ProviderIDCTC - " + ProviderCTC + " 0000000000000000000000000000000  -----", "FullCatalogFJ_Provider");
            LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "FullCatalogFJ_Provider");

            string CantidadDeProductosaPedir = "200";
            try
            {
                CantidadDeProductosaPedir = System.Configuration.ConfigurationManager.AppSettings["Cantidad_De_Productos_a_Pedir_a_FJ"];
            }
            catch (Exception g)
            {
                CantidadDeProductosaPedir = "200";
            }


            Magento m_api1 = new Magento(URL_API_FJ_BO);
            var request122 = m_api1.CreateRequest("/api/publications?maxResults=" + CantidadDeProductosaPedir + "&superadmin=1&sort=publishedAt%3Adesc&marketplaceCode=patagonia&revisionStatus=APPROVED&page=" + Page + "&seller=" + ProviderFjID, Method.GET);
            m_api1.Client.Timeout = 120000;
            request122.Timeout = 120000;
            LogSystem.WriteLogDebugDirect(" -----  Setting Timeout to 120 sg  -----", "FullCatalogFJ_Provider");
            var response122 = m_api1.Client.Execute(request122);

            for (int p = 1; p <= Convert.ToInt32(Page); p++)
            {
                LogSystem.WriteLogDebugDirect(" ----- Pagina " + p + "  -----", "FullCatalogFJ_Provider");
                if (p > 1)
                {
                    request122 = m_api1.CreateRequest("/api/publications?maxResults=" + CantidadDeProductosaPedir + "&superadmin=1&sort=publishedAt%3Adesc&marketplaceCode=patagonia&revisionStatus=APPROVED&page=" + p.ToString() + "&seller=" + ProviderFjID, Method.GET);
                    request122.Timeout = 120000;
                    response122 = m_api1.Client.Execute(request122);
                }

                if (response122.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string product = response122.Content;
                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    JObject o = JObject.Parse(product);
                    if ((Boolean)o.SelectToken("success"))
                    {
                        Page = (string)o.SelectToken("pages");
                        int countrows = o.SelectToken("result").Count();

                        for (int row = 0; row < countrows; row++)

                        {
                            try
                            {
                                string sku = (string)o.SelectToken("result[" + row + "].sku");
  
                                if ((bool)o.SelectToken("result[" + row + "].toUpdate"))
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  Error SE DESCARTA EL PRODUCTO sku=" + sku + ", por estar marcado en FJ para actualizar. Si se actualiza ahora genera un loop de precios.", "FullCatalogFJ_Provider");
                                    //return "-10| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error SE DESCARTA EL PRODUCTO DEL provider_id=" + ProviderID + "&sku=" + sku + ", por estar marcado en FJ para actualizar. Si se actualiza ahora genera un loop de precios.|Error";
                                    continue;
                                }

                                string resp = GetBy_SkuFJ_List(sku, ProviderCTC);
                                LogSystem.WriteLogDebugDirect("Respuesta --> " + resp, "StockCentralfromFJProductsCheck");
                                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromFJProductsCheck");

                            }
                            catch (Exception error_datos)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  ERROR EN DATOS PRODUCTO ----- row: " + row, "FullCatalogFJ_Provider");

                            }
                            //   return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error Actualizando producto en API CTC|" + json;
                        }
                    }
                }
                else
                {
                    LogSystem.WriteLogDebugDirect(" -----  ERROR EN FJ?? " + response122.StatusDescription + " - " + response122.ErrorMessage, "FullCatalogFJ_Provider");
                    return Res;
                }

            }
            return Res;
        }
        public string GetBy_SkuFJ_List(string sku, string ProviderID)
        {
            string Req = "";
            try
            {

                var request = CreateRequest("/query/publication?provider_id=" + ProviderID + "&sku=" + sku, Method.GET);

                var response = Client.Execute(request);
                List<string> pList = null;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string product = response.Content;
                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    JObject o = JObject.Parse(product);
                    LogSystem.WriteLogDebugDirect(" ---------------------------------------------------------------------------------------------------------------------", "FullCatalogFJ_Provider");
                    LogSystem.WriteLogDebugDirect(" -----  OBTENIENDO provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                    LogSystem.WriteLogDebugDirect(" -----  Producto = " + response.Content, "FullCatalogFJ_Provider");

                    //JsonTextReader reader = new JsonTextReader(new StringReader(product));

                    //while (reader.Read())
                    //{
                    //    if (reader.Value != null)
                    //    {
                    //        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("Token: {0}", reader.TokenType);
                    //    }
                    //}
                    if ((Boolean)o.SelectToken("success"))
                    {
                        if ( (bool)o.SelectToken("result.toUpdate"))
                        {
                            LogSystem.WriteLogDebugDirect(" -----  Error SE DESCARTA EL PRODUCTO DEL provider_id=" + ProviderID + "&sku=" + sku + ", por estar marcado en FJ para actualizar. Si se actualiza ahora genera un loop de precios.", "FullCatalogFJ_Provider");
                            return "-10| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error SE DESCARTA EL PRODUCTO DEL provider_id=" + ProviderID + "&sku=" + sku + ", por estar marcado en FJ para actualizar. Si se actualiza ahora genera un loop de precios.|Error" ;
                        }

                        CategoryBranch CB = new CategoryBranch();
                        CB.Name = (string)o.SelectToken("result.marketplaceCategory.name");
                        CB.Code = (string)o.SelectToken("result.marketplaceCategory.code");

                        if (CB.Code == null)
                        {
                            CB.Code = "1041";
                            CB.Name = "Varios";
                        }


                        List<CategoryBranch> LCB = new List<CategoryBranch>();
                        LCB.Add(CB);

                        decimal Height = 0;
                        decimal Width = 0;
                        decimal Depth = 0;
                        decimal Weight = 10;

                        try
                        {
                            Height = Math.Round((decimal)o.SelectToken("result.product.height"), 2, MidpointRounding.AwayFromZero);
                        }
                        catch (Exception d)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  Error Height provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            try { Height = Math.Round((decimal)o.SelectToken("result.product.packages[0].height"), 2, MidpointRounding.AwayFromZero); }
                            catch (Exception ds)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  Error Height 2da vez provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            }
                        }
                        try
                            {
                                Width = Math.Round((decimal)o.SelectToken("result.product.width"), 2, MidpointRounding.AwayFromZero);
                        }
                        catch (Exception d)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  Error Width provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            try { Width = Math.Round((decimal)o.SelectToken("result.product.packages[0].width"), 2, MidpointRounding.AwayFromZero); }
                            catch (Exception ds)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  Error Width 2da vez provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            }
                        }
                        try
                        {
                            Depth = Math.Round((decimal)o.SelectToken("result.product.depth"), 2, MidpointRounding.AwayFromZero);
                        } catch (Exception d)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  Error Depth provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            try { Depth = Math.Round((decimal)o.SelectToken("result.product.packages[0].depth"), 2, MidpointRounding.AwayFromZero); }
                            catch (Exception ds)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  Error Depth 2da vez provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            }
                        }
                        try
                        {
                            Weight = Math.Round((decimal)o.SelectToken("result.product.weight"), 2, MidpointRounding.AwayFromZero);
                            if(Weight == 0)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  Weight en CERO. Intento levantar el PESO con nueva estructura para  provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                                try { Weight = Math.Round((decimal)o.SelectToken("result.product.packages[0].weight"), 2, MidpointRounding.AwayFromZero); }
                                catch (Exception ds)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  Error Weight 2da vez provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                                }

                            }
                        }
                        catch (Exception d)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  Error Weight provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            try { Weight = Math.Round((decimal)o.SelectToken("result.product.packages[0].weight"), 2, MidpointRounding.AwayFromZero); }
                            catch (Exception ds)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  Error Weight 2da vez provider_id=" + ProviderID + "&sku=" + sku, "FullCatalogFJ_Provider");
                            }
                        }


                        Object objJSON = new ProductApi()
                        {
                            Sku = sku,
                            ProviderId = Convert.ToInt32(ProviderID),
                            Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                            Stock = (int)o.SelectToken("result.stockToPublish"),
                            Name = (string)o.SelectToken("result.product.name"),
                            Description = (string)o.SelectToken("result.product.description"),
                            ShortDescription = (string)o.SelectToken("result.product.shortDescription"),
                            Price = (decimal)o.SelectToken("result.priceToPublish.price"),
                            ListPrice = (decimal)o.SelectToken("result.priceToPublish.listPrice"),
                            NetPrice = (decimal)o.SelectToken("result.priceToPublish.price"),
                            Taxes = (decimal)o.SelectToken("result.product.taxes"),
                            //Height = (decimal)o.SelectToken("result.product.height"),
                            Height = Height,
                            Width = Width,
                            Depth = Depth,
                            Weight = Weight,
                            Active = (bool)o.SelectToken("result.product.active"),
                            Ean = (string)o.SelectToken("result.product.ean"),
                            Brand = (string)o.SelectToken("result.product.brand.brand"),
                            CategoryBranch = LCB


                        };
                        string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                        string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                        Req += json;
                        string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                        Magento m_api = new Magento(URL_API_CTC);
                        var request2 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                        request2.AddParameter("application/json", json, ParameterType.RequestBody);

                        var response2 = m_api.Client.Execute(request2);

                        if (response2.StatusCode == HttpStatusCode.BadRequest)
                        {
                            JObject pr = JObject.Parse(response2.Content);


                            if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                            {
                                var request22 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                var response22 = m_api.Client.Execute(request22);
                                Req += "\n........................................RESPONSE = " + response22.Content;
                            }
                            else
                                Req += "\n........................................RESPONSE = ERROR | " + response2.Content;

                        }
                        else
                            Req += "\n........................................RESPONSE = " + response2.Content;



                        string IncludeImages = "0";
                        try
                        {
                            IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                        } catch (Exception ffg)
                        {
                            IncludeImages = "NO";
                        }


                        if (IncludeImages == "1")
                        {
                            int cantImages = o.SelectToken("result.product.images").Count();

                            for (int i = 0; i < cantImages; i++)
                            {
                                string url = (string)o.SelectToken("result.product.images[" + i + "].remote_url");
                                string encodedFileAsBase64 = "";
                                using (var client = new WebClient())
                                {
                                    byte[] dataBytes = client.DownloadData(new Uri(url));
                                    encodedFileAsBase64 = Convert.ToBase64String(dataBytes);


                                    //Formato
                                    MemoryStream ms = new MemoryStream(dataBytes);

                                    try
                                    {
                                        Image image = Image.FromStream(ms);
                                    } catch (Exception eimg)
                                    {
                                        Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                        var decoder = new SimpleDecoder();
                                        var webpBytes = ms.ToArray();
                                        var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                        System.IO.MemoryStream ms1 = new MemoryStream();
                                        reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                    }
                                }

                                Object objJSONImages = new ProductImage()
                                {
                                    Base64 = encodedFileAsBase64
                                };
                                string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);

                                var request3_i = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.GET, TokenAPI);

                                var response3_1 = m_api.Client.Execute(request3_i);

                                bool ActualizarImages = true;
                                if (response3_1.StatusCode == HttpStatusCode.OK)
                                {
                                    string image = response3_1.Content;
                                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                                    JObject o_i = JObject.Parse(image);
                                    string Base64_PM = "";
                                    try
                                    {
                                        Base64_PM = (string)o_i.SelectToken("Result.Base64");
                                    }
                                    catch (Exception err) { }
                                    bool result_comp = encodedFileAsBase64.Equals(Base64_PM);
                                    if (result_comp)
                                    {
                                        ActualizarImages = false;
                                        Req += "\n........................................   ****  Image " + i + " Response = NO SE ACTUALIZA PORQUE SON IGUALES ";

                                    }
                                    else
                                    {
                                        Req += "\n........................................   ****  Image " + i + " SE ACTUALIZA ....";

                                    }
                                }
                                else
                                {
                                    Req += "\n........................................   ****  Image " + i + " Fallo chequeo de imagen en CTC ... " + response3_1.Content;

                                }
                                if (ActualizarImages) {
                                    var request3 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                    request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                    var response3 = m_api.Client.Execute(request3);

                                    if (response3.StatusCode == HttpStatusCode.BadRequest)
                                    {
                                        JObject im = JObject.Parse(response3.Content);

                                        if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                        {
                                            var request4 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                            request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                            var response4 = m_api.Client.Execute(request4);
                                            Req += "\n........................................   ****  Image " + i + " Response = " + response4.Content;

                                        }
                                        else
                                            Req += "\n........................................   ****  Image " + i + " Response = " + response3.Content;

                                    }
                                    else
                                        Req += "\n........................................   ****  Image " + i + " Response = " + response3.Content;

                                }
                            }
                        }

                        if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return "0!Request : " + Req;
                        }

                        return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error Actualizando producto en API CTC|" + json;
                    }
                    return "-3| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion. Producto no encontrado.";
                }
                return "-2| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion";
            } catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }

        public string PutCategoriasForProductTeca(string SellerAccessToken, int idCatalogoILS , string CatName)
        {
            string res = "-1|Metodo no desarrollado";
            try
            {

                var request = CreateRequest("/api/categories?access_token=" + SellerAccessToken, Method.POST);
                //int IntegrationApp = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ProductecaIntegrationAppID"]);
                //request.AddHeader("Authorization", "Bearer " + SellerAccessToken);
                string json = "";
                string innerError = "";
                try
                {
                    string Path = AppDomain.CurrentDomain.BaseDirectory;
                    string[] lines = System.IO.File.ReadAllLines(Path + "\\CategoriasProducteca.txt");

                    // Display the file contents by using a foreach loop.

                    foreach (string row in lines)
                    {
                        // Use a tab to indent each line of the file.
                        json += row;

                    }
                }
                catch(Exception FileJ)
                {
                    innerError = FileJ.Message + FileJ.InnerException;
                    json = "[]";
                }


                request.AddParameter("application/json", json, ParameterType.RequestBody);

                var response = Client.Execute(request);
                 
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    res = response.Content + "|" + innerError;
                    
                    return res;
                }
                return "-1|ERROR :" + response.Content + "|InnerError:" + innerError;
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
           
        }

        public string Get_ProductTeca_SkuList(string SellerAccessToken, int ProviderID)
        {
            try
            {

                var request = CreateRequest("/api/products/feed/current?access_token=" + SellerAccessToken, Method.GET);
                int IntegrationApp = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ProductecaIntegrationAppID"]);

                var response = Client.Execute(request);
                List<string> pList = null;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string product = response.Content;
                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return  product;
                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }


        public string Get_ProductTeca_SkuByProductecaID(string SellerAccessToken, int ProviderID, string ProductId)
        {
            try
            {

                var request = CreateRequest("/api/products/" + ProductId, Method.GET);
                int IntegrationApp = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ProductecaIntegrationAppID"]);
                request.AddHeader("Authorization", "Bearer " + SellerAccessToken);

                var response = Client.Execute(request);
                List<string> pList = null;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string product = response.Content;
                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return product;
                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string GetBy_SkuProductTeca_List(string SellerAccessToken, int ProviderID)
        {
            try
            {
                 
                var request = CreateRequest("/api/products/feed/current?access_token=" + SellerAccessToken, Method.GET);
                int IntegrationApp = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ProductecaIntegrationAppID"]);

                var response = Client.Execute(request); 
                List<string> pList = null;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string product = response.Content;
                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    JArray o = JArray.Parse(product);

                    //JsonTextReader reader = new JsonTextReader(new StringReader(product));

                    //while (reader.Read())
                    //{
                    //    if (reader.Value != null)
                    //    {
                    //        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("Token: {0}", reader.TokenType);
                    //    }
                    //}

                    if (o != null)
                    {
                        
                        if (o.Count > 0)
                        {
                            for (int i = 0; i < o.Count; i++)
                            {
                                Boolean es_producto_de_este_mkp = false;
                                Object objJSON;
                                CategoryBranch CB = new CategoryBranch();
                                List<ProductAttribute> LPA = new List<ProductAttribute>();
                                decimal Height = 0;
                                decimal Width = 0;
                                decimal Depth = 0;
                                decimal Weight = 0;
                                string sku = "";
                                
                                int Stock = 0;
                                int ID = 0;
                                string ean = "";
                                string Name = "";
                                string Description = "";
                                string ShortDescription = "";
                                decimal Price = 0;
                                decimal ListPrice = 0;
                                decimal NetPrice = 0;
                                decimal Taxes = 21;
                                //Height = (decimal)o.SelectToken("result.product.height"),
                                string Brand = "";
                                bool Active = false;

                                Active = false;
                                string ProductId_internal = (string)o[i].SelectToken("id");
                                List<JToken> integrations = o[i].SelectTokens("integrations").ToList();
                                foreach (JToken itemI in integrations)
                                {
                                    for (int a = 0; a < itemI.Count(); a++)
                                    {
                                        if ((int)itemI[a]["app"] == IntegrationApp)
                                        {
                                            Active = ((string)itemI[a]["status"] == "Active" ? true : false);
                                            es_producto_de_este_mkp = true;
                                            break;
                                        }
                                    }
                                }

                                if(!es_producto_de_este_mkp)
                                {
                                    continue;
                                }    

                                List<JToken> atributos = o[i].SelectTokens("attributes").ToList();
                                foreach (JToken itemI in atributos)
                                {
                                    for (int a = 0; a < itemI.Count(); a++)
                                    {
                                        if ((string)itemI[a]["key"] == "iva")
                                        {
                                            string tmpT = itemI[a]["value"].ToString().Replace(",", ".").Replace("%", ""); 
                                            Taxes = Convert.ToDecimal(tmpT);
                                            //Taxes = Convert.ToDecimal(itemI[a]["value"]);
                                            if (Taxes == 105) Taxes = Taxes / 10;
                                            break;
                                        }
                                        else
                                        {
                                            if ((string)itemI[a]["value"] != "")
                                            {
                                                ProductAttribute PA = new ProductAttribute();
                                                PA.Name = itemI[a]["key"].ToString();
                                                PA.Value = itemI[a]["value"].ToString();
                                                LPA.Add(PA);
                                            }
                                        }
                                    }
                                }

                                if (Taxes > 0 && Taxes < 1) Taxes = Taxes * 100;
 

                                bool hasVariation = (bool)o[i].SelectToken("hasVariations");
                                if(!hasVariation)
                                {
                                    CB.Name = "category";
                                    CB.Code = "-1";
                                    try { 
                                        CB.Code = o[i].SelectToken("listingSettings.categoryId") != null ? (string)o[i].SelectToken("listingSettings.categoryId") : "-1";
                                    
                                    }
                                    catch(Exception NoID)
                                    {
                                        CB.Code = "-1";
                                    }
                                    try { 
                                    if(CB.Code == "-1")
                                        CB.Code = o[i].SelectToken("category") != null ? (string)o[i].SelectToken("category") : "1041";

                                    }
                                    catch(Exception NOCat)
                                    {
                                        CB.Code = "1041";
                                    }

                                    List<CategoryBranch> LCB = new List<CategoryBranch>();
                                    LCB.Add(CB);

                                    sku = (string)o[i].SelectToken("variations[0].sku");
                                    Stock = (int)o[i].SelectToken("variations[0].stock");
                                    ID = (int)o[i].SelectToken("id");
                                    ean = (string)o[i].SelectToken("variations[0].barcode");
                                    Name = (string)o[i].SelectToken("name");
                                    Description = (string)o[i].SelectToken("notes");

                                    try { Height = (o[i].SelectToken("dimensions.height") != null ? (decimal)o[i].SelectToken("dimensions.height") : 0); } catch (Exception f) { Height = 0; }
                                    try { Width = (o[i].SelectToken("dimensions.width") != null ? (decimal)o[i].SelectToken("dimensions.width") : 0); } catch (Exception f) { Width = 0; }
                                    try { Depth = (o[i].SelectToken("dimensions.length") != null ? (decimal)o[i].SelectToken("dimensions.length") : 0);  } catch (Exception f) { Depth = 0; }
                                    try { Weight = (o[i].SelectToken("dimensions.weight") != null ? (decimal)o[i].SelectToken("dimensions.weight") : 0);  } catch (Exception f) { Weight = 0; }
                                    Brand = (string)o[i].SelectToken("brand");
                                    Price = (decimal)o[i].SelectToken("activeDeal.dealPrice"); ;
                                    NetPrice = (decimal)o[i].SelectToken("activeDeal.dealPrice");
                                    ListPrice = (decimal)o[i].SelectToken("activeDeal.regularPrice");
                                    //Taxes = 21;
                                    
                                    if(Price == 0)
                                    {
                                        continue;
                                    }
                                    // ********* Escritura en Console para ver avance
                                    //Console.WriteLine("Producto Nro " + i + ", SKU [" + sku + "] - " + Name);
                                    // **********************************************

                                    objJSON = new ProductApi()
                                    {
                                        Sku = sku,
                                        ProviderId = ProviderID,
                                        Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                                        ProviderCode = Convert.ToInt32(ProductId_internal),
                                        Stock = Stock,
                                        Name = Name,
                                        Description = Description,
                                        ShortDescription = ShortDescription,
                                        Price = Price,
                                        ListPrice = ListPrice,
                                        NetPrice = NetPrice,
                                        Taxes = Taxes,
                                        //Height = (decimal)o.SelectToken("result.product.height"),
                                        Height = Height,
                                        Width = Width,
                                        Depth = Depth,
                                        Weight = Weight/1000,
                                        Active = Active,
                                        Ean = ID.ToString(),
                                        Brand = Brand,
                                        CategoryBranch = LCB,
                                        ProductAttributes = LPA


                                    };


                                    string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                                    string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];

                                    string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                                    Magento m_api = new Magento(URL_API_CTC);
                                    var request2 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                                    request2.AddParameter("application/json", json, ParameterType.RequestBody);

                                    var response2 = m_api.Client.Execute(request2);

                                    if (response2.StatusCode == HttpStatusCode.BadRequest)
                                    {
                                        JObject pr = JObject.Parse(response2.Content);


                                        if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                                        {
                                            var request22 = m_api.CreateRequest("/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                            request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                            var response22 = m_api.Client.Execute(request22);
                                            if (response22.StatusCode == HttpStatusCode.InternalServerError)
                                                continue;
                                        }

                                    }
                                    if (response2.StatusCode == HttpStatusCode.InternalServerError)
                                        continue;

                                    string IncludeImages = "0";
                                    try
                                    {
                                        IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                                    }
                                    catch (Exception ffg)
                                    {
                                        IncludeImages = "NO";
                                    }


                                    if (IncludeImages == "1")
                                    {
                                        List<JToken> img = o[i].SelectToken("variations[0].pictures").ToList();
                                        int cantImages = img.Count();

                                        foreach (JToken item in img)
                                        {
                                            string url = (string)item["url"];
                                            string encodedFileAsBase64 = "";
                                            using (var client = new WebClient())
                                            {
                                                byte[] dataBytes = client.DownloadData(new Uri(url));
                                                encodedFileAsBase64 = Convert.ToBase64String(dataBytes);


                                                //Formato
                                                MemoryStream ms = new MemoryStream(dataBytes);

                                                try
                                                {
                                                    Image image = Image.FromStream(ms);
                                                    if (image.Width > 1920 || image.Height > 1200)
                                                    {
                                                        System.IO.MemoryStream ms1 = new MemoryStream();
                                                        ms1 = ConvertImageMkp(new Bitmap(ms), 1920, 1200, 100);
                                                        encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                    }
                                                }
                                                catch (Exception eimg)
                                                {
                                                    Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                                    var decoder = new SimpleDecoder();
                                                    var webpBytes = ms.ToArray();
                                                    var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                                    System.IO.MemoryStream ms1 = new MemoryStream();
                                                    reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                    if (reloaded.Width > 1920 || reloaded.Height > 1200)
                                                    {
                                                        ms1 = ConvertImageMkp(new Bitmap(ms1), 1920, 1200, 100);

                                                    }
                                                    encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                }
                                            }

                                            Object objJSONImages = new ProductImage()
                                            {
                                                Base64 = encodedFileAsBase64
                                            };
                                            string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);

                                            var request3 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                            request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                            var response3 = m_api.Client.Execute(request3);

                                            if (response3.StatusCode == HttpStatusCode.BadRequest)
                                            {
                                                JObject im = JObject.Parse(response3.Content);

                                                if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                                {
                                                    var request4 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                                    request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                    var response4 = m_api.Client.Execute(request4);
                                                }
                                            }

                                        }

                                    }


                                }
                                else
                                {
                                    CB.Name = "category";
                                    CB.Code = "-1";
                                    try
                                    {
                                        CB.Code = o[i].SelectToken("listingSettings.categoryId") != null ? (string)o[i].SelectToken("listingSettings.categoryId") : "-1";

                                    }
                                    catch (Exception NoID)
                                    {
                                        CB.Code = "1041";
                                    }
                                    try
                                    {
                                        if (CB.Code == "-1")
                                            CB.Code = o[i].SelectToken("category") != null ? (string)o[i].SelectToken("category") : "1041";

                                    }
                                    catch (Exception NOCat)
                                    {
                                        CB.Code = "1041";
                                    }

                                    List<CategoryBranch> LCB = new List<CategoryBranch>();
                                    LCB.Add(CB);
                                    List<JToken> var = o[i].SelectTokens("variations[0]").ToList();
                                    Name = (string)o[i].SelectToken("name");
                                    Description = (string)o[i].SelectToken("notes");
                                    try { Height = (o[i].SelectToken("dimensions.height") != null ? (decimal)o[i].SelectToken("dimensions.height") : 0); } catch (Exception f) { Height = 0; }
                                    try { Width = (o[i].SelectToken("dimensions.width") != null ? (decimal)o[i].SelectToken("dimensions.width") : 0); } catch (Exception f) { Width = 0; }
                                    try { Depth = (o[i].SelectToken("dimensions.length") != null ? (decimal)o[i].SelectToken("dimensions.length") : 0); } catch (Exception f) { Depth = 0; }
                                    try { Weight = (o[i].SelectToken("dimensions.weight") != null ? (decimal)o[i].SelectToken("dimensions.weight") : 0); } catch (Exception f) { Weight = 0; }
                                    Brand = (string)o[i].SelectToken("brand");
                                    Price = (decimal)o[i].SelectToken("activeDeal.dealPrice"); 
                                    NetPrice = (decimal)o[i].SelectToken("activeDeal.dealPrice");
                                    ListPrice = (decimal)o[i].SelectToken("activeDeal.regularPrice");
                                    
                                    if (Price == 0)
                                    {
                                        continue;
                                    }

                                    foreach (JToken item in var)
                                    {
                                        sku = (string)item["sku"];
                                        Stock = (int)item["stock"];
                                        ID = (int)item["id"];
                                        ean = (string)item["barcode"];
                                        

                                        // ********* Escritura en Console para ver avance
                                        //Console.WriteLine("Producto Nro " + i + ", SKU [" + sku + "] - " + Name);
                                        // **********************************************


                                        objJSON = new ProductApi()
                                        {
                                            Sku = sku,
                                            ProviderId = ProviderID,
                                            Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                                            ProviderCode = Convert.ToInt32(ProductId_internal),
                                            Stock = Stock,
                                            Name = Name,
                                            Description = Description,
                                            ShortDescription = ShortDescription,
                                            Price = Price,
                                            ListPrice = ListPrice,
                                            NetPrice = NetPrice,
                                            Taxes = Taxes,
                                            //Height = (decimal)o.SelectToken("result.product.height"),
                                            Height = Height,
                                            Width = Width,
                                            Depth = Depth,
                                            Weight = Weight/1000,
                                            Ean = ID.ToString(),
                                            Brand = Brand,
                                            Active = Active,
                                            CategoryBranch = LCB,
                                            ProductAttributes = LPA


                                        };


                                        string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                                        string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];

                                        string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                                        Magento m_api = new Magento(URL_API_CTC);
                                        var request2 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                                        request2.AddParameter("application/json", json, ParameterType.RequestBody);

                                        var response2 = m_api.Client.Execute(request2);

                                        if (response2.StatusCode == HttpStatusCode.BadRequest)
                                        {
                                            JObject pr = JObject.Parse(response2.Content);


                                            if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                                            {
                                                var request22 = m_api.CreateRequest("/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                                request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                                var response22 = m_api.Client.Execute(request22);
                                                if (response22.StatusCode == HttpStatusCode.InternalServerError)
                                                    continue;

                                            }

                                        }
                                        if (response2.StatusCode == HttpStatusCode.InternalServerError )
                                            continue;


                                        string IncludeImages = "0";
                                        try
                                        {
                                            IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                                        }
                                        catch (Exception ffg)
                                        {
                                            IncludeImages = "NO";
                                        }


                                        if (IncludeImages == "1")
                                        {
                                            List<JToken> img = o[i].SelectToken("pictures").ToList();
                                            int cantImages = img.Count();

                                            foreach (JToken itemImg in img)
                                            {
                                                string url = (string)itemImg["url"];
                                                string encodedFileAsBase64 = "";
                                                using (var client = new WebClient())
                                                {
                                                    byte[] dataBytes = client.DownloadData(new Uri(url));
                                                    encodedFileAsBase64 = Convert.ToBase64String(dataBytes);


                                                    //Formato
                                                    MemoryStream ms = new MemoryStream(dataBytes);

                                                    try
                                                    {
                                                        Image image = Image.FromStream(ms);
                                                        if (image.Width > 1920 || image.Height > 1200)
                                                        {
                                                            System.IO.MemoryStream ms1 = new MemoryStream();
                                                            ms1 = ConvertImageMkp(new Bitmap(ms), 1920, 1200, 100);
                                                            encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                        }
                                                    }
                                                    catch (Exception eimg)
                                                    {
                                                        Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                                        var decoder = new SimpleDecoder();
                                                        var webpBytes = ms.ToArray();
                                                        var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                                        System.IO.MemoryStream ms1 = new MemoryStream();
                                                        reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                        if (reloaded.Width > 1920 || reloaded.Height > 1200)
                                                        {
                                                            ms1 = ConvertImageMkp(new Bitmap(ms1), 1920, 1200, 100);

                                                        }
                                                        encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                    }
                                                }

                                                Object objJSONImages = new ProductImage()
                                                {
                                                    Base64 = encodedFileAsBase64
                                                };
                                                string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);

                                                var request3 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                                request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                var response3 = m_api.Client.Execute(request3);

                                                if (response3.StatusCode == HttpStatusCode.BadRequest)
                                                {
                                                    JObject im = JObject.Parse(response3.Content);

                                                    if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                                    {
                                                        var request4 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                                        request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                        var response4 = m_api.Client.Execute(request4);
                                                    }
                                                }

                                            }

                                        }
                                    }




                                }


                                
                                
                            }
                            return "0!Aprobada|";
                        }
                    }
                    return "-3| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion. Producto no encontrado.";
                }
                return "-2| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }

        public string GetBy_SkuProductTeca_Product(JObject o, int ProviderID, string Categoria)
        {
            try
            {
                string Req = "";
                Boolean es_producto_de_este_mkp = false;
                int IntegrationApp = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ProductecaIntegrationAppID"]);
                string CatDefault = System.Configuration.ConfigurationSettings.AppSettings["SubcategoriaDefault"].ToString();

                //JsonTextReader reader = new JsonTextReader(new StringReader(product));

                //while (reader.Read())
                //{
                //    if (reader.Value != null)
                //    {
                //        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                //    }
                //    else
                //    {
                //        Console.WriteLine("Token: {0}", reader.TokenType);
                //    }
                //}

                if (o != null)
                {
                    Object objJSON;
                    CategoryBranch CB = new CategoryBranch();
                    List<ProductAttribute> LPA = new List<ProductAttribute>();
                    decimal Height = 0;
                    decimal Width = 0;
                    decimal Depth = 0;
                    decimal Weight = 0;
                    string sku = "";
                    string ProductId_internal =  (string)o.SelectToken("id"); 


                    int Stock = 0;
                    int ID = 0;
                    string ean = "";
                    string Name = "";
                    string Description = "";
                    string ShortDescription = "";
                    decimal Price = 0;
                    decimal ListPrice = 0;
                    decimal NetPrice = 0;
                    decimal Taxes = 21;
                    //Height = (decimal)o.SelectToken("result.product.height"),
                    string Brand = "";
                    bool Active = false;

                    Active = false;

                    List<JToken> integrations = o.SelectTokens("integrations").ToList();
                    foreach (JToken itemI in integrations)
                    {
                        for (int a = 0; a < itemI.Count(); a++)
                        {
                            if ((int)itemI[a]["app"] == IntegrationApp)
                            {
                                Active = ((string)itemI[a]["status"] == "Active" ? true : false);
                                es_producto_de_este_mkp = true;
                                break;
                            }
                        }
                    }
                    if(!es_producto_de_este_mkp)
                    {
                        Req += "\n........................................RESPONSE = NO ES PRODUCTO DE ESTE MKP -> " + IntegrationApp;
                        return "1|" + Req; ;
                    }

                    List<JToken> atributos = o.SelectTokens("attributes").ToList();
                    foreach (JToken itemI in atributos)
                    {
                        for (int a = 0; a < itemI.Count(); a++)
                        {
                            if ((string)itemI[a]["key"] == "iva")
                            {
                                Req += "\nIVA RECIBIDO = " + itemI[a]["value"].ToString();
                                string tmpT = itemI[a]["value"].ToString().Replace(",", ".").Replace("%", "");
                                Req += "\nIVA Convertido = " + tmpT;

                                Taxes = Convert.ToDecimal(tmpT);
                                if (Taxes == 105) Taxes = Taxes / 10;
                                Req += "TAX a ENVIAR a API CTC = " + Taxes.ToString();
                                break;
                            }
                            else
                            {
                                if ((string)itemI[a]["key"].ToString().Trim() != "ean" && (string)itemI[a]["key"].ToString().Trim() != "EAN")
                                {
                                    if ((string)itemI[a]["value"] != "")
                                    {
                                        ProductAttribute PA = new ProductAttribute();
                                        PA.Name = itemI[a]["key"].ToString();
                                        PA.Value = itemI[a]["value"].ToString();
                                        LPA.Add(PA);
                                    }
                                }                           }
                        }
                    }

                    if (Taxes > 0 && Taxes < 1) Taxes = Taxes * 100;



                    bool hasVariation = (bool)o.SelectToken("hasVariations");
                    if (!hasVariation)
                    {
                        Req += "\n ----- PRODUCTO SIMPLE -----";
                        
                        CB.Name = "category";

                        //************************************
                                              
                        CB.Code = "-1";
                        try
                        {
                            Req += "\n........................................ Intentando obtener CATEGORIA >>> "; ;
                            CB.Code = o.SelectToken("listingSettings.categoryId") != null ? (string)o.SelectToken("listingSettings.categoryId") : "-1";
                            Req += "\n........................................ CATEGORIA >>> " + CB.Code ;

                        }
                        catch (Exception NoID)
                        {
                            CB.Code = "-1";
                            Req += "\n........................................ CATEGORIA Excepción >>> " + CB.Code;
                        }
                        try
                        {
                            if (CB.Code == "-1")
                            {
                                Req += "\n........................................ Intento recuperar CATEGORIA SC >>> " + Categoria;
                                if (Categoria == "-1")
                                    CB.Code = CatDefault;
                                else
                                    CB.Code = Categoria;
                                Req += "\n........................................ CATEGORIA FINAL >>> " + CB.Code;

                            }
                        }
                        catch (Exception NOCat)
                        {
                            CB.Code = CatDefault;
                            Req += "\n........................................ CATEGORIA FINAL POR 2DA EXCEPCION >>> " + CB.Code;
                        }




                        // Ver como resolver. Para pruebas lo ponemos en 149
                        // CB.Code = "149";
                        //************************************


                        List<CategoryBranch> LCB = new List<CategoryBranch>();
                        LCB.Add(CB);

                        sku = (string)o.SelectToken("variations[0].sku");
                        Stock = (int)o.SelectToken("variations[0].stock");
                        ID = (int)o.SelectToken("variations[0].id");
                        ean = (string)o.SelectToken("variations[0].barcode");
                        Name = (string)o.SelectToken("name");
                        Description = (string)o.SelectToken("notes");
                        
                        try
                        {   if (o.SelectToken("activeDeal.taxes[0].taxes[0].name").ToString() == "IVA")
                                Taxes = Convert.ToDecimal(o.SelectToken("activeDeal.taxes[0].taxes[0].value"));
                            else if (o.SelectToken("activeDeal.taxes[0].taxes[1].name").ToString() == "IVA")
                                Taxes = Convert.ToDecimal(o.SelectToken("activeDeal.taxes[0].taxes[1].value"));

                            Req += "\nTAXES a ENVIAR a API CTC REALMENTE POR ESTRUCTURA TAXES PRESENTE = " + Taxes.ToString();

                        }
                        catch (Exception f)
                        {
                            Req += "\n........................................ La estructura taxes contiene errores";

                        }

                        try { Height = (o.SelectToken("dimensions.height") != null ? (decimal)o.SelectToken("dimensions.height") : 0); } catch (Exception f) { Height = 0; }
                        try { Width = (o.SelectToken("dimensions.width") != null ? (decimal)o.SelectToken("dimensions.width") : 0); } catch (Exception f) { Width = 0; }
                        try { Depth = (o.SelectToken("dimensions.length") != null ? (decimal)o.SelectToken("dimensions.length") : 0); } catch (Exception f) { Depth = 0; }
                        try { Weight = (o.SelectToken("dimensions.weight") != null ? (decimal)o.SelectToken("dimensions.weight") : 0); } catch (Exception f) { Weight = 0; }
                        Brand = (string)o.SelectToken("brand");
                        Price = (decimal)o.SelectToken("activeDeal.dealPrice"); ;
                        NetPrice = (decimal)o.SelectToken("activeDeal.dealPrice");
                        ListPrice = (decimal)o.SelectToken("activeDeal.regularPrice");
                        //Taxes = 21;

                        if(Price == 0)
                        {
                            Req += "\n XXXXXXX ERROR PRECIO = 0\n\n";
                            return "-1|" + Req;
                        }

                        // ********* Escritura en Console para ver avance
                        Console.WriteLine("Producto SKU [" + sku + "] - " + Name);
                        // **********************************************

                        objJSON = new ProductApi()
                        {
                            Sku = sku,
                            ProviderId = ProviderID,
                            Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                            ProviderCode = Convert.ToInt32(ProductId_internal),
                            Stock = Stock,
                            Name = Name,
                            Description = Description,
                            ShortDescription = ShortDescription,
                            Price = Price,
                            ListPrice = ListPrice,
                            NetPrice = NetPrice,
                            Taxes = Taxes,
                            //Height = (decimal)o.SelectToken("result.product.height"),
                            Height = Height,
                            Width = Width,
                            Depth = Depth,
                            Weight = Weight/1000,
                            Active = Active,
                            Ean = ID.ToString(),
                            Brand = Brand,
                            CategoryBranch = LCB,
                            ProductAttributes = LPA

                        };


                        string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                        string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                        Req += "\n" + json;
                        string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                        Magento m_api = new Magento(URL_API_CTC);
                        var request2 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                        request2.AddParameter("application/json", json, ParameterType.RequestBody);

                        var response2 = m_api.Client.Execute(request2);

                        if (response2.StatusCode == HttpStatusCode.BadRequest)
                        {
                            JObject pr = JObject.Parse(response2.Content);


                            if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                            {
                                var request22 = m_api.CreateRequest("/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                var response22 = m_api.Client.Execute(request22);
                                Req += "\n........................................RESPONSE = " + response22.Content;
                            }
                            else 
                                Req += "\n........................................RESPONSE = ERROR | " + response2.Content;

                        }
                        else
                        {
                            if(response2.StatusCode != HttpStatusCode.OK)
                            {
                                Req += "\n........................................RESPONSE = " + response2.ErrorMessage ;
                                return Req;
                            }
                            Req += "\n........................................RESPONSE = " + response2.Content;

                        }
                           


                        string IncludeImages = "0";
                        try
                        {
                            IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                        }
                        catch (Exception ffg)
                        {
                            IncludeImages = "NO";
                        }


                        if (IncludeImages == "1")
                        {
                            List<JToken> img = o.SelectToken("variations[0].pictures").ToList();
                            int cantImages = img.Count();
                            int i = 0;
                            foreach (JToken item in img)
                            {
                                i++;
                                try
                                {
                                    string url = (string)item["url"];
                                    Req += "\n........................................   ****  Image " + i + " -- url = " + url  ;

                                    string encodedFileAsBase64 = "";
                                    using (var client = new WebClient())
                                    {
                                        byte[] dataBytes;
                                        try
                                        {
                                            dataBytes = client.DownloadData(new Uri(url));
                                            encodedFileAsBase64 = Convert.ToBase64String(dataBytes);
                                        }catch
                                        {
                                            Req += "\n........................................   **** Error Image " + i + " -- Header usados = " + client.Headers.ToString();

                                            Req += "\n........................................   **** Error Image " + i + " -- url = " + url;
                                            url = url.Replace("https", "http");
                                            Req += "\n........................................   **** Intento Image " + i + " -- url = " + url;
                                            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                                            dataBytes = client.DownloadData(new Uri(url));
                                            encodedFileAsBase64 = Convert.ToBase64String(dataBytes);

                                        }

                                        //Formato
                                        MemoryStream ms = new MemoryStream(dataBytes);

                                        try
                                        {
                                            Image image = Image.FromStream(ms);
                                            Req += "\n........................................   ****  Image " + i + " Ancho = " + image.Width + ", Alto = " + image.Height; 
                                            
                                            if(image.Width > 1920 || image.Height > 1200)
                                            {
                                                System.IO.MemoryStream ms1 = new MemoryStream();
                                                ms1 = ConvertImageMkp(new Bitmap(ms),1920,1200,100);
                                                encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                            }
                                        }
                                        catch (Exception eimg)
                                        {
                                            Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                            var decoder = new SimpleDecoder();
                                            var webpBytes = ms.ToArray();
                                            var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                            System.IO.MemoryStream ms1 = new MemoryStream();
                                            reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);

                                            Req += "\n........................................   ****  Image Reloaded " + i + " Ancho = " + reloaded.Width  + ", Alto = " + reloaded.Height;

                                            if (reloaded.Width > 1920 || reloaded.Height > 1200)
                                            {
                                                ms1 = ConvertImageMkp(new Bitmap(ms1), 1920, 1200, 100);
                                                
                                            }
                                            encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                        }
                                    }

                                    Object objJSONImages = new ProductImage()
                                    {
                                        Base64 = encodedFileAsBase64
                                    };
                                    string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);
                                    Req += "\n........................................   ****  Image " + i + " TAMAÑO = " + jsonImages.Length + " Bytes";
                                    var request3_i = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.GET, TokenAPI);

                                    var response3_1 = m_api.Client.Execute(request3_i);

                                    bool ActualizarImages = true;
                                    if (response3_1.StatusCode == HttpStatusCode.OK)
                                    {
                                        string image = response3_1.Content;
                                        JObject o_i = JObject.Parse(image);
                                        string Base64_PM = "";
                                        try
                                        {
                                            Base64_PM = (string)o_i.SelectToken("Result.Base64");
                                        }
                                        catch (Exception err) { }
                                        bool result_comp = encodedFileAsBase64.Equals(Base64_PM);
                                        if (result_comp)
                                        {
                                            ActualizarImages = false;
                                            Req += "\n........................................   ****  Image " + i + " Response = NO SE ACTUALIZA PORQUE SON IGUALES ";

                                        }
                                    }

                                    if (ActualizarImages)
                                    {

                                        var request3 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                        request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                        var response3 = m_api.Client.Execute(request3);

                                        if (response3.StatusCode == HttpStatusCode.BadRequest || response3.StatusCode == HttpStatusCode.InternalServerError )
                                        {
                                            JObject im = JObject.Parse(response3.Content);

                                            if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                            {
                                                var request4 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                                request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                var response4 = m_api.Client.Execute(request4);
                                                Req += "\n........................................   ****  Image " + i + " Response = " + response4.Content;

                                            }
                                            else
                                                Req += "\n........................................   ****  Image " + i + " Response = " + response3.Content;

                                        }
                                        else
                                        {
                                            if (response3.StatusCode != HttpStatusCode.OK)
                                            {
                                                Req += "\n........................................RESPONSE = " + response3.ErrorMessage;

                                            }
                                            else
                                                Req += "\n........................................   ****  Image " + i + " Response = " + response3.Content;

                                        }
                                    }
                                }
                                catch(Exception Imgk)
                                {
                                    Req += "\n........................................   ****  Image " + i + " Response = " + Imgk.Message + " -- " + (Imgk.InnerException != null ? Imgk.InnerException.ToString() : "") + " -- " + (Imgk.StackTrace != null? Imgk.StackTrace:"") ;
                                }
                            }

                        }


                    }
                    else
                    {
                        Req += "\n ----- PRODUCTO COMPUESTO -----";
                        CB.Name = "category";

                        //************************************

                        CB.Code = "-1";
                        try
                        {
                            CB.Code = o.SelectToken("listingSettings.categoryId") != null ? (string)o.SelectToken("listingSettings.categoryId") : "-1";

                        }
                        catch (Exception NoID)
                        {
                            CB.Code = "-1";
                        }
                        try
                        {
                            if (CB.Code == "-1")
                            {
                                if (Categoria == "-1")
                                    CB.Code = CatDefault;
                                else
                                    CB.Code = Categoria;
                            }
                        }
                        catch (Exception NOCat)
                        {
                            CB.Code = CatDefault;
                        }




                        // Ver como resolver. Para pruebas lo ponemos en 149
                        // CB.Code = "149";
                        //************************************


                        List<CategoryBranch> LCB = new List<CategoryBranch>();
                        LCB.Add(CB);
                        List<JToken> var = o.SelectTokens("variations").ToList();
                        Name = (string)o.SelectToken("name");
                        Description = (string)o.SelectToken("notes");
                        try { Height = (o.SelectToken("dimensions.height") != null ? (decimal)o.SelectToken("dimensions.height") : 0); } catch (Exception f) { Height = 0; }
                        try { Width = (o.SelectToken("dimensions.width") != null ? (decimal)o.SelectToken("dimensions.width") : 0); } catch (Exception f) { Width = 0; }
                        try { Depth = (o.SelectToken("dimensions.length") != null ? (decimal)o.SelectToken("dimensions.length") : 0); } catch (Exception f) { Depth = 0; }
                        try { Weight = (o.SelectToken("dimensions.weight") != null ? (decimal)o.SelectToken("dimensions.weight") : 0); } catch (Exception f) { Weight = 0; }
                        Brand = (string)o.SelectToken("brand");
                        Price = (decimal)o.SelectToken("activeDeal.dealPrice");
                        NetPrice = (decimal)o.SelectToken("activeDeal.dealPrice");
                        ListPrice = (decimal)o.SelectToken("activeDeal.regularPrice");
                        
                        if (Price == 0)
                        {
                            Req += "\n XXXXXXX ERROR PRECIO = 0\n\n";
                            return "-1|" + Req;
                        }

                        foreach (JToken item in var)
                        {
                            if (item.Count() > 0)
                            {
                                for (int j = 0; j < item.Count(); j++)
                                {

                                    sku = (string)item[j]["sku"] + '-' + (item[j]["attributesHash"] != null? (string)item[j]["attributesHash"] : "j");
                                    Stock = (int)item[j]["stock"];
                                    ID = (int)item[j]["id"];
                                    ean = (string)item[j]["barcode"];


                                    // ********* Escritura en Console para ver avance
                                    Console.WriteLine("Producto SKU [" + sku + "] - " + Name);
                                    // **********************************************


                                    objJSON = new ProductApi()
                                    {
                                        Sku = sku,
                                        ProviderId = ProviderID,
                                        Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                                        ProviderCode = Convert.ToInt32(ProductId_internal),
                                        Stock = Stock,
                                        Name = Name,
                                        Description = Description,
                                        ShortDescription = ShortDescription,
                                        Price = Price,
                                        ListPrice = ListPrice,
                                        NetPrice = NetPrice,
                                        Taxes = Taxes,
                                        //Height = (decimal)o.SelectToken("result.product.height"),
                                        Height = Height,
                                        Width = Width,
                                        Depth = Depth,
                                        Weight = Weight/1000,
                                        Ean = ID.ToString(),
                                        Brand = Brand,
                                        Active = Active,
                                        CategoryBranch = LCB,
                                        ProductAttributes = LPA


                                    };


                                    string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                                    Req += "\n" + json;
                                    string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];

                                    string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                                    Magento m_api = new Magento(URL_API_CTC);
                                    var request2 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                                    request2.AddParameter("application/json", json, ParameterType.RequestBody);

                                    var response2 = m_api.Client.Execute(request2);

                                    if (response2.StatusCode == HttpStatusCode.BadRequest)
                                    {
                                        JObject pr = JObject.Parse(response2.Content);


                                        if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                                        {
                                            var request22 = m_api.CreateRequest("/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                            request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                            var response22 = m_api.Client.Execute(request22);
                                            Req += "\n........................................RESPONSE = " + response22.Content;
                                        }
                                        else
                                            Req += "\n........................................RESPONSE = ERROR | " + response2.Content;

                                    }
                                    else
                                        Req += "\n........................................RESPONSE = " + response2.Content;


                                    string IncludeImages = "0";
                                    try
                                    {
                                        IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                                    }
                                    catch (Exception ffg)
                                    {
                                        IncludeImages = "NO";
                                    }


                                    if (IncludeImages == "1")
                                    {
                                        List<JToken> img = item[j]["pictures"].ToList();
                                        int cantImages = img.Count();
                                        int i = 0;
                                        foreach (JToken itemImg in img)
                                        {
                                            i++;
                                            try
                                            {
                                                string url = (string)itemImg["url"];
                                                Req += "\n........................................   ****  Image " + i + " -- url = " + url;
                                                string encodedFileAsBase64 = "";
                                                using (var client = new WebClient())
                                                {
                                                   
                                                    byte[] dataBytes;
                                                    try
                                                    {
                                                        dataBytes = client.DownloadData(new Uri(url));
                                                        encodedFileAsBase64 = Convert.ToBase64String(dataBytes);
                                                    }
                                                    catch
                                                    {
                                                        Req += "\n........................................   **** Error Image " + i + " -- Header usados = " + client.Headers.ToString();

                                                        Req += "\n........................................   **** Error Image " + i + " -- url = " + url;
                                                        url = url.Replace("https", "http");
                                                        Req += "\n........................................   **** Intento Image " + i + " -- url = " + url;
                                                        client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                                                        dataBytes = client.DownloadData(new Uri(url));
                                                        encodedFileAsBase64 = Convert.ToBase64String(dataBytes);

                                                    }



                                                    //Formato
                                                    MemoryStream ms = new MemoryStream(dataBytes);

                                                    try
                                                    {
                                                        Image image = Image.FromStream(ms);
                                                        Req += "\n........................................   ****  Image " + i + " Ancho = " + image.Width + ", Alto = " + image.Height;
                                                        if (image.Width > 1920 || image.Height > 1200)
                                                        {
                                                            System.IO.MemoryStream ms1 = new MemoryStream();
                                                            ms1 = ConvertImageMkp(new Bitmap(ms), 1920, 1200, 100);
                                                            encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                        }
                                                    }
                                                    catch (Exception eimg)
                                                    {
                                                        Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                                        var decoder = new SimpleDecoder();
                                                        var webpBytes = ms.ToArray();
                                                        var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                                        System.IO.MemoryStream ms1 = new MemoryStream();
                                                        reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                        Req += "\n........................................   ****  Image Reloaded " + i + " Ancho = " + reloaded.Width + ", Alto = " + reloaded.Height;

                                                        if (reloaded.Width > 1920 || reloaded.Height > 1200)
                                                        {
                                                            ms1 = ConvertImageMkp(new Bitmap(ms1), 1920, 1200, 100);

                                                        }
  
                                                        encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                                    }
                                                }

                                                Object objJSONImages = new ProductImage()
                                                {
                                                    Base64 = encodedFileAsBase64
                                                };
                                                string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);
                                                Req += "\n........................................   ****  Image " + i + " TAMAÑO = " + jsonImages.Length + " Bytes";

                                                var request3_i = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.GET, TokenAPI);

                                                var response3_1 = m_api.Client.Execute(request3_i);

                                                bool ActualizarImages = true;
                                                if (response3_1.StatusCode == HttpStatusCode.OK)
                                                {
                                                    string image = response3_1.Content;
                                                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                                                    JObject o_i = JObject.Parse(image);
                                                    string Base64_PM = "";
                                                    try
                                                    {
                                                        Base64_PM = (string)o_i.SelectToken("Result.Base64");
                                                    }
                                                    catch (Exception err) { }
                                                    bool result_comp = encodedFileAsBase64.Equals(Base64_PM);
                                                    if (result_comp)
                                                    {
                                                        ActualizarImages = false;
                                                        Req += "\n........................................   ****  Image " + i + " Response = NO SE ACTUALIZA PORQUE SON IGUALES ";

                                                    }
                                                }
                                                if (ActualizarImages)
                                                {

                                                    var request3 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                                    request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                    var response3 = m_api.Client.Execute(request3);

                                                    if (response3.StatusCode == HttpStatusCode.BadRequest || response3.StatusCode == HttpStatusCode.InternalServerError)
                                                    {
                                                        JObject im = JObject.Parse(response3.Content);

                                                        if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                                        {
                                                            var request4 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                                            request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                                            var response4 = m_api.Client.Execute(request4);
                                                            Req += "\n........................................   ****  Image " + i + " Response = " + response4.Content;

                                                        }
                                                        else
                                                            Req += "\n........................................   ****  Image " + i + " Response = " + response3.Content;

                                                    }
                                                    else
                                                        Req += "\n........................................   ****  Image " + i + " Response = " + response3.Content;
                                                }
                                            }
                                            catch(Exception Imgk)
                                            {
                                                Req += "\n........................................   ****  Image " + i + " Response = " + Imgk.Message + " -- " + (Imgk.InnerException != null ? Imgk.InnerException.ToString() : "") + " -- " + (Imgk.StackTrace != null ? Imgk.StackTrace : "");

                                            }
                                        }
                                    }
                                }
                            }
                        }




                    }

                    return "0|" + Req;




                }
                return "-3| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion. Producto no encontrado.";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string GetProductBy_Sku_VTEX(string SellerAccessToken, string IntegratorApiKey, string ProductId, string MkpSellerID)
        {
            try
            {

                var request = CreateRequest("/api/catalog_system/pvt/products/productgetbyrefid/" + ProductId, Method.GET);
                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                LogSystem.WriteLogDebugDirect(" -----  GetProductBy_Sku_VTEX  -----> /api/catalog_system/pvt/products/productgetbyrefid/" + ProductId, "VTEX_ProductAPI");

                var response = Client.Execute(request);
                LogSystem.WriteLogDebugDirect(" -----  GetProductBy_Sku_VTEX  <-----  resp = " + response.Content, "VTEX_ProductAPI");

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JObject o = JObject.Parse(response.Content);

                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return "OK|"+ o.SelectToken("Id").ToString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Enviar sugerencia de producto producto
                    return "NOT FOUND|";

                }
                else
                {
                    // ERROR

                }
                return "ERROR|" + response.Content;
            }
            catch (Exception f)
            {
                return "ERROR| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }

        public string GetProductDetailBy_Sku_VTEX(string SellerAccessToken, string IntegratorApiKey, string ProductId, string MkpSellerID)
        {
            try
            {
                LogSystem.WriteLogDebugDirect(" ---------------------------------------------------------------------------------------------------", "VTEX_ProductAPI");

                var request = CreateRequest("/api/catalog_system/pvt/products/productgetbyrefid/" + ProductId, Method.GET);
                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                LogSystem.WriteLogDebugDirect(" -----  GetProductBy_Sku_VTEX  -----> /api/catalog_system/pvt/products/productgetbyrefid/" + ProductId, "VTEX_ProductAPI");

                var response = Client.Execute(request);
                LogSystem.WriteLogDebugDirect(" -----  GetProductBy_Sku_VTEX  <-----  resp = " + response.Content, "VTEX_ProductAPI");

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JObject o = JObject.Parse(response.Content);

                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return "OK|" + o.SelectToken("Id").ToString() + "|" + response.Content;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Enviar sugerencia de producto producto
                    return "NOT FOUND|";

                }
                else
                {
                    // ERROR

                }
                return "ERROR|" + response.Content;
            }
            catch (Exception f)
            {
                return "ERROR| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string GetProductDetailBy_Sku_VTEXbySeller(string SellerAccessToken, string IntegratorApiKey, string ProductId, string MkpSellerID)
        {
            try
            {
                LogSystem.WriteLogDebugDirect(" ---------------------------------------------------------------------------------------------------", "VTEX_ProductAPI");

                var request = CreateRequest("/api/catalog-seller-portal/products/external-id=" + ProductId, Method.GET);
                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                LogSystem.WriteLogDebugDirect(" -----  GetProductBy_Sku_VTEX  -----> /api/catalog-seller-portal/products/external-id=" + ProductId, "VTEX_ProductAPI");

                var response = Client.Execute(request);
                LogSystem.WriteLogDebugDirect(" -----  GetProductBy_Sku_VTEX  <-----  resp = " + response.Content, "VTEX_ProductAPI");

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JObject o = JObject.Parse(response.Content);

                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return "OK|" + o.SelectToken("id").ToString() + "|" + response.Content;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Enviar sugerencia de producto producto
                    return "NOT FOUND|";

                }
                else
                {
                    // ERROR

                }
                return "ERROR|" + response.Content;
            }
            catch (Exception f)
            {
                return "ERROR| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }

        public string PutNotificationBy_Sku_VTEX(string SellerAccessToken, string IntegratorApiKey,  string ProductId, string MkpSellerID)
        {
            try
            {
                
                var request = CreateRequest("/"+ MkpSellerID + "/" + ProductId, Method.POST);
                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);


                var response = Client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {

                    //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return "OK";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Enviar sugerencia de producto producto
                    return "NOT FOUND";

                }
                else
                {
                    // ERROR

                }
                return "ERROR|" + response.Content;
            }
            catch (Exception f)
            {
                return "ERROR| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }

        public string GetProductsFromCatalogoCTC(int Pagina, int ProductType, int CantidadFilas, string ProviderId, string Brand, int Active, int inStock)
        {
            string response = "";
            string Command = "";

            Command += "page=" + Pagina;
            if (ProductType > 0)
                Command += "&ProductType=" + ProductType;
            if (Brand != "")
                Command += "&Brand=" + Brand;
            if (Active != -1)
                Command += "&Active=" + Active;
            if (inStock != -1)
                Command += "&inStock=" + inStock;
            Command += "&rows=" + CantidadFilas;

            string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
            string ID_Deposito = System.Configuration.ConfigurationManager.AppSettings["ID_Deposito_FJ"];
            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenCTC"];
            Magento m_api = new Magento(URL_API_CTC);

            var request1 = m_api.CreateRequest("/providers/" + ProviderId + "/products?" + Command, Method.GET, TokenAPI);

            var response1 = m_api.Client.Execute(request1);
            if (response1.StatusCode == HttpStatusCode.OK)
            {
                LogSystem.WriteLogDebugDirect(" -----  000000000000  RESPUESTA DE GET + FILTROS de SC oooooooooo\n\n " + response1.Content, "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" ------------------------------------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" ------------------------------------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" -----  I N I C I O        A D A P T A C I O N     P R O D U C T O S  ---------", "StockCentralToFJasVendorProductsCheck");

                try
                {
                    JObject o = JObject.Parse(response1.Content);
                    //ProductApiVendorByFJ[] product = JsonConvert.DeserializeObject<ProductApiVendorByFJ[]>(o.SelectToken("Result").ToString());
                    //ProductApiVendorByFJv2[] product_A = new ProductApiVendorByFJv2[product.Length];
                    ProductApiVendorByFJv2[] product = JsonConvert.DeserializeObject<ProductApiVendorByFJv2[]>(o.SelectToken("Result").ToString());
                    ProductApiVendorByFJ[] productTMP = JsonConvert.DeserializeObject<ProductApiVendorByFJ[]>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "StockCentralToFJasVendorProductsCheck");

                    for (int j = 0; j < product.Length; j++)
                    {
                        product[j].providerCode = product[j].sku;
                        product[j].cost = product[j].price;
                        product[j].model = "";
                        product[j].warranty = "6";
                        product[j].warrantyType = "OWNER_WARRANTY";
                        product[j].description =
                            "<h3>Detalle</h3><p>" +
                            product[j].description +
                            "</p><BR/><BR/><h3>Términos y condiciones</h3><p>" + productTMP[j].TyC + "</p>";

                        product[j].packages = new List<packages>();
                        packages paq = new packages();
                        paq.depth = (product[j].depth == 0 ? (decimal)0.01 : product[j].depth);
                        paq.height = (product[j].height == 0 ? (decimal)0.01 : product[j].height);
                        paq.name = "Paquete Estandar";
                        paq.weight = (product[j].weight == 0 ? (decimal)0.01 : product[j].weight); ;
                        paq.width = (product[j].width == 0 ? (decimal)0.01 : product[j].width); ;
                        paq.measured_weight = (product[j].weight == 0 ? (decimal)0.01 : product[j].weight); ;
                        product[j].packages.Add(paq);
                        product[j].shippingWarehouse = ID_Deposito;
                        if (product[j].children == null)
                        {
                            List<children> CB = new List<children>();
                            product[j].children = CB;
                        }
                        product[j].description = product[j].description.Replace("\r\n", "<br/><br/>");
                        product[j].description = product[j].description.Replace("\n", "<br/>");
                        product[j].description = product[j].description.Replace("\\n", "<br/>");
                    }

                    response = JsonConvert.SerializeObject(product, Formatting.Indented);
                }
                catch (Exception f)
                {
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  ERROR en obtención productos de CTC y transformarlos a FJ : " + f.Message + ", " + f.InnerException + ", " + f.Source, "StockCentralToFJasVendorProductsCheck");
                    return "ERROR|";
                }
                return "OK|" + response;

            }
            else
            {
                LogSystem.WriteLogDebugDirect(" -----  000000000000  ERROR en obtención productos de CTC : " + response1.Content, "StockCentralToFJasVendorProductsCheck");
                return "ERROR|No hay productos con este pedido";
            }
        }

        public string GetProductsFromCatalogoCTC(int CatalogoID, int Pagina, int ProductType, int CantidadFilas, string ProviderId, string Brand, int Active, int inStock )
        {
            string response = "";
            string Command = "";
  
            Command += "page=" + Pagina;
            if (ProductType > 0)
                Command += "&ProductType=" + ProductType;
            if (Brand != "")
                Command += "&Brand=" + Brand;
            if (Active != -1)
                Command += "&Active=" + Active;
            if (inStock != -1)
                Command += "&inStock=" + inStock;
            Command += "&rows=" + CantidadFilas;

            string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
            string ID_Deposito = System.Configuration.ConfigurationManager.AppSettings["ID_Deposito_FJ"];
            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenCTC_" + CatalogoID ];
            Magento m_api = new Magento(URL_API_CTC);
            LogSystem.WriteLogDebugDirect(" -----  Token asignado : " + TokenAPI.Substring(0,10) + "..." , "StockCentralToFJasVendorProductsCheck");

            var request1 = m_api.CreateRequest("/providers/" + ProviderId + "/products?" + Command, Method.GET, TokenAPI);
            
            var response1 = m_api.Client.Execute(request1);
            if (response1.StatusCode == HttpStatusCode.OK)
            {
                LogSystem.WriteLogDebugDirect(" -----  000000000000  RESPUESTA OK DE GET + FILTROS de SC oooooooooo\n\n " /*+ response1.Content*/, "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" ------------------------------------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" ------------------------------------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" -----  I N I C I O        A D A P T A C I O N     P R O D U C T O S  ---------", "StockCentralToFJasVendorProductsCheck");

                try
                {
                    JObject o = JObject.Parse(response1.Content);
                    //ProductApiVendorByFJ[] product = JsonConvert.DeserializeObject<ProductApiVendorByFJ[]>(o.SelectToken("Result").ToString());
                    //ProductApiVendorByFJv2[] product_A = new ProductApiVendorByFJv2[product.Length];
                    ProductApiVendorByFJv2[] product = JsonConvert.DeserializeObject<ProductApiVendorByFJv2[]>(o.SelectToken("Result").ToString());
                    ProductApiVendorByFJ[] productTMP = JsonConvert.DeserializeObject<ProductApiVendorByFJ[]>(o.SelectToken("Result").ToString());

                    //LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "StockCentralToFJasVendorProductsCheck");

                    for (int j = 0; j < product.Length; j++)
                    {
                        product[j].providerCode = product[j].sku;
                        product[j].cost = product[j].price;
                        product[j].model = "";
                        product[j].warranty = "6";
                        product[j].warrantyType = "OWNER_WARRANTY";
                        product[j].description =
                            "<h3>Detalle</h3><p>" +
                            product[j].description +
                            "</p><BR/><BR/><h3>Términos y condiciones</h3><p>" + productTMP[j].TyC + "</p>";

                        product[j].packages = new List<packages>();
                        packages paq = new packages();
                        paq.depth = (product[j].depth==0?(decimal)0.01: product[j].depth);
                        paq.height = (product[j].height == 0 ? (decimal)0.01 : product[j].height);
                        paq.name = "Paquete Estandar";
                        paq.weight = (product[j].weight == 0 ? (decimal)0.01 : product[j].weight); ;
                        paq.width = (product[j].width == 0 ? (decimal)0.01 : product[j].width); ;
                        paq.measured_weight = (product[j].weight == 0 ? (decimal)0.01 : product[j].weight); ;
                        product[j].packages.Add(paq);
                        product[j].shippingWarehouse = ID_Deposito;
                        if (product[j].children == null)
                        {
                            List<children> CB = new List<children>();
                            product[j].children = CB; 
                        }
                        product[j].description = product[j].description.Replace("\r\n", "<br/><br/>");
                        product[j].description = product[j].description.Replace("\n", "<br/>");
                        product[j].description = product[j].description.Replace("\\n", "<br/>");
                        product[j].description = product[j].description.Replace('"', 'p');
                        product[j].description = product[j].description.Replace('\"', 'p');
                        product[j].description = product[j].description.Replace("\"", "p");
                        product[j].description = product[j].description.Replace("\\" + "\"", "p");



                    }

                    response = JsonConvert.SerializeObject(product, Formatting.Indented);
                }
                catch(Exception f)
                {
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  ERROR en obtención productos de CTC y transformarlos a FJ : " + f.Message + ", " +f.InnerException  + ", " + f.Source , "StockCentralToFJasVendorProductsCheck");
                    return "ERROR";
                }
                return  response;

            }
            else
            {
                LogSystem.WriteLogDebugDirect(" -----  000000000000  ERROR en obtención productos de CTC : " + response1.Content, "StockCentralToFJasVendorProductsCheck");
                return "ERROR|No hay productos con este pedido";
            }
        }

        public string UpdateProduct_ToFj(string json)
        {
            string gres = "OK";
            int milisegundos_entre_paginas = 120000;
            
            try
            {
                milisegundos_entre_paginas = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Milisegundos_de_espera_entre_paginas_a_enviar_a_FJ"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro milisegundos_entre_paginas no configurado.", "StockCentralToFJasVendorProductsCheck");
            }


            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
            var request = CreateRequest("/batch/products", Method.PUT);
            request.AddHeader("X-Auth-Token", TokenAPI);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            ProductApiVendorByFJv2[] product = JsonConvert.DeserializeObject<ProductApiVendorByFJv2[]>(json);

            for (int j = 0; j < 100; j++)
            {

                LogSystem.WriteLogDebugDirect(" -----  000000000000  JSON A ENVIAR A FULLJAUS (intento " + (j + 1) + ")>>>>>>>>>> "  + json.Length + " Caracteres"  , "StockCentralToFJasVendorProductsCheck");

                var response = Client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string res = response.Content;
                    // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);

                    JObject o = JObject.Parse(res);

                    LogSystem.WriteLogDebugDirect(" ----- Respuesta FJ -> "+res , "StockCentralToFJasVendorProductsCheck");
                    LogSystem.WriteLogDebugDirect(" ----- success = " + o.SelectToken("success").ToString().ToUpper(), "StockCentralToFJasVendorProductsCheck");

                    if (o.SelectToken("success").ToString().ToUpper() == "TRUE")
                    {
                        Tid = o.SelectToken("tid").ToString();
                        return "0|" + res;
                    }
                    else
                    {
                        gres = "-1|" + res;
                    }
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX  Dió error. Esperamos "+milisegundos_entre_paginas+"ms para reenvío del mismo paquete", "StockCentralToFJasVendorProductsCheck");
                    Thread.Sleep(milisegundos_entre_paginas);
                }
                else
                {
                    string res = response.Content;
                    // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    gres = response.StatusCode.ToString() + "|" + res;
                }
            }

            return gres;
        }
        public string SetGoldenruleProduct_ToFj(string ProductId, int Mkp, decimal Precio)
        {
            string response1 = "OK";

            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
            var request = CreateRequest("/golden_rule_product", Method.POST);
            request.AddHeader("X-Auth-Token", TokenAPI);

            string GR = "{ " +
                "\"product\": \""+ ProductId + "\","+
                "\"precioespecifico\": "+ Precio+","+
                "\"fechainicio\": \"2024-01-01T00:00:00\"," +
                "\"fechafinal\": \"2034-01-01T00:00:00\", "+
                "\"empresamarketplace\": "+ Mkp + ""+
                "}";
            LogSystem.WriteLogDebugDirect(" -----  000000000000  JSON A ENVIAR A FULLJAUS >>>>>>>>>> " + GR, "StockCentralToFJasVendorProductsCheck");


            request.AddParameter("application/json", GR, ParameterType.RequestBody);


            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {


                JObject o = JObject.Parse(response.Content);
                string PubID = o.SelectToken("result.id").ToString();
                return "0|" + PubID;
            }
            else
            {
                string res = response.Content;
                // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                return response.StatusCode.ToString() + "|" + res;
            }

            
        }

        public string PublicateProduct_ToFj(string ProductId, int Mkp)
        {
            string response1 = "OK";
            if (response1 != "Error")
            {
                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
                var request = CreateRequest("/publications", Method.POST);
                request.AddHeader("X-Auth-Token", TokenAPI);
                string pb = "{  \"sku\": \"" + ProductId + "\", \"marketplace_connection\": \"" + Mkp + "\"}";
                request.AddParameter("application/json", pb, ParameterType.RequestBody);

                LogSystem.WriteLogDebugDirect(" -----  000000000000  JSON A ENVIAR A FULLJAUS >>>>>>>>>> \n" + pb, "StockCentralToFJasVendorProductsCheck");

                var response = Client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {


                    JObject o = JObject.Parse(response.Content);
                    string PubID = o.SelectToken("result.id").ToString();

                    return "0|" + PubID;
                }
                else
                {
                    string res = response.Content;
                    // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return response.StatusCode.ToString() + "|" + res;
                }
            }
            return response1;
        }

        public string PublicateProduct_ToFj(string ProductId, int Mkp, int stock, Boolean active)
        {
            string response1 = "OK";
            if (response1 != "Error")
            {
                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
                var request = CreateRequest("/publications", Method.POST);
                request.AddHeader("X-Auth-Token", TokenAPI);
                string pb = "{  \"sku\": \"" + ProductId + "\", \"marketplace_connection\": \"" + Mkp + "\"}";
                request.AddParameter("application/json", pb , ParameterType.RequestBody);

                LogSystem.WriteLogDebugDirect(" -----  000000000000  JSON A ENVIAR A FULLJAUS >>>>>>>>>> \n" + pb, "StockCentralToFJasVendorProductsCheck");

                var response = Client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {

                     
                    JObject o = JObject.Parse(response.Content);
                    string PubID = o.SelectToken("result.id").ToString();

                    var requestU = CreateRequest("/publications/" + PubID, Method.PUT);
                    requestU.AddHeader("X-Auth-Token", TokenAPI);
                    string pbU = "{  \"sku\": \"" + ProductId + "\", \"active\": "+active.ToString().ToLower()+" , \"product\": {  \"sku\": \"" + ProductId + "\", \"stock\": "+ stock + "}}";
                    requestU.AddParameter("application/json", pbU, ParameterType.RequestBody);

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  JSON A ENVIAR A FULLJAUS UPDATE PUB >>>>>>>>>> \n" + pbU, "StockCentralToFJasVendorProductsCheck");

                    var responseU = Client.Execute(requestU);
                    if (responseU.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return "0|" + PubID;
                    }
                    else
                    {
                        string res = responseU.Content;
                        // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                        return responseU.StatusCode.ToString() + "|" + res;
                    }
                
                }
                else
                {
                    string res = response.Content;
                    // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                    return response.StatusCode.ToString() + "|" + res;
                }
            }
            return response1;
        }

        
         public string PublicationUpdateProduct_ToFj(string SKU, string PubID, Boolean Active, int Stock, string Price, string ListPrice, string IVA, int Mkp)
        {
            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
            var request = CreateRequest("/publications/" + PubID, Method.PUT);

            decimal beforeTaxesPrice = Convert.ToDecimal(Price) / (1+Convert.ToDecimal(IVA)/100);
            decimal beforeTaxesListPrice = Convert.ToDecimal(ListPrice) / (1 + Convert.ToDecimal(IVA) / 100); ;

            request.AddHeader("X-Auth-Token", TokenAPI);
            string upd = "{"+
                        "   \"priceToPublish\": { "+
                        "   \"price\": "+ Price +", "+
                        "   \"listPrice\": "+ ListPrice +", " +
                        "   \"beforeTaxesPrice\": "+beforeTaxesPrice+", " +
                        "   \"beforeTaxesListPrice\": "+beforeTaxesListPrice+", " +
                        "   \"discount\": 0, : "+
                        "   \"roundedDiscount\": 0 "+
                        "  }, "+
                        "  \"sku\": \""+SKU+"\", "+
                        "  \"active\": "+(Active == true ? "true": "false")+" , "+
                        "  \"stockToPublish\": "+Stock+" "+
                    " }";

            request.AddParameter("application/json",  upd , ParameterType.RequestBody);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string res = response.Content;
                // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                return "0|" + res;
            }
            else
            {
                string res = response.Content;
                // List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                return response.StatusCode.ToString() + "|" + res;
            }
             
             
        }

        public string AddImageProduct_ToFile(string ProductId, string ProviderID, string TokenAPICTC)
        {

            string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
            string row = "";
            if(TokenAPICTC == "")
                TokenAPICTC = System.Configuration.ConfigurationManager.AppSettings["TokenCTC"];
            
            try
            {
                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPICTC);

                var response1 = m_api.Client.Execute(request1);
                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "ProductsTools");

                    int imgind = 0;
                    string images = "";
                    row = ProductId + ";" + product.Stock  ;
                    foreach (MG2Connector.Images img in product.Images)
                    {
                        imgind++;
                        if (imgind < 9)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "ProductsTools");
                            images += ";" + img.Url;
                        }
                    }
                    for(int i = 0; i < 8-imgind; i++)
                    {
                        images+= ";";
                    }
                    row += images;
                    LogSystem.WriteLogDebugDirect(" -----  ROW : " + row, "ProductsTools");
                }
            }
            catch (Exception f)
            {
                LogSystem.WriteLogDebugDirect(" -----  XXXX  ERROR Evaluando Producto de SC : " + ProductId, "ProductsTools");
                return "";
            }
            return row;

        }

        public string UpdateImageProduct_ToFj(string ProductId, string ProviderID )
        {

            string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
          
            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
            string TokenAPICTC = System.Configuration.ConfigurationManager.AppSettings["TokenCTC"];
            try
            {
                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPICTC);

                var response1 = m_api.Client.Execute(request1);
                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "StockCentralToFJasVendorProductsCheck");

                    string images = "";
                    int imgind = 0;
                    foreach (MG2Connector.Images img in product.Images)
                    {
                        imgind++;
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "StockCentralToFJasVendorProductsCheck");
                        images =

                                "{ " +
                                     "\"url\": \"" + img.Url + "\", " +
                                     "\"position\": " + imgind +
                                "} ";
                        var request = CreateRequest("/products/" + ProductId + "/images/" + imgind, Method.PUT);
                        request.AddHeader("X-Auth-Token", TokenAPI);
                        request.AddParameter("application/json", images, ParameterType.RequestBody);


                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Imagen Body -> " + images, "StockCentralToFJasVendorProductsCheck");

                        var response = Client.Execute(request);

                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Respuesta FJ Imagen -> " + response.Content, "StockCentralToFJasVendorProductsCheck");

                    }
                }
            }
            catch (Exception f)
            {
                LogSystem.WriteLogDebugDirect(" -----  XXXX  ERROR Evaluando Producto de SC : " + ProductId, "StockCentralToFJasVendorProductsCheck");
                return "-1|ERROR Imagenes producto";
            }
            return "0|";

        }

        public string UpdateImageProduct_ToFj(string ProductId, string ProviderID, int CatalogoID)
        {

            string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];

            string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
            string TokenAPICTC = System.Configuration.ConfigurationManager.AppSettings["TokenCTC_" + CatalogoID];
            try
            {
                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPICTC);

                var response1 = m_api.Client.Execute(request1);
                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "StockCentralToFJasVendorProductsCheck");

                    string images = "";
                    int imgind = 0;
                    foreach (MG2Connector.Images img in product.Images)
                    {
                        imgind++;
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "StockCentralToFJasVendorProductsCheck");
                        images =

                                "{ " +
                                     "\"url\": \"" + img.Url + "\", " +
                                     "\"position\": " + imgind +
                                "} ";
                        var request = CreateRequest("/products/" + ProductId + "/images/" + imgind, Method.PUT);
                        request.AddHeader("X-Auth-Token", TokenAPI);
                        request.AddParameter("application/json", images, ParameterType.RequestBody);


                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Imagen Body -> " + images, "StockCentralToFJasVendorProductsCheck");

                        var response = Client.Execute(request);

                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Respuesta FJ Imagen -> " + response.Content, "StockCentralToFJasVendorProductsCheck");

                    }
                }
            }
            catch (Exception f)
            {
                LogSystem.WriteLogDebugDirect(" -----  XXXX  ERROR Evaluando Producto de SC : " + ProductId, "StockCentralToFJasVendorProductsCheck");
                return "-1|ERROR Imagenes producto";
            }
            return "0|";

        }


        public string NewProduct_VTEX(string SellerAccessToken, string IntegratorApiKey, string ProviderID, string ProductId, string MkpSellerID)
        {
            try
            {
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                string URL_API_VTEX_PRICE = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_suggestion_Product"];

                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000  C R E A T E   000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU = " + ProductId, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Provider = " + ProviderID, "VTEX_ProductAPI");

                string SecurityStock = System.Configuration.ConfigurationManager.AppSettings["ControlStockSeguridad"];
                int StockControl = 0;
                try
                {
                    StockControl = Convert.ToInt32(SecurityStock);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Security Stock Control : " + StockControl, "VTEX_ProductAPI");
                }
                catch (Exception frt)
                {
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  ERROR Security Stock Control. SeurityStock: " + StockControl, "VTEX_ProductAPI");

                }



                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPI);

                var response1 = m_api.Client.Execute(request1);

                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "VTEX_ProductAPI");

                    if(product.CategoryPath == "")
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN SC", "VTEX_ProductAPI");
                        return "23|RROR - CATEGORIA NO PRESENTE EN SC";
                    }

                    string Product = "{" +
                                        "  \"Name\": \"" + (product.ProductType != 1? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"CategoryPath\": \""+product.CategoryPath+"\"," +
                                        "  \"BrandName\": \"" + product.Brand + "\"," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"Title\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"LinkId\": \"" + (product.ProductType != 1 ? product.Brand + "-" : "") + product.Name.Replace(" ", "-").Replace('"', 'p') + "\"," +
                                        "  \"Description\": \"" + product.Description.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"' , ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "<br/><br/><b >Términos y Condiciones</b><br>" + product.TyC.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "\"," +
                                        "  \"DescriptionShort\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +

                                        "  \"ReleaseDate\": \"" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "\"," +
                                        "  \"IsVisible\": " + product.Active.ToString().ToLower() + "," +
                                        "  \"IsActive\": " + product.Active.ToString().ToLower() + "," +
                                        "  \"TaxCode\": \"1\" ," +
                                        "  \"MetaTagDescription\":  \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +

                                        "  \"ShowWithoutStock\": false, \"KeyWords\": null," +
                                        "  \"AdWordsRemarketingCode\": \"\", \"LomadeeCampaignCode\": \"\"," +


                                        "  \"Score\": 1 " +
                                        
                                        "  }";

 
                    /*
                                        "  \"ProductSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Fabric\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Cotton\"," +
                                        "        \"Velvet\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +
                                        "  \"SkuSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Color\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Red\"," +
                                        "        \"Blue\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +

                     */
                    var request = CreateRequest("/api/catalog/pvt/product" , Method.POST);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                    request.AddParameter("application/json", Product, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando productos. \n" + Product, "VTEX_ProductAPI");

                    var response = Client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Producto Creado.", "VTEX_ProductAPI");

                        JObject o_vtex = JObject.Parse(response.Content);

                        
                        string SKU_product  = "{" +
                                        "  \"Name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"ProductId\": "+ o_vtex.SelectToken("Id").ToString() + "," +
                                        "  \"ActivateIfPossible\": true," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"Ean\": \"" + product.Sku + "\"," +
                                        "  \"IsKit\": false," +
                                        "  \"Height\": " + product.Height.ToString().Replace(",",".") + "," +
                                        "  \"Width\": " + product.Width.ToString().Replace(",", ".") + "," +
                                        "  \"Length\": " + product.Depth.ToString().Replace(",", ".") + "," +
                                        "  \"Weight\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"WeightKg\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"PackagedHeight\": " + product.Height.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWidth\": " + product.Width.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedLength\": " + product.Depth.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWeight\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWeightKg\": " + Convert.ToInt32((product.Weight * 1000)).ToString().Replace(",", ".") + "," +
                                        "  \"CubicWeight\": 1," +

                                        "  \"CommercialConditionId\":1," +
                                        "  \"MeasurementUnit\": \"un\" ," +
                                        
                                        "  \"UnitMultiplier\": 1 ," +
                                        "  \"KitItensSellApart\":  false" +

                                        
                                        "  }";
                        request = CreateRequest("/api/catalog/pvt/stockkeepingunit", Method.POST);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", SKU_product, ParameterType.RequestBody);
 
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando SKU ", "VTEX_ProductAPI");

                        response = Client.Execute(request);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU Creado.", "VTEX_ProductAPI");


                            JObject o_vtex_sku = JObject.Parse(response.Content);



                            string images = "";
                            int imgind = 1;
                            foreach (MG2Connector.Images img in product.Images)
                            {

                                LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen "+imgind, "VTEX_ProductAPI");


                                string isMain = "\"isMain\" : " + (imgind == 1 ? "true," : "false,");
                                images =
                                             "    {" + isMain +
                                             "      \"Label\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                             "      \"Name\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                             "      \"Text\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                             "      \"Url\": \"" + img.Url + "\"" +
                                             "    }";

                                request = CreateRequest("/api/catalog/pvt/stockkeepingunit/"+ o_vtex_sku.SelectToken("Id").ToString()+ "/file", Method.POST);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                request.AddParameter("application/json", images, ParameterType.RequestBody);
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   Imagen " + imgind + " Creada " , "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR CREANDO  Imagen " + imgind , "VTEX_ProductAPI");

                                }


                            }
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de creaciones de Imagenes ", "VTEX_ProductAPI");
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Precios ", "VTEX_ProductAPI");

                            string Pricing = "{" +
                                                                   "  \"markup\": 0," +
                                                                   "  \"basePrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                                   "  \"listPrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                                   "  \"fixedPrices\": [] " +

                                                                   "  }";


                            Magento m_apiPricing = new Magento(URL_API_VTEX_PRICE);

                            //request = m_apiPricing.CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.POST);
                            request = m_apiPricing.CreateRequest("/pricing/prices/" + o_vtex_sku.SelectToken("Id").ToString(), Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                            request.AddParameter("application/json", Pricing, ParameterType.RequestBody);
                            response = m_apiPricing.Client.Execute(request);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   PRICE OK ", "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR PRICE ", "VTEX_ProductAPI");

                            }

                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de  Seteo de Precios ", "VTEX_ProductAPI");
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Stock", "VTEX_ProductAPI");

                            string Stock = "{" +
                                                                   "  \"unlimitedQuantity\": false," +
                                                                   "  \"dateUtcOnBalanceSystem\": \"null\"," +
                                                                   "  \"quantity\": " + ((product.Stock - StockControl) <= 0 ? 0 : product.Stock - StockControl) + "" +

                                                                   "  }";




                            request = CreateRequest("/api/logistics/pvt/inventory/skus/" + o_vtex_sku.SelectToken("Id").ToString() + "/warehouses/1_1", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                            request.AddParameter("application/json", Stock, ParameterType.RequestBody);
                            response = Client.Execute(request);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   Stock OK ", "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Stock ", "VTEX_ProductAPI");

                            }


                            // Asignar propiedades extendidas
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Asignando propiedades a SKU ", "VTEX_ProductAPI");
                            string Activar_Propiedades_Producto = "NO";
                            try
                            {
                                Activar_Propiedades_Producto = System.Configuration.ConfigurationManager.AppSettings["Activar_Propiedades_Producto"];
                            }
                            catch
                            {
                                Activar_Propiedades_Producto = "NO";
                            }

                            LogSystem.WriteLogDebugDirect(" -----  Activar_Propiedades_Producto = " + Activar_Propiedades_Producto, "VTEX_ProductAPI");
                            if (Activar_Propiedades_Producto == "SI")
                                foreach (ProductAttribute pa in product.ProductAttributes)
                                {

                                    if (pa.Name.ToUpper() != "EAN" && pa.Name.ToUpper() != "BRAND" && pa.Name.ToUpper() != "IVA" && pa.Name.ToUpper() != "IMPUESTOS INTERNOS" && pa.Name.ToUpper() != "PRIORIDAD" && pa.Name.ToUpper() != "IMPORTE")
                                    {
                                        request = CreateRequest("/api/catalog/pvt/product/" + o_vtex.SelectToken("Id").ToString() + "/specificationvalue", Method.PUT);
                                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                        string prop = "{ \"FieldName\": \"" + pa.Name + "\", " +
                                          "\"GroupName\": \"Global\", " +
                                          "\"RootLevelSpecification\": true, " +
                                          "\"FieldValues\": [ " +
                                            "\"" + pa.Value + "\" " +
                                          "] " +
                                         "} ";
                                        request.AddParameter("application/json", prop, ParameterType.RequestBody);
                                        response = Client.Execute(request);
                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : " + pa.Name + " --> OK ", "VTEX_ProductAPI");

                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : " + pa.Name + " : " + response.Content, "VTEX_ProductAPI");

                                        }

                                    }


                                }
                            else
                            {
                                if ((int)product.ProductType == 11
                                    || (int)product.ProductType == 12
                                    || (int)product.ProductType == 14
                                    || (int)product.ProductType == 16
                                    || (int)product.ProductType == 18
                                    || (int)product.ProductType == 26)
                                {
                                    foreach (ProductAttribute pa in product.ProductAttributes)
                                    {

                                        if (pa.Name.ToUpper() == "VIGENCIA" || pa.Name.ToUpper() == "USO")
                                        {
                                            request = CreateRequest("/api/catalog/pvt/product/" + o_vtex.SelectToken("Id").ToString() + "/specificationvalue", Method.PUT);
                                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                            string prop = "{ \"FieldName\": \"" + pa.Name + "\", " +
                                              "\"GroupName\": \"Global\", " +
                                              "\"RootLevelSpecification\": true, " +
                                              "\"FieldValues\": [ " +
                                                "\"" + pa.Value + "\" " +
                                              "] " +
                                             "} ";
                                            request.AddParameter("application/json", prop, ParameterType.RequestBody);
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop, "VTEX_ProductAPI");
                                            response = Client.Execute(request);
                                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : " + pa.Name + " --> OK " + response.Content, "VTEX_ProductAPI");

                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : " + pa.Name + " : " + response.Content, "VTEX_ProductAPI");

                                            }

                                        }


                                    }
                                    request = CreateRequest("/api/catalog/pvt/product/" + o_vtex.SelectToken("Id").ToString() + "/specificationvalue", Method.PUT);
                                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                    string prop11 = "{ \"FieldName\": \"Importe\", " +
                                      "\"GroupName\": \"Global\", " +
                                      "\"RootLevelSpecification\": true, " +
                                      "\"FieldValues\": [ " +
                                        "\"" + product.Name + "\" " +
                                      "] " +
                                     "} ";
                                    request.AddParameter("application/json", prop11, ParameterType.RequestBody);
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop11, "VTEX_ProductAPI");
                                    response = Client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Importe --> OK " + response.Content, "VTEX_ProductAPI");

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : Importe : " + response.Content, "VTEX_ProductAPI");

                                    }



                                }
                            }


                            //****** IVA

                            request = CreateRequest("/api/catalog_system/pvt/products/" + o_vtex.SelectToken("Id").ToString() + "/specification", Method.POST);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            string prop1 = "[{ \"Name\": \"IVA\", " +
                                  "\"Id\": 563, " +
                                   "\"Value\": [ " +
                                    "\"" + product.Taxes.ToString().Replace(",", ".") + "\" " +
                                  "] " +
                                 "}, ";
                            //prop1 += "{ \"FieldName\": \"Importe\", " +
                            // "\"Id\": 569, " +
                            //  "\"Value\": [ " +
                            //   "\"" + product.Name + "\" " +
                            // "] " +
                            //"}, ";
                            prop1 += "{ \"FieldName\": \"Impuestos internos\", " +
                              "\"Id\": 564, " +
                               "\"Value\": [ " +
                                "\"0\" " +
                              "] " +
                             "} ]";

                            request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop IVA e II --> OK " + response.Content, "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop IVA e II : " + response.Content, "VTEX_ProductAPI");

                            }



                            // ******* DELETE 565 y 566
                            //**************************
                            request = CreateRequest("/api/catalog/pvt/product/" + o_vtex.SelectToken("Id").ToString() + "/specification", Method.GET);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            LogSystem.WriteLogDebugDirect(" -----  000000000000   Consultando propiedades prod : " + o_vtex.SelectToken("Id").ToString(), "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop  --> OK " + response.Content, "VTEX_ProductAPI");
                                JObject o_prop = JObject.Parse(response.Content);

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : " + response.Content, "VTEX_ProductAPI");

                            }






                            // *************************




                        }
                        else 
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR SKU NO CREADO ", "VTEX_ProductAPI");

                        }

                    }
                    LogSystem.WriteLogDebugDirect(" -----  Response :  "+ response.StatusCode + "|" + response.Content, "VTEX_ProductAPI");

                    return  "0|" + response.Content;


                }
                else
                {
                    // ERROR
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR obteniendo producto : " + ProductId, "VTEX_ProductAPI");

                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string NewProduct_VTEXbySeller(string SellerAccessToken, string IntegratorApiKey, string ProviderID, string ProductId, string MkpSellerID)
        {
            try
            {
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                string URL_API_VTEX_PRICE = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_suggestion_Product"];
                string Vtex_Seller_Id = System.Configuration.ConfigurationManager.AppSettings["Vtex_Seller_Id"];
                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
                string URL_VTEX_TokenImages = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_TokenImages"];
                string URL_VTEX_app_IO = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_app_IO"];
                string PathImagenes = System.Configuration.ConfigurationManager.AppSettings["PathImagenes"]; 
                string VTEX_Warehouse = System.Configuration.ConfigurationManager.AppSettings["VTEX_Warehouse"];

                LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000  C R E A T E   000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU = " + ProductId, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Provider = " + ProviderID, "VTEX_ProductAPI");

                string SecurityStock = System.Configuration.ConfigurationManager.AppSettings["ControlStockSeguridad"];
                int StockControl = 0;
                try
                {
                    StockControl = Convert.ToInt32(SecurityStock);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Security Stock Control : " + StockControl, "VTEX_ProductAPI");
                }
                catch (Exception frt)
                {
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  ERROR Security Stock Control. SeurityStock: " + StockControl, "VTEX_ProductAPI");

                }



                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPI);

                var response1 = m_api.Client.Execute(request1);

                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "VTEX_ProductAPI");

                    if (product.CategoryPath == "")
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN SC", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "SIN_Categoria_SC");

                        return "23|RROR - CATEGORIA NO PRESENTE EN SC";
                    }


                    // Buscar / Crear brand en VTEX
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Buscando BRAND ", "VTEX_ProductAPI");

                    string brand = "";
                    var request = CreateRequest("api/catalog-seller-portal/brands?q=&from=1&to=1&orderBy=status,asc;name,asc&name=" + product.Brand.Replace("&", "%26"), Method.GET);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                    var responseb = Client.Execute(request);
                    if (responseb.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  FIN BUSQUEDA BRAND.", "VTEX_ProductAPI");

                        JObject o_brand = JObject.Parse(responseb.Content);
                        brand = o_brand.SelectToken("id").ToString();

                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  CREANDO BRAND ", "VTEX_ProductAPI");
                        
                        request = CreateRequest("api/catalog-seller-portal/brands", Method.POST);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                        string json = "{\r\n\"name\": \""+product.Brand +"\",\r\n\"isActive\": true }";
                        
                        request.AddParameter("application/json", json, ParameterType.RequestBody);

                        responseb = Client.Execute(request);
                        if (responseb.StatusCode == System.Net.HttpStatusCode.Created )
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  OK BRAND.", "VTEX_ProductAPI");

                            JObject o_brand = JObject.Parse(responseb.Content);
                            brand = o_brand.SelectToken("id").ToString();

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  ERROR - No se pudo crear BRAND en VTEX.", "VTEX_ProductAPI");
                            LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_CREANDO_BRAND_VTEX");
                            return "32|ERROR - No se pudo crear BRAND en VTEX";
                        }
                    }
                    // *****************************


                    // Buscar / Crear categoria en VTEX
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Buscando CATEGORY " + product.CategoryBranch[0].Name, "VTEX_ProductAPI");

                    string categ = "NEW";
                    request = CreateRequest("api/catalog-seller-portal/category-tree" /*/categories/" + product.CategoryBranch[0].Code */, Method.GET);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                    responseb = Client.Execute(request);
                    if (responseb.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  FIN Obtención ARBOL de CATEGORIAS.", "VTEX_ProductAPI");
                        JObject o_cat = JObject.Parse(responseb.Content);

                        int Cant = (int) o_cat.SelectToken("roots").Count();
                        bool EncontreCategoria = false;
                        for (int i = 0; i < Cant; i++)
                        {
                            try
                            {
                                int Cant1 = o_cat.SelectToken("roots")[i]["children"].Count();
                                for (int j = 0; j < Cant1; j++)
                                {
                                    try
                                    {
                                        int Cant2 = o_cat.SelectToken("roots")[i]["children"][j]["children"].Count();
                                        for (int jj = 0; jj < Cant1; jj++)
                                        {
                                            try
                                            {
                                                if (o_cat.SelectToken("roots")[i]["children"][j]["children"][jj]["value"]["name"].ToString() == product.CategoryBranch[0].Name)
                                                {

                                                    categ = o_cat.SelectToken("roots")[i]["children"][j]["children"][jj]["value"]["id"].ToString();
                                                    LogSystem.WriteLogDebugDirect(" oooooooo  CATEGORIA OK", "VTEX_ProductAPI");
                                                    EncontreCategoria = true;
                                                    break;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  xxxxxxx Error nivel 3 de categorias.", "VTEX_ProductAPI");

                                            }

                                        }
                                        if (EncontreCategoria)
                                            break;
                                    }
                                    catch (Exception e)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  xxxxxxx Error nivel 2 de categorias.", "VTEX_ProductAPI");

                                    }

                                }
                            }
                            catch (Exception e)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  xxxxxxx Error nivel 1 de categorias.", "VTEX_ProductAPI");

                            }

                            if (EncontreCategoria)
                                break;
                        }



                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN VTEX", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_CATEGORIA_NO_PRESENTE_EN_VTEX");
                        return "33|RROR - CATEGORIA NO PRESENTE EN VTEX";
                    }

                    if (categ == "NEW")
                    {

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN VTEX", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_CATEGORIA_NO_PRESENTE_EN_VTEX");
                        return "33|ERROR - CATEGORIA NO PRESENTE EN VTEX";

                    }
                    
                    // ********************************


                        // Armar Atributos 
                        string attributes = "[ ";
                    int pu = 0;
                    string prop = "";
                    foreach (ProductAttribute pa in product.ProductAttributes)
                    {

                        if (pa.Name.ToUpper() != "EAN" && pa.Name.ToUpper() != "BRAND")
                        {
                            if (pu > 0)
                                prop += ",";

                            prop += "{ \"name\": \"" + pa.Name + "\", " +
                             
                              "\"value\":  " +
                                "\"" + pa.Value + "\" " +
                              
                             "} ";
                        }
                        pu++;

                    }

                    if (pu > 0)
                        prop += ",";

                    prop += "{ \"name\": \"IIN\", " +

                      "\"value\":  " +
                        "\"NO\" " +

                     "}, ";

                    prop += "{ \"name\": \"III\", " +

                      "\"value\":  " +
                        "\"0\" " +

                     "}, ";

                    prop += "{ \"name\": \"IVA\", " +

                      "\"value\":  " +
                        "\"" + product.Taxes + "\" " +

                     "} ";



                    attributes += prop + "]";

                    // ***************




                    // Armar imagenes
                    // --------------
                    string imagesJ = "[";
                    string imagesS = imagesJ;


                    string images = "";
                    string tokenImages = "";
                    int imgind = 1;


                    // 1 - Obtengo KEY
                    Magento v_api = new Magento(URL_VTEX_TokenImages);
                    request = v_api.CreateRequest("/api/vtexid/apptoken/login?an=" + Vtex_Seller_Id, Method.POST);
                    //request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    //request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                    string app_json = "{\r\n    \"appkey\": \"" + IntegratorApiKey + "\",\r\n    \"apptoken\": \"" + SellerAccessToken + "\"\r\n}";

                    request.AddParameter("application/json", app_json, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Solicitando Key de Catalog Images. \n" + app_json, "VTEX_ProductAPI");

                    var responseI = v_api.Client.Execute(request);
                    if (responseI.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Respuesta Key Catalog Images.", "VTEX_ProductAPI");

                        JObject i_vtex = JObject.Parse(responseI.Content);
                        tokenImages = i_vtex.SelectToken("token").ToString();

                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  ERROR  Respuesta Key Catalog Images. --> " + response1.Content, "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_NO_SE_PUEDE_OBTENER_KEY_IMAGENES_VTEX");
                        return "34|ERROR - NO SE PUEDE OBTENER KEY IMAGENS DE VTEX";
                    }


                    Magento IO_api = new Magento(URL_VTEX_app_IO);


                    foreach (MG2Connector.Images img in product.Images)
                    {
                        // revisando si las imagenes existen
                        // *********************************

                        // 2 - Cargo imagen
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "VTEX_ProductAPI");

                        request = IO_api.CreateRequest("/vtex.catalog-images/v0/" + Vtex_Seller_Id + "/master/images/save/" + product.Sku + "_" + imgind + ".jpg", Method.POST);
                        request.AddHeader("VtexIdclientAutCookie", tokenImages);

                        if(imgind == 1)
                            request.AddFile("", PathImagenes + "\\" + product.Sku + "_b.jpg");
                        else
                            request.AddFile("", PathImagenes + "\\" + product.Sku + "_b_" + imgind + ".jpg");


                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Solicitando Key de Catalog Images. \n" + app_json, "VTEX_ProductAPI");
                        string UrlImages = "";
                        JObject img_vtex;
                        var responseIO = IO_api.Client.Execute(request);
                        if (responseIO.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  ooooooo Imagen subida ", "VTEX_ProductAPI");
                            img_vtex = JObject.Parse(responseIO.Content);
                            UrlImages = img_vtex.SelectToken("fullUrl").ToString();

                        }
                        else
                        {
                            if (responseIO.StatusCode == System.Net.HttpStatusCode.Conflict)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  ooooooo Imagen ya existe ", "VTEX_ProductAPI");
                                img_vtex = JObject.Parse(responseIO.Content);
                                UrlImages = img_vtex.SelectToken("file").ToString();

                            }
                        }

                        if (imgind > 1)
                        {
                            images += ",";
                            imagesS += ",";

                        }
                        imagesS += "\"" + product.Sku + "_" + imgind + ".jpg\"";
                        images +=
                                     "    {" + 
                                     "      \"id\": \"" + product.Sku + "_" + imgind + ".jpg\"," +
                                     "      \"alt\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                     "      \"url\": \"" + UrlImages + "\"" +
                                     "    }";
 
                    }
                    imagesJ += images + "]";
                    imagesS += "]";
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de creaciones de Imagenes ", "VTEX_ProductAPI");

                    // **************


                    // Armar Variaciones / SKUS

                    string SKU_product = "[{" +
                             "  \"name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                             "  \"isActive\": true," +
                             "  \"externalId\": \"" + product.Sku + "\"," +
                             "  \"dimensions\": {" +
                             "  \"height\": " + (product.Height == 0 ? (decimal)0.01 : product.Height).ToString().Replace(",", ".") + "," +
                             "  \"width\": " + (product.Width == 0 ? (decimal)0.01 : product.Width).ToString().Replace(",", ".") + "," +
                             "  \"length\": " + (product.Depth == 0 ? (decimal)0.01 : product.Depth).ToString().Replace(",", ".") + "" +
                             "  }, \"weight\": " + ((product.Weight == 0 ? (decimal)0.01 : product.Weight) * 1000).ToString().Replace(",", ".") + "," +
                             "  \"specs\":[], " +
                             "  \"images\": "+ imagesS +
                             "  }]";


                    //Armar especificaciones
                    string specs = "[]";

                    // *********************


                    // ************************


                    // Crear Producto
                    string Product = "{" +
                                        "  \"name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"categoryIds\": [\"" + categ + "\"]," +
                                        "  \"brandId\": \"" + brand + "\"," +
                                        "  \"externalId\": \"" + product.Sku + "\"," +
                                        "  \"slug\": \"/" + product.Sku  + "\"," +
                                        "  \"origin\": \""+ Vtex_Seller_Id + "\"," + // Poner parametro para cuando necesitaemos conectar otr MKP de VTEX
                                        "  \"description\": \"" + product.Description.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "<br/><br/><b >Términos y Condiciones</b><br>" + product.TyC.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "\"," +
                                      
                                        "  \"transportModal\": \"\"," +
                                        "  \"status\": \"" + (product.Active==true?"active":"inactive")+ "\" ," +
                                        "  \"taxCode\": \"1\" ," +
                                        "\"specs\":" + specs + "," +
                                        "\"attributes\":" + attributes + "," +
                                        "\"images\":" + imagesJ + "," +
                                        "\"skus\":" + SKU_product + "" +

                                        "  }";


                    request = CreateRequest("api/catalog-seller-portal/products", Method.POST);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                    request.AddParameter("application/json", Product, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando productos. \n" + Product, "VTEX_ProductAPI");

                    var response = Client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Producto Creado.", "VTEX_ProductAPI");

                        JObject o_vtex = JObject.Parse(response.Content);

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Precios ", "VTEX_ProductAPI");

                        string Pricing = "{" +
                                                               "  \"markup\": 0," +
                                                               "  \"basePrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                               "  \"listPrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                               "  \"fixedPrices\": [] " +

                                                               "  }";

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX  Precios " + Pricing, "VTEX_ProductAPI");

                        Magento m_apiPricing = new Magento(URL_API_VTEX_PRICE);

                        //request = m_apiPricing.CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.POST);
                        request = m_apiPricing.CreateRequest("/pricing/prices/" + o_vtex.SelectToken("id").ToString(), Method.PUT);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", Pricing, ParameterType.RequestBody);
                        response = m_apiPricing.Client.Execute(request);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   PRICE OK ", "VTEX_ProductAPI");

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR PRICE ", "VTEX_ProductAPI");

                        }

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de  Seteo de Precios ", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Stock", "VTEX_ProductAPI");

                        string Stock = "{" +
                                                               "  \"unlimitedQuantity\": false," +
                                                               "  \"dateUtcOnBalanceSystem\": \"null\"," +
                                                               "  \"quantity\": " + ((product.Stock - StockControl) <= 0 ? 0 : product.Stock - StockControl) + "" +

                                                               "  }";




                        request = CreateRequest("/api/logistics/pvt/inventory/skus/" + o_vtex.SelectToken("id").ToString() + "/warehouses/" + VTEX_Warehouse, Method.PUT);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", Stock, ParameterType.RequestBody);
                        response = Client.Execute(request);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   Stock OK ", "VTEX_ProductAPI");

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Stock ", "VTEX_ProductAPI");

                        }


                        // Asignar propiedades extendidas
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Asignando propiedades a SKU ", "VTEX_ProductAPI");



                    }
                    LogSystem.WriteLogDebugDirect(" -----  Response :  " + response.StatusCode + "|" + response.Content, "VTEX_ProductAPI");

                    return "0|" + response.Content;


                }
                else
                {
                    // ERROR
                    LogSystem.WriteLogDebugDirect(" -----  " + ProductId, "ERROR_Obteniendo_Producto");

                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR obteniendo producto : " + ProductId, "VTEX_ProductAPI");

                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                LogSystem.WriteLogDebugDirect(" -----  " + ProductId, "ERROR_Obteniendo_Producto");
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string UpdateProductwDetail_VTEX(string SellerAccessToken, string IntegratorApiKey, string ProviderID, string ProductId, string MkpSellerID, string VtexProdId, string ProductInVtex)
        {
            try
            {
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                int ActualizarImagenes = 0;

                JObject pr = JObject.Parse(ProductInVtex);

                ActualizarImagenes = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ActualizarImagenes"]);
                string URL_API_VTEX_PRICE = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_suggestion_Product"];

                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
 
                LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000   U P D A T E  V 2.2 - UpdateProductwDetail_VTEX 000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU = " + ProductId, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Provider = " + ProviderID, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Product in VTEX = " + ProductInVtex, "VTEX_ProductAPI");

                // Obtener Parametro si se actualizan imagenes o no.
                try
                {
                    ActualizarImagenes = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ActualizarImagenes"]);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Parametro Actualizar Iamgenes = " + ActualizarImagenes, "VTEX_ProductAPI");

                }
                catch (Exception Mod)
                {
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  ERROR xxxx ---- Parametro Actualizar Iamgenes = " + ActualizarImagenes, "VTEX_ProductAPI");

                }
                string SecurityStock = System.Configuration.ConfigurationManager.AppSettings["ControlStockSeguridad"];
                int StockControl = 0;
                try
                {
                    StockControl = Convert.ToInt32(SecurityStock);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Security Stock Control : " + StockControl, "VTEX_ProductAPI");
                }
                catch (Exception frt)
                {
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  ERROR Security Stock Control. SeurityStock: " + StockControl, "VTEX_ProductAPI");

                }

                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPI);

                var response1 = m_api.Client.Execute(request1);
                ProductApi product;
                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content , "VTEX_ProductAPI");


                    string Product = "{" +
                                        //"  \"Id\":" + VtexProdId + "," +
                                        "  \"Name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"CategoryId\": " + pr.SelectToken("CategoryId").ToString() + "," +
                                        "  \"DepartmentId\": " + pr.SelectToken("DepartmentId").ToString() + "," +
                                        "  \"BrandId\": " + pr.SelectToken("BrandId").ToString() + "," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"Title\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                         // "  \"LinkId\": \"" + (product.ProductType != 1 ? product.Brand + "-" : "") + product.Name.Replace(" ", "-").Replace('"', 'p') + "\"," +
                                         "  \"LinkId\": \"" + pr.SelectToken("LinkId").ToString() + "\","  +

                                        "  \"Description\": \"" + product.Description.Replace("\\n","<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "<br/><br/><b>Términos y Condiciones</b><br>" + product.TyC.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "\"," +
                                        "  \"DescriptionShort\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p').Replace('\t', ' ') + "\"," +

                                        "  \"ReleaseDate\": \"" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "\"," +
                                        "  \"IsVisible\": " + product.Active.ToString().ToLower() + "," +
                                        "  \"IsActive\": " + product.Active.ToString().ToLower() + "," +
                                        "  \"TaxCode\": \"1\" ," +
                                        "  \"MetaTagDescription\":  \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +

                                        "  \"ShowWithoutStock\": false,\"KeyWords\":null,\"SupplierId\":null,\"ListStoreId\":null," +
                                        "  \"AdWordsRemarketingCode\": null,\"LomadeeCampaignCode\":null," +

                                        "  \"Score\": 1 " +

                                        "  }";


                    /*
                                        "  \"ProductSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Fabric\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Cotton\"," +
                                        "        \"Velvet\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +
                                        "  \"SkuSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Color\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Red\"," +
                                        "        \"Blue\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +

                     */
                    var request = CreateRequest("/api/catalog/pvt/product/"+ VtexProdId, Method.PUT);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                    request.AddParameter("application/json", Product, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Enviando actualización de productos. \n" + Product, "VTEX_ProductAPI");

                    var response = Client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Producto actualizado.", "VTEX_ProductAPI");

                        var requestS = CreateRequest("/api/catalog/pvt/stockkeepingunit?refId=" + ProductId, Method.GET);
                        requestS.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        requestS.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        //requestS.AddParameter("application/json", Product, ParameterType.RequestBody);

                        var responseS = Client.Execute(requestS);
                        bool update = false;
                        JObject o_vtexSKU = new JObject();
                        if (responseS.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            o_vtexSKU = JObject.Parse(responseS.Content);
                            update = true;
                            LogSystem.WriteLogDebugDirect(" -----  oooooooo  SKU ENCONTRADO. Modo UPDATE", "VTEX_ProductAPI");
                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXX ERROR  SKU NO ENCONTRADO. MODO CREATE", "VTEX_ProductAPI");
                            
                        }

                        // Nueva lógica - Si el SKU existe para poder hacer una actualización tiene que tener al menos una imagen. Vamos a tratar las imagenes y luego intentamos actualizar. Como el update y Create están en la misma logica, esto solo lo hacemos si update = true
                        if(update)
                        {

                            LogSystem.WriteLogDebugDirect(" -----  oooooooo  Modo UPDATE - Asignando imagenes para poder actualizar y activar SKU", "VTEX_ProductAPI");
                            if (ActualizarImagenes > 0)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  oooooooo  Modo Actualizar imagenes ACTIVO", "VTEX_ProductAPI");
                                string images = "";
                                int imgind = 1;

                                request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtexSKU.SelectToken("Id").ToString() + "/file", Method.DELETE);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   Delete All Imagen : OK - " + response.Content, "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Delete All Imagen : " + response.Content, "VTEX_ProductAPI");

                                }


                                foreach (MG2Connector.Images img in product.Images)
                                {



                                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "VTEX_ProductAPI");


                                    string isMain = "\"isMain\" : " + (imgind == 1 ? "true," : "false,");
                                    images =
                                                 "    {" + isMain +
                                                 "      \"Label\": \"imagen " + product.Sku + "_" + imgind + "\"," +
                                                 "      \"Name\": \"imagen " + product.Sku + "_" + imgind + "\"," +
                                                 "      \"Text\": \"imagen " + product.Sku + "_" + imgind + "\"," +
                                                 "      \"Url\": \"" + img.Url + "\"" +
                                                 "    }";

                                    request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtexSKU.SelectToken("Id").ToString() + "/file", Method.POST);
                                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                    request.AddParameter("application/json", images, ParameterType.RequestBody);
                                    response = Client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Imagen " + imgind + " Creada ", "VTEX_ProductAPI");

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR CREANDO  Imagen " + imgind, "VTEX_ProductAPI");

                                    }
                                    imgind++;

                                }
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de creaciones de Imagenes ", "VTEX_ProductAPI");
                            }
                            else
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXX  Modo Actualizar imagenes INACTIVO --- HAY QUE ACTIVARLO en CONFIG PARA ACTUALIZAR SKU", "VTEX_ProductAPI");


                        }


                        string SKU_product = "{" + (update? "  \"Id\": " + o_vtexSKU.SelectToken("Id").ToString() + "," : "") + 
                                        "  \"Name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"ProductId\": " + VtexProdId + "," +
                                        (update? "  \"IsActive\": " + product.Active.ToString().ToLower() + "," : "" )+
                                        "  \"ActivateIfPossible\": true," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"Ean\": \"" + product.Sku + "\"," +
                                        "  \"IsKit\": false," +
                                        "  \"Height\": " + product.Height.ToString().Replace(",", ".") + "," +
                                        "  \"Width\": " + product.Width.ToString().Replace(",", ".") + "," +
                                        "  \"Length\": " + product.Depth.ToString().Replace(",", ".") + "," +
                                        "  \"Weight\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"WeightKg\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"PackagedHeight\": " + product.Height.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWidth\": " + product.Width.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedLength\": " + product.Depth.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWeight\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWeightKg\": " + Convert.ToInt32((product.Weight * 1000)).ToString().Replace(",", ".") + "," +
                                        "  \"CubicWeight\": 1," +

                                        "  \"CommercialConditionId\":1," +
                                        "  \"MeasurementUnit\": \"un\" ," +

                                        "  \"UnitMultiplier\": 1 ," +
                                        "  \"KitItensSellApart\":  false" +


                                        "  }";
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando SKU . \n" + SKU_product, "VTEX_ProductAPI");

                        if (update)
                        {
                            request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtexSKU.SelectToken("Id").ToString(), Method.PUT);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Actualizando SKU ", "VTEX_ProductAPI");
                        }
                        else
                        {
                            request = CreateRequest("/api/catalog/pvt/stockkeepingunit", Method.POST);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando SKU ", "VTEX_ProductAPI");
                        }
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", SKU_product, ParameterType.RequestBody);

                        
                        response = Client.Execute(request);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU PROCESADO OK.", "VTEX_ProductAPI");
                            JObject o_vtex_sku = JObject.Parse(response.Content);


                            if (ActualizarImagenes>0 && !update)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  oooooooo  Modo CREATE SKU - Asignando imagenes para poder actualizar y activar SKU", "VTEX_ProductAPI");
                                string images = "";
                                int imgind = 1;

                                request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.DELETE);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   Delete All Imagen : OK - " + response.Content , "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Delete All Imagen : " + response.Content, "VTEX_ProductAPI");

                                }


                                foreach (MG2Connector.Images img in product.Images)
                                {



                                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "VTEX_ProductAPI");


                                    string isMain = "\"isMain\" : " + (imgind == 1 ? "true," : "false,");
                                    images =
                                                 "    {" + isMain +
                                                 "      \"Label\": \"imagen " + product.Sku + "_" + imgind + "\"," +
                                                 "      \"Name\": \"imagen " + product.Sku + "_" + imgind + "\"," +
                                                 "      \"Text\": \"imagen " + product.Sku + "_" + imgind + "\"," +
                                                 "      \"Url\": \"" + img.Url + "\"" +
                                                 "    }";

                                    request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.POST);
                                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                    request.AddParameter("application/json", images, ParameterType.RequestBody);
                                    response = Client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Imagen " + imgind + " Creada ", "VTEX_ProductAPI");

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR CREANDO  Imagen " + imgind, "VTEX_ProductAPI");

                                    }
                                    imgind++;

                                }
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de creaciones de Imagenes ", "VTEX_ProductAPI");
                            }
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Precios ", "VTEX_ProductAPI");

                            string Pricing = "{" +
                                                                   "  \"markup\": 0," +
                                                                   "  \"basePrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                                   "  \"listPrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                                   "  \"fixedPrices\": [] " +

                                                                   "  }";
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando PRICE . \n" + Pricing, "VTEX_ProductAPI");


                            Magento m_apiPricing = new Magento(URL_API_VTEX_PRICE);

                            request = m_apiPricing.CreateRequest("/pricing/prices/" + o_vtex_sku.SelectToken("Id").ToString() , Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                            request.AddParameter("application/json", Pricing, ParameterType.RequestBody);
                            response = m_apiPricing.Client.Execute(request);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   PRICE OK ", "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR PRICE ", "VTEX_ProductAPI");

                            }

                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de  Seteo de Precios ", "VTEX_ProductAPI");
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Stock", "VTEX_ProductAPI");

                            string Stock = "{" +
                                                                   "  \"unlimitedQuantity\": false," +
                                                                   "  \"dateUtcOnBalanceSystem\": \"null\"," +
                                                                   "  \"quantity\": " + ((bool)product.Active ? ((product.Stock - StockControl) <=0 ? 0: product.Stock - StockControl):0) + "" +

                                                                   "  }";

                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando Stock . \n" + Stock, "VTEX_ProductAPI");



                            request = CreateRequest("/api/logistics/pvt/inventory/skus/" + o_vtex_sku.SelectToken("Id").ToString() + "/warehouses/1_1", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                            request.AddParameter("application/json", Stock, ParameterType.RequestBody);


                            response = Client.Execute(request);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   Stock OK ", "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Stock ", "VTEX_ProductAPI");

                            }


                            // Asignar propiedades extendidas
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Asignando propiedades a SKU ", "VTEX_ProductAPI");
                            string Activar_Propiedades_Producto = "NO";
                            try
                            {
                                Activar_Propiedades_Producto = System.Configuration.ConfigurationManager.AppSettings["Activar_Propiedades_Producto"];
                            }
                            catch
                            {
                                Activar_Propiedades_Producto = "NO";
                            }
                            LogSystem.WriteLogDebugDirect(" -----  Activar_Propiedades_Producto = " + Activar_Propiedades_Producto, "VTEX_ProductAPI");

                            // MEDIDAS
                            string prop1 = "";
                            if (Activar_Propiedades_Producto == "SI")
                            {
                                request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                prop1 = "{ \"FieldName\": \"Peso\", " +
                                  "\"GroupName\": \"Global\", " +
                                  "\"RootLevelSpecification\": true, " +
                                  "\"FieldValues\": [ " +
                                    "\"" + (product.Weight * 1000).ToString().Replace(",", ".") + " g" + "\" " +
                                  "] " +
                                 "} ";
                                request.AddParameter("application/json", prop1, ParameterType.RequestBody);

                                LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");


                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : PESO --> OK " + response.Content, "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : PESO : " + response.Content, "VTEX_ProductAPI");

                                }

                                request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                prop1 = "{ \"FieldName\": \"Alto\", " +
                                  "\"GroupName\": \"Global\", " +
                                  "\"RootLevelSpecification\": true, " +
                                  "\"FieldValues\": [ " +
                                    "\"" + product.Height.ToString().Replace(",", ".") + " cm" + "\" " +
                                  "] " +
                                 "} ";
                                request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Alto --> OK " + response.Content, "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Alto : PESO : " + response.Content, "VTEX_ProductAPI");

                                }

                                request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                prop1 = "{ \"FieldName\": \"Ancho\", " +
                                  "\"GroupName\": \"Global\", " +
                                  "\"RootLevelSpecification\": true, " +
                                  "\"FieldValues\": [ " +
                                    "\"" + product.Width.ToString().Replace(",", ".") + " cm" + "\" " +
                                  "] " +
                                 "} ";
                                request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Ancho --> OK " + response.Content, "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Ancho : PESO : " + response.Content, "VTEX_ProductAPI");

                                }

                                request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                prop1 = "{ \"FieldName\": \"Profundidad\", " +
                                  "\"GroupName\": \"Global\", " +
                                  "\"RootLevelSpecification\": true, " +
                                  "\"FieldValues\": [ " +
                                    "\"" + product.Depth.ToString().Replace(",", ".") + " cm" + "\" " +
                                  "] " +
                                 "} ";
                                request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Profundidad --> OK " + response.Content, "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Profundidad : PESO : " + response.Content, "VTEX_ProductAPI");

                                }
                            }
                            //****** IVA

                            request = CreateRequest("/api/catalog_system/pvt/products/" + VtexProdId + "/specification", Method.POST);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            prop1 = "[{ \"Name\": \"IVA\", " +
                              "\"Id\": 563, " +
                               "\"Value\": [ " +
                                "\"" + product.Taxes.ToString().Replace(",", ".") + "\" " +
                              "] " +
                             "}, ";
                            //prop1 += "{ \"FieldName\": \"Importe\", " +
                            //  "\"Id\": 569, " +
                            //   "\"Value\": [ " +
                            //    "\"" + product.Name +"\" " +
                            //  "] " +
                            // "}, ";
                            prop1 += "{ \"FieldName\": \"Impuestos internos\", " +
                              "\"Id\": 564, " +
                               "\"Value\": [ " +
                                "\"0\" " +
                              "] " +
                             "} ]";

                            request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : IVA e Impuestos --> OK " + response.Content, "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product IVA e Impuestos : PESO : " + response.Content, "VTEX_ProductAPI");

                            }



                            // ******* DELETE 565 y 566
                            //**************************
                            request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specification", Method.GET);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            LogSystem.WriteLogDebugDirect(" -----  000000000000   Consultando propiedades prod : " + VtexProdId, "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop   --> OK " + response.Content, "VTEX_ProductAPI");
                                JArray o_prop = JArray.Parse(response.Content);
                                foreach (JObject prop in o_prop)
                                {
                                    if (prop.SelectToken("FieldId").ToString() == "565")
                                    {
                                        request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specification/" + prop.SelectToken("Id").ToString(), Method.DELETE);
                                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Eliminando propiedad prod ["+ prop.SelectToken("FieldId").ToString() + " -> "+ prop.SelectToken("Id").ToString() + "] del prod: " + VtexProdId, "VTEX_ProductAPI");
                                        response = Client.Execute(request);
                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : DELETE --> OK " + response.Content, "VTEX_ProductAPI");
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product DELETE : ERROR : " + response.Content, "VTEX_ProductAPI");

                                        }

                                    }
                                    if (prop.SelectToken("FieldId").ToString() == "566")
                                    {
                                        request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specification/" + prop.SelectToken("Id").ToString(), Method.DELETE);
                                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Eliminando propiedad prod [" + prop.SelectToken("FieldId").ToString() + " -> " + prop.SelectToken("Id").ToString() + "] del prod: " + VtexProdId, "VTEX_ProductAPI");
                                        response = Client.Execute(request);
                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : DELETE --> OK " + response.Content, "VTEX_ProductAPI");
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product DELETE : ERROR : " + response.Content, "VTEX_ProductAPI");

                                        }

                                    }
                                    if (prop.SelectToken("FieldId").ToString() == "570")
                                    {
                                        request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specification/" + prop.SelectToken("Id").ToString(), Method.DELETE);
                                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Eliminando propiedad prod [" + prop.SelectToken("FieldId").ToString() + " -> " + prop.SelectToken("Id").ToString() + "] del prod: " + VtexProdId, "VTEX_ProductAPI");
                                        response = Client.Execute(request);
                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : DELETE --> OK " + response.Content, "VTEX_ProductAPI");
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product DELETE : ERROR : " + response.Content, "VTEX_ProductAPI");

                                        }

                                    }
                                    if (prop.SelectToken("FieldId").ToString() == "571")
                                    {
                                        request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specification/" + prop.SelectToken("Id").ToString(), Method.DELETE);
                                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Eliminando propiedad prod [" + prop.SelectToken("FieldId").ToString() + " -> " + prop.SelectToken("Id").ToString() + "] del prod: " + VtexProdId, "VTEX_ProductAPI");
                                        response = Client.Execute(request);
                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : DELETE --> OK " + response.Content, "VTEX_ProductAPI");
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product DELETE : ERROR : " + response.Content, "VTEX_ProductAPI");

                                        }

                                    }
                                }
                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product    " + response.Content, "VTEX_ProductAPI");

                            }


                            // ****** PROPIEDADES
                            

                            if(Activar_Propiedades_Producto == "SI" )
                                foreach (ProductAttribute pa in product.ProductAttributes )
                                {

                                    if(pa.Name.ToUpper() != "EAN" && pa.Name.ToUpper() != "BRAND" && pa.Name.ToUpper() != "IVA" && pa.Name.ToUpper() != "IMPUESTOS INTERNOS" && pa.Name.ToUpper() != "PRIORIDAD" && pa.Name.ToUpper() != "IMPORTE")
                                    {
                                        request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
 
                                        string prop = "{ \"FieldName\": \""+pa.Name+"\", "+ 
                                          "\"GroupName\": \"Global\", "+
                                          "\"RootLevelSpecification\": true, "+
                                          "\"FieldValues\": [ "+
                                            "\""+ pa.Value + "\" "+
                                          "] " + 
                                         "} ";
                                        request.AddParameter("application/json", prop, ParameterType.RequestBody);
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop, "VTEX_ProductAPI");
                                        response = Client.Execute(request);
                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : "+pa.Name+" --> OK " + response.Content, "VTEX_ProductAPI");

                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : " + pa.Name + " : " + response.Content, "VTEX_ProductAPI");

                                        }

                                    }


                                }
                            else
                            {
                                if ((int)product.ProductType == 11
                                    || (int)product.ProductType == 12
                                    || (int)product.ProductType == 14
                                    || (int)product.ProductType == 16
                                    || (int)product.ProductType == 18
                                    || (int)product.ProductType == 26)
                                {
                                    foreach (ProductAttribute pa in product.ProductAttributes)
                                    {

                                        if (pa.Name.ToUpper() == "VIGENCIA" || pa.Name.ToUpper() == "USO" )
                                        {
                                            request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                            string prop = "{ \"FieldName\": \"" + pa.Name + "\", " +
                                              "\"GroupName\": \"Global\", " +
                                              "\"RootLevelSpecification\": true, " +
                                              "\"FieldValues\": [ " +
                                                "\"" + pa.Value + "\" " +
                                              "] " +
                                             "} ";
                                            request.AddParameter("application/json", prop, ParameterType.RequestBody);
                                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop, "VTEX_ProductAPI");
                                            response = Client.Execute(request);
                                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : " + pa.Name + " --> OK " + response.Content, "VTEX_ProductAPI");

                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : " + pa.Name + " : " + response.Content, "VTEX_ProductAPI");

                                            }

                                        }


                                    }
                                    request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                    string prop12 = "{ \"FieldName\": \"Importe\", " +
                                      "\"GroupName\": \"Global\", " +
                                      "\"RootLevelSpecification\": true, " +
                                      "\"FieldValues\": [ " +
                                        "\"" + product.Name + "\" " +
                                      "] " +
                                     "} ";
                                    request.AddParameter("application/json", prop12, ParameterType.RequestBody);
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop12, "VTEX_ProductAPI");
                                    response = Client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Importe --> OK " + response.Content, "VTEX_ProductAPI");

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : Importe : " + response.Content, "VTEX_ProductAPI");

                                    }

                                }
                            }
                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Procesando SKU " + response.StatusCode + ": Error = " + response.Content, "VTEX_ProductAPI");

                        }

                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  Producto Error." + response.StatusCode + ": Error = " + response.Content , "VTEX_ProductAPI");

                    }
                    return response.StatusCode.ToString();


                }
                else
                {
                    // ERROR
                    LogSystem.WriteLogDebugDirect(" -----  No hay productos para publicar o error al obtener producto --> " + ProductId, "VTEX_ProductAPI");

                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string UpdateProductwDetail_VTEXbySeller(string SellerAccessToken, string IntegratorApiKey, string ProviderID, string ProductId, string MkpSellerID, string VtexProdId, string ProductInVtex)
        {
            try
            {
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                string URL_API_VTEX_PRICE = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_suggestion_Product"];
                string Vtex_Seller_Id = System.Configuration.ConfigurationManager.AppSettings["Vtex_Seller_Id"];
                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];
                string URL_VTEX_TokenImages = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_TokenImages"];
                string URL_VTEX_app_IO = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_app_IO"];
                string PathImagenes = System.Configuration.ConfigurationManager.AppSettings["PathImagenes"];
                string VTEX_Warehouse = System.Configuration.ConfigurationManager.AppSettings["VTEX_Warehouse"]; 

                LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000000000000000000000000  U P D A T E   000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU = " + ProductId, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Provider = " + ProviderID, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Product in VTEX = " + ProductInVtex, "VTEX_ProductAPI");

                JObject pivtex = JObject.Parse(ProductInVtex);




                string SecurityStock = System.Configuration.ConfigurationManager.AppSettings["ControlStockSeguridad"];
                int StockControl = 0;
                try
                {
                    StockControl = Convert.ToInt32(SecurityStock);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Security Stock Control : " + StockControl, "VTEX_ProductAPI");
                }
                catch (Exception frt)
                {
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  ERROR Security Stock Control. SeurityStock: " + StockControl, "VTEX_ProductAPI");

                }



                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPI);

                var response1 = m_api.Client.Execute(request1);

                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "VTEX_ProductAPI");

                    if (product.CategoryPath == "")
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN SC", "VTEX_ProductAPI");

                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku , "SIN_Categoria_SC");

                        return "23|ERROR - CATEGORIA NO PRESENTE EN SC";
                    }


                    // Buscar / Crear brand en VTEX
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Buscando BRAND ", "VTEX_ProductAPI");

                    string brand = "";
                    var request = CreateRequest("api/catalog-seller-portal/brands?q=&from=1&to=1&orderBy=status,asc;name,asc&name=" + product.Brand, Method.GET);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                    var responseb = Client.Execute(request);
                    if (responseb.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  FIN BUSQUEDA BRAND.", "VTEX_ProductAPI");

                        JObject o_brand = JObject.Parse(responseb.Content);
                        brand = o_brand.SelectToken("id").ToString();

                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  CREANDO BRAND ", "VTEX_ProductAPI");

                        request = CreateRequest("api/catalog-seller-portal/brands", Method.POST);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                        string json = "{\r\n\"name\": \"" + product.Brand + "\",\r\n\"isActive\": true }";

                        request.AddParameter("application/json", json, ParameterType.RequestBody);

                        responseb = Client.Execute(request);
                        if (responseb.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  OK BRAND.", "VTEX_ProductAPI");

                            JObject o_brand = JObject.Parse(responseb.Content);
                            brand = o_brand.SelectToken("id").ToString();

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  ERROR - No se pudo crear BRAND en VTEX.", "VTEX_ProductAPI");
                            LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_CREANDO_BRAND_VTEX");

                            return "32|ERROR - No se pudo crear BRAND en VTEX";
                        }
                    }
                    // *****************************


                    // Buscar / Crear categoria en VTEX
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Buscando CATEGORY " + product.CategoryBranch[0].Name, "VTEX_ProductAPI");

                    string categ = "NEW";
                    request = CreateRequest("api/catalog-seller-portal/category-tree" /*/categories/" + product.CategoryBranch[0].Code */, Method.GET);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                    responseb = Client.Execute(request);
                    if (responseb.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  FIN Obtención ARBOL de CATEGORIAS.", "VTEX_ProductAPI");
                        JObject o_cat = JObject.Parse(responseb.Content);

                        int Cant = (int)o_cat.SelectToken("roots").Count();

                        bool EncontreCategoria = false;
                        for (int i = 0; i < Cant; i++)
                        {
                            try
                            {
                                int Cant1 = o_cat.SelectToken("roots")[i]["children"].Count();
                                for (int j = 0; j < Cant1; j++)
                                {
                                    try
                                    {
                                        int Cant2 = o_cat.SelectToken("roots")[i]["children"][j]["children"].Count();
                                        for (int jj = 0; jj < Cant1; jj++)
                                        {
                                            try
                                            {
                                                if (o_cat.SelectToken("roots")[i]["children"][j]["children"][jj]["value"]["name"].ToString() == product.CategoryBranch[0].Name)
                                                {

                                                    categ = o_cat.SelectToken("roots")[i]["children"][j]["children"][jj]["value"]["id"].ToString();
                                                    LogSystem.WriteLogDebugDirect(" oooooooo  CATEGORIA OK", "VTEX_ProductAPI");
                                                    EncontreCategoria = true;
                                                    break;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                LogSystem.WriteLogDebugDirect(" -----  xxxxxxx Error nivel 3 de categorias.", "VTEX_ProductAPI");

                                            }

                                        }
                                        if (EncontreCategoria)
                                            break;
                                    }
                                    catch (Exception e)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  xxxxxxx Error nivel 2 de categorias.", "VTEX_ProductAPI");

                                    }

                                }
                            }
                            catch (Exception e)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  xxxxxxx Error nivel 1 de categorias.", "VTEX_ProductAPI");

                            }

                            if (EncontreCategoria)
                                break;
                        }


                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN VTEX", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_CATEGORIA_NO_PRESENTE_EN_VTEX");

                        return "33|RROR - CATEGORIA NO PRESENTE EN VTEX";
                    }

                    if(categ == "NEW")
                    {

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXXXXX  ERROR - CATEGORIA NO PRESENTE EN VTEX", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_CATEGORIA_NO_PRESENTE_EN_VTEX");
                        return "33|ERROR - CATEGORIA NO PRESENTE EN VTEX";


                        /*                        LogSystem.WriteLogDebugDirect(" -----  000000000000  CREANDO CATEGORIA ", "VTEX_ProductAPI");

                                                request = CreateRequest("/api/catalog-seller-portal/category-tree/categories", Method.POST);
                                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                                string json = "{\r\n\"name\": \"" + product.Brand + "\",\r\n\"isActive\": true }";

                                                request.AddParameter("application/json", json, ParameterType.RequestBody);

                                                responseb = Client.Execute(request);
                                                if (responseb.StatusCode == System.Net.HttpStatusCode.Created)
                                                {
                                                    LogSystem.WriteLogDebugDirect(" -----  000000000000  OK BRAND.", "VTEX_ProductAPI");

                                                    JObject o_brand = JObject.Parse(responseb.Content);
                                                    brand = o_brand.SelectToken("id").ToString();

                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebugDirect(" -----  ERROR - No se pudo crear BRAND en VTEX.", "VTEX_ProductAPI");
                                                    return "32|ERROR - No se pudo crear BRAND en VTEX";
                                                }
                        */
                    }
                    
                    // ********************************


                    // Armar Atributos 
                    string attributes = "[ ";
                    int pu = 0;
                    string prop = "";
                    foreach (ProductAttribute pa in product.ProductAttributes)
                    {

                        if (pa.Name.ToUpper() != "EAN" && pa.Name.ToUpper() != "BRAND" && pa.Name.ToUpper() != "PRIORIDAD")
                        {
                            if (pu > 0)
                                prop += ",";

                            prop += "{ \"name\": \"" + pa.Name + "\", " +

                              "\"value\":  " +
                                "\"" + pa.Value + "\" " +

                             "} ";
                        }
                        pu++;

                    }
                    if (pu > 0)
                        prop += ",";

                    prop += "{ \"name\": \"IIN\", " +

                      "\"value\":  " +
                        "\"NO\" " +

                     "}, ";

                    prop += "{ \"name\": \"III\", " +

                      "\"value\":  " +
                        "\"0\" " +

                     "}, ";

                    prop += "{ \"name\": \"IVA\", " +

                      "\"value\":  " +
                        "\"" + product.Taxes  + "\" " +

                     "} ";

                    attributes += prop + "]";

                    // ***************




                    // Armar imagenes
                    // --------------
                    string imagesJ = "[";
                    string imagesS = imagesJ;


                    string images = "";
                    string tokenImages = "";
                    int imgind = 1;


                    // 1 - Obtengo KEY
                    Magento v_api = new Magento(URL_VTEX_TokenImages);
                    request = v_api.CreateRequest("/api/vtexid/apptoken/login?an=" + Vtex_Seller_Id, Method.POST);
                    //request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    //request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                    string app_json = "{\r\n    \"appkey\": \"" + IntegratorApiKey + "\",\r\n    \"apptoken\": \"" + SellerAccessToken + "\"\r\n}";

                    request.AddParameter("application/json", app_json, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Solicitando Key de Catalog Images. \n" + app_json, "VTEX_ProductAPI");

                    var responseI = v_api.Client.Execute(request);
                    if (responseI.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Respuesta Key Catalog Images.", "VTEX_ProductAPI");

                        JObject i_vtex = JObject.Parse(responseI.Content);
                        tokenImages = i_vtex.SelectToken("token").ToString();

                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  ERROR  Respuesta Key Catalog Images. --> " + response1.Content, "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  " + product.Sku, "ERROR_NO_SE_PUEDE_OBTENER_KEY_IMAGENES_VTEX");

                        return "34|ERROR - NO SE PUEDE OBTENER KEY IMAGENS DE VTEX";
                    }


                    Magento IO_api = new Magento(URL_VTEX_app_IO);


                    foreach (MG2Connector.Images img in product.Images)
                    {
                        // revisando si las imagenes existen
                        // *********************************

                        // 2 - Cargo imagen
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "VTEX_ProductAPI");

                        request = IO_api.CreateRequest("/vtex.catalog-images/v0/" + Vtex_Seller_Id + "/master/images/save/" + product.Sku + "_" + imgind + ".jpg", Method.POST);
                        request.AddHeader("VtexIdclientAutCookie", tokenImages);

                        if (imgind == 1)
                            request.AddFile("", PathImagenes + "\\" + product.Sku + "_b.jpg");
                        else
                            request.AddFile("", PathImagenes + "\\" + product.Sku + "_b_" + imgind + ".jpg");

                        //request.AddFile("", PathImagenes + "\\" + product.Sku + "_" + imgind + ".jpg");
                        string UrlImages = "";
                        JObject img_vtex;
                        var responseIO = IO_api.Client.Execute(request);
                        if (responseIO.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  ooooooo Imagen subida ", "VTEX_ProductAPI");
                            img_vtex = JObject.Parse(responseIO.Content);
                            UrlImages = img_vtex.SelectToken("fullUrl").ToString();

                        }
                        else
                        {
                            if (responseIO.StatusCode == System.Net.HttpStatusCode.Conflict)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  ooooooo Imagen ya existe ", "VTEX_ProductAPI");
                                img_vtex = JObject.Parse(responseIO.Content);
                                UrlImages = img_vtex.SelectToken("file").ToString();

                            }
                        }

                        if (imgind > 1)
                        {
                            images += ",";
                            imagesS += ",";

                        }
                        imagesS += "\"" + product.Sku + "_" + imgind + ".jpg\"";
                        images +=
                                     "    {" +
                                     "      \"id\": \"" + product.Sku + "_" + imgind + ".jpg\"," +
                                     "      \"alt\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                     "      \"url\": \"" + UrlImages + "\"" +
                                     "    }";

                    }
                    imagesJ += images + "]";
                    imagesS += "]";
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de creaciones de Imagenes ", "VTEX_ProductAPI");

                    // **************


                    // Armar Variaciones / SKUS

                    string SKU_product = "[{" +
                             "  \"id\": \"" + pivtex.SelectToken("skus[0].id").ToString() + "\"," +
                             "  \"name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                             "  \"isActive\": true," +
                             "  \"externalId\": \"" + product.Sku + "\"," +
                             "  \"dimensions\": {" +
                             "  \"height\": " + (product.Height == 0 ? (decimal)0.01 : product.Height).ToString().Replace(",", ".") + "," +
                             "  \"width\": " + (product.Width == 0 ? (decimal)0.01 : product.Width).ToString().Replace(",", ".") + "," +
                             "  \"length\": " + (product.Depth == 0 ? (decimal)0.01 : product.Depth).ToString().Replace(",", ".") + "" +
                             "  }, \"weight\": " + ((product.Weight == 0 ? (decimal)0.01 : product.Weight) * 1000).ToString().Replace(",", ".") + "," +
                             "  \"specs\":[], " +
                             "  \"images\": " + imagesS +
                             "  }]";


                    //Armar especificaciones
                    string specs = "[]";

                    // *********************


                    // ************************


                    // Actualizar Producto
                    string Product = "{" +
                                        "  \"id\": \"" + VtexProdId + "\"," +
                                        "  \"name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name.Replace('"', 'p') + "\"," +
                                        "  \"categoryIds\": [\"" + categ + "\"]," +
                                        "  \"brandId\": \"" + brand + "\"," +
                                        "  \"externalId\": \"" + product.Sku + "\"," +
                                        "  \"slug\": \"/" + product.Sku + "\"," +
                                        "  \"origin\": \""+ Vtex_Seller_Id + "\"," + // Poner parametro para cuando necesitaemos conectar otr MKP de VTEX
                                        "  \"description\": \"" + product.Description.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "<br/><br/><b >Términos y Condiciones</b><br>" + product.TyC.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ').Replace('\t', ' ') +
                                        "\"," +

                                        "  \"transportModal\": \"\"," +
                                        "  \"status\": \"" + (product.Active == true ? "active" : "inactive") + "\" ," +
                                        "  \"taxCode\": \"1\" ," +
                                        "\"specs\":" + specs + "," +
                                        "\"attributes\":" + attributes + "," +
                                        "\"images\":" + imagesJ + "," +
                                        "\"skus\":" + SKU_product + "" +

                                        "  }";


                    request = CreateRequest("api/catalog-seller-portal/products/" + VtexProdId, Method.PUT);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                    request.AddParameter("application/json", Product, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando productos. \n" + Product, "VTEX_ProductAPI");

                    var response = Client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Producto actualizado.", "VTEX_ProductAPI");

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Precios ", "VTEX_ProductAPI");

                        string Pricing = "{" +
                                                               "  \"markup\": 0," +
                                                               "  \"basePrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                               "  \"listPrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                               "  \"fixedPrices\": [] " +

                                                               "  }";
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX  Precios " + Pricing, "VTEX_ProductAPI");


                        Magento m_apiPricing = new Magento(URL_API_VTEX_PRICE);

                        //request = m_apiPricing.CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.POST);
                        request = m_apiPricing.CreateRequest("/pricing/prices/" + VtexProdId, Method.PUT);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", Pricing, ParameterType.RequestBody);
                        response = m_apiPricing.Client.Execute(request);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   PRICE OK ", "VTEX_ProductAPI");

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR PRICE ", "VTEX_ProductAPI");

                        }

                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de  Seteo de Precios ", "VTEX_ProductAPI");
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Stock", "VTEX_ProductAPI");

                        string Stock = "{" +
                                                               "  \"unlimitedQuantity\": false," +
                                                               "  \"dateUtcOnBalanceSystem\": \"null\"," +
                                                               "  \"quantity\": " + ((product.Stock - StockControl) <= 0 ? 0 : product.Stock - StockControl) + "" +

                                                               "  }";




                        request = CreateRequest("/api/logistics/pvt/inventory/skus/" + VtexProdId + "/warehouses/" + VTEX_Warehouse, Method.PUT);
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", Stock, ParameterType.RequestBody);
                        response = Client.Execute(request);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   Stock OK ", "VTEX_ProductAPI");

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Stock ", "VTEX_ProductAPI");

                        }


                        // Asignar propiedades extendidas
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Asignando propiedades a SKU ", "VTEX_ProductAPI");



                    }
                    LogSystem.WriteLogDebugDirect(" -----  Response :  " + response.StatusCode + "|" + response.Content, "VTEX_ProductAPI");

                    return "0|" + response.Content;


                }
                else
                {
                    // ERROR
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR obteniendo producto : " + ProductId, "VTEX_ProductAPI");

                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR obteniendo producto : " + ProductId + "\n\n" + f.Message + f.InnerException + f.Source , "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  " + ProductId, "ERROR_Obteniendo_Producto");

                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        public string UpdateProduct_VTEX(string SellerAccessToken, string IntegratorApiKey, string ProviderID, string ProductId, string MkpSellerID, string VtexProdId)
        {
            try
            {
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                int ActualizarImagenes = 0;


                ActualizarImagenes = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ActualizarImagenes"]);
                string URL_API_VTEX_PRICE = System.Configuration.ConfigurationManager.AppSettings["URL_VTEX_suggestion_Product"];

                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                LogSystem.WriteLogDebugDirect(" -----  0000000000000000000000000000000000   U P D A T E  V 2.1 000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000  -----", "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU = " + ProductId, "VTEX_ProductAPI");
                LogSystem.WriteLogDebugDirect(" -----  000000000000  Provider = " + ProviderID, "VTEX_ProductAPI");

                // Obtener Parametro si se actualizan imagenes o no.
                try
                {
                    ActualizarImagenes = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ActualizarImagenes"]);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Parametro Actualizar Iamgenes = " + ActualizarImagenes, "VTEX_ProductAPI");

                }
                catch (Exception Mod)
                {
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  ERROR xxxx ---- Parametro Actualizar Iamgenes = " + ActualizarImagenes, "VTEX_ProductAPI");

                }
                string SecurityStock = System.Configuration.ConfigurationManager.AppSettings["ControlStockSeguridad"];
                int StockControl = 0;
                try
                {
                    StockControl = Convert.ToInt32(SecurityStock);
                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Security Stock Control : " + StockControl, "VTEX_ProductAPI");
                }
                catch (Exception frt)
                {
                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  ERROR Security Stock Control. SeurityStock: " + StockControl, "VTEX_ProductAPI");

                }

                Magento m_api = new Magento(URL_API_CTC);


                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPI);

                var response1 = m_api.Client.Execute(request1);
                ProductApi product;
                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());

                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Evaluando Producto de SC : " + response1.Content, "VTEX_ProductAPI");


                    string Product = "{" +
                                        "  \"Id\":" + VtexProdId + "," +
                                        "  \"Name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name + "\"," +
                                        "  \"CategoryPath\": \"" + product.CategoryPath + "\"," +
                                        "  \"BrandName\": \"" + product.Brand + "\"," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"Title\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name + "\"," +
                                        "  \"LinkId\": \"" + (product.ProductType != 1 ? product.Brand + "-" : "") + product.Name.Replace(" ", "-") + "\"," +
                                        "  \"Description\": \"" + product.Description.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ') +
                                        "<br/><br/><b>Términos y Condiciones</b><br>" + product.TyC.Replace("\\n", "<br/>").Replace("\n", "<br/>").Replace('"', ' ').Replace('\r', ' ') +
                                        "\"," +

                                        "  \"ReleaseDate\": \"" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "\"," +
                                        "  \"IsVisible\": " + product.Active.ToString().ToLower() + "," +
                                        "  \"IsActive\": " + product.Active.ToString().ToLower() + "," +
                                        "  \"TaxCode\": 1 ," +
                                        "  \"MetaTagDescription\":  \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name + "\"," +

                                        "  \"ShowWithoutStock\": " + 0 + "," +
                                        "  \"AdWordsRemarketingCode\": null," +

                                        "  \"Score\": 1 " +

                                        "  }";


                    /*
                                        "  \"ProductSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Fabric\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Cotton\"," +
                                        "        \"Velvet\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +
                                        "  \"SkuSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Color\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Red\"," +
                                        "        \"Blue\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +

                     */
                    var request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId, Method.PUT);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                    request.AddParameter("application/json", Product, ParameterType.RequestBody);


                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando productos. \n" + Product, "VTEX_ProductAPI");

                    var response = Client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Producto Creado.", "VTEX_ProductAPI");

                        var requestS = CreateRequest("/api/catalog/pvt/stockkeepingunit?refId=" + ProductId, Method.GET);
                        requestS.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        requestS.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        //requestS.AddParameter("application/json", Product, ParameterType.RequestBody);

                        var responseS = Client.Execute(requestS);
                        bool update = false;
                        JObject o_vtexSKU = new JObject();
                        if (responseS.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            o_vtexSKU = JObject.Parse(responseS.Content);
                            update = true;
                            LogSystem.WriteLogDebugDirect(" -----  oooooooo  SKU ENCONTRADO. Modo UPDATE", "VTEX_ProductAPI");
                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXX ERROR  SKU NO ENCONTRADO. MODO CREATE", "VTEX_ProductAPI");

                        }

                        string SKU_product = "{" + (update ? "  \"Id\": " + o_vtexSKU.SelectToken("Id").ToString() + "," : "") +
                                        "  \"Name\": \"" + (product.ProductType != 1 ? product.Brand + " - " : "") + product.Name + "\"," +
                                        "  \"ProductId\": " + VtexProdId + "," +
                                        "  \"ActivateIfPossible\": true," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"Ean\": \"" + product.Sku + "\"," +
                                        "  \"IsKit\": false," +
                                        "  \"Height\": " + product.Height.ToString().Replace(",", ".") + "," +
                                        "  \"Width\": " + product.Width.ToString().Replace(",", ".") + "," +
                                        "  \"Length\": " + product.Depth.ToString().Replace(",", ".") + "," +
                                        "  \"Weight\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"WeightKg\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"PackagedHeight\": " + product.Height.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWidth\": " + product.Width.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedLength\": " + product.Depth.ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWeight\": " + (product.Weight * 1000).ToString().Replace(",", ".") + "," +
                                        "  \"PackagedWeightKg\": " + Convert.ToInt32((product.Weight * 1000)).ToString().Replace(",", ".") + "," +
                                        "  \"CubicWeight\": 1," +
                                        (update ? "  \"IsActive\": " + product.Active.ToString().ToLower() + "," : "") +

                                        "  \"CommercialConditionId\":1," +
                                        "  \"MeasurementUnit\": \"un\" ," +

                                        "  \"UnitMultiplier\": 1 ," +
                                        "  \"KitItensSellApart\":  false" +


                                        "  }";
                        LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando SKU . \n" + SKU_product, "VTEX_ProductAPI");

                        if (update)
                        {
                            request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtexSKU.SelectToken("Id").ToString(), Method.PUT);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Actualizando SKU ", "VTEX_ProductAPI");
                        }
                        else
                        {
                            request = CreateRequest("/api/catalog/pvt/stockkeepingunit", Method.POST);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando SKU ", "VTEX_ProductAPI");
                        }
                        request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                        request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                        request.AddParameter("application/json", SKU_product, ParameterType.RequestBody);


                        response = Client.Execute(request);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            LogSystem.WriteLogDebugDirect(" -----  000000000000  SKU PROCESADO OK.", "VTEX_ProductAPI");
                            JObject o_vtex_sku = JObject.Parse(response.Content);


                            if (ActualizarImagenes > 0)
                            {
                                string images = "";
                                int imgind = 1;

                                request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.DELETE);
                                request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                response = Client.Execute(request);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   Delete All Imagen : OK - " + response.Content, "VTEX_ProductAPI");

                                }
                                else
                                {
                                    LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Delete All Imagen : " + response.Content, "VTEX_ProductAPI");

                                }


                                foreach (MG2Connector.Images img in product.Images)
                                {



                                    LogSystem.WriteLogDebugDirect(" -----  000000000000  Creando Imagen " + imgind, "VTEX_ProductAPI");


                                    string isMain = "\"isMain\" : " + (imgind == 1 ? "true," : "false,");
                                    images =
                                                 "    {" + isMain +
                                                 "      \"Label\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                                 "      \"Name\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                                 "      \"Text\": \"imagen " + product.Sku + "_" + imgind++ + "\"," +
                                                 "      \"Url\": \"" + img.Url + "\"" +
                                                 "    }";

                                    request = CreateRequest("/api/catalog/pvt/stockkeepingunit/" + o_vtex_sku.SelectToken("Id").ToString() + "/file", Method.POST);
                                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                                    request.AddParameter("application/json", images, ParameterType.RequestBody);
                                    response = Client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000   Imagen " + imgind + " Creada ", "VTEX_ProductAPI");

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR CREANDO  Imagen " + imgind, "VTEX_ProductAPI");

                                    }


                                }
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de creaciones de Imagenes ", "VTEX_ProductAPI");
                            }
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Precios ", "VTEX_ProductAPI");

                            string Pricing = "{" +
                                                                   "  \"markup\": 0," +
                                                                   "  \"basePrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                                   "  \"listPrice\": " + product.Price.ToString().Replace(",", ".") + "," +
                                                                   "  \"fixedPrices\": [] " +

                                                                   "  }";
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando PRICE . \n" + Pricing, "VTEX_ProductAPI");


                            Magento m_apiPricing = new Magento(URL_API_VTEX_PRICE);

                            request = m_apiPricing.CreateRequest("/pricing/prices/" + o_vtex_sku.SelectToken("Id").ToString(), Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                            request.AddParameter("application/json", Pricing, ParameterType.RequestBody);
                            response = m_apiPricing.Client.Execute(request);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   PRICE OK ", "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR PRICE ", "VTEX_ProductAPI");

                            }

                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Saliendo de  Seteo de Precios ", "VTEX_ProductAPI");
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX Iniciando Seteo de Stock", "VTEX_ProductAPI");

                            string Stock = "{" +
                                                                   "  \"unlimitedQuantity\": false," +
                                                                   "  \"dateUtcOnBalanceSystem\": \"null\"," +
                                                                   "  \"quantity\": " + ((bool)product.Active ? ((product.Stock - StockControl) <= 0 ? 0 : product.Stock - StockControl) : 0) + "" +

                                                                   "  }";

                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Cargando Stock . \n" + Stock, "VTEX_ProductAPI");



                            request = CreateRequest("/api/logistics/pvt/inventory/skus/" + o_vtex_sku.SelectToken("Id").ToString() + "/warehouses/1_1", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                            request.AddParameter("application/json", Stock, ParameterType.RequestBody);


                            response = Client.Execute(request);

                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000   Stock OK ", "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Stock ", "VTEX_ProductAPI");

                            }


                            // Asignar propiedades extendidas
                            LogSystem.WriteLogDebugDirect(" -----  000000000000  Asignando propiedades a SKU ", "VTEX_ProductAPI");

                            // MEDIDAS
                            request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            string prop1 = "{ \"FieldName\": \"Peso\", " +
                              "\"GroupName\": \"Global\", " +
                              "\"RootLevelSpecification\": true, " +
                              "\"FieldValues\": [ " +
                                "\"" + (product.Weight * 1000).ToString().Replace(",", ".") + " g" + "\" " +
                              "] " +
                             "} ";
                            request.AddParameter("application/json", prop1, ParameterType.RequestBody);

                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");


                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : PESO --> OK " + response.Content, "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : PESO : " + response.Content, "VTEX_ProductAPI");

                            }

                            request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            prop1 = "{ \"FieldName\": \"Alto\", " +
                              "\"GroupName\": \"Global\", " +
                              "\"RootLevelSpecification\": true, " +
                              "\"FieldValues\": [ " +
                                "\"" + product.Height.ToString().Replace(",", ".") + " cm" + "\" " +
                              "] " +
                             "} ";
                            request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Alto --> OK " + response.Content, "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Alto : PESO : " + response.Content, "VTEX_ProductAPI");

                            }

                            request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            prop1 = "{ \"FieldName\": \"Ancho\", " +
                              "\"GroupName\": \"Global\", " +
                              "\"RootLevelSpecification\": true, " +
                              "\"FieldValues\": [ " +
                                "\"" + product.Width.ToString().Replace(",", ".") + " cm" + "\" " +
                              "] " +
                             "} ";
                            request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Ancho --> OK " + response.Content, "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Ancho : PESO : " + response.Content, "VTEX_ProductAPI");

                            }

                            request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                            request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                            request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                            prop1 = "{ \"FieldName\": \"Profundidad\", " +
                              "\"GroupName\": \"Global\", " +
                              "\"RootLevelSpecification\": true, " +
                              "\"FieldValues\": [ " +
                                "\"" + product.Depth.ToString().Replace(",", ".") + " cm" + "\" " +
                              "] " +
                             "} ";
                            request.AddParameter("application/json", prop1, ParameterType.RequestBody);
                            LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop1, "VTEX_ProductAPI");
                            response = Client.Execute(request);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : Profundidad --> OK " + response.Content, "VTEX_ProductAPI");

                            }
                            else
                            {
                                LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Profundidad : PESO : " + response.Content, "VTEX_ProductAPI");

                            }

                            //******

                            foreach (ProductAttribute pa in product.ProductAttributes)
                            {

                                if (pa.Name.ToUpper() != "EAN" && pa.Name.ToUpper() != "BRAND")
                                {
                                    request = CreateRequest("/api/catalog/pvt/product/" + VtexProdId + "/specificationvalue", Method.PUT);
                                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);

                                    string prop = "{ \"FieldName\": \"" + pa.Name + "\", " +
                                      "\"GroupName\": \"Global\", " +
                                      "\"RootLevelSpecification\": true, " +
                                      "\"FieldValues\": [ " +
                                        "\"" + pa.Value + "\" " +
                                      "] " +
                                     "} ";
                                    request.AddParameter("application/json", prop, ParameterType.RequestBody);
                                    LogSystem.WriteLogDebugDirect(" -----  000000000000   enviando propiedad : " + prop, "VTEX_ProductAPI");
                                    response = Client.Execute(request);
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  000000000000 Product Prop : " + pa.Name + " --> OK " + response.Content, "VTEX_ProductAPI");

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Product Prop : " + pa.Name + " : " + response.Content, "VTEX_ProductAPI");

                                    }

                                }


                            }

                        }
                        else
                        {
                            LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXXX ERROR Procesando SKU " + response.StatusCode + ": Error = " + response.Content, "VTEX_ProductAPI");

                        }

                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect(" -----  XXXXXXXXXXX  Producto Error." + response.StatusCode + ": Error = " + response.Content, "VTEX_ProductAPI");

                    }
                    return response.StatusCode.ToString();


                }
                else
                {
                    // ERROR
                    LogSystem.WriteLogDebugDirect(" -----  No hay productos para publicar o error al obtener producto --> " + ProductId, "VTEX_ProductAPI");

                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }

        public string PutSuggestionBy_Sku_VTEX(string SellerAccessToken, string IntegratorApiKey, string ProviderID, string ProductId, string MkpSellerID)
        {
            try
            {
                string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];
                string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                Magento m_api = new Magento(URL_API_CTC);

             
                // Enviar sugerencia de producto producto

                var request1 = m_api.CreateRequest("/providers/" + ProviderID + "/products/" + ProductId, Method.GET, TokenAPI);

                var response1 = m_api.Client.Execute(request1);

                if (response1.StatusCode == HttpStatusCode.OK)
                {

                    JObject o = JObject.Parse(response1.Content);

                    ProductApi product = JsonConvert.DeserializeObject<ProductApi>(o.SelectToken("Result").ToString());



                    string Product = "{" +
                                        "  \"ProductId\": \"" + product.Sku + "\"," +
                                        "  \"ProductName\": \"" + product.Name + "\"," +
                                        "  \"NameComplete\": \"" + product.Name + "\"," +
                                        "  \"ProductDescription\": \"" + product.Description + "\"," +
                                        "  \"BrandName\": \"" + product.Brand + "\"," +
                                        "  \"SkuName\": \"" + product.Name + "\"," +
                                        "  \"SellerId\": \"" + MkpSellerID + "\"," +
                                        "  \"Height\": " + product.Height + "," +
                                        "  \"Width\": " + product.Width + "," +
                                        "  \"Length\": " + product.Depth + "," +
                                        "  \"Weight\": " + product.Weight + "," +
                                        "  \"Updated\": null," +
                                        "  \"RefId\": \"" + product.Sku + "\"," +
                                        "  \"SellerStockKeepingUnitId\": " + product.Stock + "," +
                                        "  \"CategoryFullPath\": \"Category 1\"," +
                                         "  \"EAN\": \"" + product.Sku + "\"," +
                                        "  \"MeasurementUnit\": \"un\"," +
                                        "  \"UnitMultiplier\": 1," +
                                        "  \"AvailableQuantity\": " + product.Stock + "," +
                                        "  \"Pricing\": {" +
                                        "    \"Currency\": \"ARG\"," +
                                        "    \"SalePrice\": " + product.Price + "," +
                                        "    \"CurrencySymbol\": \"$\"" +
                                        "  }";

                    string images = "";
                    int imgind = 1;
                    foreach (MG2Connector.Images img in product.Images)
                    {
                        if (imgind > 1)
                            images = images + ",";
                        images +=
                                     "    {" +
                                     "      \"imageName\": \"imagen " + imgind++ + "\"," +
                                     "      \"imageUrl\": \"" + img.Url + "\"" +
                                     "    }";

                    }
                    if (images != "")
                    {
                        images = ",  \"Images\": [" + images;
                        images = images + "  ]";
                    }

                    Product += images + "}";


                    /*
                                        "  \"ProductSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Fabric\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Cotton\"," +
                                        "        \"Velvet\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +
                                        "  \"SkuSpecifications\": [" +
                                        "    {" +
                                        "      \"fieldName\": \"Color\"," +
                                        "      \"fieldValues\": [" +
                                        "        \"Red\"," +
                                        "        \"Blue\"" +
                                        "      ]" +
                                        "    }" +
                                        "  ]," +

                     */
                    var request = CreateRequest("/" + MkpSellerID + "/" + ProductId, Method.PUT);
                    request.AddHeader("X-VTEX-API-AppKey", IntegratorApiKey);
                    request.AddHeader("X-VTEX-API-AppToken", SellerAccessToken);
                    request.AddParameter("application/json", Product, ParameterType.RequestBody);



                    var response = Client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return "0";
                    }
                    return response.StatusCode.ToString();


                }
                else
                {
                    // ERROR

                }
                return "-1|No se encontraron productos";
            }
            catch (Exception f)
            {
                return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
            }
        }
        /*
          public string GetBy_CatalogFJ_List(string Catalog, string ProviderID)
          {
              try
              {

                  var request = CreateRequest("/query/publication?provider_id=" + ProviderID + "&sku=" + sku, Method.GET);

                  var response = Client.Execute(request);
                  List<string> pList = null;
                  if (response.StatusCode == System.Net.HttpStatusCode.OK)
                  {
                      string product = response.Content;
                      //List<string> pList = JsonConvert.DeserializeObject<List<string>>(product);
                      JObject o = JObject.Parse(product);

                      //JsonTextReader reader = new JsonTextReader(new StringReader(product));

                      //while (reader.Read())
                      //{
                      //    if (reader.Value != null)
                      //    {
                      //        Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                      //    }
                      //    else
                      //    {
                      //        Console.WriteLine("Token: {0}", reader.TokenType);
                      //    }
                      //}
                      if ((Boolean)o.SelectToken("success"))
                      {
                          CategoryBranch CB = new CategoryBranch();
                          CB.Name = (string)o.SelectToken("result.product.publications[0].marketplaceCategory.name");
                          CB.Code = (string)o.SelectToken("result.product.publications[0].marketplaceCategory.code");
                          List<CategoryBranch> LCB = new List<CategoryBranch>();
                          LCB.Add(CB);

                          decimal Height = Math.Round((decimal)o.SelectToken("result.product.height"), 2, MidpointRounding.AwayFromZero);
                          decimal Width = Math.Round((decimal)o.SelectToken("result.product.width"), 2, MidpointRounding.AwayFromZero);
                          decimal Depth = Math.Round((decimal)o.SelectToken("result.product.depth"), 2, MidpointRounding.AwayFromZero);
                          decimal Weight = Math.Round((decimal)o.SelectToken("result.product.weight"), 2, MidpointRounding.AwayFromZero);

                          Object objJSON = new ProductApi()
                          {
                              Sku = sku,
                              ProviderId = Convert.ToInt32(ProviderID),
                              Provider = System.Configuration.ConfigurationManager.AppSettings["ProviderIntegrator"],
                              Stock = (int)o.SelectToken("result.stockToPublish"),
                              Name = (string)o.SelectToken("result.product.name"),
                              Description = (string)o.SelectToken("result.product.description"),
                              ShortDescription = (string)o.SelectToken("result.product.shortDescription"),
                              Price = (decimal)o.SelectToken("result.product.priceToPublish.price"),
                              ListPrice = (decimal)o.SelectToken("result.product.priceToPublish.listPrice"),
                              NetPrice = (decimal)o.SelectToken("result.product.priceToPublish.price"),
                              Taxes = (decimal)o.SelectToken("result.product.taxes"),
                              //Height = (decimal)o.SelectToken("result.product.height"),
                              Height = Height,
                              Width = Width,
                              Depth = Depth,
                              Weight = Weight,
                              Active = (bool)o.SelectToken("result.product.active"),
                              Ean = (string)o.SelectToken("result.product.ean"),
                              Brand = (string)o.SelectToken("result.product.brand.brand"),
                              CategoryBranch = LCB


                          };
                          string json = JsonConvert.SerializeObject(objJSON, Formatting.None);
                          string URL_API_CTC = System.Configuration.ConfigurationManager.AppSettings["URL_API_CTC"];

                          string TokenAPI = System.Configuration.ConfigurationManager.AppSettings["TokenProviderIntegrator"];

                          Magento m_api = new Magento(URL_API_CTC);
                          var request2 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku, Method.PUT, TokenAPI);
                          request2.AddParameter("application/json", json, ParameterType.RequestBody);

                          var response2 = m_api.Client.Execute(request2);

                          if (response2.StatusCode == HttpStatusCode.BadRequest)
                          {
                              JObject pr = JObject.Parse(response2.Content);


                              if ((string)pr.SelectToken("Result").SelectToken("Description") == "Producto inexistente")
                              {
                                  var request22 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products", Method.POST, TokenAPI);
                                  request22.AddParameter("application/json", json, ParameterType.RequestBody);

                                  var response22 = m_api.Client.Execute(request22);
                              }

                          }


                          string IncludeImages = "0";
                          try
                          {
                              IncludeImages = System.Configuration.ConfigurationManager.AppSettings["SincronizarImagenes"];

                          }
                          catch (Exception ffg)
                          {
                              IncludeImages = "NO";
                          }


                          if (IncludeImages == "1")
                          {
                              int cantImages = o.SelectToken("result.product.images").Count();

                              for (int i = 0; i < cantImages; i++)
                              {
                                  string url = (string)o.SelectToken("result.product.images[" + i + "].remote_url");
                                  string encodedFileAsBase64 = "";
                                  using (var client = new WebClient())
                                  {
                                      byte[] dataBytes = client.DownloadData(new Uri(url));
                                      encodedFileAsBase64 = Convert.ToBase64String(dataBytes);


                                      //Formato
                                      MemoryStream ms = new MemoryStream(dataBytes);

                                      try
                                      {
                                          Image image = Image.FromStream(ms);
                                      }
                                      catch (Exception eimg)
                                      {
                                          Imazen.WebP.Extern.LoadLibrary.LoadWebPOrFail();
                                          var decoder = new SimpleDecoder();
                                          var webpBytes = ms.ToArray();
                                          var reloaded = decoder.DecodeFromBytes(webpBytes, webpBytes.LongLength);
                                          System.IO.MemoryStream ms1 = new MemoryStream();
                                          reloaded.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                                          encodedFileAsBase64 = Convert.ToBase64String(ms1.GetBuffer());
                                      }
                                  }

                                  Object objJSONImages = new ProductImage()
                                  {
                                      Base64 = encodedFileAsBase64
                                  };
                                  string jsonImages = JsonConvert.SerializeObject(objJSONImages, Formatting.None);

                                  var request3 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images/" + i, Method.PUT, TokenAPI);
                                  request3.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                  var response3 = m_api.Client.Execute(request3);

                                  if (response3.StatusCode == HttpStatusCode.BadRequest)
                                  {
                                      JObject im = JObject.Parse(response3.Content);

                                      if ((string)im.SelectToken("TransactionId") == "34|Imagen inexistente")
                                      {
                                          var request4 = m_api.CreateRequest("/Mp_ProductsAPI_CTC/providers/" + ProviderID + "/products/" + sku + "/images", Method.POST, TokenAPI);
                                          request4.AddParameter("application/json", jsonImages, ParameterType.RequestBody);

                                          var response4 = m_api.Client.Execute(request4);
                                      }
                                  }

                              }

                          }

                          if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                          {
                              return "0!Aprobada|" + json;
                          }

                          return "-1| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error Actualizando producto en API CTC|" + json;
                      }
                      return "-3| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion. Producto no encontrado.";
                  }
                  return "-2| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Error ubicando producto en Proveedor de Intergracion";
              }
              catch (Exception f)
              {
                  return "-99| XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ERROR Excepcion  --- " + f.Message + " - " + f.InnerException + " - " + f.StackTrace;
              }
          }
  */
        public string SetStockItemsSkuCollinson(string accessKey, string secretkey, int Qty, string sku)
        {
            string res = "";
            string body = "{\"stockItem\":[{\"sku\":\"" + sku + "\", \"qty\":" + Qty.ToString() + "}]}";

            //var request = CreateRequestPOSTAWS("/rest/V1/inventory/batch/stockItems/1", body, accessKey, secretkey);
            var request = CreateRequestPOSTAWS("ctcgroupsrl", body, accessKey, secretkey);
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                
                return "0|"+response.Content;
            }
            else
            {
                res =  (response.ErrorMessage!=null?response.ErrorMessage:response.Content);
            }
            return res;
        }
        public string SetStatusSku(string token, int Status, M2Product PSku)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(PSku.Sku) + "", Method.PUT, token);

            PSku.Status = (Status == 1 ? Status : 2);
            string json = JsonConvert.SerializeObject(PSku, Formatting.Indented);

            request.AddParameter("application/json", "{\"product\":" + json + "} ", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }

        public string SetStatusSku(string token, int Status, M2Product PSku, int StoreID)
        {
            var request = CreateRequest("/rest/V1/products/" + HttpUtility.UrlEncode(PSku.Sku) + ( Status == 1 ? "/websites" : "/websites/" + StoreID), (Status == 1? Method.POST :Method.DELETE), token);

            if (Status == 1)
            {
                request.AddParameter("application/json", "{\"productWebsiteLink\": { \"sku\": \""+ PSku.Sku + "\" , \"website_id\": "+ StoreID.ToString() +  "} } ", ParameterType.RequestBody);
            }
            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString() + "| " + response.Content;
        }

        

        public string SetPriceSkuStore(string token, int Store, string sku, string Price)
        {
            var request = CreateRequest("/rest/V1/products/base-prices", Method.POST, token);

            request.AddParameter("application/json", "{\"prices\":[{\"price\":" + Price + ", \"store_id\": "+ Store.ToString()+ ", \"sku\": \""+sku+"\"}]}", ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }


        public string GetModules(string token)
        {
            var request = CreateRequest("/rest/V1/modules", Method.GET, token);

            var response = Client.Execute(request);
            string modulos = "";
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                modulos = response.Content;
            }
            return modulos;
        }

        public void GetCategory(string token, string cat)
        {
            var request = CreateRequest("/rest/V1/categories/" + cat, Method.GET, token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Category CatResp = JsonConvert.DeserializeObject<Category>(response.Content);
            }

        }

        public Category GetCategoryByID(string token, string ID)
        {
            var request = CreateRequest("/rest/V1/categories/" + ID, Method.GET, token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<Category>(response.Content);
            }
            else
                return null;
        }




        public Category GetCategoryByIDByStore(string token, string ID, string StoreView)
        {
            var request = CreateRequest("/rest/"+StoreView+"/V1/categories/" + ID, Method.GET, token);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<Category>(response.Content);
            }
            else
                return null;
        }
        public CategoryList GetCategories(string token, Filter filter)
        {
            var request = CreateRequest("/rest/V1/categories" , Method.GET, token);
            AddFilterToRequest(filter, request);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                CategoryList CatResp = JsonConvert.DeserializeObject<CategoryList>(response.Content);
                return CatResp;
            }
            return new CategoryList();
        }

        public CategoryList GetCategoriesByStoreView(string token, Filter filter, string StoreView)
        {
            var request = CreateRequest("/rest/"+ StoreView +"/V1/categories", Method.GET, token);
            AddFilterToRequest(filter, request);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                CategoryList CatResp = JsonConvert.DeserializeObject<CategoryList>(response.Content);
                return CatResp;
            }
            return new CategoryList();
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="request"></param>
        protected void AddFilterToRequest(Filter filter, IRestRequest request)
        {
            if (filter == null) return;
            var inExpressions = new Dictionary<string, ISet<string>>();
            var index = 0;
            foreach (var expression in filter.FilterExpressions)
            {
                if (expression.ExpressionOperator == ExpressionOperator.@in)
                {
                    ISet<string> values;
                    if (!inExpressions.TryGetValue(expression.FieldName, out values))
                    {
                        values = new HashSet<string>();
                        inExpressions[expression.FieldName] = values;
                    }
                    values.Add(expression.FieldValue);
                    continue;
                }
                request.AddParameter("searchCriteria[filter_groups][" + index + "][filters][0][field]=" , expression.FieldName);
                request.AddParameter("searchCriteria[filter_groups][" + index + "][filters][0][value]=" , expression.FieldValue);
                request.AddParameter("searchCriteria[filter_groups][" + index + "][filters][0][condition_type]=" , expression.ExpressionOperator );
              
                index++;
            }
            foreach (var expression in inExpressions)
            {
                request.AddParameter("filter[" + index + "][attribute]", expression.Key);

                var valueIndex = 0;
                foreach (var value in expression.Value)
                {
                    request.AddParameter("filter[" + index + "][" + ExpressionOperator.@in + "][" + valueIndex + "]", value);
                    valueIndex++;
                }
                index++;
            }
            if (filter.Page > 1)
            {
                    request.AddParameter("searchCriteria[currentPage]", filter.Page);
            }
            if (filter.PageSize > 1)
            {
                    request.AddParameter("searchCriteria[pageSize]", filter.PageSize);
            }
            if (!string.IsNullOrEmpty(filter.SortField))
            {
                    request.AddParameter("searchCriteria[sortOrders][0][field]", filter.SortField);
                    request.AddParameter("searchCriteria[sortOrders][0][direction]", filter.SortDirection);

               
            }
            
        }

     










        public void ModifyCategory(string Token, Category category)
        {
            var request = CreateRequest("/rest/V1/categories", Method.POST, Token);
            var cat = new ProductCategory();
            cat.Category = category;
            
            string json = JsonConvert.SerializeObject(cat, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //return response.Content;
            }
            else
            {
                //return "";
            }

        }

        
       




        public string GetStringQuotesByFilter(string Token, Filter filter)
        {
            var request = CreateRequest("/rest/V1/carts/search", Method.GET, Token);

            AddFilterToRequest(filter, request);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0|" + response.Content ;
            }
            else
            {
                return "-1|" + response.StatusCode + ", " + response.StatusDescription;
            }

        }

        public Quote GetQuotesByFilter(string Token, Filter filter)
        {
            var request = CreateRequest("/rest/V1/carts/search", Method.GET, Token);

            AddFilterToRequest(filter, request);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Quote QResp = JsonConvert.DeserializeObject<Quote>(response.Content);
                return QResp;

                //return "0|" + response.Content ;
            }
            else
            {
                return null;//"-1|" + response.StatusCode + ", " + response.StatusDescription;
            }

        }
                
        public OrderList GetOrder(string Token, Filter filter)
        {
            var request = CreateRequest("/rest/V1/orders", Method.GET, Token);

            AddFilterToRequest(filter, request);

            var response = Client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                OrderList OResp = JsonConvert.DeserializeObject<OrderList>(response.Content);
                return OResp;
            }
            else
            {
                return null;
            }

        }
        public string GetStringOrderbyID(string Token, string ID)
        {
            var request = CreateRequest("/rest/V1/orders/" + ID, Method.GET, Token);
            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0|" + response.Content;
            }
            else
            {
                return "-1|" + response.StatusCode + ", " + response.StatusDescription;
            }

        }


        public string OrderCancelbyID(string Token, string ID)
        {
            var request = CreateRequest("/rest/V1/orders/" + ID + "/cancel", Method.POST, Token);
            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0|" + response.Content;
            }
            else
            {
                return "-1|" + response.StatusCode + ", " + response.StatusDescription;
            }

        }



        public string AddOrderComments(string token, int Order_id, string comment, string Status)
        {
            var request = CreateRequest("/rest/V1/orders/" + Order_id.ToString() + "/comments", Method.POST, token);

            /*
            {
                "statusHistory": {
                    "comment": "Your Comment Here",
                    "created_at": "2018-06-05  15:22:04",
                    "parent_id": {order_id},
                    "is_customer_notified": 0,
                    "is_visible_on_front": 1,
                    "status": "pending"
                }
            }
            */
            string json = "{\"statusHistory\":{\"comment\": \"" + comment + "\", \"parent_id\": " + Order_id.ToString() + ", \"status\": \"" + Status + "\" , \"created_at\": \"" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + "\", \"is_customer_notified\": 0,  \"is_visible_on_front\": 1        }}";
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }

        public string AddHistoryOrderComments(string token, int Order_id, string comment, string Status, string TrnDate)
        {
            var request = CreateRequest("/rest/V1/orders/" + Order_id.ToString() + "/comments", Method.POST, token);

            /*
            {
                "statusHistory": {
                    "comment": "Your Comment Here",
                    "created_at": "2018-06-05  15:22:04",
                    "parent_id": {order_id},
                    "is_customer_notified": 0,
                    "is_visible_on_front": 1,
                    "status": "pending"
                }
            }
            */
            string json = "{\"statusHistory\":{\"comment\": \"" + comment + "\", \"parent_id\": " + Order_id.ToString() + ", \"status\": \"" + Status + "\" , \"created_at\": \"" + TrnDate + "\", \"is_customer_notified\": 0,  \"is_visible_on_front\": 1        }}";
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            return response.StatusCode.ToString();
        }




        public string ModifyCategoryByStoreView(string Token, Category category , string StoreView)
        {
            var request = CreateRequest("/rest/"+ StoreView +"/V1/categories", Method.POST, Token);
            var cat = new ProductCategory();
            cat.Category = category;
            
            string json = JsonConvert.SerializeObject(cat, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0|";
            }
            else
            {
                return "-1|" + response.StatusCode + ", " + response.StatusDescription ;
            }

        }


        public string  CreateProduct(M2Product pr)
        {
            var request = CreateRequest("/rest/V1/products", Method.PUT, Token);
           
            
            string json = JsonConvert.SerializeObject(pr, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "0";
            }
            else
            {
                return "-1|" + response.Content ;
            }

        }
        private RestRequest CreateRequest(string endPoint, Method method,string token)
        {
            var request = new RestRequest(endPoint, method);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Accept", "application/json");
            return request;
        }
        static byte[] HmacSHA256(String data, byte[] key)
        {
            String algorithm = "HmacSHA256";
            KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create(algorithm);
            kha.Key = key;

            return kha.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        static byte[] getSignatureKey(String key, String dateStamp, String regionName, String serviceName)
        {
            byte[] kSecret = Encoding.UTF8.GetBytes(("AWS4" + key).ToCharArray());
            byte[] kDate = HmacSHA256(dateStamp, kSecret);
            byte[] kRegion = HmacSHA256(regionName, kDate);
            byte[] kService = HmacSHA256(serviceName, kRegion);
            byte[] kSigning = HmacSHA256("aws4_request", kService);

            return kSigning;
        }
        private RestRequest CreateRequestPOSTAWS(string endPoint, string body,  string access_key, string secret_key)
        {
            // Paso 1 Creación de una solicitud canónica para Signature Version 4
            /*
                CanonicalRequest =
                          HTTPRequestMethod + '\n' +
                          CanonicalURI + '\n' +
                          CanonicalQueryString + '\n' +
                          CanonicalHeaders + '\n' +
                          SignedHeaders + '\n' +
                          HexEncode(Hash(RequestPayload))
             */

            var uri = new Uri(Client.BaseUrl + endPoint);

            // precompute hash of the body content
            var contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(body));
            var contentHashString = AWS4SignerBase.ToHexString(contentHash, true);

            var headers = new Dictionary<string, string>
            {
                {AWS4SignerBase.X_Amz_Content_SHA256, contentHashString},
                {"Accept", "*/*"},
                {"Accept-Encoding", "gzip, deflate"},
                {"cache-control", "no-cache,no-cache"},
                {"Content-Length", body.Length.ToString()},
                {"Content-Type", "application/json"}
            };

            var signer = new AWS4SignerForAuthorizationHeader
            {
                EndpointUri = uri,
                HttpMethod = "POST",
                Service = "execute-api",
                Region = "eu-west-2"
            };
            var authorization = signer.ComputeSignature(headers,
                                                        "",   // no query parameters
                                                        contentHashString,
                                                        access_key,
                                                        secret_key);

            // express authorization for this as a header
            headers.Add("Authorization", authorization);

           
            var request = new RestRequest("/"+endPoint, Method.POST);
            request.RequestFormat = DataFormat.Json;
            foreach (var item in headers)
            {
               request.AddHeader(item.Key.ToString(), item.Value.ToString());
            }
           
            return request;
        }

        public static System.IO.MemoryStream ConvertImageMkp(Bitmap image, int maxWidth, int maxHeight, int quality)
        {
            // Get the image's original width and height
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // To preserve the aspect ratio
            float ratioX = (float)maxWidth / (float)originalWidth;
            float ratioY = (float)maxHeight / (float)originalHeight;
            float ratio = Math.Min(ratioX, ratioY);

            // New width and height based on aspect ratio
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            // Convert other formats (including CMYK) to RGB.
            Bitmap newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            // Draws the image in the specified size with quality mode set to HighQuality
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }


            // Get an ImageCodecInfo object that represents the JPEG codec.
            ImageCodecInfo imageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg);

            // Create an Encoder object for the Quality parameter.
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object. 
            EncoderParameters encoderParameters = new EncoderParameters(1);

            // Save the image as a JPEG file with quality level.
            EncoderParameter encoderParameter = new EncoderParameter(encoder, quality);
            encoderParameters.Param[0] = encoderParameter;

            System.IO.MemoryStream ms1 = new MemoryStream();


            newImage.Save(ms1, imageCodecInfo, encoderParameters);

            return ms1;
        }
        /// <summary>
        /// Method to get encoder infor for given image format.
        /// </summary>
        /// <param name="format">Image format</param>
        /// <returns>image codec info.</returns>
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == format.Guid);
        }



    }
}
