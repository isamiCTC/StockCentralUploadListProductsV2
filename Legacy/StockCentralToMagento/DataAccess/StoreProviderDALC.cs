using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StockCentralToMagento.Entities;
using System.Data.SqlClient;
using System.Data;
using Redemption.Core.Common;

namespace StockCentralToMagento.DataAccess
{
    public class StoreProviderDALC : SqlDataComponent
    {
        /// <summary>
        /// Retorna una lista de StoreProviderEntity
        /// </summary>
        /// <returns>List[StockProviderEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<StoreProviderEntity> GetAddressDealer(string BarCode)
        {
            List<StoreProviderEntity> StoreProvList = new List<StoreProviderEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getStoreProvByBarCode");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar,32).Value = BarCode;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        StoreProvList.Add(DataConvert.FillStoreProvider(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return StoreProvList;

        }
    }
}
