using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using Redemption.Core.Common;
using StockCentralToMagento.Entities;

namespace StockCentralToMagento.DataAccess
{
    public class ProviderVoucherDALC : SqlDataComponent    
    {
        public ProviderVoucherEntity GetProviderVoucherById(int providerId)
        {
            SqlCommand cmd = new SqlCommand("ProviderVoucherGetById");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id", providerId);
 
            return DataConvert.ToProviderVoucher(this.ExecuteDataSet(cmd));
        }
        public ProviderVoucherEntity GetProviderVoucherBySubCategoryName(string subCategoryName, int includeImages)
        {
            ProviderVoucherEntity PVE;
            SqlCommand cmd = new SqlCommand("ProviderVoucherGetBySubCategoryId");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id", subCategoryName);
            cmd.Parameters.AddWithValue("@includeimage", includeImages);
            using (DataSet ds = this.ExecuteDataSet(cmd))
            {
                PVE = DataConvert.ToProviderVoucher(ds);
            }

            return PVE;
        }


        public ProviderVoucherEntity GetProviderVoucherByArt(int catalogId, string barCode, int rubroId)
        {
            SqlCommand cmd = new SqlCommand("ProviderVoucherGetByArt");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@BarCode", barCode);
            cmd.Parameters.AddWithValue("@CatalogID", catalogId);
            cmd.Parameters.AddWithValue("@RubroID", rubroId);

            return DataConvert.ToProviderVoucher(this.ExecuteDataSet(cmd));
        }

        public void ProviderVoucher_iu(ProviderVoucherEntity entity)
        {
            SqlCommand cmd = new SqlCommand("ProviderVoucher_iu");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@idProvider", entity.id);
            cmd.Parameters.AddWithValue("@image", entity.image);
            cmd.Parameters.AddWithValue("@tycVoucher", entity.tycVoucher);
            cmd.Parameters.AddWithValue("@tycHotel", entity.tycHotel);
            cmd.Parameters.AddWithValue("@tycVirtual", entity.tycVirtual);
            cmd.Parameters.AddWithValue("@meVoucher", entity.meVoucher);
            cmd.Parameters.AddWithValue("@meHotel", entity.meHotel);
            cmd.Parameters.AddWithValue("@meVirtual", entity.meVirtual);
            cmd.Parameters.AddWithValue("@imageGC", entity.imageGC);

            this.ExecuteDataSet(cmd);
        }
    }
}
