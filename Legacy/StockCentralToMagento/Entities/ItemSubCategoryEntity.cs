using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class ItemSubCategoryEntity
    {
        #region Private Members
        private int id;
        private string name1;
        private string name2;
        private string name3;
        private decimal markup;
        private int category;
        private DateTime fechamodif;
        private byte[] categoryImage;

        #endregion

        #region Constructors
        public ItemSubCategoryEntity()
        {
            id = -1;
            name1 = name2 = name3 = null;
            markup = 1;
            category = 1;
            fechamodif = new DateTime();
        }
        public ItemSubCategoryEntity(string categoryName1, string categoryName2, string categoryName3)
        {
            id = -1;
            name1 = categoryName1;
            name2 = categoryName2;
            name3 = categoryName3;
            markup = 1;
            category = 1;
            fechamodif = new DateTime();
        }
        #endregion

        #region Properties
        public int ItemCategoryID
        {
            get { return id; }
            set { id = value; }
        }
        public int CategoryID
        {
            get { return category; }
            set { category = value; }
        }
        public string Name1
        {
            get { return name1; }
            set { name1 = value; }
        }
        public string Name2
        {
            get { return name2; }
            set { name2 = value; }
        }
        public string Name3
        {
            get { return name3; }
            set { name3 = value; }
        }
        public decimal Markup
        {
            get { return markup; }
            set { markup = value; }
        }
        public DateTime UpdateDate
        {
            get { return fechamodif; }
            set { fechamodif = value; }
        }
        public byte[] CategoryImage
        {
            get { return categoryImage; }
            set { categoryImage = value; }
        }
        #endregion
    }
}
