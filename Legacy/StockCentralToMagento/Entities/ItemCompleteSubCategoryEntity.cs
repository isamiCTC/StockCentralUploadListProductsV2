using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class ItemCompleteSubCategoryEntity
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
        private byte[] categoryImage2;
        private byte[] categoryImage3;
        private byte[] categoryImage4;
        private string shortdesc;
        private string largedesc;

        #endregion

        #region Constructors
        public ItemCompleteSubCategoryEntity()
        {
            id = -1;
            name1 = name2 = name3 = null;
            markup = 1;
            category = 1;
            fechamodif = new DateTime();

            
        }
        public ItemCompleteSubCategoryEntity(string categoryName1, string categoryName2, string categoryName3)
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
        public byte[] CategoryImage2
        {
            get { return categoryImage2; }
            set { categoryImage2 = value; }
        }
        public byte[] CategoryImage3
        {
            get { return categoryImage3; }
            set { categoryImage3 = value; }
        }
        public byte[] CategoryImage4
        {
            get { return categoryImage4; }
            set { categoryImage4 = value; }
        }
        public string ShortDescription
        {
            get { return shortdesc ; }
            set { shortdesc  = value; }
        }
        public string LargeDescription
        {
            get { return largedesc ; }
            set { largedesc  = value; }
        }
        #endregion
    }
}
