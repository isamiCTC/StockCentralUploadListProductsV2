using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockCentralToMagento.Entities
{
    public class ProviderVoucherEntity
    {
        private int _id;
        private byte[] _image;
        private string _tycVoucher;
        private string _tycHotel;
        private string _tycVirtual;
        private string _meVoucher;
        private string _meHotel;
        private string _meVirtual;
        private byte[] _imageGC;
        public ProviderVoucherEntity()
        {
            _id = -1;
            _image = null;
            _tycVoucher = string.Empty;
            _tycHotel = string.Empty;
            _tycVirtual = string.Empty;
            _meVoucher = string.Empty;
            _meHotel = string.Empty;
            _meVirtual = string.Empty;
            _imageGC = null;
        }

        public int id
        {
            set { _id = value; }
            get { return _id; }
        }

        public byte[] image
        {
            set { _image = value; }
            get { return _image; }
        }
        public string tycVoucher
        {
            set { _tycVoucher = value; }
            get { return _tycVoucher; }
        }

        public string tycHotel
        {
            set { _tycHotel = value; }
            get { return _tycHotel; }
        }

        public string tycVirtual
        {
            set { _tycVirtual = value; }
            get { return _tycVirtual; }
        }
        public byte[] imageGC
        {
            set { _imageGC = value; }
            get { return _imageGC; }
        }

        public string meVoucher
        {
            set { _meVoucher = value; }
            get { return _meVoucher; }
        }

        public string meHotel
        {
            set { _meHotel = value; }
            get { return _meHotel; }
        }

        public string meVirtual
        {
            set { _meVirtual = value; }
            get { return _meVirtual; }
        }
    }
}
