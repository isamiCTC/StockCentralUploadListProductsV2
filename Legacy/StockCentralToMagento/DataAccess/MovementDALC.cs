using System;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;

using Redemption.Core.Common;
using StockCentralToMagento.Entities;

namespace StockCentralToMagento.DataAccess
{
	public class MovementDALC : SqlDataComponent
	{
		public MovementDALC()
		{
		}
		public decimal Insert(MovementEntity item)
		{   
			SqlCommand cmd = new SqlCommand("MovementInsertEx");
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.Add("@MovimientoTipoID",SqlDbType.SmallInt);
			cmd.Parameters.Add("@SucursalOrigen",SqlDbType.NVarChar,12);
			cmd.Parameters.Add("@SucursalFinal",SqlDbType.NVarChar,12);
			cmd.Parameters.Add("@DepositoOrigen",SqlDbType.NVarChar,6);
			cmd.Parameters.Add("@DepositoFinal",SqlDbType.NVarChar,6);
			cmd.Parameters.Add("@ArticuloCodigo",SqlDbType.NVarChar,32);
			cmd.Parameters.Add("@Inicial",SqlDbType.Float);
			cmd.Parameters.Add("@Final",SqlDbType.Float);
			cmd.Parameters.Add("@CuentaID",SqlDbType.NVarChar,50);
			cmd.Parameters.Add("@Descripcion",SqlDbType.NVarChar,256);
			cmd.Parameters.Add("@UserID",SqlDbType.SmallInt);
			cmd.Parameters.Add("@OperacionID",SqlDbType.Decimal);
			cmd.Parameters.Add("@AdicionalData",SqlDbType.NVarChar,512);
			cmd.Parameters.Add("@TckCount",SqlDbType.Float);
			cmd.Parameters.Add("@MonCount",SqlDbType.Float);
			cmd.Parameters.Add("@MilesCount",SqlDbType.Float);
			cmd.Parameters.Add("@Ret",SqlDbType.Decimal );

			cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;
			cmd.Parameters["@OperacionID"].Precision = 18;
			cmd.Parameters["@OperacionID"].Scale= 0;

			cmd.Parameters["@MovimientoTipoID"].Value = item.MovimientoTipoID;
			cmd.Parameters["@SucursalOrigen"].Value = item.SucursalOrigen;
			cmd.Parameters["@SucursalFinal"].Value = item.SucursalFinal;
			cmd.Parameters["@DepositoOrigen"].Value = item.DepositoOrigen;
			cmd.Parameters["@DepositoFinal"].Value = item.DepositoFinal;
			cmd.Parameters["@ArticuloCodigo"].Value = item.ArticuloCodigo;
			cmd.Parameters["@Inicial"].Value = item.Inicial;
			cmd.Parameters["@Final"].Value = item.Final;
			cmd.Parameters["@CuentaID"].Value = item.CuentaID;
			cmd.Parameters["@Descripcion"].Value = item.Descripcion;
			cmd.Parameters["@UserID"].Value = item.UserID;
			cmd.Parameters["@OperacionID"].Value = item.OperacionID;
			cmd.Parameters["@AdicionalData"].Value = item.AdicionalData;
			cmd.Parameters["@TckCount"].Value = item.TckCount;
			cmd.Parameters["@MonCount"].Value = item.MonCount;
			cmd.Parameters["@MilesCount"].Value = item.MilesCount;

            cmd.Parameters.AddWithValue("@CatalogId", item.CatalogId);
			this.ExecuteNonQuery(cmd);
			return Convert.ToDecimal (cmd.Parameters["@Ret"].Value);
		}

