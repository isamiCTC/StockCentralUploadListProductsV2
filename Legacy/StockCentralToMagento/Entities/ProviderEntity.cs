using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockCentralToMagento.Entities
{
    public class ProviderEntity
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set { _city = value; }
        }

        private string _country;
        public string Country
        {
            get { return _country; }
            set { _country = value; }
        }

        private string _telephone;
        public string Telephone
        {
            get { return _telephone; }
            set { _telephone = value; }
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        private string _fax;
        public string Fax
        {
            get { return _fax; }
            set { _fax = value; }
        }

        private string _contact;
        public string Contact
        {
            get { return _contact; }
            set { _contact = value; }
        }

        private string _web;
        public string Web
        {
            get { return _web; }
            set { _web = value; }
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        private string _cod;
        public string COD
        {
            get { return _cod; }
            set { _cod = value; }
        }

        private string _state;
        public string State
        {
            get { return _state; }
            set { _state = value; }
        }

        private string _zipcode;
        public string ZipCode
        {
            get { return _zipcode; }
            set { _zipcode = value; }
        }

        private string _addr;
        public string Addr
        {
            get { return _addr; }
            set { _addr = value; }
        }

        private bool _deleted;
        public bool Deleted
        {
            get { return _deleted; }
            set { _deleted = value; }
        }
        private int _integratorid;
        public int IntegratorID
        {

            get { return _integratorid; }
            set { _integratorid = value; }
        }

        private string _token;
        public string Token
        {
            // Token de Producteca que depende de cada seller
            get { return _token; }
            set { _token = value; }
        }

        private string _externalProviderAccountID;
        public string ExternalProviderAccountID
        {
            // Token de Producteca que depende de cada seller
            get { return _externalProviderAccountID; }
            set { _externalProviderAccountID = value; }
        }

        private bool _vendor;
        public bool Vendor
        {

            get { return _vendor; }
            set { _vendor = value; }
        }

    }
}
