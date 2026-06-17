using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

using StockCentralToMagento.Entities;
using Redemption.Core.Common;


namespace StockCentralToMagento.DataAccess
{
    public class CatalogDALC : SqlDataComponent
    {

        /// <summary>
        /// Retorna una lista de CatalogStockEntity
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStock(int CatalogID, int getImage)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtInventario");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 720;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity filtrando por vendor
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStock(int CatalogID, int getImage, bool vendor)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtInventarioVendor");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@vendor", SqlDbType.Bit ).Value = vendor;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity filtrando por vendor filtrando por cantidad de stock mayor o menor al SORT
        /// Sort = 0 -> Obtiene productos con stock < a StockControlTope
        /// Sort = 1 -> Obtiene productos con stock >= a StockControlTope
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStock(int CatalogID, int getImage, bool vendor, int StockControlTope, bool ordenamiento, bool IncludeZeroStock, int Integrador )
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtInventarioVendorbyTopeStockandSort");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@vendor", SqlDbType.Bit).Value = vendor;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;
                cmd.Parameters.Add("@sort", SqlDbType.Bit).Value = ordenamiento;
                cmd.Parameters.Add("@StockQty", SqlDbType.Int).Value = StockControlTope;
                cmd.Parameters.Add("@IncludeZeroStock", SqlDbType.Bit).Value = IncludeZeroStock;
                cmd.Parameters.Add("@Integrador", SqlDbType.Int).Value = Integrador;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }


        /// <summary>
        /// Retorna una lista de CatalogStockEntity
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStock(int CatalogID, int getImage, int ProviderID)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtInventarioProvider");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ProviderID", SqlDbType.Int).Value = ProviderID;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity de productos desactivados
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStockDisabled(int CatalogID, int getImage)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtInventarioDisable");
                cmd.CommandTimeout = 360;
                
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity de productos desactivados para el proveedor
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStockDisabled(int CatalogID, int getImage, int ProviderId)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtInventarioDisablebyProviderId");
                cmd.CommandTimeout = 360;

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;
                cmd.Parameters.Add("@ProviderId", SqlDbType.Int).Value = ProviderId;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity de productos que hay que actualizar precios
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogPriceUpdated(int CatalogID, int getImage)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllMagentoArtPriceUpdated");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = getImage;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de ID de productos que notificó producteca que hay que actualizar 
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<ProductNotificationEntity> GetAllProductsNotifications(int ProviderID, int Estado)
        {
            List<ProductNotificationEntity> ProductList = new List<ProductNotificationEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllNotifiedProductsByState");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ProviderID", SqlDbType.NVarChar).Value = ProviderID;
                cmd.Parameters.Add("@Estado", SqlDbType.Int).Value = Estado;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        ProductList.Add(DataConvert.ToProductNotification(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ProductList;
        }


        public int UpdateArtExtNotifications(int ID, int estado )
        {
            SqlCommand cmd = new SqlCommand("UpdateArtExtNotifications");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@id", SqlDbType.Decimal).Value = ID;
              cmd.Parameters.Add("@estado", SqlDbType.NVarChar).Value = estado;

            return ExecuteNonQuery(cmd);

        }




        /// <summary>
        /// Retorna una lista de CatalogStockEntity
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllFeaturedProduct(int CatalogID)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getFeaturedProduct");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        public string[] GetDataToImportListProductsToMagento(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoUPD(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_UPD");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }

        public string[] GetDataToImportListProductsToMagentoNEW(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_MKP_NEW");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }

        public string[] GetPriceDataToImportListProductsToMagento(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportPricesListProductsToMagento_CreateFile");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetPriceDataToImportListProductsToMagentoUpdate(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportPricesListProductsToMagento_CreateFileUpdate");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetPriceDataToImportListProductsToMagento(int CatalogID, string Productos, int StoreView)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportPricesListProductsToMagento_CreateFile");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = StoreView;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }

        public string[] GetPriceDataToImportListProductsToMagentoUpdate(int CatalogID, string Productos, int StoreView)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportPricesListProductsToMagento_CreateFileUpdate");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = StoreView;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }

        public string[] GetDataToImportListProductsToMagentoMkp(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoCatMkp(int CatalogID, int Cat)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = Cat;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "-3";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoSubCatMkp(int CatalogID, int SCat)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = SCat;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "-4";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoMkp(int CatalogID, int Proveedor)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = Proveedor;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "0";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoMkpUpd(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_UPD");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoMkpNew(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_NEW");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoMkpUpd(int CatalogID, int Proveedor)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_UPD");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = Proveedor;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "0";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsPricesToMagentoMkpUpd(int CatalogID, int Proveedor)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_UPD_PRICES");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = Proveedor;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "0";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToMagentoMkpNew(int CatalogID, int Proveedor)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_NEW");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = Proveedor;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "0";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsPricesToMagentoMkpUpdPromo(int CatalogID, int Proveedor)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_UPD_PRICES_PROMO");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = Proveedor;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = "0";
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsPricesToMagentoMkpUpd(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_UPD_PRICES");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsPricesToMagentoMkpUpdPromo(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToMagento_CreateFile_Mkp_UPD_PRICES_PROMO");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@ImportPrice", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@ImportProductFull", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                cmd.CommandTimeout = 360;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }

        public string[] GetDataToImportListProductsToPIN_COLLINSON(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToPIN_COLLINSON_CreateFile");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsPriceUpdatedToPIN_COLLINSON(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsPriceUpdatedToPIN_COLLINSON_CreateFile");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public string[] GetDataToImportListProductsToPIN_COLLINSON_PROMOTIONS(int CatalogID, string Productos)
        {
            string[] Lista = null;
            try
            {
                SqlCommand cmd = new SqlCommand("ImportListProductsToPIN_COLLINSON_PROMOTION_CreateFile");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@catalogo", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@Products", SqlDbType.NVarChar).Value = Productos;
                using (DataSet ds = ExecuteDataSet(cmd))
                {
                    Lista = dataset_to_file_csv(ds);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Lista;
        }
        public List<MagentoCatalogEntity> GetCatalogMagento()
        {
            List<MagentoCatalogEntity> CatalogList = new List<MagentoCatalogEntity>();
            try
            {
                SqlCommand cmd = new SqlCommand("getCatalogMagento");
                cmd.CommandType = CommandType.StoredProcedure;
             

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogList.Add(DataConvert.FillCatalogMagento (dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogList;
        }


        public MagentoCatalogEntity GetCatalogMagentoByILSCatalogID( int CatalogID)
        {
            MagentoCatalogEntity Catalog = new MagentoCatalogEntity();
            try
            {
                SqlCommand cmd = new SqlCommand("getCatalogMagentoByILScatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        Catalog = DataConvert.FillCatalogMagento(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Catalog;
        }

        public MagentoStoreEntity GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(int MagentoWebSiteId, int CatalogID)
        {
            MagentoStoreEntity MagentoStore = new MagentoStoreEntity();
            try
            {
                SqlCommand cmd = new SqlCommand("GetStoreIdfromMagentoIDAndIlsCatalogID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@MagentoWebSiteID", SqlDbType.Int).Value = MagentoWebSiteId;

                

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        MagentoStore = DataConvert.FillStoreMagento(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return MagentoStore;
        }
        private string[] dataset_to_file_csv(DataSet ds)
        {
            String[] texto;
            texto = new String[ds.Tables[0].Rows.Count + 1];
            //Rellenamos la cabecera del fichero
            texto[0] = String.Empty;
            for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
            {
                texto[0] += ds.Tables[0].Columns[i].ColumnName + ";";
            }
            texto[0] = texto[0].Substring(0, texto[0].Length - 1); 
            //Rellenamos el detalle del fichero
            String linea;
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                linea = String.Empty;
                for (int j = 0; j < ds.Tables[0].Columns.Count; j++)
                {
                    linea += ds.Tables[0].Rows[i][j].ToString() + ";";
                }
                texto[i + 1] = linea.Substring(0, linea.Length - 1); ;
            }
            //File.WriteAllLines(path + ".csv", texto);
            return texto;
        }
        /// <summary>
        /// Retorna una lista de CatalogStockEntity
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public CatalogStockEntity GetFeaturedProduct(int CatalogID)
        {
            CatalogStockEntity CatalogStock = new CatalogStockEntity();

            try
            {
                SqlCommand cmd = new SqlCommand("getFeaturedProduct");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        CatalogStock = DataConvert.FillCatalogStock(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStock;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetFeaturedProductGroupByCategory(int CatalogID)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getFeaturedProductGroupByCategory");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;

        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity
        /// </summary>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetFeaturedProductByCategory(int CatalogID, int CategoryID)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getFeaturedProductByCategory");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;

        }


        /// <summary>
        /// Retorna una lista de CatalogStockEntity según parametro especificado
        /// </summary>
        /// <param name="ccp">Cuenta Club Patagonia</param>
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStockByParameters(int MinPoints,
            int MaxPoints,
            int Category,
            int SubCategory,
            int CatalogID,
            int deliveryType,
            int GetImage)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getArtInventarioByParameters");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@minPoints", SqlDbType.Int).Value = MinPoints;
                cmd.Parameters.Add("@maxPoints", SqlDbType.Int).Value = MaxPoints;
                cmd.Parameters.Add("@Category", SqlDbType.Int).Value = Category;
                cmd.Parameters.Add("@SubCategory", SqlDbType.Int).Value = SubCategory;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@DeliveryType", SqlDbType.Int).Value = deliveryType;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = GetImage;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        /// <summary>
        /// Retorna una lista de CatalogStockEntity según parametro especificado
        /// </summary>
        /// int MinPoints,
        /// int MaxPoints,
        /// int Category,
        /// int SubCategory,
        /// int CatalogID,
        /// int deliveryType,
        /// int GetImage
        /// <returns>List[CatalogStockEntity], cuando existe el registro. Null, cuando no existe el registro</returns>
        public List<CatalogStockEntity> GetAllCatalogStockByParametersAndSearchKey(int MinPoints,
            int MaxPoints,
            int Category,
            int SubCategory,
            int CatalogID,
            int deliveryType,
            int GetImage,
            string SearchKey)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getArtInventarioByParametersAndSearchKey");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@minPoints", SqlDbType.Int).Value = MinPoints;
                cmd.Parameters.Add("@maxPoints", SqlDbType.Int).Value = MaxPoints;
                cmd.Parameters.Add("@Category", SqlDbType.Int).Value = Category;
                cmd.Parameters.Add("@SubCategory", SqlDbType.Int).Value = SubCategory;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@DeliveryType", SqlDbType.Int).Value = deliveryType;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = GetImage;
                cmd.Parameters.Add("@SearchKey", SqlDbType.NVarChar ).Value = SearchKey ;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }

        public List<CatalogStockEntity> GetAllCatalogStockByParametersAndProviderID(int MinPoints,
            int MaxPoints,
            int Category,
            int SubCategory,
            int CatalogID,
            int deliveryType,
            int GetImage,
            int attributeID1,
            int attributeID2,
            int providerID)
        {
            List<CatalogStockEntity> CatalogStockList = new List<CatalogStockEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getArtInventarioByParametersAndProviderID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@minPoints", SqlDbType.Int).Value = MinPoints;
                cmd.Parameters.Add("@maxPoints", SqlDbType.Int).Value = MaxPoints;
                cmd.Parameters.Add("@Category", SqlDbType.Int).Value = Category;
                cmd.Parameters.Add("@SubCategory", SqlDbType.Int).Value = SubCategory;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;
                cmd.Parameters.Add("@DeliveryType", SqlDbType.Int).Value = deliveryType;
                cmd.Parameters.Add("@GetImage", SqlDbType.Int).Value = GetImage;

                cmd.Parameters.Add("@AttributeID1", SqlDbType.Int).Value = attributeID1;
                cmd.Parameters.Add("@AttributeID2", SqlDbType.Int).Value = attributeID2;
                cmd.Parameters.Add("@ProviderID", SqlDbType.Int).Value = providerID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        CatalogStockList.Add(DataConvert.FillCatalogStock(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return CatalogStockList;
        }


        /// <summary>
        /// Retorna una entidad de CatalogStockEntity según parametro especificado
        /// </summary>
        /// <param name="BarCode">Codigo de Barra</param>
        /// <returns>CatalogStockEntity, cuando existe el registro. Null, cuando no existe el registro</returns>
        public CatalogStockEntity GetProductStockByBarCode(string BarCode, int CatalogID)
        {
            CatalogStockEntity cse = null;

            try
            {
                SqlCommand cmd = new SqlCommand("getArtInventarioByBarCodeSCTM");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar,64).Value = BarCode;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        cse = DataConvert.FillCatalogStock(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return cse;
        }

        /// <summary>
        /// Retorna una entidad de CatalogStockEntity según parametro especificado
        /// </summary>
        /// <param name="BarCode">Codigo de Barra</param>
        /// <returns>CatalogStockEntity, cuando existe el registro. Null, cuando no existe el registro</returns>
        public CatalogStockEntity GetProductPointRatioByBarCode(string BarCode, int CatalogID)
        {
            CatalogStockEntity cse = null;

            try
            {
                SqlCommand cmd = new SqlCommand("GetProductPointRatioByBarCode");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar, 64).Value = BarCode;
                cmd.Parameters.Add("@CatalogID", SqlDbType.Int).Value = CatalogID;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        cse = DataConvert.FillCatalogStock(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return cse;
        }

        /// <summary>
        /// Retorna el LargeImage según parametro especificado
        /// </summary>
        /// <param name="BarCode">Codigo de Barra</param>
        /// <returns>Image, cuando existe el registro. Null, cuando no existe el registro</returns>
        public ImageEntity GetLargeImageByBarCode(string BarCode)
        {
            ImageEntity img = null;

            try
            {
                SqlCommand cmd = new SqlCommand("getLargeImageByBarCode");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar, 64).Value = BarCode;
                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        img = DataConvert.FillImage(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return img;
        }


        public List<ImageEntity> GetLargeImageByBarCodeList(string BarCode)
        {
            List<ImageEntity> ImageList = new List<ImageEntity>();

            try
            {
                SqlCommand cmd = new SqlCommand("getAllLargeImageByNBarCode");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@BarCode", SqlDbType.NVarChar, 250).Value = BarCode;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        ImageList.Add(DataConvert.FillImage(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ImageList;

        }


        public decimal GetProdcutCostPriceByBarcode(string barcode)
        {
            decimal costPrice = -1;

            try
            {
                SqlCommand cmd = new SqlCommand("GetArticlebyBarCode");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@codigobarra", SqlDbType.NVarChar).Value = barcode;

                using (IDataReader dr = ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        costPrice = Convert.ToDecimal(dr["preciocosto"]);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return costPrice;
        }

        public decimal GetProductCostPriceByBarcode(string barcode, int catalogId)
        {
            decimal costPrice = -1;

            try
            {
                SqlCommand cmd = new SqlCommand("GetArticlebyBarCodeAndCatalog");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@codigobarra", barcode);
                cmd.Parameters.AddWithValue("@catalogid", catalogId);

                using (IDataReader dr = ExecuteReader(cmd))
                    if (dr.Read())
                        costPrice = Convert.ToDecimal(dr["preciocosto"]);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return costPrice;
        }

        public decimal GetProductSalePriceByBarcode(string barcode, int catalogId)
        {
            decimal salePrice = -1;

            try
            {
                SqlCommand cmd = new SqlCommand("GetArticlebyBarCodeAndCatalog");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@codigobarra", barcode);
                cmd.Parameters.AddWithValue("@catalogid", catalogId);

                using (IDataReader dr = ExecuteReader(cmd))
                    if (dr.Read())
                        salePrice = Convert.ToDecimal(dr["precioventa"]);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return salePrice;
        }

        public bool CatalogExists(int catalogId)
        {
            SqlCommand cmd = new SqlCommand("GetCatalogByID");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@id", catalogId);

            DataSet ds = this.ExecuteDataSet(cmd);

            return (ds.Tables[0].Rows.Count > 0);
        }

        public int GetStockByBarCode(string Barcode, int CatalogId)
        {
            SqlCommand cmd = new SqlCommand("InventarioGetStockByBarCode");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar).Value  = Barcode;
            cmd.Parameters.Add("@CatalogoID", SqlDbType.Int).Value = CatalogId;
            cmd.Parameters.Add("@Ret", SqlDbType.Int);
            cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;
            this.ExecuteNonQuery(cmd);
            return Convert.ToInt32(cmd.Parameters["@Ret"].Value);
        }

        public int GetStockByBarCodeCollinson(string Barcode, int CatalogId)
        {
            SqlCommand cmd = new SqlCommand("InventarioGetStockByBarCodeCollinson");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar).Value = Barcode;
            cmd.Parameters.Add("@CatalogoID", SqlDbType.Int).Value = CatalogId;
            cmd.Parameters.Add("@Ret", SqlDbType.Int);
            cmd.Parameters["@Ret"].Direction = ParameterDirection.Output;
            this.ExecuteNonQuery(cmd);
            return Convert.ToInt32(cmd.Parameters["@Ret"].Value);
        }

    }
}