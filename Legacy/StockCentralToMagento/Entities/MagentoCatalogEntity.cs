using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class MagentoCatalogEntity
    {
        private int _idcatalogomagento;
        public int idCatalogoMagento
        {
            get { return _idcatalogomagento; }
            set { _idcatalogomagento = value; }
        }
        private int _idcatalogoils;
        public int idCatalogoILS
        {
            get { return _idcatalogoils; }
            set { _idcatalogoils = value; }
        }
        private int _id;
        public int id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _magentowebsite;
        public string MagentoWebSite
        {
            get { return _magentowebsite; }
            set { _magentowebsite = value; }
        }
        private string _categoriaraiz;
        public string CategoriaRaiz
        {
            get { return _categoriaraiz; }
            set { _categoriaraiz = value; }
        }

        private string _urlcatalogo;
        public string UrlCatalogo
        {
            get { return _urlcatalogo; }
            set { _urlcatalogo = value; }
        }

    }

    public class MagentoStoreEntity
    {
        private int _idcatalogomagento;
        public int idCatalogoMagento
        {
            get { return _idcatalogomagento; }
            set { _idcatalogomagento = value; }
        }
        private int _idcatalogoils;
        public int idCatalogoILS
        {
            get { return _idcatalogoils; }
            set { _idcatalogoils = value; }
        }
        private int _id;
        public int idStoreMagento
        {
            get { return _id; }
            set { _id = value; }
        }

        

    }
}

