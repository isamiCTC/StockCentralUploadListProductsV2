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
    public class CategoryDALC : SqlDataComponent
    {
        /// <summary>
        /// Retorna una lista de Categorias
        /// </summary>
        /// <returns>List[ItemCategoryEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<ItemCategoryEntity> GetAllCategory()
        {
            List<ItemCategoryEntity> CategoryList = new List<ItemCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getCategory");
                cmd.CommandType = CommandType.StoredProcedure;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CategoryList.Add(DataConvert.FillCategory(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CategoryList;

        }

        public List<ItemCategoryEntity> GetCategoriesByProviderIDAndCatalogID(int providerID, int catalogID)
        {
            List<ItemCategoryEntity> CategoryList = new List<ItemCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("GetCategoriesByProviderIDAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ProviderID", SqlDbType.Int).Value = providerID;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CategoryList.Add(DataConvert.FillCategory(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CategoryList;
        }

        public List<ItemCategoryEntity> GetCategoriesByMenuID( int MenuID)
        {
            List<ItemCategoryEntity> CategoryList = new List<ItemCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("GetCategoriesByMenuID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@menuID", SqlDbType.Int ).Value = MenuID ;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CategoryList.Add(DataConvert.FillCategory(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CategoryList;
        }

        public List<ItemCategoryEntity> GetCategoriesByMenuIDAndCatalogId(int MenuID, int CatalogId)
        {
            List<ItemCategoryEntity> CategoryList = new List<ItemCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("GetCategoriesByMenuIDAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@menuID", SqlDbType.Int).Value = MenuID;
                cmd.Parameters.Add("@CatalogId", SqlDbType.Int).Value = CatalogId;

                using (IDataReader dr = ExecuteReader(cmd))
                    while (dr.Read())
                        CategoryList.Add(DataConvert.FillCategory(dr));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CategoryList;
        }

        public List<ItemCategoryEntity> GetAllCategoriesByCatalogId(int CatalogId)
        {
            List<ItemCategoryEntity> CategoryList = new List<ItemCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("GetAllCategoriesByCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogId", SqlDbType.Int).Value = CatalogId;

                using (IDataReader dr = ExecuteReader(cmd))
                    while (dr.Read())
                        CategoryList.Add(DataConvert.FillCategory(dr));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CategoryList;
        }


    }
}