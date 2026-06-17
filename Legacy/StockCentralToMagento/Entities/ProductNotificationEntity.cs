using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;


namespace StockCentralToMagento.Entities
{
    public class ProductNotificationEntity
    {
        private int id;
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        private string productID;
        public string ProductID
        {
            get { return productID; }
            set { productID = value; }
        }

        private string providerID;
        public string ProviderID
        {
            get { return providerID; }
            set { providerID = value; }
        }
        private int estado;
        public int Estado
        {
            get { return estado; }
            set { estado = value; }
        }
        private DateTime fecha;
        public DateTime FechaEvento
        {
            get { return fecha; }
            set { fecha = value; }
        }

    }
}
