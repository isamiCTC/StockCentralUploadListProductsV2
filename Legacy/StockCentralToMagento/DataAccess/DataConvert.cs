using System;
using System.Data;

using StockCentralToMagento.Entities;

namespace StockCentralToMagento.DataAccess
{
    public class DataConvert
    {
        public DataConvert()
        {
        }

        #region Articulos
        public static CatalogStockEntity FillCatalogStock(IDataReader dr)
        {
            CatalogStockEntity cs = new CatalogStockEntity();
            try
            {
                cs.idarticulo = Convert.ToInt32(dr["idarticulo"]);
                cs.codigobarra = Convert.ToString(dr["codigobarra"]);
                cs.nombre = Convert.ToString(dr["nombre"]);
                cs.fechaalta = Convert.ToDateTime(dr["fechaalta"]);
                cs.fechamodif = Convert.ToDateTime(dr["fechamodif"]);
                cs.preciocosto = (float)(dr["preciocosto"]);
                cs.precioreal = (float)(dr["precioreal"]);
                try { cs.rubro = DataConvert.ToItemSubCategory(dr); }
                catch { }
                //cs.subrubro = Convert.ToInt32(dr["subrubro"]);
                cs.stockmin = Convert.ToInt32(dr["stockmin"]);
                //cs.stockminredemption = Convert.ToInt32(dr["stockminredemption"]);
                cs.enable = Convert.ToBoolean(dr["enable"]);
                cs.precioventa = (float)(dr["precioventa"]);
                cs.preciounidad = (float)(dr["preciounidad"]);
                cs.piezasunidad = Convert.ToString(dr["piezasunidad"]);
                //cs.unidadcasepack = Convert.ToString(dr["unidadcasepack"]);
                //cs.codigointerno = Convert.ToString(dr["codigointerno"]);
                cs.millas = Convert.ToBoolean(dr["millas"]);
                cs.preciomillas = (float)(dr["preciomillas"]);
                cs.providerid = Convert.ToInt32(dr["providerid"]);
                cs.orderquantity = Convert.ToInt32(dr["orderquantity"]);
                cs.stock = Convert.ToInt32(dr["cantidad"]);
                cs.DeliveryType = Convert.ToInt32(dr["DeliveryType"]);
                cs.AttributeID1 = (dr["AttributeID1"] is DBNull ? -1 : Convert.ToInt32(dr["AttributeID1"]));
                cs.AttributeID2 = (dr["AttributeID2"] is DBNull ? -1 : Convert.ToInt32(dr["AttributeID2"]));
                cs.AttributeLabel1 = (dr["AttributeLabel1"] is DBNull ? "" : Convert.ToString(dr["AttributeLabel1"]));
                cs.AttributeLabel2 = (dr["AttributeLabel2"] is DBNull ? "" : Convert.ToString(dr["AttributeLabel2"]));
                cs.AttributeName1 = (dr["AttributeName1"] is DBNull ? "" : Convert.ToString(dr["AttributeName1"]));
                cs.AttributeName2 = (dr["AttributeName2"] is DBNull ? "" : Convert.ToString(dr["AttributeName2"]));
                cs.ArtDescription = (dr["ArtDescription"] is DBNull ? "" : Convert.ToString(dr["ArtDescription"]));
                if (!(dr["LargeImage"] is DBNull)) cs.LargeImage = (byte[])(dr["LargeImage"]);
                if (!(dr["SmallImage"] is DBNull)) cs.SmallImage = (byte[])(dr["SmallImage"]);
                cs.URL_PDF = "";
                try
                {
                    if (!(dr["URL"] is DBNull)) cs.URL_PDF = dr["URL"].ToString();
                }
                catch { }
                try
                {
                    if (!(dr["ListPrice"] is DBNull)) 
                        cs.preciolista = Convert.ToSingle( dr["ListPrice"]);
                    else
                        cs.preciolista = 0;
                }
                catch {
                    cs.preciolista = 0;
                
                }
                try
                {
                    if (!(dr["AlicuotaIVA"] is DBNull)) 
                        cs.alicuotaIVA = Convert.ToDecimal(dr["AlicuotaIVA"]);
                    else
                        cs.alicuotaIVA = 0;
                }
                catch
                {
                    cs.alicuotaIVA = 0;

                }

            }
            catch (Exception err)
            {
                throw new Exception("Error en datos del catalogo", err.InnerException);      
            }
            return cs;
        }