        public decimal Insert(MovementEntity item, IDbTransaction Trn)//Juan 28/11/2017 Duplicado para insertar transacciones
        {
            SqlTransaction Tran = (SqlTransaction)Trn;
            SqlConnection Cnn = Tran.Connection;

            //SqlCommand cmd = new SqlCommand("MovementInsertEx",Cnn,Tran);
            SqlCommand cmd = new SqlCommand("MovementInsertEx",Cnn,Tran);
            //cmd.Transaction = Tran;
            //cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@MovimientoTipoID", SqlDbType.SmallInt);
            cmd.Parameters.Add("@SucursalOrigen", SqlDbType.NVarChar, 12);
            cmd.Parameters.Add("@SucursalFinal", SqlDbType.NVarChar, 12);
            cmd.Parameters.Add("@DepositoOrigen", SqlDbType.NVarChar, 6);
            cmd.Parameters.Add("@DepositoFinal", SqlDbType.NVarChar, 6);
            cmd.Parameters.Add("@ArticuloCodigo", SqlDbType.NVarChar, 32);
            cmd.Parameters.Add("@Inicial", SqlDbType.Float);
            cmd.Parameters.Add("@Final", SqlDbType.Float);
            cmd.Parameters.Add("@CuentaID", SqlDbType.NVarChar, 50);
            cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 256);
            cmd.Parameters.Add("@UserID", SqlDbType.SmallInt);
            cmd.Parameters.Add("@OperacionID", SqlDbType.Decimal);
            cmd.Parameters.Add("@AdicionalData", SqlDbType.NVarChar, 512);
            cmd.Parameters.Add("@TckCount", SqlDbType.Float);
            cmd.Parameters.Add("@MonCount", SqlDbType.Float);
            cmd.Parameters.Add("@MilesCount", SqlDbType.Float);
            cmd.Parameters.Add("@Ret", SqlDbType.Decimal);

            cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;
            cmd.Parameters["@OperacionID"].Precision = 18;
            cmd.Parameters["@OperacionID"].Scale = 0;

            cmd.Parameters["@MovimientoTipoID"].Value = item.MovimientoTipoID;
            cmd.Parameters["@SucursalOrigen"].Value = item.SucursalOrigen;
            cmd.Parameters["@SucursalFinal"].Value = item.SucursalFinal;
            cmd.Parameters["@DepositoOrigen"].Value = item.DepositoOrigen;
            cmd.Parameters["@DepositoFinal"].Value = item.DepositoFinal;
            cmd.Parameters["@ArticuloCodigo"].Value = item.ArticuloCodigo;
            cmd.Parameters["@Inicial"].Value = item.Inicial;
            cmd.Parameters["@Final"].Value = item.Final;
            cmd.Parameters["@CuentaID"].Value = item.CuentaID;
            cmd.Parameters["@Descripcion"].Value = item.Descripcion;
            cmd.Parameters["@UserID"].Value = item.UserID;
            cmd.Parameters["@OperacionID"].Value = item.OperacionID;
            cmd.Parameters["@AdicionalData"].Value = item.AdicionalData;
            cmd.Parameters["@TckCount"].Value = item.TckCount;
            cmd.Parameters["@MonCount"].Value = item.MonCount;
            cmd.Parameters["@MilesCount"].Value = item.MilesCount;

            cmd.Parameters.AddWithValue("@CatalogId", item.CatalogId);
            
            cmd.CommandType = CommandType.StoredProcedure;

