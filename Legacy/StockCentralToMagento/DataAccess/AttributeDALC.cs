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
    public class AttributeDALC : SqlDataComponent
    {
        public List<AttributeEntity> GetAttribute1ListBySubCategoryIDAndCatalogID(int subCategoryID, int catalogID)
        {
            List<AttributeEntity> attributeList = new List<AttributeEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("Attribute1GetListBySubCatIDAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SubCategoryID", SqlDbType.Int).Value = subCategoryID;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        attributeList.Add(DataConvert.ToAttribute(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return attributeList;

        }

        public List<AttributeEntity> GetAttribute2ListBySubCategoryIDAttribute1IDAndCatalogID(int subCategoryID, int attribute1ID, int catalogID)
        {
            List<AttributeEntity> attributeList = new List<AttributeEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("Attribute2GetListBySubCatIDAtt1IDAndCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SubCategoryID", SqlDbType.Int).Value = subCategoryID;
                cmd.Parameters.Add("@Attribute1ID", SqlDbType.Int).Value = attribute1ID;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = catalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        attributeList.Add(DataConvert.ToAttribute(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return attributeList;
        }
    }
}