        public static ImageEntity FillImage(IDataReader dr)
        {
            ImageEntity im = new ImageEntity();

            if (!(dr["Img"] is DBNull)) im.Image = (byte[])(dr["Img"]);
            im.BarCode =Convert.ToString(dr["codigobarra"]);


            return im;
        }

        #endregion
        /*
        public static ItemSubCategoryEntity ToItemSubCategory(DataSet ds)
        {
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return ToItemSubCategory(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }*/

        #region Attribute
        public static AttributeEntity ToAttribute(IDataReader dr)
        {
            AttributeEntity attribute = new AttributeEntity();
            attribute.AttributeID = (int)dr["AttributeID"];
            attribute.AttributeLabel = dr["AttributeLabel"] as string;
            attribute.AttributeName = dr["AttributeName"] as string;

            return attribute;
        }
        #endregion

        #region SubCategory
        public static ItemSubCategoryEntity ToItemSubCategory(IDataReader dr)
        {
            ItemSubCategoryEntity ret = new ItemSubCategoryEntity();
            ret.ItemCategoryID = Convert.ToInt32(dr["ArticuloCategoriaID"]);
            ret.CategoryID = Convert.ToInt32(dr["ArticuloCategoriaRaiz"]);
            ret.Name1 = Convert.ToString(dr["ArticuloCategoriaNombre1"]);
            ret.Name2 = Convert.ToString(dr["ArticuloCategoriaNombre2"]);
            ret.Name3 = Convert.ToString(dr["ArticuloCategoriaNombre3"]);
            ret.Markup = Convert.ToDecimal(dr["ArticuloCategoriaMarkup"]);
            ret.UpdateDate = Convert.ToDateTime(dr["ArticuloCategoriaFechaModificacion"]);
            if (!(dr["CategoryImage"] is DBNull)) ret.CategoryImage = (byte[])(dr["CategoryImage"]);
            return ret;
        }
        #endregion

        #region Menu
        //public static ItemMenuEntity FillMenu(IDataReader dr)
        //{
        //    ItemMenuEntity m = new ItemMenuEntity();

        //    m.ItemMenuID = Convert.ToInt32(dr["ID"]);
        //    m.Name = Convert.ToString(dr["Nombre"]);
        //    try
        //    {
        //        m.ItemMenuPadreID = Convert.ToInt32(dr["idPadre"]);
        //    }
        //    catch(Exception e){}
            
        //    return m;
        //}

        #endregion

        #region Category
        public static ItemCategoryEntity FillCategory(IDataReader dr)
        {
            ItemCategoryEntity cat = new ItemCategoryEntity();

            cat.ItemCategoryID = Convert.ToInt32(dr["CategoriaRubroID"]);
            cat.Name = Convert.ToString(dr["Nombre"]);
            if (!(dr["CategoryIcon"] is DBNull)) cat.CategoryIcon = (byte[])(dr["CategoryIcon"]);


            return cat;
        }

        #endregion