            //return (DAO.ExecuteNonQuery(cmd));
            //this.ExecuteNonQuery(cmd);
             cmd.ExecuteNonQuery();
             return Convert.ToDecimal(cmd.Parameters["@Ret"].Value);
        }

//		public decimal InsertConcil(MovementConcilEntity item)
//		{
//			SqlCommand cmd = new SqlCommand("MovementConcilInsert");
//			cmd.CommandType = CommandType.StoredProcedure;
//
//			cmd.Parameters.Add("@MovimientoTipoID",SqlDbType.SmallInt);
//			cmd.Parameters.Add("@Sucursal",SqlDbType.NVarChar,20);
//			cmd.Parameters.Add("@SucursalOrigen",SqlDbType.NVarChar,20);
//			cmd.Parameters.Add("@SucursalFinal",SqlDbType.NVarChar,20);
//			cmd.Parameters.Add("@DepositoOrigen",SqlDbType.NVarChar,6);
//			cmd.Parameters.Add("@DepositoFinal",SqlDbType.NVarChar,6);
//			cmd.Parameters.Add("@ArticuloCodigo",SqlDbType.NVarChar,32);
//			cmd.Parameters.Add("@Inicial",SqlDbType.Float);
//			cmd.Parameters.Add("@Final",SqlDbType.Float);
//			cmd.Parameters.Add("@FechaAlta",SqlDbType.DateTime);
//			cmd.Parameters.Add("@CuentaID",SqlDbType.NVarChar,50);
//			cmd.Parameters.Add("@Descripcion",SqlDbType.NVarChar,256);
//			cmd.Parameters.Add("@UserID",SqlDbType.SmallInt);
//			cmd.Parameters.Add("@OperacionID",SqlDbType.Int);
//			cmd.Parameters.Add("@AdicionalData",SqlDbType.NVarChar,500);
//			cmd.Parameters.Add("@ConcilID",SqlDbType.NVarChar,128);
//			cmd.Parameters.Add("@TckCount",SqlDbType.Float);
//			cmd.Parameters.Add("@MonCount",SqlDbType.Float);
//			cmd.Parameters.Add("@MlsCount",SqlDbType.Float);
//			cmd.Parameters.Add("@Ret",SqlDbType.Decimal );
//
//			cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;
//
//			cmd.Parameters["@MovimientoTipoID"].Value = item.MovimientoTipoID;
//			cmd.Parameters["@Sucursal"].Value = item.Sucursal ;
//			cmd.Parameters["@SucursalOrigen"].Value = item.SucursalOrigen;
//			cmd.Parameters["@SucursalFinal"].Value = item.SucursalFinal;
//			cmd.Parameters["@DepositoOrigen"].Value = item.DepositoOrigen;
//			cmd.Parameters["@DepositoFinal"].Value = item.DepositoFinal;
//			cmd.Parameters["@ArticuloCodigo"].Value = item.ArticuloCodigo;
//			cmd.Parameters["@Inicial"].Value = item.Inicial;
//			cmd.Parameters["@Final"].Value = item.Final;
//			cmd.Parameters["@FechaAlta"].Value = item.FechaAlta ;
//			cmd.Parameters["@ConcilID"].Value = item.ConcilID ;
//			cmd.Parameters["@CuentaID"].Value = item.CuentaID;
//			cmd.Parameters["@Descripcion"].Value = item.Descripcion;
//			cmd.Parameters["@UserID"].Value = item.UserID;
//			cmd.Parameters["@OperacionID"].Value = item.OperacionID;
//			cmd.Parameters["@AdicionalData"].Value = item.AdicionalData;
//			cmd.Parameters["@TckCount"].Value = item.TckCount;
//			cmd.Parameters["@MonCount"].Value = item.MonCount;
//			cmd.Parameters["@MlsCount"].Value = item.MlsCount;
//			
//			this.ExecuteNonQuery(cmd);
//
//			return Convert.ToDecimal(cmd.Parameters["@Ret"].Value);
//		}

