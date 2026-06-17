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
    public class SubCategoryDALC : SqlDataComponent
    {
        /// <summary>
        /// Retorna una lista de Categorias
        /// </summary>
        /// <returns>List[ItemCategoryEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<ItemSubCategoryEntity> GetSubCategoryByCateg (int CategoryID)
        {
            List<ItemSubCategoryEntity> SubCategoryList = new List<ItemSubCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getRubroByCateg");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Category", SqlDbType.Int).Value = CategoryID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        SubCategoryList.Add(DataConvert.ToItemSubCategory(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SubCategoryList;

        }
        public List<ItemSubCategoryEntity> GetSubCategoryByCategAndCatalogId(int CategoryID, int CatalogId)
        {
            List<ItemSubCategoryEntity> SubCategoryList = new List<ItemSubCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getRubroByCategAndCatalog");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Category", SqlDbType.Int).Value = CategoryID;
                cmd.Parameters.Add("@CatalogId", SqlDbType.Int).Value = CatalogId;

                using (IDataReader dr = ExecuteReader(cmd))
                    while (dr.Read())
                        SubCategoryList.Add(DataConvert.ToItemSubCategory(dr));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SubCategoryList;
        }

        public List<ItemSubCategoryEntity> GetSubCategoryByCategoryAndProviderAndCatalogId(int categoryID, int providerID, int catalogID)
        {
            List<ItemSubCategoryEntity> SubCategoryList = new List<ItemSubCategoryEntity>();

            try
            {
                //SqlCommand cmd = new SqlCommand("getRubroByCategAndCatalog");
                SqlCommand cmd = new SqlCommand("getRubroByCategAndProviderAndCatalog");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                cmd.Parameters.Add("@ProviderID", SqlDbType.Int).Value = providerID;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                    while (dr.Read())
                        SubCategoryList.Add(DataConvert.ToItemSubCategory(dr));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SubCategoryList;
        }

        public List<ItemSubCategoryEntity> GetAllSubcategoriesByCategoryIDAndCatalogID(int CategoryID, int CatalogId)
        {
            List<ItemSubCategoryEntity> SubCategoryList = new List<ItemSubCategoryEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("GetAllSubcategoriesByCategoryIDAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Category", SqlDbType.Int).Value = CategoryID;
                cmd.Parameters.Add("@CatalogId", SqlDbType.Int).Value = CatalogId;

                using (IDataReader dr = ExecuteReader(cmd))
                    while (dr.Read())
                        SubCategoryList.Add(DataConvert.ToItemSubCategory(dr));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SubCategoryList;
        }

        //
        public ItemCompleteSubCategoryEntity GetSubCategoryByID(int SubCategoryID)
        {
            ItemCompleteSubCategoryEntity SubCategory = new ItemCompleteSubCategoryEntity();

            try
            {
                SqlCommand cmd = new SqlCommand("spGetGrupoId");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@rubroid", SqlDbType.Int).Value = SubCategoryID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        SubCategory = DataConvert.ToItemCompleteSubCategory (dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SubCategory;

        }
        public ItemCompleteSubCategoryEntity GetSubCategoryByProviderCategoryName(int Provider, string CategoryName)
        {
            ItemCompleteSubCategoryEntity SubCategory = new ItemCompleteSubCategoryEntity();

            try
            {
                SqlCommand cmd = new SqlCommand("spGetSubCategoryByIntegratorCategoryName");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ProviderID", SqlDbType.Int).Value = Provider;
                cmd.Parameters.Add("@CategoryName", SqlDbType.NVarChar).Value = CategoryName;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        SubCategory = DataConvert.ToItemCompleteSubCategory(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SubCategory;

        }

    }
}
