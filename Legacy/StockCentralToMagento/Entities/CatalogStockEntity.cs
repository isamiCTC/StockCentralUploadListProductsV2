using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class CatalogStockEntity
    {
        private int _idarticulo;
        public int idarticulo
        {
            get { return _idarticulo; }
            set { _idarticulo = value; }
        }

        private string _codigobarra;
        public string codigobarra
        {
            get { return _codigobarra; }
            set { _codigobarra = value; }
        }

        private string _nombre;
        public string nombre
        {
            get { return _nombre; }
            set { _nombre = value; }
        }

        private DateTime _fechaalta;
        public DateTime fechaalta
        {
            get { return _fechaalta; }
            set { _fechaalta = value; }
        }

        private DateTime _fechamodif;
        public DateTime fechamodif
        {
            get { return _fechamodif; }
            set { _fechamodif = value; }
        }

        private float _preciocosto;
        public float preciocosto
        {
            get { return _preciocosto; }
            set { _preciocosto = value; }
        }

        private float _precioreal;
        public float precioreal
        {
            get { return _precioreal; }
            set { _precioreal = value; }
        }

        private ItemSubCategoryEntity _rubro;
        public ItemSubCategoryEntity rubro
        {
            get { return _rubro; }
            set { _rubro = value; }
        }
        /*
        private int _subrubro;
        public int subrubro
        {
            get { return _subrubro; }
            set { _subrubro = value; }
        }*/

        private int _stockmin;
        public int stockmin
        {
            get { return _stockmin; }
            set { _stockmin = value; }
        }

        private int _stockminredemption;
        public int stockminredemption
        {
            get { return _stockminredemption; }
            set { _stockminredemption = value; }
        }

        private bool _enable;
        public bool enable
        {
            get { return _enable; }
            set { _enable = value; }
        }

        private float _precioventa;
        public float precioventa
        {
            get { return _precioventa; }
            set { _precioventa = value; }
        }

        private float _preciounidad;
        public float preciounidad
        {
            get { return _preciounidad; }
            set { _preciounidad = value; }
        }

        private string _piezasunidad;
        public string piezasunidad
        {
            get { return _piezasunidad; }
            set { _piezasunidad = value; }
        }

        private string _unidadcasepack;
        public string unidadcasepack
        {
            get { return _unidadcasepack; }
            set { _unidadcasepack = value; }
        }

        private string _codigointerno;
        public string codigointerno
        {
            get { return _codigointerno; }
            set { _codigointerno = value; }
        }

        private bool _millas;
        public bool millas
        {
            get { return _millas; }
            set { _millas = value; }
        }

        private float _preciomillas;
        public float preciomillas
        {
            get { return _preciomillas; }
            set { _preciomillas = value; }
        }

        private int _providerid;
        public int providerid
        {
            get { return _providerid; }
            set { _providerid = value; }
        }

        private int _orderquantity;
        public int orderquantity
        {
            get { return _orderquantity; }
            set { _orderquantity = value; }
        }

        private int _stock;
        public int stock
        {
            get { return _stock; }
            set { _stock = value; }
        }

        private int _AttributeID1;
        public int AttributeID1
        {
            get { return _AttributeID1; }
            set { _AttributeID1 = value; }
        }

        private int _AttributeID2;
        public int AttributeID2
        {
            get { return _AttributeID2; }
            set { _AttributeID2 = value; }
        }

        private string _AttributeLabel1;
        public string AttributeLabel1
        {
            get { return _AttributeLabel1; }
            set { _AttributeLabel1 = value; }
        }

        private string _AttributeLabel2;
        public string AttributeLabel2
        {
            get { return _AttributeLabel2; }
            set { _AttributeLabel2 = value; }
        }

        private string _AttributeName1;
        public string AttributeName1
        {
            get { return _AttributeName1; }
            set { _AttributeName1 = value; }
        }

        private string _AttributeName2;
        public string AttributeName2
        {
            get { return _AttributeName2; }
            set { _AttributeName2 = value; }
        }

        private byte[] _SmallImage;
        public byte[] SmallImage
        {
            get { return _SmallImage; }
            set { _SmallImage = value; }
        }


        private byte[] _LargeImage;
        public byte[] LargeImage
        {
            get { return _LargeImage; }
            set { _LargeImage = value; }
        }

        private int _DeliveryType;
        public int DeliveryType
        {
            get { return _DeliveryType; }
            set { _DeliveryType = value; }
        }

        private string _ArtDescription;
        public string ArtDescription
        {
            get { return _ArtDescription; }
            set { _ArtDescription = value; }
        }

        private string _UrlPDF;
        public string URL_PDF
        {
            get { return _UrlPDF; }
            set { _UrlPDF = value; }
        }

        // Agregar precio de lista para comenzar a mostrar las promos originales de los vendors
        //*************************
        
        private float _preciolista;
        public float preciolista
        {
            get { return _preciolista; }
            set { _preciolista = value; }
        }

        private decimal _alicuotaIVA;
        public decimal alicuotaIVA
        {
            get { return _alicuotaIVA; }
            set { _alicuotaIVA = value; }
        }


        //*************************
    }

 
    public class FullCategoryItem
    {
        private int? id;
        public int? ID
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}
