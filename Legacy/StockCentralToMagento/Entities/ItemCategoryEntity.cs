using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class ItemCategoryEntity
    {
        #region Private Members
        private int id;
        private string name1;
        private byte[] categoryIcon;


        #endregion

        #region Constructors
        public ItemCategoryEntity()
        {
            id = -1;
            name1 = string.Empty;

        }
        public ItemCategoryEntity(string categoryName1)
        {
            id = -1;
            name1 = categoryName1;
        }
        #endregion

        #region Properties
        public int ItemCategoryID
        {
            get { return id; }
            set { id = value; }
        }
        public string Name
        {
            get { return name1; }
            set { name1 = value; }
        }

        public byte[] CategoryIcon
        {
            get { return categoryIcon; }
            set { categoryIcon = value; }
        }
        #endregion
    }
}
