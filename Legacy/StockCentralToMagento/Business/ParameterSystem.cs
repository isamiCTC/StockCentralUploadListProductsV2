using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
//using Microsoft.Practices.EnterpriseLibrary.Common;
//using Microsoft.Practices.EnterpriseLibrary.Data;
using StockCentralToMagento.DataAccess;

namespace StockCentralToMagento.Business
{
    public static class ParameterSystem
    {


        /// <summary>
        /// Retorna el valor del Paramtro
        /// </summary>
        /// <param name="name">Nombre del Parametro</param>
        /// <returns>String, cuando existe el registro. Null, cuando no existe el registro</returns>
        public static string GetParameterValueByName(string name)
        {
            string value = null;

            try
            {
                DbCommand dbcommand = ConexionDataBase.db.GetStoredProcCommand("ParametrosGetParametrosByName");
                ConexionDataBase.db.AddInParameter(dbcommand, "Name", DbType.String, name);

                using (IDataReader dr = ConexionDataBase.db.ExecuteReader(dbcommand))
                {
                    if (dr.Read())
                    {
                        value = dr["Value"] as string;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return value;
        }
    }
}
