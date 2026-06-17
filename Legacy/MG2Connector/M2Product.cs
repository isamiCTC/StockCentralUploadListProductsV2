using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG2Connector
{
    public class StockItem
    {

        /*
         * 
         * 
        {
          "id": 0,
          "sku": "string",
          "name": "string",
          "attribute_set_id": 0,
          "price": 0,
          "status": 0,
          "visibility": 0,
          "type_id": "string",
          "created_at": "string",
          "updated_at": "string",
          "weight": 0,
          "extension_attributes": {
            "stock_item": {
              "item_id": 0,
              "product_id": 0,
              "stock_id": 0,
              "qty": 0,
              "is_in_stock": true,
              "is_qty_decimal": true,
              "show_default_notification_message": true,
              "use_config_min_qty": true,
              "min_qty": 0,
              "use_config_min_sale_qty": 0,
              "min_sale_qty": 0,
              "use_config_max_sale_qty": true,
              "max_sale_qty": 0,
              "use_config_backorders": true,
              "backorders": 0,
              "use_config_notify_stock_qty": true,
              "notify_stock_qty": 0,
              "use_config_qty_increments": true,
              "qty_increments": 0,
              "use_config_enable_qty_inc": true,
              "enable_qty_increments": true,
              "use_config_manage_stock": true,
              "manage_stock": true,
              "low_stock_date": "string",
              "is_decimal_divided": true,
              "stock_status_changed_auto": 0,
              "extension_attributes": {}
            },
            "bundle_product_options": [
              {
                "option_id": 0,
                "title": "string",
                "required": true,
                "type": "string",
                "position": 0,
                "sku": "string",
                "product_links": [
                  {
                    "id": "string",
                    "sku": "string",
                    "option_id": 0,
                    "qty": 0,
                    "position": 0,
                    "is_default": true,
                    "price": 0,
                    "price_type": 0,
                    "can_change_quantity": 0,
                    "extension_attributes": {}
                  }
                ],
                "extension_attributes": {}
              }
            ],
            "downloadable_product_links": [
              {
                "id": 0,
                "title": "string",
                "sort_order": 0,
                "is_shareable": 0,
                "price": 0,
                "number_of_downloads": 0,
                "link_type": "string",
                "link_file": "string",
                "link_file_content": {
                  "file_data": "string",
                  "name": "string",
                  "extension_attributes": {}
                },
                "link_url": "string",
                "sample_type": "string",
                "sample_file": "string",
                "sample_file_content": {
                  "file_data": "string",
                  "name": "string",
                  "extension_attributes": {}
                },
                "sample_url": "string",
                "extension_attributes": {}
              }
            ],
            "downloadable_product_samples": [
              {
                "id": 0,
                "title": "string",
                "sort_order": 0,
                "sample_type": "string",
                "sample_file": "string",
                "sample_file_content": {
                  "file_data": "string",
                  "name": "string",
                  "extension_attributes": {}
                },
                "sample_url": "string",
                "extension_attributes": {}
              }
            ],
            "giftcard_amounts": [
              {
                "attribute_id": 0,
                "website_id": 0,
                "value": 0,
                "website_value": 0,
                "extension_attributes": {}
              }
            ],
            "configurable_product_options": [
              {
                "id": 0,
                "attribute_id": "string",
                "label": "string",
                "position": 0,
                "is_use_default": true,
                "values": [
                  {
                    "value_index": 0,
                    "extension_attributes": {}
                  }
                ],
                "extension_attributes": {},
                "product_id": 0
              }
            ],
            "configurable_product_links": [
              0
            ]
          },
          "product_links": [
            {
              "sku": "string",
              "link_type": "string",
              "linked_product_sku": "string",
              "linked_product_type": "string",
              "position": 0,
              "extension_attributes": {
                "qty": 0
              }
            }
          ],
          "options": [
            {
              "product_sku": "string",
              "option_id": 0,
              "title": "string",
              "type": "string",
              "sort_order": 0,
              "is_require": true,
              "price": 0,
              "price_type": "string",
              "sku": "string",
              "file_extension": "string",
              "max_characters": 0,
              "image_size_x": 0,
              "image_size_y": 0,
              "values": [
                {
                  "title": "string",
                  "sort_order": 0,
                  "price": 0,
                  "price_type": "string",
                  "sku": "string",
                  "option_type_id": 0
                }
              ],
              "extension_attributes": {}
            }
          ],
          "media_gallery_entries": [
            {
              "id": 0,
              "media_type": "string",
              "label": "string",
              "position": 0,
              "disabled": true,
              "types": [
                "string"
              ],
              "file": "string",
              "content": {
                "base64_encoded_data": "string",
                "type": "string",
                "name": "string"
              },
              "extension_attributes": {
                "video_content": {
                  "media_type": "string",
                  "video_provider": "string",
                  "video_url": "string",
                  "video_title": "string",
                  "video_description": "string",
                  "video_metadata": "string"
                }
              }
            }
          ],
          "tier_prices": [
            {
              "customer_group_id": 0,
              "qty": 0,
              "value": 0,
              "extension_attributes": {}
            }
          ],
          "custom_attributes": [
            {
              "attribute_code": "string",
              "value": "string"
            }
          ]
        }
         
         */

        [JsonProperty("item_id")]
        public int ItemId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("stock_id")]
        public int StockId { get; set; }

        [JsonProperty("qty")]
        public int Qty { get; set; }

        [JsonProperty("is_in_stock")]
        public bool IsInStock { get; set; }

        [JsonProperty("is_qty_decimal")]
        public bool IsQtyDecimal { get; set; }

        [JsonProperty("show_default_notification_message")]
        public bool ShowDefaultNotificationMessage { get; set; }

        [JsonProperty("use_config_min_qty")]
        public bool UseConfigMinQty { get; set; }

        [JsonProperty("min_qty")]
        public int MinQty { get; set; }

        [JsonProperty("use_config_min_sale_qty")]
        public int UseConfigMinSaleQty { get; set; }

        [JsonProperty("min_sale_qty")]
        public int MinSaleQty { get; set; }

        [JsonProperty("use_config_max_sale_qty")]
        public bool UseConfigMaxSaleQty { get; set; }

        [JsonProperty("max_sale_qty")]
        public int MaxSaleQty { get; set; }

        [JsonProperty("use_config_backorders")]
        public bool UseConfigBackorders { get; set; }

        [JsonProperty("backorders")]
        public int Backorders { get; set; }

        [JsonProperty("use_config_notify_stock_qty")]
        public bool UseConfigNotifyStockQty { get; set; }

        [JsonProperty("notify_stock_qty")]
        public int NotifyStockQty { get; set; }

        [JsonProperty("use_config_qty_increments")]
        public bool UseConfigQtyIncrements { get; set; }

        [JsonProperty("qty_increments")]
        public int QtyIncrements { get; set; }

        [JsonProperty("use_config_enable_qty_inc")]
        public bool UseConfigEnableQtyInc { get; set; }

        [JsonProperty("enable_qty_increments")]
        public bool EnableQtyIncrements { get; set; }

        [JsonProperty("use_config_manage_stock")]
        public bool UseConfigManageStock { get; set; }

        [JsonProperty("manage_stock")]
        public bool ManageStock { get; set; }

        [JsonProperty("low_stock_date")]
        public object LowStockDate { get; set; }

        [JsonProperty("is_decimal_divided")]
        public bool IsDecimalDivided { get; set; }

        [JsonProperty("stock_status_changed_auto")]
        public int StockStatusChangedAuto { get; set; }
    }

    public class ExtensionAttributes
    {

        [JsonProperty("stock_item")]
        public StockItem StockItem { get; set; }

        [JsonProperty("website_ids")]
        public int[] WebSites { get; set; }
    }





    public class CustomAttribute
    {

        public CustomAttribute()
        { }

        [JsonProperty("attribute_code")]
        public string AttributeCode { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
        public CustomAttribute(string pattributeCode, object pValue)
        {
            AttributeCode = pattributeCode;
            Value = pValue;

        }
   
    }

    public class M2Product
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("attribute_set_id")]
        public int AttributeSetId { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("visibility")]
        public int Visibility { get; set; }

        [JsonProperty("type_id")]
        public string TypeId { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("extension_attributes")]
        public ExtensionAttributes ExtensionAttributes { get; set; }

        [JsonProperty("product_links")]
        public IList<object> ProductLinks { get; set; }

        [JsonProperty("options")]
        public IList<object> Options { get; set; }

        [JsonProperty("media_gallery_entries")]
        public IList<object> MediaGalleryEntries { get; set; }

        [JsonProperty("tier_prices")]
        public IList<object> TierPrices { get; set; }

        [JsonProperty("custom_attributes")]
        public IList<CustomAttribute> CustomAttributes { get; set; }
    }
    public class ProductSku
    {
        [JsonProperty("skus")]
        public string[] Sku { get; set; }

    }


    public class ProductPriceSku
    {
        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("store_id")]
        public int StoreID { get; set; }
    }


    #region 0920
    public class M2Product_PayLoad
    {


        [JsonIgnore]
        public string Store { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("attribute_set_id")]
        public int AttributeSetId { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("visibility")]
        public int Visibility { get; set; }

        [JsonProperty("type_id")]
        public string TypeId { get; set; }

        [JsonProperty("created_at")]
        [JsonIgnore]
        public string CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        [JsonIgnore]
        public string UpdatedAt { get; set; }

        [JsonProperty("weight")]
        public int Weigth { get; set; }

        [JsonProperty("extension_attributes")]
        [JsonIgnore]
        public ExtensionAttributes ExtensionAttributes { get; set; }

        //[JsonProperty("product_links")]
        //public IList<object> ProductLinks { get; set; }

        //[JsonProperty("options")]
        //public IList<object> Options { get; set; }

        [JsonProperty("media_gallery_entries")]
        public IList<Media_Gallery_Entries> MediaGalleryEntries { get; set; }

        //[JsonProperty("tier_prices")]
        //public IList<object> TierPrices { get; set; }

        [JsonProperty("custom_attributes")]
       
        public IList<CustomAttribute> CustomAttributes { get; set; }

     
    }
    public class Media_Gallery_Entries
    {
        //[JsonProperty("id")]
        //public int id { get; set; }

        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("types")]
        public IList<string> Types { get; set; }
        [JsonProperty("content")]
        public Media_Gallery_Content Content { get; set; }
    }

    public class Media_Gallery_Entries_PayLoad
    {
        [JsonProperty("id")]
        public int id { get; set; }

        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("types")]
        public IList<string> Types { get; set; }
        [JsonProperty("content")]
        public Media_Gallery_Content Content { get; set; }
    }


    public class Media_Gallery_Content
    {
        [JsonProperty("base64_encoded_data")]
        public string base_64 { get; set; }
        [JsonProperty("type")]
        public string type { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }

    }

    public class Product_Links

    {
        [JsonProperty("sku")]
        public string sku { get; set; }
        [JsonProperty("link_type")]
        public string link_type { get; set; }
        [JsonProperty("linked_product_sku")]
        public string linked_product_sku { get; set; }
        [JsonProperty("linked_product_type")]
        public string linked_product_type { get; set; }

        [JsonProperty("position")]
        public int position { get; set; }
    }

    public class Price

    {
        [JsonProperty("sku")]
        public string sku { get; set; }

        [JsonProperty("price")]
        public decimal price { get; set; }
        [JsonProperty("store_id")]
        public int store_id { get; set; }
        [JsonProperty("price_from")]
        public string price_from { get; set; }
        [JsonProperty("price_to")]
        public string price_to { get; set; }

       
    }


    #endregion

}