        #region Catalogos Magento
        public static MagentoCatalogEntity FillCatalogMagento(IDataReader dr)
        {
            MagentoCatalogEntity cat = new MagentoCatalogEntity();

            cat.idCatalogoMagento  = Convert.ToInt32(dr["idCatalogoMagento"]);
            cat.idCatalogoILS  = Convert.ToInt32(dr["idCatalogoILS"]);
            cat.id  = Convert.ToInt32(dr["id"]);
            cat.MagentoWebSite  = Convert.ToString(dr["MagentoWebSite"]);
            cat.CategoriaRaiz  = Convert.ToString(dr["Categoria"]);
            try
            {
                cat.UrlCatalogo = (dr["UrlCatalogo"] != System.DBNull.Value ? Convert.ToString(dr["UrlCatalogo"]) : "");
            }catch (Exception y) {
                cat.UrlCatalogo = "";
            }
            return cat;
        }

        #endregion

        #region Store Magento
        public static MagentoStoreEntity FillStoreMagento(IDataReader dr)
        {
            MagentoStoreEntity cat = new MagentoStoreEntity();

            cat.idCatalogoMagento = Convert.ToInt32(dr["idCatalogoMagento"]);
            cat.idCatalogoILS = Convert.ToInt32(dr["idCatalogoILS"]);
            cat.idStoreMagento = Convert.ToInt32(dr["idStoreMagento"]);


            return cat;
        }

        #endregion


        #region Provider
        public static ProviderEntity FillProvider(IDataReader dr)
        {
            ProviderEntity pro = new ProviderEntity();
             
            pro.ID = Convert.ToInt32(dr["ID"]);
            pro.Name = Convert.ToString(dr["Name"]);
            pro.City = Convert.ToString(dr["City"]);
            pro.Country = Convert.ToString(dr["Country"]);
            pro.Telephone = Convert.ToString(dr["Telephone"]);
            pro.Email = Convert.ToString(dr["Email"]);
            pro.Fax = Convert.ToString(dr["Fax"]);
            pro.Contact = Convert.ToString(dr["Contact"]);
            pro.Web = Convert.ToString(dr["Web"]);
            pro.Enabled = Convert.ToBoolean(dr["Enabled"]);
            pro.COD = Convert.ToString(dr["COD"]);
            pro.State = Convert.ToString(dr["State"]);
            pro.ZipCode = Convert.ToString(dr["ZipCode"]);
            pro.Addr = Convert.ToString(dr["Addr"]);
            pro.Deleted = Convert.ToBoolean(dr["Deleted"]);
            try
            {
                if (!(dr["IntegratorID"] is DBNull))
                    pro.IntegratorID = Convert.ToInt32(dr["IntegratorID"]);
                else
                    pro.IntegratorID = 0;
            }
            catch
            {
                pro.IntegratorID = 0;

            }
            try
            {
                if (!(dr["ProviderTokenIntegrator"] is DBNull))
                    pro.Token =(string)(dr["ProviderTokenIntegrator"]);
                else
                    pro.Token = "";
            }
            catch
            {
                pro.Token = "";

            }
            try
            {
                if (!(dr["ExternalProviderAccountID"] is DBNull))
                    pro.ExternalProviderAccountID = dr["ExternalProviderAccountID"].ToString();
                else
                    pro.ExternalProviderAccountID = "";
            }
            catch
            {
                pro.ExternalProviderAccountID = "";

            }

            try
            {
                if (!(dr["Vendor"] is DBNull))
                    pro.Vendor = Convert.ToBoolean(dr["Vendor"]);
                else
                    pro.Vendor = false;
            }
            catch
            {
                pro.Vendor = false;

            }
            return pro;
        }
        #endregion

        #region StoreProvider
        public static StoreProviderEntity FillStoreProvider(IDataReader dr)
        {
            StoreProviderEntity pro = new StoreProviderEntity();

            pro.StoreID = Convert.ToInt32(dr["StoreID"]);
            pro.ProviderID = Convert.ToInt32(dr["ProviderID"]);
            pro.StoreName = Convert.ToString(dr["StoreName"]);
            pro.Direction = Convert.ToString(dr["Direction"]);
            pro.ProviderName = Convert.ToString(dr["ProviderName"]);
            pro.Enabled = Convert.ToBoolean(dr["Enabled"]);

            return pro;
        }
        #endregion

