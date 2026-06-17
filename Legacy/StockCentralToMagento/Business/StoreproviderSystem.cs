using System;
using CTCProviders.Entities;
using CTCProviders.DataAccess;
using DBAcces;

namespace CTCProviders.System
{
    class StoreProvidersSystem
    {
        StoreProviderDAO _PDAO;
		string _CS;
		DBTypes _Type;
		
		public StoreProvidersSystem(string CS,DBTypes Type)
		{
			_CS=CS;
			_Type=Type;
		}
        public int CreateStoreProvider(StoreProvider P)
		{
			return PDAO.InsertStoreProvider(P);
		}

		public StoreProvider GetStoreProviderById(int Id)
		{
			return PDAO.GetStoreProviderById(Id);
		}

		public int UpdateStoreProvider(StoreProvider P)
		{
			return PDAO.UpdateStoreProvider(P);
		}

		public int DeleteStoreProvider(int Id)
		{
			return PDAO.DeleteStoreProvider(Id);
		}

		
		public StoreProvider[] GetSTbyProvider(int P)
		{
            return PDAO.GetStoreProviderByProvider(P);
		}

		private StoreProviderDAO PDAO
		{
			get{
				if(_PDAO==null)
					_PDAO=new StoreProviderDAO(_CS,_Type);
				
				return _PDAO;
			}
		}
    }
}