		public decimal InsertConcil(MovementConcilEntity item)
		{
			SqlCommand cmd = new SqlCommand("MovementConcilInsert_176");
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.Add("@MovimientoTipoID",SqlDbType.SmallInt);
			cmd.Parameters.Add("@Sucursal",SqlDbType.NVarChar,20);
			cmd.Parameters.Add("@SucursalOrigen",SqlDbType.NVarChar,20);
			cmd.Parameters.Add("@SucursalFinal",SqlDbType.NVarChar,20);
			cmd.Parameters.Add("@DepositoOrigen",SqlDbType.NVarChar,6);
			cmd.Parameters.Add("@DepositoFinal",SqlDbType.NVarChar,6);
			cmd.Parameters.Add("@ArticuloCodigo",SqlDbType.NVarChar,32);
			cmd.Parameters.Add("@Inicial",SqlDbType.Float);
			cmd.Parameters.Add("@Final",SqlDbType.Float);
			cmd.Parameters.Add("@FechaAlta",SqlDbType.DateTime);
			cmd.Parameters.Add("@CuentaID",SqlDbType.NVarChar,50);
			cmd.Parameters.Add("@Descripcion",SqlDbType.NVarChar,256);
			cmd.Parameters.Add("@UserID",SqlDbType.SmallInt);
			//SAFnew agrego userName
			cmd.Parameters.Add("@UserName",SqlDbType.NVarChar,256);
			cmd.Parameters.Add("@OperacionID",SqlDbType.Int);
			cmd.Parameters.Add("@AdicionalData",SqlDbType.NVarChar,512);
			cmd.Parameters.Add("@ConcilID",SqlDbType.NVarChar,128);
			cmd.Parameters.Add("@TckCount",SqlDbType.Float);
			cmd.Parameters.Add("@MonCount",SqlDbType.Float);
			cmd.Parameters.Add("@MlsCount",SqlDbType.Float);
			cmd.Parameters.Add("@Cost_FIFO",SqlDbType.Decimal );
			cmd.Parameters.Add("@Cost_PPP",SqlDbType.Decimal );
			cmd.Parameters.Add("@Sale_Amount",SqlDbType.Decimal );
//			cmd.Parameters.Add("@Tck_Amount",SqlDbType.Int);
//			cmd.Parameters.Add("@Mls_Amount",SqlDbType.Int);
			cmd.Parameters.Add("@Supplier_name",SqlDbType.NVarChar,40);

			cmd.Parameters.Add("@Ret",SqlDbType.Decimal );

			cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;

			cmd.Parameters["@MovimientoTipoID"].Value = item.MovimientoTipoID;
			cmd.Parameters["@Sucursal"].Value = item.Sucursal ;
			cmd.Parameters["@SucursalOrigen"].Value = item.SucursalOrigen;
			cmd.Parameters["@SucursalFinal"].Value = item.SucursalFinal;
			cmd.Parameters["@DepositoOrigen"].Value = item.DepositoOrigen;
			cmd.Parameters["@DepositoFinal"].Value = item.DepositoFinal;
			cmd.Parameters["@ArticuloCodigo"].Value = item.ArticuloCodigo;
			cmd.Parameters["@Inicial"].Value = item.Inicial;
			cmd.Parameters["@Final"].Value = item.Final;
			cmd.Parameters["@FechaAlta"].Value = item.FechaAlta ;
			cmd.Parameters["@ConcilID"].Value = item.ConcilID ;
			cmd.Parameters["@CuentaID"].Value = item.CuentaID;
			cmd.Parameters["@Descripcion"].Value = item.Descripcion;
			cmd.Parameters["@UserID"].Value = item.UserID;
			cmd.Parameters["@UserName"].Value = item.UserName;
			cmd.Parameters["@OperacionID"].Value = item.OperacionID;
			cmd.Parameters["@AdicionalData"].Value = item.AdicionalData;
			cmd.Parameters["@TckCount"].Value = item.TckCount;
			cmd.Parameters["@MonCount"].Value = item.MonCount;
			cmd.Parameters["@MlsCount"].Value = item.MlsCount;
			cmd.Parameters["@Cost_FIFO" ].Value = item.Cost_FIFO;
			cmd.Parameters["@Cost_PPP"].Value = item.Cost_PPP;
			cmd.Parameters["@Sale_Amount" ].Value = item.Sale_Amount;
//			cmd.Parameters["@Tck_Amount"].Value = item.Tck_Amount;
//			cmd.Parameters["@Mls_Amount"].Value = item.Mls_Amount;
			cmd.Parameters["@Supplier_name"].Value = item.Supplier_name;

			
			this.ExecuteNonQuery(cmd);

			return Convert.ToDecimal(cmd.Parameters["@Ret"].Value);
		}

        public void Update(MovementEntity movement)
        {
            SqlCommand cmd = new SqlCommand("MovementUpdate");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@idmovimiento", SqlDbType.Decimal).Value = movement.MovimientoID;
            cmd.Parameters.Add("@adicionaldata", SqlDbType.NVarChar).Value = movement.AdicionalData;
            cmd.Parameters.Add("@articulocode", SqlDbType.NVarChar).Value = movement.ArticuloCodigo;
            cmd.Parameters.Add("@cuentaid", SqlDbType.NVarChar).Value = movement.CuentaID;
            cmd.Parameters.Add("@deposito_final", SqlDbType.NVarChar).Value = movement.DepositoFinal;
            cmd.Parameters.Add("@deposito_origen", SqlDbType.NVarChar).Value = movement.DepositoOrigen;
            cmd.Parameters.Add("@fechaalta", SqlDbType.DateTime).Value = movement.FechaAlta;
            cmd.Parameters.Add("@cantvalfinal", SqlDbType.Float).Value = movement.Final;
            cmd.Parameters.Add("@cantvalinicial", SqlDbType.Float).Value = movement.Inicial;
            cmd.Parameters.Add("@moncount", SqlDbType.Float).Value = movement.MonCount;
            cmd.Parameters.Add("@tipomovimid", SqlDbType.Int).Value = movement.MovimientoTipoID;
            cmd.Parameters.Add("@redemptionid", SqlDbType.Decimal).Value = movement.OperacionID;
            cmd.Parameters.Add("@sucursal_final", SqlDbType.NVarChar).Value = movement.SucursalFinal;
            cmd.Parameters.Add("@sucursal_origen", SqlDbType.NVarChar).Value = movement.SucursalOrigen;
            cmd.Parameters.Add("@tckcount", SqlDbType.Float).Value = movement.TckCount;
            cmd.Parameters.Add("@adminid", SqlDbType.Int).Value = movement.UserID;
            cmd.Parameters.Add("@milescount", SqlDbType.Float).Value = movement.MilesCount;
            cmd.Parameters.Add("@fechaconcil", SqlDbType.DateTime).Value = movement.FechaConcil;
            cmd.Parameters.Add("@idconcil", SqlDbType.NVarChar).Value = movement.ConcilID;

            ExecuteNonQuery(cmd);

        }