        #region CompleteSubCategory
        public static ItemCompleteSubCategoryEntity ToItemCompleteSubCategory(IDataReader dr)
        {
            ItemCompleteSubCategoryEntity ret = new ItemCompleteSubCategoryEntity();
            ret.ItemCategoryID = Convert.ToInt32(dr["idrubro"]);
            ret.CategoryID = Convert.ToInt32(dr["category"]);
            ret.Name1 = Convert.ToString(dr["descripcionrubro"]);
            ret.Name2 = Convert.ToString(dr["descripcionrubroii"]);
            ret.Name3 = Convert.ToString(dr["descripcionrubroiii"]);
            ret.Markup = Convert.ToDecimal(dr["markup"]);
            ret.UpdateDate = Convert.ToDateTime(dr["fechamodif"]);
            if (!(dr["RubroImage"] is DBNull)) ret.CategoryImage = (byte[])(dr["RubroImage"]);
            if (!(dr["RubroImage2"] is DBNull)) ret.CategoryImage2 = (byte[])(dr["RubroImage2"]);
            if (!(dr["RubroImage3"] is DBNull)) ret.CategoryImage3 = (byte[])(dr["RubroImage3"]);
            if (!(dr["RubroImage4"] is DBNull)) ret.CategoryImage4 = (byte[])(dr["RubroImage4"]);
            ret.LargeDescription = Convert.ToString(dr["LargeDescription"]);
            ret.ShortDescription = Convert.ToString(dr["ShortDescription"]);
            return ret;
        }
        #endregion

        #region MovementEx

        public static MovementExEntity ToMovementEx(DataRow dr)
        {
            MovementExEntity entity = new MovementExEntity();

            entity.AccountID = Convert.ToInt32(dr["AccountID"]);
            entity.AditionalData = Convert.ToString(dr["AditionalData"]);
            entity.OperationCode = Convert.ToString(dr["OperationCode"]);
            try { entity.AuxDate = Convert.ToDateTime(dr["AuxDate"]); }
            catch { entity.AuxDate = DateTime.MaxValue; }
            entity.CardID = Convert.ToString(dr["CardID"]);
            entity.PointChargeID = Convert.ToInt32(dr["PointChargeID"]);
            entity.CommerceID = Convert.ToInt32(dr["CommerceID"]);
            entity.CreationDate = Convert.ToDateTime(dr["CreationDate"]);
            entity.EndValue = Convert.ToDecimal(dr["EndValue"]);
            entity.InitValue = Convert.ToDecimal(dr["InitValue"]);
            entity.MoneyCount = Convert.ToDecimal(dr["MoneyCount"]);
            entity.MovementID = Convert.ToInt32(dr["MovementID"]);
            entity.MovementTypeID = Convert.ToInt32(dr["MovementTypeID"]);
            entity.PersonID = Convert.ToInt32(dr["PersonID"]);
            entity.PointCount = Convert.ToDecimal(dr["PointCount"]);
            entity.Remark = Convert.ToString(dr["Remark"]);
            entity.StoreID = Convert.ToInt32(dr["storeID"]);
            entity.UserID = Convert.ToInt32(dr["UserID"]);

            try { entity.AmountPesos = Convert.ToString(dr["Pesos"]); } catch (Exception r) { }
            try { entity.Cuotas = Convert.ToInt32(dr["Cuotas"]); } catch (Exception r) { }
            try { entity.MarcaTarjeta = Convert.ToString(dr["Tarjeta"]); } catch (Exception r) { }
            try { entity.Tarjeta = Convert.ToString(dr["NroTarjeta"]); } catch (Exception r) { }
            try
            {
                entity.PointCountGlobal = Convert.ToDecimal(dr["MontoGlobalPuntos"]);
            }
            catch (Exception r) { }
            try
            {
                entity.MoneyCountGlobal = Convert.ToDecimal(dr["MontoGlobalPesos"]);
            }
            catch (Exception r) { }

            return entity;
        }

