using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class AttributeEntity
    {
        private int attributeID;
        public int AttributeID
        {
            get { return attributeID; }
            set { attributeID = value; }
        }

        private string attributeLabel;
        public string AttributeLabel
        {
            get { return attributeLabel; }
            set { attributeLabel = value; }
        }

        private string attributeName;
        public string AttributeName
        {
            get { return attributeName; }
            set { attributeName = value; }
        }

    }
}