        public int GetLastMovimID()
        {
            SqlCommand cmd = new SqlCommand("GetLastMovementID");
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;
            cmd.Parameters.Add("@Ret", SqlDbType.Int);
            cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;
            this.ExecuteNonQuery(cmd);
            return Convert.ToInt32(cmd.Parameters["@Ret"].Value);
        }

      
		public int UpdateMovimID(decimal id, DateTime fechaconcil, string concilid)
		{
			SqlCommand cmd = new SqlCommand("MovementUpdateID");
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.Add("@MovimientoID",SqlDbType.Decimal);
			cmd.Parameters.Add("@FechaConcil",SqlDbType.DateTime);
			cmd.Parameters.Add("@ConcilID",SqlDbType.VarChar);
			cmd.Parameters.Add("@Ret",SqlDbType.Int);

			cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;

			cmd.Parameters["@MovimientoID"].Value = id;
			cmd.Parameters["@FechaConcil"].Value = fechaconcil;
			cmd.Parameters["@ConcilID"].Value = concilid;
			
			this.ExecuteNonQuery(cmd);

			return Convert.ToInt32(cmd.Parameters["@Ret"].Value);
		}
		public int InsertConcilState(decimal id, DateTime fechaconcil, string concilid)
		{
			SqlCommand cmd = new SqlCommand("InsertConcilState");
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.Add("@MovimientoID",SqlDbType.Decimal);
			cmd.Parameters.Add("@FechaConcil",SqlDbType.DateTime);
			cmd.Parameters.Add("@ConcilID",SqlDbType.VarChar);
			cmd.Parameters.Add("@Ret",SqlDbType.Int);

			cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;

			cmd.Parameters["@MovimientoID"].Value = id;
			cmd.Parameters["@FechaConcil"].Value = fechaconcil;
			cmd.Parameters["@ConcilID"].Value = concilid;
			
			this.ExecuteNonQuery(cmd);

			return Convert.ToInt32(cmd.Parameters["@Ret"].Value);
		}

        public MovementEntity[] GetMovementByTypeAndRedemption(int movementTypeID, int Redemption)
        {
            SqlCommand cmd = new SqlCommand("GetMovementViewByTypeAndRedemption");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@redemptionid", SqlDbType.Decimal).Value = Convert.ToDecimal(Redemption);
            cmd.Parameters.Add("@tipomovimid", SqlDbType.Int).Value = movementTypeID;

            return DataConvert.ToMovementCollection(this.ExecuteDataSet(cmd));
        }

		
		public MovementEntity GetByTypeIDAdicData(int movementTypeID, string adicionalData)
		{
			SqlCommand cmd = new SqlCommand("MovimientoGetByTypeIDAdicData");
			cmd.CommandType = CommandType.StoredProcedure;
			
			cmd.Parameters.Add("@MovimientoTipoID",SqlDbType.Int);
			cmd.Parameters.Add("@AdicionalData",SqlDbType.NVarChar,50);
			cmd.Parameters["@MovimientoTipoID"].Value = movementTypeID;
			cmd.Parameters["@AdicionalData"].Value = adicionalData;

			return DataConvert.ToMovement(this.ExecuteDataSet(cmd));
		}
        public MovementEntity[] GetByTypeID_BarCode(int movementTypeID, string BarCode)
        {
            SqlCommand cmd = new SqlCommand("MovimientoGetByTypeID_BarCode");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@MovimientoTipoID", SqlDbType.Int);
            cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar, 50);
            cmd.Parameters["@MovimientoTipoID"].Value = movementTypeID;
            cmd.Parameters["@BarCode"].Value = BarCode;

