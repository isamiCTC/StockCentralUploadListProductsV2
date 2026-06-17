using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class StoreProviderEntity
    {
        private int _storeID;
        public int StoreID
        {
            get { return _storeID; }
            set { _storeID = value; }
        }

        private string _storeName;
        public string StoreName
        {
            get { return _storeName; }
            set { _storeName = value; }
        }

        private string _direction;
        public string Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        private int _providerID;
        public int ProviderID
        {
            get { return _providerID; }
            set { _providerID = value; }
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        private string _providerName;
        public string ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }
    }
}
