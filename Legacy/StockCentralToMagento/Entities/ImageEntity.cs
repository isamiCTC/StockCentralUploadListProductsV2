using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class ImageEntity
    {
        private byte[] _Image;
        public byte[] Image
        {
            get { return _Image; }
            set { _Image = value; }
        }

        private string _BarCode;
        public string BarCode
        {
            get { return _BarCode; }
            set { _BarCode = value; }
        }

        private DateTime _LastUpdate;
        public DateTime LastUpdate
        {
            get { return _LastUpdate; }
            set { _LastUpdate = value; }
        }

    }
}