            return DataConvert.ToMovementCollection(this.ExecuteDataSet(cmd));
        }


        public MovementEntity[] GetMovementsFromID(int movementID)
        {
            SqlCommand cmd = new SqlCommand("MovimientosGetFromID");
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;
            cmd.Parameters.Add("@MovimientoID", SqlDbType.Int);
            cmd.Parameters["@MovimientoID"].Value = movementID;

            return DataConvert.ToMovementCollection(this.ExecuteDataSet(cmd));
        }
        public MovementEntity[] GetMovementsFromID(int movementID, string ListaMovType)
        {
            SqlCommand cmd = new SqlCommand("MovimientosGetFromIDandTypeList");
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;
            cmd.Parameters.Add("@MovimientoID", SqlDbType.Int);
            cmd.Parameters["@MovimientoID"].Value = movementID;
            cmd.Parameters.Add("@MovimientoTipoID", SqlDbType.VarChar);
            cmd.Parameters["@MovimientoTipoID"].Value = ListaMovType;

            return DataConvert.ToMovementCollection(this.ExecuteDataSet(cmd));
        }

        public MovementEntity GetLastByTypeIDAndBarCode(int movementTypeID, string barcode)
        {
            SqlCommand cmd = new SqlCommand("MovimientoGetLastByTypeIDAndBarcode");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@MovementTypeID", SqlDbType.Int).Value = movementTypeID;
            cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar).Value = barcode;

            return DataConvert.ToMovement(this.ExecuteDataSet(cmd));
        }

        public MovementConcilEntity GetByConcilID(string movementID)
		{
			SqlCommand cmd = new SqlCommand("MovimientoConcilGetByID");
			cmd.CommandType = CommandType.StoredProcedure;
			
			cmd.Parameters.Add("@MovimientoID",SqlDbType.VarChar);
			cmd.Parameters["@MovimientoID"].Value = movementID; 

			return DataConvert.ToMovementConcil(this.ExecuteDataSet(cmd));
		}
		public MovementConcilEntity GetByConcil()
		{
			SqlCommand cmd = new SqlCommand("MovimientoGetByConcil");
			cmd.CommandType = CommandType.StoredProcedure;
			return DataConvert.ToMovementConcil(this.ExecuteDataSet(cmd));
		}
        public MovementEntity GetByID(decimal movementID)
        {
            SqlCommand cmd = new SqlCommand("GetMovementbyID");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@idmovimiento", SqlDbType.Decimal);
            cmd.Parameters["@idmovimiento"].Value = movementID;

            return DataConvert.ToMovement(this.ExecuteDataSet(cmd));
        }

        public MovementEntity GetByTypeRedempExtCodeCCPAndBCode(int movType, decimal redempID, string extOpeCode, string ccp, string barcode)
        {
            SqlCommand cmd = new SqlCommand("MovimientoGetByTypeRedempExtCodeCCPAndBCode");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@TipomovimID", SqlDbType.Int);
            cmd.Parameters.Add("@RedemptionID", SqlDbType.Decimal);
            cmd.Parameters.Add("@AdicionalData", SqlDbType.NVarChar);
            cmd.Parameters.Add("@CuentaID", SqlDbType.NVarChar);
            cmd.Parameters.Add("@ArticuloCode", SqlDbType.NVarChar);

            cmd.Parameters["@TipomovimID"].Value = movType;
            cmd.Parameters["@RedemptionID"].Value = redempID;
            cmd.Parameters["@AdicionalData"].Value = extOpeCode;
            cmd.Parameters["@CuentaID"].Value = ccp;
            cmd.Parameters["@ArticuloCode"].Value = barcode;

            return DataConvert.ToMovement(this.ExecuteDataSet(cmd));
        }

        public float GetMoncountByRedemptionIdAndCatalogId(decimal redemptionId, int catalogId)
        {
            SqlCommand cmd = new SqlCommand("MovimientoGetMoncountByRedemptionIdAndCatalogId");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RedemptionID", redemptionId);
            cmd.Parameters.AddWithValue("@CatalogID", catalogId);
            cmd.Parameters.Add("@res", SqlDbType.Float);
            cmd.Parameters["@res"].Direction = ParameterDirection.Output;

            this.ExecuteNonQuery(cmd);

            return Convert.ToSingle(cmd.Parameters["@res"].Value);
        }

        public decimal GetIdMovimientoByRedemptionIdAndCatalogId(decimal redemptionId, int catalogId)
        {
            SqlCommand cmd = new SqlCommand("MovimientoGetIdMovByRedemptionIdAndCatalogId");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RedemptionID", redemptionId);
            cmd.Parameters.AddWithValue("@CatalogID", catalogId);
            cmd.Parameters.Add("@res", SqlDbType.Decimal);
            cmd.Parameters["@res"].Direction = ParameterDirection.Output;

            this.ExecuteNonQuery(cmd);

            return Convert.ToDecimal(cmd.Parameters["@res"].Value);
        }
    }
}