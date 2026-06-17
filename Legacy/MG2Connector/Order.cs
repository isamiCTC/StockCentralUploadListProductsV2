using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG2Connector
{
    public class OrderList
    {
        [JsonProperty("items")]
        
        public Order[] Order { get; set; }
    }
    public class Order
    {
        [JsonProperty("entity_id")]
        public int Id { get; set; }

        [JsonProperty("quote_id")]
        public int QuoteId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class Quote
    {
        [JsonProperty("items")]
        public QuoteItems[] Item { get; set; }
    }
     
    
    public class QuoteItems
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("reserved_order_id")]
        public string OrderID { get; set; }

        
    }

}