        public static MovementExEntity ToMovementEx(DataSet ds)
        {
            if (ds.Tables[0].Rows.Count > 0 && ds.Tables.Count > 0 && ds != null)
            {
                return ToMovementEx(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static MovementExEntity[] ToMovementExCollection(DataSet ds)
        {
            int n;
            int x;
            MovementExEntity[] ret;

            if (ds.Tables[0].Rows.Count > 0 && ds != null && ds.Tables.Count > 0)
            {
                DataRowCollection dr = ds.Tables[0].Rows;
                ret = new MovementExEntity[dr.Count];
                n = ret.GetLength(0);

                for (x = 0; x < n; x++)
                {
                    ret[x] = ToMovementEx(dr[x]);
                }
                return ret;
            }
            else
            {
                ret = new MovementExEntity[0];
                return ret;
            }
        }

        public static MovementExEntity[] ToMovementExCollection(DataTable dt)
        {
            int n;
            int x;
            MovementExEntity[] ret;

            if (dt.Rows.Count > 0)
            {
                DataRowCollection dr = dt.Rows;
                ret = new MovementExEntity[dr.Count];
                n = ret.GetLength(0);

                for (x = 0; x < n; x++)
                {
                    ret[x] = ToMovementEx(dr[x]);
                }
                return ret;
            }
            else
            {
                ret = new MovementExEntity[0];
                return ret;
            }
        }

        #endregion


        public static MovementEntity[] ToMovementCollection(DataSet ds)
        {
            int n;
            MovementEntity[] ret;

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRowCollection rows = ds.Tables[0].Rows;
                ret = new MovementEntity[rows.Count];
                n = ret.GetLength(0);
                for (int i = 0; i < n; i++)
                {
                    ret[i] = ToMovement(rows[i]);
                }
                return ret;
            }
            else
            {
                ret = new MovementEntity[0];
                return ret;
            }
        }
        public static MovementEntity ToMovement(DataSet ds)
        {
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return ToMovement(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public static MovementEntity ToMovement(DataRow dr)
        {
            MovementEntity ret = new MovementEntity();
            ret.AdicionalData = (dr["MovimientoAdicionalData"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoAdicionalData"]) : "");
            ret.ArticuloCodigo = (dr["MovimientoArticuloCodigo"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoArticuloCodigo"]) : "");
            ret.CuentaID = (dr["MovimientoCuentaID"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoCuentaID"]) : "");
            ret.DepositoFinal = (dr["MovimientoDepositoFinal"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoDepositoFinal"]) : "");
            ret.DepositoOrigen = (dr["MovimientoDepositoOrigen"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoDepositoOrigen"]) : "");
            ret.Descripcion = (dr["MovimientoDescripcion"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoDescripcion"]) : "");
            ret.FechaAlta = Convert.ToDateTime(dr["MovimientoFechaAlta"]);
            ret.Final = Convert.ToSingle(dr["MovimientoFinal"]);
            ret.Inicial = Convert.ToSingle(dr["MovimientoInicial"]);
            ret.MonCount = Convert.ToSingle(dr["MovimientoMonCount"]);
            ret.MovimientoID = Convert.ToDecimal(dr["MovimientoID"]);
            ret.MovimientoTipoID = Convert.ToInt32(dr["MovimientoTipoID"]);
            ret.OperacionID = Convert.ToDecimal(dr["MovimientoOperacionID"]);
            ret.SucursalFinal = (dr["MovimientoSucursalFinal"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoSucursalFinal"]) : "");
            ret.SucursalOrigen = (dr["MovimientoSucursalOrigen"] != System.DBNull.Value ? Convert.ToString(dr["MovimientoSucursalOrigen"]) : "");
            ret.TckCount = Convert.ToSingle(dr["MovimientoTckCount"]);
            ret.UserID = Convert.ToInt32(dr["MovimientoUsuarioID"]);
            ret.MilesCount = Convert.ToSingle(dr["MovimientoMilesCount"]);

            try
            {
                ret.FechaConcil = dr["MovimientoFechaConcil"] as DateTime?;
            }
            catch (Exception)
            {
            }

            try
            {
                ret.ConcilID = dr["MovimientoConcilID"] as string;
            }
            catch (Exception)
            {
            }

            try
            {
                ret.CatalogId = Convert.ToInt32(dr["MovimientoCatalogID"]);
            }
            catch (Exception)
            {
            }

            return ret;
        }



        public static MovementConcilEntity[] ToMovementConcilCollection(DataSet ds)
        {
            int n;
            MovementConcilEntity[] ret;

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRowCollection rows = ds.Tables[0].Rows;
                ret = new MovementConcilEntity[rows.Count];
                n = ret.GetLength(0);
                for (int i = 0; i < n; i++)
                {
                    ret[i] = ToMovementConcil(rows[i]);
                }
                return ret;
            }
            else
            {
                ret = new MovementConcilEntity[0];
                return ret;
            }
        }
        public static MovementConcilEntity ToMovementConcil(DataSet ds)
        {
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return ToMovementConcil(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static MovementConcilEntity ToMovementConcil(DataRow dr)
        {
            MovementConcilEntity ret = new MovementConcilEntity();
            ret.AdicionalData = Convert.ToString(dr["MovimientoAdicionalData"]);
            ret.ArticuloCodigo = Convert.ToString(dr["MovimientoArticuloCodigo"]);
            ret.CuentaID = Convert.ToString(dr["MovimientoCuentaID"]);
            ret.DepositoFinal = Convert.ToString(dr["MovimientoDepositoFinal"]);
            ret.DepositoOrigen = Convert.ToString(dr["MovimientoDepositoOrigen"]);
            ret.Descripcion = Convert.ToString(dr["MovimientoDescripcion"]);
            ret.FechaAlta = Convert.ToDateTime(dr["MovimientoFechaAlta"]);
            ret.Final = Convert.ToSingle(dr["MovimientoFinal"]);
            ret.Inicial = Convert.ToSingle(dr["MovimientoInicial"]);
            ret.MonCount = Convert.ToSingle(dr["MovimientoMonCount"]);
            ret.MovimientoID = Convert.ToDecimal(dr["MovimientoID"]);
            ret.MovimientoTipoID = Convert.ToInt32(dr["MovimientoTipoID"]);
            ret.OperacionID = Convert.ToDecimal(dr["MovimientoOperacionID"]);
            ret.Sucursal = Convert.ToString(dr["MovimientoSucursal"]);
            ret.SucursalFinal = Convert.ToString(dr["MovimientoSucursalFinal"]);
            ret.SucursalOrigen = Convert.ToString(dr["MovimientoSucursalOrigen"]);
            ret.TckCount = Convert.ToSingle(dr["MovimientoTckCount"]);
            ret.UserID = Convert.ToInt32(dr["MovimientoUsuarioID"]);
            //SAFnew agregado userName
            ret.UserName = Convert.ToString(dr["MovimientoUsuarioNombre"]);
            ret.ConcilID = Convert.ToString(dr["MovimientoConcilID"]);
            ret.FechaConcil = Convert.ToDateTime(dr["MovimientoFechaConcil"]);
            ret.MlsCount = Convert.ToSingle(dr["MovimientoMilesCount"]);
            ret.Cost_FIFO = Convert.ToDecimal(dr["MovimientoCost_FIFO"]);
            ret.Cost_PPP = Convert.ToDecimal(dr["MovimientoCost_PPP"]);
            ret.Sale_Amount = Convert.ToDecimal(dr["MovimientoSale_Amount"]);
            //			ret.Mls_Amount =		Convert.ToInt32		(dr["MovimientoMls_Amount"]);
            //			ret.Tck_Amount =		Convert.ToInt32		(dr["MovimientoTck_Amount"]);
            ret.Supplier_name = Convert.ToString(dr["MovimientoSupplier_name"]);

            return ret;
        }


        #region ProviderVoucher
        public static ProviderVoucherEntity ToProviderVoucher(DataSet ds)
        {
            if (ds.Tables[0].Rows.Count > 0 && ds.Tables.Count > 0 && ds != null)
            {
                return ToProviderVoucher(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static ProviderVoucherEntity ToProviderVoucher(DataRow dr)
        {
            ProviderVoucherEntity entity = new ProviderVoucherEntity();

            //entity.id = Convert.ToInt32(dr["providerId"]);
            entity.id = Convert.ToInt32(dr["rubroId"].ToString());
            try
            {
                entity.image = (byte[])(dr["image"]);
            }
            catch { }
            entity.tycVoucher = Convert.ToString(dr["tycVoucher"]);
            entity.tycHotel = Convert.ToString(dr["tycHotel"]);
            entity.tycVirtual = Convert.ToString(dr["tycVirtual"]);

            entity.meVoucher = Convert.ToString(dr["meVoucher"]);
            entity.meHotel = Convert.ToString(dr["meHotel"]);
            entity.meVirtual = Convert.ToString(dr["meVirtual"]);

            try
            {
                entity.imageGC = (byte[])(dr["imageGC"]);
            }
            catch { }

            return entity;
        }

        public static ProviderVoucherEntity[] ToProviderVoucherCollection(DataSet ds)
        {
            int x;
            int n;

            ProviderVoucherEntity[] ret;

            if (ds.Tables[0].Rows.Count > 0 && ds.Tables.Count > 0 && ds != null)
            {
                DataRowCollection dr = ds.Tables[0].Rows;
                ret = new ProviderVoucherEntity[dr.Count];
                n = ret.GetLength(0);

                for (x = 0; x < n; x++)
                {
                    ret[x] = ToProviderVoucher(dr[x]);
                }

                return ret;
            }
            else
            {
                ret = new ProviderVoucherEntity[0];
                return ret;
            }
        }

        #endregion



        public static ProductNotificationEntity ToProductNotification(IDataReader dr)
        {
            ProductNotificationEntity ret = new ProductNotificationEntity();
            ret.ID = (dr["id"] != System.DBNull.Value ? (int)dr["id"] : -1);
            ret.ProductID = (dr["InternalCode"] != System.DBNull.Value ? Convert.ToString(dr["InternalCode"]) : "");
            ret.ProviderID = (dr["ProviderID"] != System.DBNull.Value ? Convert.ToString(dr["ProviderID"]) : "");
            ret.Estado = (dr["Estado"] != System.DBNull.Value ? (int)(dr["Estado"]) : -1);
            ret.FechaEvento = (dr["DateNotif"] != System.DBNull.Value ? (DateTime)(dr["DateNotif"]) : new DateTime(1900, 1, 1));


            return ret;
        }



        //#region SerialID_Barcode
        //public static SerialID_BarcodeEntity ToSerialID_Barcode(IDataReader dr)
        //{
        //    SerialID_BarcodeEntity sb = new SerialID_BarcodeEntity();
        //    sb.Barcode = dr["Barcode"].ToString();
        //    sb.SerialID  = dr["SerialID"].ToString();
        //    sb.State = (int) dr["State"] ;
        //    sb.Date = (DateTime) dr["CreationDate"] ;

        //    try
        //    {
        //        if (dr["CodeType"] != DBNull.Value)
        //            sb.CodeType = (int)dr["CodeType"];
        //    }
        //    catch (Exception) { }

        //    try
        //    {
        //        if (dr["WarehouseID"] != DBNull.Value)
        //            sb.WarehouseID = (int)dr["WarehouseID"];
        //    }
        //    catch (Exception) { }


        //     return sb;
        //}
        //#endregion

    }

}
