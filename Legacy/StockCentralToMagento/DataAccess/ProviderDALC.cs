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
    public class ProviderDALC : SqlDataComponent
    {

        /// <summary>
        /// Retorna el ProviderEntity según parametro especificado
        /// </summary>
        /// <param name="BarCode">Codigo de Barra</param>
        /// <returns>ProviderEntity, cuando existe el registro. Null, cuando no existe el registro</returns>
        public ProviderEntity GetProviderByBarCode(string BarCode)
        {
            ProviderEntity pro = null;

            try
            {
                SqlCommand cmd = new SqlCommand("getProviderByBarCode");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar, 64).Value = BarCode;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        pro = DataConvert.FillProvider(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return pro;
        }




        public ProviderEntity GetByID(int providerID)
        {
            ProviderEntity provider = null;

            try
            {
                SqlCommand cmd = new SqlCommand("GetProviderById");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ID", SqlDbType.BigInt).Value = providerID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        provider = DataConvert.FillProvider(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return provider;
        }

        public List<ProviderEntity> GetListByEnabledAndDeleted(bool enabled, bool deleted)
        {
            List<ProviderEntity> providerList = new List<ProviderEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("ProvidersGetListByEnabledAndDeleted");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Enabled", SqlDbType.Bit).Value = enabled;
                cmd.Parameters.Add("@Deleted", SqlDbType.Bit).Value = deleted;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        providerList.Add(DataConvert.FillProvider(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return providerList;
        }

        public List<ProviderEntity> GetListByEnabledAndDeletedAndCatalogID(bool enabled, bool deleted, int catalogID)
        {
            List<ProviderEntity> providerList = new List<ProviderEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("ProvidersGetListByEnabledAndDeletedAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Enabled", SqlDbType.Bit).Value = enabled;
                cmd.Parameters.Add("@Deleted", SqlDbType.Bit).Value = deleted;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        providerList.Add(DataConvert.FillProvider(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return providerList;
        }
        public List<StockCentralToMagento.Entities.ProviderEntity> GetListByEnabledAnVendorAndCatalogID(bool enabled, bool vendor, int catalogID)
        {
            List<ProviderEntity> providerList = new List<ProviderEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("ProvidersGetListByEnabledAndVendorAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Enabled", SqlDbType.Bit).Value = enabled;
                cmd.Parameters.Add("@Vendor", SqlDbType.Bit).Value = vendor;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        providerList.Add(DataConvert.FillProvider(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return providerList;
        }

        public List<StockCentralToMagento.Entities.ProviderEntity> GetListByEnabledAndIntegratorAndCatalogID(bool enabled, int Integrator, int catalogID)
        {
            List<ProviderEntity> providerList = new List<ProviderEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("ProvidersGetListByEnabledAndIntegratorAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Enabled", SqlDbType.Bit).Value = enabled;
                cmd.Parameters.Add("@IntegratorID", SqlDbType.Int).Value = Integrator;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        providerList.Add(DataConvert.FillProvider(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return providerList;
        }

        public void Insert(ProviderEntity provider)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("InsertProvider");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = provider.Name != null ? provider.Name : (object)DBNull.Value;
                cmd.Parameters.Add("@city", SqlDbType.NVarChar).Value = provider.City != null ? provider.City : (object)DBNull.Value;
                cmd.Parameters.Add("@country", SqlDbType.NVarChar).Value = provider.Country != null ? provider.Country : (object)DBNull.Value;
                cmd.Parameters.Add("@tel", SqlDbType.NVarChar).Value = provider.Telephone != null ? provider.Telephone : (object)DBNull.Value;
                cmd.Parameters.Add("@mail", SqlDbType.NVarChar).Value = provider.Email != null ? provider.Email : (object)DBNull.Value;
                cmd.Parameters.Add("@fax", SqlDbType.NVarChar).Value = provider.Fax != null ? provider.Fax : (object)DBNull.Value;
                cmd.Parameters.Add("@contact", SqlDbType.NVarChar).Value = provider.Contact != null ? provider.Contact : (object)DBNull.Value;
                cmd.Parameters.Add("@web", SqlDbType.NVarChar).Value = provider.Web != null ? provider.Web : (object)DBNull.Value;
                cmd.Parameters.Add("@enabled", SqlDbType.Bit).Value = provider.Enabled;
                cmd.Parameters.Add("@cod", SqlDbType.NVarChar).Value = provider.COD != null ? provider.COD : (object)DBNull.Value;
                cmd.Parameters.Add("@state", SqlDbType.NVarChar).Value = provider.State != null ? provider.State : (object)DBNull.Value;
                cmd.Parameters.Add("@zipcode", SqlDbType.NVarChar).Value = provider.ZipCode != null ? provider.ZipCode : (object)DBNull.Value;
                cmd.Parameters.Add("@addr", SqlDbType.NVarChar).Value = provider.Addr != null ? provider.Addr : (object)DBNull.Value;
                cmd.Parameters.Add("@ret", SqlDbType.BigInt).Direction = ParameterDirection.Output;

                ExecuteNonQuery(cmd);

                provider.ID = Convert.ToInt32(cmd.Parameters["@ret"].Value);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Update(ProviderEntity provider)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("UpdateProvider");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@id", SqlDbType.Int).Value = provider.ID;
                cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = provider.Name != null ? provider.Name : (object)DBNull.Value;
                cmd.Parameters.Add("@city", SqlDbType.NVarChar).Value = provider.City != null ? provider.City : (object)DBNull.Value;
                cmd.Parameters.Add("@country", SqlDbType.NVarChar).Value = provider.Country != null ? provider.Country : (object)DBNull.Value;
                cmd.Parameters.Add("@tel", SqlDbType.NVarChar).Value = provider.Telephone != null ? provider.Telephone : (object)DBNull.Value;
                cmd.Parameters.Add("@mail", SqlDbType.NVarChar).Value = provider.Email != null ? provider.Email : (object)DBNull.Value;
                cmd.Parameters.Add("@fax", SqlDbType.NVarChar).Value = provider.Fax != null ? provider.Fax : (object)DBNull.Value;
                cmd.Parameters.Add("@contact", SqlDbType.NVarChar).Value = provider.Contact != null ? provider.Contact : (object)DBNull.Value;
                cmd.Parameters.Add("@web", SqlDbType.NVarChar).Value = provider.Web != null ? provider.Web : (object)DBNull.Value;
                cmd.Parameters.Add("@enabled", SqlDbType.Bit).Value = provider.Enabled;
                cmd.Parameters.Add("@cod", SqlDbType.NVarChar).Value = provider.COD != null ? provider.COD : (object)DBNull.Value;
                cmd.Parameters.Add("@state", SqlDbType.NVarChar).Value = provider.State != null ? provider.State : (object)DBNull.Value;
                cmd.Parameters.Add("@zipcode", SqlDbType.NVarChar).Value = provider.ZipCode != null ? provider.ZipCode : (object)DBNull.Value;
                cmd.Parameters.Add("@addr", SqlDbType.NVarChar).Value = provider.Addr != null ? provider.Addr : (object)DBNull.Value;

                ExecuteNonQuery(cmd);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
