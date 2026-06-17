using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG2Connector
{

    public class Category
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int ParentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("children")]
        public string Children { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("available_sort_by")]
        public IList<string> AvailableSortBy { get; set; }

        [JsonProperty("include_in_menu")]
        public bool IncludeInMenu { get; set; }

        [JsonProperty("custom_attributes")]
        public IList<CustomAttribute> CustomAttributes { get; set; }


    }

    public class CategoryList
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int ParentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("product_count")]
        public string ProductCount { get; set; }

        [JsonProperty("children_data")]
        public IList<CategoryList> ChildrenData { get; set; }


        public CategoryList Find(Func<CategoryList, bool> myFunc)
        {
            foreach (CategoryList node in ChildrenData)
            {
                if (myFunc(node))
                {
                    return node;
                }
                else 
                {
                    CategoryList test = node.Find(myFunc);
                    if (test != null)
                        return test;
                }
            }

            return null;
        }

    }

    public class ProductCategory
    {

        [JsonProperty("category")]
        public Category Category { get; set; }
    }

    /*

    public class CustomAttribute
    {

        [JsonProperty("attribute_code")]
        public string AttributeCode { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
     
     */
}
