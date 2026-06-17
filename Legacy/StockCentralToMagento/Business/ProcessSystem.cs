using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MG2Connector;
using StockCentralToMagento.DataAccess;
using StockCentralToMagento.Entities;
using ET.Comun;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ctcILS.CoreV4.Business;
using ctcILS.CoreV4.Entities;
using System.Configuration;
using WinSCP;
using MagentoAPI;

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.HSSF.Util;
using NPOI.POIFS.FileSystem;
using NPOI.HPSF;

using NPOI.XSSF;
using NPOI.XSSF.UserModel;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System.Threading;
using System.Security.Policy;
using ctcILS.CoreV4.DataAccess;
using System.Runtime.CompilerServices;
using System.Diagnostics.Eventing.Reader;

namespace StockCentralToMagento.Business
{
    public static class ProcessSystem
    {
        static private string Token;// = ConfigurationManager.AppSettings["Token"];
        static private string URL = System.Configuration.ConfigurationSettings.AppSettings["URL_Magento"];
        static private string User;
        static private string Pass;
        static public string FechaInicioProceso;
        static public DateTime ControlFechaUltProceso;

        public static void InicializarCatalogo_Productos(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();

            int vendorCTC = 0;

            try
            {
                vendorCTC = Convert.ToInt32(ConfigurationSettings.AppSettings["ActivarVendorCTC"]);
            }
            catch
            {
                vendorCTC = -1;
            }


            int ProviderId = -1;

            try {
                ProviderId = Convert.ToInt32(ConfigurationSettings.AppSettings["ActivarProcesamientoProviderIDNro"]);
            }
            catch
            {
                ProviderId = -1;
            }

            int SeparadorDecimalesComa = 0;

            try
            {
                SeparadorDecimalesComa = Convert.ToInt32(ConfigurationSettings.AppSettings["SeparadorDecimalesComa"]);
            }
            catch
            {
                SeparadorDecimalesComa = 0;
            }




            List<CatalogStockEntity> sprod;
            if (ProviderId > 0)
            {
                LogSystem.WriteLogDebug("......... Obteniendo productos de Seller " + ProviderId + " - Catalogo " + CatalogoID, ref log);
                sprod = cDalc.GetAllCatalogStock(CatalogoID, 0, ProviderId);
            }
            else
            {
                if (vendorCTC == 1)
                {
                    LogSystem.WriteLogDebug("......... Obteniendo productos de CTC - Catalogo " + CatalogoID, ref log);
                    sprod = cDalc.GetAllCatalogStock(CatalogoID, 0, false);
                }
                else
                {
                    LogSystem.WriteLogDebug("......... Obteniendo productos de todo el catalogo " + CatalogoID, ref log);
                    sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);
                }
            }
            var magento = new Magento(URL);
            int GenerarImagenesTodoelCatalogo = 0;
            int StoreID_Magento = -1;
            int CargaDescuento = 0;
            LogSystem.WriteLogDebug("Cantidad de productos a procesar: " + sprod.Count , ref log);

            try
            {
                MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                StoreID_Magento = Store_Magento.idStoreMagento;
            }
            catch (Exception dd)
            {
                LogSystem.WriteLogDebug("Parametro de Store MAgento no OBTENIDO + " + dd.Message, ref log);
                return;
            }
            string user = ConfigurationSettings.AppSettings["MagentoAdmin"]; //"admin";
            string passWord = ConfigurationSettings.AppSettings["MagentoPass"]; //"ctc2018";

            DateTime timeToken = DateTime.Now;

            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"] );
            }
            catch(Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de imagenes de todo el catalogo -> Desactivado", ref log); ;
            }

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

            List<CatalogStockEntity> sprod_w_errors = new List<CatalogStockEntity>();
            List<CatalogStockEntity> sprod_to_enable = new List<CatalogStockEntity>();
            foreach (CatalogStockEntity ccc in sprod)
            {
                i++;
                try
                {

                    ProcessSystem.GetToken(user, passWord);
                    magento = new Magento(URL);
                    CatalogStockEntity c = cDalc.GetProductStockByBarCode(ccc.codigobarra, CatalogoID);
                    LogSystem.WriteLogDebug("   -----------------------------------------------------------------------------------------------------------", ref log);

                    LogSystem.WriteLogDebug("   ------------  PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "] -- CAT = " + cat.CategoriaRaiz + "   -----------------", ref log);
                    ProductPriceSku[] price_array_stores;
                    M2Product Prod;
                    try
                    {
                        Prod = magento.GetSku(Token, c.codigobarra, StoreID_Magento);
                    }
                    catch (Exception ess)
                    {
                        LogSystem.WriteLogDebug("xxxxxxxxxxxx    PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "] ---->  ERROR OBTENIENDO PROD EN MAGENTO :" + ess.Message, ref log);
                        if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                            sprod_w_errors.Add(c);
                        continue;
                    }

                    int stk = -1000;
                    try
                    {
                        stk = Convert.ToInt32(magento.GetStockSku(Token, c.codigobarra));
                    }
                    catch (Exception ix)
                    {
                        stk = -2;
                        LogSystem.WriteLogDebug("xxxxxxxxxx ERROR - No pudo obtener el STOCK del producto Nro " + i.ToString() + " [" + c.codigobarra + "] ", ref log);
                    }
                    // Activación de producto o desactivación del Catalogo si es Stock 0
                    //magento = new Magento("http://" + cat.UrlCatalogo, Token);

                    try
                    {
                        
                        string rest = "";
                        int status = 1;
                        
                        // Cambio en la forma de tratamiento de la desactivación de productos. No desactivaremos por stock en cero 
                        // *******************************************************************************************************
                        //if (c.stock <= 0)
                        //    status = 0;
                        // *******************************************************************************************************
                        
                        if (c.enable == false)
                            status = 0;

                        if (Prod != null)
                        {
                            bool NoEstaEnWebsite = true;

                            foreach (int St in Prod.ExtensionAttributes.WebSites)
                            {
                                if (St == StoreID_Magento)
                                    NoEstaEnWebsite = false;
                            }

                        
                            if (NoEstaEnWebsite)
                            {
                                LogSystem.WriteLogDebug("------ Producto NO ACTIVO en Web Site", ref log);
                                if (status == 1)
                                {
                                    rest = magento.SetStatusSku(Token, status, Prod, cat.idCatalogoMagento);
                                    if (rest == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooooo Activado en WEB SITE -" + status + "- actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR Activado en WEB SITE Status :" + rest, ref log);
                                    }
                                    magento = new Magento("http://" + cat.UrlCatalogo, Token);

                                    rest = magento.SetStatusSku(Token, status, Prod);
                                    if (rest == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooooo Status -" + status + "- actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Status :" + rest, ref log);
                                    }

                                    if (!sprod_to_enable.Exists(x => x.codigobarra == c.codigobarra))
                                        sprod_to_enable.Add(c);


                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("--------------- Estado en MAGENTO SIN CAMBIOS Status --> " + status, ref log);
                                }
                            }
                            else if (status == 0)
                            {
                                // 20240806 - No vamos a quitar más el producto de la Vista. Lo vamos a desactivar/
                                //rest = magento.SetStatusSku(Token, status, Prod, cat.idCatalogoMagento);
                                magento = new Magento("http://" + cat.UrlCatalogo, Token);

                                rest = magento.SetStatusSku(Token, status, Prod);
                                // ********************************************************************************
                                if (rest == "0")
                                {
                                    LogSystem.WriteLogDebug("ooooooooooooo Status -" + status + "- actualizado exitósamente", ref log);
                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Status :" + rest, ref log);
                                }
                            }
                            else
                            {
                                LogSystem.WriteLogDebug("--------------- Estado en MAGENTO SIN CAMBIOS Status --> " + status, ref log);
                            }
                        }
                        else
                        {
                            if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                sprod_w_errors.Add(c);
                            LogSystem.WriteLogDebug("xxxxxxxxx Producto no EXISTE EN MAGENTO", ref log);
                            continue;
                        }

                    }
                    catch (Exception st)
                    {
                        LogSystem.WriteLogDebug("xxxxxxxxxx ERROR - No pudo Actualizar STATUS Prod nro: " + i.ToString() + " [" + c.codigobarra + "] ", ref log);

                    }

                    if (c.enable && c.stock > 0)
                    {
                        LogSystem.WriteLogDebug("------ PRODUCTO ACTIVO EN STOCK CENTRAL -- Iniciando actualización ...", ref log);


                        decimal PVP = 0;
                        try
                        {
                            foreach (CustomAttribute ca in Prod.CustomAttributes)
                            {
                                if (ca.AttributeCode == "precio_en_pesos")
                                {
                                    if (SeparadorDecimalesComa ==1)
                                        PVP = Convert.ToDecimal(ca.Value.ToString().Replace(".", ","));
                                    else
                                        PVP = Convert.ToDecimal(ca.Value.ToString());
                                    break;
                                }
                            }
                        }
                        catch (Exception pv)
                        {
                            LogSystem.WriteLogDebug("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "] ---->  PERROR OBTENIENDO Precio en PESOS EN MAGENTO :" + pv.Message, ref log);
                            //continue;
                        }

                        LogSystem.WriteLogDebug("------ PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "] ---->  Precio VENTA Pesos detectado en Magento :" + PVP.ToString() + " - Precio Venta Pesos StockCentral : " + c.precioventa, ref log);

                        try
                        {
                            price_array_stores = magento.GetPriceSku(Token, c.codigobarra);
                            if (price_array_stores != null)
                            {
                                Boolean ProductoEnCatalogo = false;
                                foreach (ProductPriceSku pr in price_array_stores)
                                {
                                    if (pr.StoreID == StoreID_Magento)
                                    {
                                        ProductoEnCatalogo = true;
                                        //float conv = (float)0.025;
                                        float conv = c.preciounidad;

                                        int precioL = Convert.ToInt32(c.precioreal);
                                        if (CatalogoID == 31)
                                        {
                                            CatalogStockEntity cc = cDalc.GetProductStockByBarCode(c.codigobarra, CatalogoID);
                                            precioL = Convert.ToInt32((cc.preciolista / (Convert.ToSingle((100 + cc.alicuotaIVA)) / 100)) / conv);
                                            LogSystem.WriteLogDebug("------ Precio de LISTA de Referencia para " + cc.codigobarra + ". Valor referencia LISTA en PUNTOS = " + precioL.ToString() + ", CatalogoID = " + CatalogoID.ToString() + ", Precio de LISTA REAL en PESOS = " + cc.preciolista.ToString(), ref log);

                                            if (precioL < Convert.ToInt32(c.precioreal))
                                                precioL = Convert.ToInt32(c.precioreal);
                                        }
                                        string res = "";
                                        //if (pr.Price != precioL || PVP != Convert.ToDecimal(c.precioventa))
                                        //{
                                        if (CatalogoID == 31)
                                        {

                                            if (pr.Price != precioL || PVP != Convert.ToDecimal(c.precioventa))
                                            {
                                                LogSystem.WriteLogDebug("------ Cambio de Precio LISTA Puntos en SKU " + c.codigobarra + ". Valor Anterior = " + pr.Price.ToString() + ". Valor Actual = " + precioL.ToString(), ref log);
                                                res = magento.SetPriceSkuStore(Token, StoreID_Magento, c.codigobarra, precioL.ToString());
                                                if (res == "0")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                                    if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                                        sprod_w_errors.Add(c);
                                                }
                                            }
                                            LogSystem.WriteLogDebug("------ Comienza análisis de Cambio de Precio Especial en SKU " + c.codigobarra + ". Valor a setear = " + Convert.ToInt32(c.precioreal).ToString(), ref log);

                                            List<Price> ePrice = new List<Price>();
                                            //ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento });


                                            List<string> SkuL = new List<string>();
                                            SkuL.Add(c.codigobarra);

                                            List<Price> EspPriceList = magento.getEspecialPrice(SkuL, Token);

                                            Price newEspPrice = new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento };
                                            bool activarprecioespecial = true;
                                            foreach (Price p in EspPriceList)
                                            {
                                                LogSystem.WriteLogDebug("------ Especial Price existente : store=" + p.store_id + ", price=" + p.price, ref log);
                                                LogSystem.WriteLogDebug("------ Especial Price existente : from=" + p.price_from, ref log);
                                                if (p.store_id == StoreID_Magento)
                                                {
                                                    newEspPrice.price_from = p.price_from;
                                                    if (p.price == newEspPrice.price)
                                                        activarprecioespecial = false;
                                                    //newEspPrice.price_to = p.price_to;
                                                }
                                            }

                                            if (activarprecioespecial)
                                            {
                                                ePrice.Add(newEspPrice);

                                                //res = magento.SetEspecialPrice(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString());
                                                res = magento.SetEspecialPrice(ePrice, StoreID_Magento.ToString(), Token);
                                                if (res == "0")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooo Precio especial actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial :" + res, ref log);
                                                }

                                                
                                                try
                                                {
                                                    CargaDescuento = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ActualizaDescuento"].ToString());
                                                }
                                                catch(Exception f)
                                                {
                                                    CargaDescuento = 0;
                                                }

                                                if (CargaDescuento == 1)
                                                {
                                                    // Actualizar variable descuento
                                                    M2Product PD = new M2Product();
                                                    PD.Sku = c.codigobarra;


                                                    LogSystem.WriteLogDebug("------ AGREGANDO Descuento a Prod  ", ref log);
                                                    CustomAttribute attr_d = new CustomAttribute();

                                                    attr_d.AttributeCode = "descuento";
                                                    attr_d.Value = Convert.ToInt32(Math.Truncate((Convert.ToSingle(precioL) - c.precioreal) / Convert.ToSingle(precioL))).ToString();
                                                    PD.CustomAttributes = new List<CustomAttribute>();
                                                    PD.CustomAttributes.Add(attr_d);
                                                    // LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                                    LogSystem.WriteLogDebug("[ " + attr_d.AttributeCode + "] \n" + attr_d.Value, ref log);


                                                    res = magento.UpdateProductoTyC(Token, PD, cat.idCatalogoMagento.ToString());
                                                    if (res == "OK")
                                                    {
                                                        LogSystem.WriteLogDebug("ooooooooooooo Descuento actualizado exitósamente", ref log);
                                                    }
                                                    else
                                                    {
                                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Descuento :" + res, ref log);
                                                    }
                                                }
                                            }
                                            else
                                                LogSystem.WriteLogDebug("ooooooooooo Precio especial Magento Ya existente -> " + newEspPrice.price, ref log);

                                        }
                                        else
                                        {
                                            if (pr.Price != precioL || PVP != Convert.ToDecimal(c.precioventa))
                                            {
                                                LogSystem.WriteLogDebug("------ Cambio de Precio en SKU " + c.codigobarra + ". Valor Anterior PUNTOS = " + pr.Price.ToString() + ". Valor Actual PUNTOS = " + Convert.ToInt32(c.precioreal).ToString(), ref log);
                                                res = magento.SetPriceSkuStore(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString());
                                                if (res == "0")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                                    if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                                        sprod_w_errors.Add(c);
                                                }
                                            }


                                            LogSystem.WriteLogDebug("ooooooo -------------------------  Análisis de Cambio de Precio Especial en SKU " + c.codigobarra + ". Valor a setear = " + Convert.ToInt32(c.precioreal).ToString(), ref log);
                                            

                                            magento = new Magento(URL);

                                            List<Price> ePrice = new List<Price>();
                                            ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento });

                                            List<string> skuL = new List<string>();
                                            skuL.Add(c.codigobarra);
                                            //ePrice = magento.getEspecialPrice(skuL, Token);

                                            List<Price> EspPriceList = magento.getEspecialPrice(skuL, Token);

                                            Price newEspPrice = new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento };
                                            bool activarprecioespecial = true;
                                            int idPrecioStore = 0;
                                            foreach (Price p in EspPriceList)
                                            {
                                                LogSystem.WriteLogDebug("------ Especial Price existente : store=" + p.store_id + ", price=" + p.price + ", from = " + p.price_from, ref log);
                                                if (p.store_id == StoreID_Magento)
                                                {
                                                    newEspPrice.price_from = p.price_from;
                                                    if (p.price == newEspPrice.price)
                                                    {
                                                        //activarprecioespecial = false;
                                                        //newEspPrice.price_to = p.price_to;
                                                        idPrecioStore++;
                                                        if (idPrecioStore > 1)
                                                        {
                                                            LogSystem.WriteLogDebug("------ Especial Price eliminando ... " + p.price_from, ref log);
                                                            res = magento.EspecialPriceDelete(Token, StoreID_Magento, c.codigobarra, p.price.ToString(), cat.MagentoWebSite, p.price_from, p.price_to);
                                                            if (res == "0")
                                                            {
                                                                LogSystem.WriteLogDebug("ooooooooooo Precio especial eliminado exitósamente", ref log);
                                                            }
                                                            else
                                                            {
                                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ELIMINAR Precio especial :" + res, ref log);
                                                            }
                                                            activarprecioespecial = true;
                                                        }
                                                        else
                                                        {
                                                            activarprecioespecial = false;
                                                        }
                                                    }
                                                }
                                            }


                                            //CONTROL DE PRECIO ESPECIALES
                                            EspPriceList = magento.getEspecialPrice(skuL, Token);
                                            foreach (Price p in EspPriceList)
                                            {
                                                LogSystem.WriteLogDebug("----------- Especial Price existente : store=      " + p.store_id + "    , price=" + p.price + "     , from = " + p.price_from, ref log);
                                            }
                                            //**************************


                                            if (activarprecioespecial)
                                            {
                                                LogSystem.WriteLogDebug("------ >>>>>>>>>>>>>>>>>>>>>> Precio especial actualización ....", ref log);
                                                //res = magento.SetEspecialPrice(ePrice, StoreID_Magento.ToString());
                                                res = magento.SetEspecialPrice(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString());
                                                if (res == "0")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooo Precio especial actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial :" + res, ref log);
                                                    /*
                                                       res = magento.SetEspecialPriceAlt(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString(), cat.MagentoWebSite);
                                                       if (res == "0")
                                                       {
                                                           LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo actualizado exitósamente", ref log);
                                                       }
                                                       else
                                                       {
                                                           LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial Alternativo :" + res, ref log);
                                                           foreach (Price p in EspPriceList)
                                                           {
                                                               LogSystem.WriteLogDebug("----------- Especial Price existente : store=" + p.store_id + ", price=" + p.price, ref log);
                                                               LogSystem.WriteLogDebug("----------- Especial Price existente : from=" + p.price_from, ref log);
                                                               if (p.store_id == StoreID_Magento)
                                                               {
                                                                   res = magento.EspecialPriceDelete(Token, StoreID_Magento, c.codigobarra, p.price.ToString(), cat.MagentoWebSite, p.price_from, p.price_to);
                                                                   if (res == "0")
                                                                   {
                                                                       LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo DEFAULT eliminado exitósamente", ref log);
                                                                   }
                                                                   else
                                                                   {
                                                                       LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ELIMINAR Precio especial DEFAULT Alternativo :" + res, ref log);
                                                                   }
                                                               }
                                                               LogSystem.WriteLogDebug("----------- FIN verificación Especial Price ", ref log);


                                                           }
                                                      }
                                                    */


                                                }
                                            }
                                            else
                                                LogSystem.WriteLogDebug("ooooooooooo Precio especial Magento Ya existente -> " + newEspPrice.price, ref log);


                                           
                                            foreach (CustomAttribute pp in Prod.CustomAttributes)
                                            {
                                                if (pp.AttributeCode == "special_price")
                                                {
                                                    LogSystem.WriteLogDebug("------ Especial Price como Custom Attribute - Valor = " + pp.Value.ToString(), ref log);
                                                    M2Product P2SP = new M2Product();
                                                    P2SP.Sku = Prod.Sku;


                                                    LogSystem.WriteLogDebug("------ Asignando precio ESPECIAL default del custom Attribute a 100.000.000", ref log);
                                                    CustomAttribute attrP2SP = new CustomAttribute();

                                                    attrP2SP.AttributeCode = "special_price";
                                                    attrP2SP.Value = "100000000";
                                                    P2SP.CustomAttributes = new List<CustomAttribute>();
                                                    P2SP.CustomAttributes.Add(attrP2SP);

                                                    LogSystem.WriteLogDebug("[ " + attrP2SP.AttributeCode + "] \n" + attrP2SP.Value.ToString().Replace(",", "."), ref log);
                                                    res = magento.UpdateProductoAttributeStoreViewNumber(Token, P2SP, "all");
                                                    LogSystem.WriteLogDebug("------ Respuesta de Asignando precio ESPECIAL default del custom Attribute a 10.000.000 :" + res, ref log);

                                                    if (res == "OK")
                                                    {
                                                        LogSystem.WriteLogDebug("ooooooooooooo Asignando precio ESPECIAL default del custom Attribute a 10.000.000 actualizado exitósamente", ref log);
                                                    }

                                                }
                                            }
                                        }

                                        magento = new Magento("http://" + cat.UrlCatalogo, Token);

                                        //}
                                        //else
                                        //    LogSystem.WriteLogDebug("===== Precios OK. Valor = " + pr.Price.ToString(), ref log);

                                        if (pr.Price != precioL || PVP != Convert.ToDecimal(c.precioventa))
                                        {
                                            LogSystem.WriteLogDebug("------ Cambio de Precio Costo en SKU " + c.codigobarra + ". Valor a setear = " + Convert.ToInt32(c.precioventa).ToString(), ref log);
                                            res = magento.SetCostPrice(Token, c.codigobarra, c.precioventa.ToString(), StoreID_Magento.ToString());
                                            if (res == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooo Precio costo actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio costo :" + res, ref log);
                                                if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                                    sprod_w_errors.Add(c);
                                            }

                                            M2Product P2 = new M2Product();
                                            P2.Sku = c.codigobarra;


                                            LogSystem.WriteLogDebug("------ AGREGANDO Precio a Prod  ", ref log);
                                            CustomAttribute attr = new CustomAttribute();

                                            attr.AttributeCode = "precio_en_pesos";
                                            attr.Value = Convert.ToDecimal(c.precioventa).ToString();
                                            P2.CustomAttributes = new List<CustomAttribute>();
                                            P2.CustomAttributes.Add(attr);
                                            LogSystem.WriteLogDebug("asignando  [ " + attr.AttributeCode + "] --->  " + attr.Value, ref log);

                                            res = magento.UpdateProductoGlobalAttribute(Token, P2, StoreID_Magento.ToString());
                                            if (res.Split('|')[0] == "OK")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos :" + res, ref log);
                                                
                                            }
                                           

                                        }
                                        else
                                            LogSystem.WriteLogDebug("===== Precios OK. Valor = " + pr.Price.ToString(), ref log);


                                        break;
                                    }

                                }
                                if (!ProductoEnCatalogo)
                                {
                                    if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                        sprod_w_errors.Add(c);
                                    LogSystem.WriteLogDebug("xxxxxxxx PRODUCTO NO CARGADO EN CATALOGO: " + c.codigobarra, ref log);

                                    ProductoEnCatalogo = true;
                                    //float conv = (float)0.025;
                                    float conv = c.preciounidad;

                                    int precioL = Convert.ToInt32(c.precioreal);
                                    if (CatalogoID == 31)
                                    {
                                        CatalogStockEntity cc = cDalc.GetProductStockByBarCode(c.codigobarra, CatalogoID);
                                        precioL = Convert.ToInt32((cc.preciolista / (Convert.ToSingle((100 + cc.alicuotaIVA)) / 100)) / conv);
                                        LogSystem.WriteLogDebug("------ Precio de LISTA de Referencia para " + cc.codigobarra + ". Valor referencia LISTA = " + precioL.ToString() + ", CatalogoID = " + CatalogoID.ToString() + ", Precio de LISTA REAL en PESOS = " + cc.preciolista.ToString(), ref log);

                                        if (precioL < Convert.ToInt32(c.precioreal))
                                            precioL = Convert.ToInt32(c.precioreal);
                                    }

                                    //if (pr.Price != precioL || PVP != Convert.ToDecimal(c.precioventa))
                                    //{
                                    string res = "";
                                    if (CatalogoID == 31)
                                    {

                                        LogSystem.WriteLogDebug("------ Activar Precio en SKU " + c.codigobarra + ". Valor Actual = " + precioL.ToString(), ref log);
                                        res = magento.SetPriceSkuStore(Token, StoreID_Magento, c.codigobarra, precioL.ToString());
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                            if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                                sprod_w_errors.Add(c);
                                        }

                                        LogSystem.WriteLogDebug("------ Cambio de Precio Especial en SKU " + c.codigobarra + ". Valor a setear = " + Convert.ToInt32(c.precioreal).ToString(), ref log);

                                        List<Price> ePrice = new List<Price>();
                                        //ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento });


                                        List<string> SkuL = new List<string>();
                                        SkuL.Add(c.codigobarra);

                                        List<Price> EspPriceList = magento.getEspecialPrice(SkuL, Token);

                                        Price newEspPrice = new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento };

                                        foreach (Price p in EspPriceList)
                                        {
                                            if (p.store_id == StoreID_Magento)
                                            {
                                                newEspPrice.price_from = p.price_from;
                                                //newEspPrice.price_to = p.price_to;
                                            }
                                        }

                                        ePrice.Add(newEspPrice);

                                        //res = magento.SetEspecialPrice(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString());
                                        res = magento.SetEspecialPrice(ePrice, StoreID_Magento.ToString(), Token);
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooo Precio especial actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial :" + res, ref log);
                                        }

                                        //int CargaDescuento = 9;
                                        try
                                        {
                                            CargaDescuento = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ActualizaDescuento"].ToString());
                                        }
                                        catch (Exception f)
                                        {
                                            CargaDescuento = 0;
                                        }

                                        if (CargaDescuento == 1)
                                        {
                                            // Actualizar variable descuento
                                            M2Product PD = new M2Product();
                                            PD.Sku = c.codigobarra;


                                            LogSystem.WriteLogDebug("------ AGREGANDO Descuento a Prod  ", ref log);
                                            CustomAttribute attr_d = new CustomAttribute();

                                            attr_d.AttributeCode = "descuento";
                                            attr_d.Value = Convert.ToInt32(Math.Truncate((Convert.ToSingle(precioL) - c.precioreal) / Convert.ToSingle(precioL))).ToString();
                                            PD.CustomAttributes = new List<CustomAttribute>();
                                            PD.CustomAttributes.Add(attr_d);
                                            // LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                            LogSystem.WriteLogDebug("[ " + attr_d.AttributeCode + "] \n" + attr_d.Value, ref log);


                                            res = magento.UpdateProductoTyC(Token, PD, cat.idCatalogoMagento.ToString());
                                            if (res == "OK")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooooo Descuento actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Descuento :" + res, ref log);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("------ Activar Precio en SKU " + c.codigobarra + ". Valor Actual = " + Convert.ToInt32(c.precioreal).ToString(), ref log);
                                        res = magento.SetPriceSkuStore(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString());
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                            if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                                sprod_w_errors.Add(c);
                                        }

                                        LogSystem.WriteLogDebug("------ Cambio de Precio Especial en SKU " + c.codigobarra + ". Valor a setear = " + Convert.ToInt32(c.precioreal).ToString(), ref log);

                                        List<Price> ePrice = new List<Price>();
                                        ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = StoreID_Magento });


                                        /*
                                        List<string> skuL = new List<string>();
                                        skuL.Add(c.codigobarra);
                                        ePrice = magento.getEspecialPrice(skuL, Token);
                                        */

                                        //res = magento.SetEspecialPrice(ePrice, StoreID_Magento.ToString());
                                        res = magento.SetEspecialPrice(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString());
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooo Precio especial actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial :" + res, ref log);
                                            res = magento.SetEspecialPriceAlt(Token, StoreID_Magento, c.codigobarra, Convert.ToInt32(c.precioreal).ToString(), cat.MagentoWebSite);
                                            if (res == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial Alternativo :" + res, ref log);
                                            }


                                        }
                                    }

                                    //}
                                    //else
                                    //    LogSystem.WriteLogDebug("===== Precios OK. Valor = " + pr.Price.ToString(), ref log);

                                    //if (pr.Price != precioL || PVP != Convert.ToDecimal(c.precioventa))
                                    //{
                                    LogSystem.WriteLogDebug("------ Activar Precio Costo en SKU " + c.codigobarra + ". Valor a setear = " + Convert.ToInt32(c.precioventa).ToString(), ref log);
                                    res = magento.SetCostPrice(Token, c.codigobarra, c.precioventa.ToString(), StoreID_Magento.ToString());
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio costo actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio costo :" + res, ref log);
                                        if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                            sprod_w_errors.Add(c);
                                    }

                                    M2Product P2 = new M2Product();
                                    P2.Sku = c.codigobarra;

                                    if (PVP != Convert.ToDecimal(c.precioventa))
                                    {
                                        LogSystem.WriteLogDebug("------ AGREGANDO Precio en PESOS a Prod  ", ref log);

                                        
                                        CustomAttribute attr = new CustomAttribute();

                                        attr.AttributeCode = "precio_en_pesos";
                                        attr.Value = Convert.ToDecimal(c.precioventa).ToString();
                                        P2.CustomAttributes = new List<CustomAttribute>();
                                        P2.CustomAttributes.Add(attr);
                                        LogSystem.WriteLogDebug("[ " + attr.AttributeCode + "] \n" + attr.Value, ref log);

                                        
                                        res = magento.UpdateProductoGlobalAttribute(Token, P2, StoreID_Magento.ToString());
                                        if (res.Split('|')[0] == "OK")
                                        {
                                            
                                            LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos :" + res, ref log);
                                        
                                        }
                                       //res = magento.UpdateProductoAttributeStoreViewPut(Token, P2, /* cat.idCatalogoMagento.ToString() */ "all");
                                       // LogSystem.WriteLogDebug("xxxxxxxxxxxxx ULTIMO INTENTO ACTUALIZAR Precio en Pesos :" + res, ref log);

                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("===== Precios PESOS OK. Valor Magento : " + PVP.ToString() + ", Valor StockCentral: " + c.precioventa.ToString(), ref log);

                                    }
                                    //}
                                    //else
                                    //    LogSystem.WriteLogDebug("===== Precios OK. Valor = " + pr.Price.ToString(), ref log);






                                }
                            }
                            else
                            {
                                if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                    sprod_w_errors.Add(c);


                                LogSystem.WriteLogDebug("xxxxxxxx PRODUCTO NO CARGADO EN MAGENTO o error en Precio: " + c.codigobarra, ref log);
                            }
                        }
                        catch (Exception prc)
                        {
                            LogSystem.WriteLogDebug("xxxxxxxxx PRODUCTO NO CARGADO EN MAGENTO o error en Precio: " + c.codigobarra + ". Err : " + prc.Message, ref log);

                        }
                    }
                    else
                        LogSystem.WriteLogDebug("PRODUCTO --NO-- ACTIVO EN STOCK CENTRAL o STOCK en CERO -- Descartada actualización de precios ", ref log);

                    if (stk != c.stock)
                    {
                        LogSystem.WriteLogDebug("------ Actualización de Stock del producto Nro " + i.ToString() + " [" + c.codigobarra + "]. Valor Anterior = " + stk.ToString() + ". Valor Actual = " + Convert.ToInt32(c.stock).ToString(), ref log);
                        string res = magento.SetStockItemsSku(Token, c.stock, c.codigobarra);

                        if (res == "0")
                        {
                            LogSystem.WriteLogDebug("ooooooooooooo Stock actualizado exitósamente", ref log);
                        }
                        else
                        {
                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res, ref log);
                            if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                sprod_w_errors.Add(c);
                        }

                    }
                    else
                    {
                        LogSystem.WriteLogDebug("===== Stock OK --> " + stk, ref log);
                    }

                }
                catch (Exception glb)
                {
                    LogSystem.WriteLogDebug("xxxxxxxxxx ERROR en producto :" + i.ToString(), ref log);
                    LogSystem.WriteLogDebug("xxxxxxxxxx ERROR -> " + glb.Message + '\n' + glb.InnerException + '\n' + glb.StackTrace, ref log);


                }

                // Si supera el tiempo de timeZ de procesamiento, reiniciamos el ciclo por si hay inconsistencias.

                if (timeToken.AddHours(1) < DateTime.Now)
                {

                    ProcessSystem.GetToken(user, passWord);
                    timeToken = DateTime.Now;
                }
            }

            // Desactivar productos que no pudieron ser informados.+
            try
            {
                LogSystem.WriteLogDebug("ANALIZAR PRODUCTOS DEL CATALOGO DESACTIVADOS", ref log);
                List<CatalogStockEntity> sprod_disable;
                if (ProviderId > 0)
                    sprod_disable = cDalc.GetAllCatalogStockDisabled(CatalogoID, 0, ProviderId);
                else
                    sprod_disable = cDalc.GetAllCatalogStockDisabled(CatalogoID, 0);
                i = 0;
                LogSystem.WriteLogDebug("Cantidad de productos a procesar: " + sprod_disable.Count, ref log);

                foreach (CatalogStockEntity c in sprod_disable)
                {
                    i++;
                    LogSystem.WriteLogDebug("ooooooo Quitando producto [" + c.codigobarra + "] del Catálogo " + CatalogoID.ToString(), ref log);
                    try
                    {
                        M2Product Prod = magento.GetSku(Token, c.codigobarra, StoreID_Magento);

                        bool EstaEnWebsite = false;

                        foreach (int St in Prod.ExtensionAttributes.WebSites)
                        {
                            if (St == StoreID_Magento)
                                EstaEnWebsite = true;
                        }

                        if (EstaEnWebsite)
                        {
                            // Cambiamos la forma de desactivar un producto. no lo quitamos mas del catalogo
                            //string res = magento.SetStatusSku(Token, 0, Prod, cat.idCatalogoMagento);
                            if (Prod.Status != 2)
                            {
                                string res = magento.SetStatusSku(Token, 0, Prod);
                                if (res == "0")
                                {
                                    LogSystem.WriteLogDebug("ooooooooooooo Status actualizado exitósamente", ref log);
                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Status :" + res, ref log);
                                }
                            }
                            else
                            {
                                LogSystem.WriteLogDebug("oooooooooooooo  El producto ya estaba DESACTIVADO", ref log);

                            }
                        }
                        else
                        {
                            LogSystem.WriteLogDebug("ooooooooooooo Producto NO PRESENTE EN WEB SITE", ref log);
                        }
                    }
                    catch (Exception dis)
                    {
                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR :" + dis.Message, ref log);
                    }

                }
            }
            catch(Exception f)
            {
                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR en productos a desactivar :" + f.Message + f.Source + f.StackTrace + f.InnerException, ref log);

            }
            string ListaProductos = "";
            foreach (CatalogStockEntity c in sprod_w_errors )
            {
                LogSystem.WriteLogDebug("===== Producto con error en MAGENTO --> " + c.codigobarra, ref log);
                ListaProductos += c.codigobarra + ",";
                /*
                MG2Connector.M2Product p = new M2Product();
                p.Name = c.nombre;
                p.Sku = c.codigobarra;
                p.Price = Convert.ToInt32(c.precioreal);
                p.Status = 0;
                p.TypeId = "simple";
                p.Visibility = 0;
                */
            }




            if (GenerarImagenesTodoelCatalogo > 0)
            {
                if(CatalogoID == 31 )
                    ProcessExportImagesFTP(sprod, CatalogoID, ref log);
                else
                    ProcessExportImages(sprod, CatalogoID, ref log);
            }
            else
            {
                if (CatalogoID == 31) 
                    ProcessExportImagesFTP(sprod_w_errors, CatalogoID, ref log);
                else
                    ProcessExportImages(sprod_w_errors, CatalogoID, ref log);
            }
            
            try
            {
                string[] ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkp(CatalogoID, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ImportStockCentralToMagentoProducts_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Actualizacion");
            }catch(Exception d )
            {
                LogSystem.WriteLogDebug("Error creando archivo de actualización de productos." + d.Message + "\n" + d.StackTrace , ref log);
            }

            // Armar archivo para productos a activar por las dudas
            ListaProductos = "";
            foreach (CatalogStockEntity c in sprod_to_enable)
            {
                LogSystem.WriteLogDebug("===== Producto que hay que activar en MAGENTO --> " + c.codigobarra, ref log);
                ListaProductos += c.codigobarra + ",";
            }
            try
            {
                string[] ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkp(CatalogoID, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ISCToMagentoProducts_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Activar");
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Error creando archivo de activación de productos." + d.Message + "\n" + d.StackTrace, ref log);
            }



        }
        public static void InicializarCatalogo_CheckProductosFromFJ(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            string URL_FJ = System.Configuration.ConfigurationSettings.AppSettings["URL_FJ"];
            int TopeStockUrgente = 10;
            int Integrador = 1;
            try
            {
                Integrador = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Integrador"]);
                LogSystem.WriteLogDebugDirect("Parametro Integrador -> " + Integrador, "StockCentralfromFJProductsCheck");

            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Falta Parametro Integrador se adopta -> 1", "StockCentralfromFJProductsCheck");
            }

            try
            {
                TopeStockUrgente = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["TopeUrgenteControlStock"]);
                LogSystem.WriteLogDebugDirect("Parametro TopeUrgenteControlStock -> " + TopeStockUrgente, "StockCentralfromFJProductsCheck");

            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro TopeUrgenteControlStock -> 10", "StockCentralfromFJProductsCheck");
            }

            Boolean IncludeZeroStock = false;
 
            try
            {
                IncludeZeroStock = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IncludeZeroStock"]);
                LogSystem.WriteLogDebugDirect("Parametro IncludeZeroStock -> " + IncludeZeroStock, "StockCentralfromFJProductsCheck");

            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro IncludeZeroStock -> Error  " + d.Message, "StockCentralfromFJProductsCheck");
                LogSystem.WriteLogDebugDirect("Parametro IncludeZeroStock -> 0", "StockCentralfromFJProductsCheck");
            }

            Boolean ObtieneProductosXVendorsCompleto = false;

            try
            {
                ObtieneProductosXVendorsCompleto = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["ObtieneProductosXVendorsCompleto"]);
                LogSystem.WriteLogDebugDirect("Parametro ObtieneProductosXVendorsCompleto -> " + ObtieneProductosXVendorsCompleto, "StockCentralfromFJProductsCheck");

            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro ObtieneProductosXVendorsCompleto -> Error  " + d.Message, "StockCentralfromFJProductsCheck");
                LogSystem.WriteLogDebugDirect("Parametro ObtieneProductosXVendorsCompleto -> 0", "StockCentralfromFJProductsCheck");
            }

            Boolean ObtieneProductosXVendorsCompletoAPIOficial = false;

            try
            {
                ObtieneProductosXVendorsCompletoAPIOficial = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["ObtieneProductosXVendorsCompletoAPIOficial"]);
                LogSystem.WriteLogDebugDirect("Parametro ObtieneProductosXVendorsCompletoAPIOficial -> " + ObtieneProductosXVendorsCompletoAPIOficial, "StockCentralfromFJProductsCheck");

            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro ObtieneProductosXVendorsCompletoAPIOficial -> Error  " + d.Message, "StockCentralfromFJProductsCheck");
                LogSystem.WriteLogDebugDirect("Parametro ObtieneProductosXVendorsCompletoAPIOficial -> 0", "StockCentralfromFJProductsCheck");
            }
 
            Boolean ObtenerProdConCantidadMAyorATopeControlStock = false;
            try
            {
                ObtenerProdConCantidadMAyorATopeControlStock = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["ObtenerProdConCantidadMAyorATopeControlStock"]);
                LogSystem.WriteLogDebugDirect("Parametro ObtenerProdConCantidadMAyorATopeControlStock -> " + ObtenerProdConCantidadMAyorATopeControlStock, "StockCentralfromFJProductsCheck");

            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro ObtenerProdConCantidadMAyorATopeControlStock -> 0", "StockCentralfromFJProductsCheck");
            }
            var magento = new Magento(URL_FJ);


            if (ObtieneProductosXVendorsCompleto)
            {
                LogSystem.WriteLogDebugDirect("Iniciando obtención de productos por Vendor ", "StockCentralfromFJProductsCheck");
                ProviderDALC pdac = new ProviderDALC();
                List<StockCentralToMagento.Entities.ProviderEntity> p = pdac.GetListByEnabledAndIntegratorAndCatalogID(true, 1, CatalogoID);
                LogSystem.WriteLogDebugDirect("Cantidad de Vendors encontrados: " + p.Count, "StockCentralfromFJProductsCheck");
                foreach (StockCentralToMagento.Entities.ProviderEntity pp in p)
                {
                    LogSystem.WriteLogDebugDirect("Inicio Vendor --> " + pp.Name + " [" + pp.ID + "]", "StockCentralfromFJProductsCheck");
                    string resp = magento.GetBy_SkuFJ_CompleteList(pp.ExternalProviderAccountID.ToString(), pp.ID.ToString());
                    LogSystem.WriteLogDebugDirect("FIN Vendor <== " + pp.Name + " [" + pp.ID + "]", "StockCentralfromFJProductsCheck");
                }
            }
            else if (ObtieneProductosXVendorsCompletoAPIOficial)
            {
                LogSystem.WriteLogDebugDirect("Iniciando obtención de productos por Vendor via API oficial ", "StockCentralfromFJProductsCheck");
                ProviderDALC pdac = new ProviderDALC();
                List<StockCentralToMagento.Entities.ProviderEntity> p = pdac.GetListByEnabledAndIntegratorAndCatalogID(true, 1, CatalogoID);
                LogSystem.WriteLogDebugDirect("Cantidad de Vendors encontrados: " + p.Count, "StockCentralfromFJProductsCheck");
                foreach (StockCentralToMagento.Entities.ProviderEntity pp in p)
                {
                    LogSystem.WriteLogDebugDirect("Inicio Vendor --> " + pp.Name + " [" + pp.ID + "]", "StockCentralfromFJProductsCheck");
                    string resp = magento.GetBy_SkuFJ_CompleteListAPIOficial(pp.ExternalProviderAccountID.ToString(), pp.ID.ToString());
                    LogSystem.WriteLogDebugDirect("FIN Vendor <== " + pp.Name + " [" + pp.ID + "]", "StockCentralfromFJProductsCheck");
                }
            }

            else
            {

                StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
                List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0, true, TopeStockUrgente, ObtenerProdConCantidadMAyorATopeControlStock, IncludeZeroStock, Integrador);
                int GenerarImagenesTodoelCatalogo = 0;
                int StoreID_Magento = -1;

                try
                {
                    MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                    StoreID_Magento = Store_Magento.idStoreMagento;
                }
                catch (Exception dd)
                {
                    LogSystem.WriteLogDebugDirect("Parametro de Store MAgento no OBTENIDO + " + dd.Message, "StockCentralfromFJProductsCheck");
                    return;
                }

                DateTime timeToken = DateTime.Now;

                try
                {
                    GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
                }
                catch (Exception d)
                {
                    LogSystem.WriteLogDebugDirect("Parametro de generación de imagenes de todo el catalogo -> Desactivado", "StockCentralfromFJProductsCheck");
                }

                LogSystem.WriteLogDebugDirect("Cantidad de Productos obtenidas con esta parametrización : " + sprod.Count.ToString(), "StockCentralfromFJProductsCheck");

                MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

                List<CatalogStockEntity> sprod_w_errors = new List<CatalogStockEntity>();


                foreach (CatalogStockEntity c in sprod)
                {
                    i++;
                    LogSystem.WriteLogDebugDirect(" ", "StockCentralfromFJProductsCheck");
                    LogSystem.WriteLogDebugDirect("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralfromFJProductsCheck");
                    string response = magento.GetBy_SkuFJ_List(c.codigobarra.Substring(5), c.providerid.ToString());
                    LogSystem.WriteLogDebugDirect("Respuesta --> " + response, "StockCentralfromFJProductsCheck");
                    LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromFJProductsCheck");

                }

            }
        }

        public static void InicializarCatalogo_CheckProductosFromProductecaByNotifications(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            string URL_Producteca = System.Configuration.ConfigurationSettings.AppSettings["URL_Producteca"];
            int TopeStockUrgente = 10;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            var magento = new Magento(URL_Producteca);
            int GenerarImagenesTodoelCatalogo = 0;
            int StoreID_Magento = -1;

            try
            {
                MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                StoreID_Magento = Store_Magento.idStoreMagento;
            }
            catch (Exception dd)
            {
                LogSystem.WriteLogDebugDirect("Parametro de Store MAgento no OBTENIDO + " + dd.Message, "StockCentralfromProductecaProductsCheckByNotifications");
                return;
            }

            DateTime timeToken = DateTime.Now;

            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro de generación de imagenes de todo el catalogo -> Desactivado", "StockCentralfromProductecaProductsCheckByNotifications");
            }

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

            // REVISAR EL SP DEL METODO DE ABAJO
            StockCentralToMagento.DataAccess.ProviderDALC p_dalc = new ProviderDALC();
            List<StockCentralToMagento.Entities.ProviderEntity> Providers = p_dalc.GetListByEnabledAndIntegratorAndCatalogID(true, 2, CatalogoID);

            foreach (StockCentralToMagento.Entities.ProviderEntity p in Providers)
            {
                LogSystem.WriteLogDebugDirect("Inicio Proceso Proveedor -> " + p.Name, "StockCentralfromProductecaProductsCheckByNotifications");

                List<ProductNotificationEntity> prodNotif = cDalc.GetAllProductsNotifications(p.ID, 0);
                foreach (ProductNotificationEntity prod in prodNotif)
                {
                    string msg = magento.Get_ProductTeca_SkuByProductecaID(p.Token, p.ID, prod.ProductID);
                    string[] msg_arr = msg.Split('|');
                    if (msg_arr[0] != "-1" && msg_arr[0] != "-99")
                    {
                        string msgarr = "[" + msg + "]";
                        JArray o = JArray.Parse(msgarr);
                        if (o != null)
                        {
                            if (o.Count > 0)
                            {
                                for (int j = 0; j < o.Count; j++)
                                {
                                    LogSystem.WriteLogDebugDirect("Producto encontrado en Producteca --> " + o[j].ToString() /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheckByNotifications");
                                    try
                                    {
                                        string CatName = "-1";
                                        try
                                        {
                                            string catSearch = (string)o[j].SelectToken("category");
                                            ItemCompleteSubCategoryEntity rub = scdal.GetSubCategoryByProviderCategoryName(p.ID, catSearch);
                                            CatName = rub.ItemCategoryID.ToString();
                                            LogSystem.WriteLogDebugDirect("Se convirtió y encontró categoria del producto de producteca a --> " + CatName /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                        }
                                        catch (Exception eenocat)
                                        {
                                            LogSystem.WriteLogDebugDirect("Categoria del producto de producteca no encontrada en mapeo" /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                            CatName = "-1";
                                        }

                                        if (CatName == "-1")
                                        {
                                            try
                                            {
                                                string BarCode = "00" + p.ID + (string)o[j].SelectToken("variations[0].sku");
                                                LogSystem.WriteLogDebugDirect("Intentado obtener Categoria en SC de " + BarCode + " - Catalogo " + CatalogoID, "StockCentralfromProductecaProductsCheck");

                                                CatalogStockEntity prod_sc = cDalc.GetProductStockByBarCode(BarCode, CatalogoID);
                                                CatName = prod_sc.rubro.ItemCategoryID.ToString();
                                                LogSystem.WriteLogDebugDirect("Se encontró categoria del producto en CTC --> " + CatName /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                            }
                                            catch (Exception sct)
                                            {
                                                LogSystem.WriteLogDebugDirect("Producto no categorizado en CTC" /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                                CatName = "-1";
                                            }
                                        }

                                        string responseTr = magento.GetBy_SkuProductTeca_Product(o[j].ToObject<JObject>(), p.ID, CatName);

                                        LogSystem.WriteLogDebugDirect("Respuesta --> " + responseTr, "StockCentralfromProductecaProductsCheckByNotifications");
                                        int resp = cDalc.UpdateArtExtNotifications(prod.ID, 1);
                                        LogSystem.WriteLogDebugDirect("Respuesta a grabado de notificación ID :  "+ prod.ID  + "--> " + resp, "StockCentralfromProductecaProductsCheckByNotifications");
                                    }
                                    catch (Exception pr)
                                    {
                                        LogSystem.WriteLogDebugDirect("Respuesta ERRORRRRR --> " + pr.Message + "  ----  " + pr.InnerException.ToString() + " --- " + pr.StackTrace, "StockCentralfromProductecaProductsCheckByNotifications");
                                        int resp2 = cDalc.UpdateArtExtNotifications(prod.ID, 2);
                                        LogSystem.WriteLogDebugDirect("Respuesta a grabado de notificación ID :  "+ prod.ID  + "--> " + resp2, "StockCentralfromProductecaProductsCheckByNotifications");

                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        LogSystem.WriteLogDebugDirect("Respuesta ERRORRRRR --> " + msg_arr[0] + "  ----  " + msg_arr[1] , "StockCentralfromProductecaProductsCheckByNotifications");
                        int resp2 = cDalc.UpdateArtExtNotifications(prod.ID, 3);
                        LogSystem.WriteLogDebugDirect("Respuesta a grabado de notificación ID :  "+ prod.ID  + "--> " + resp2, "StockCentralfromProductecaProductsCheckByNotifications");

                    }
                    //string responseProvider = magento.GetBy_SkuProductTeca_List(p.Token, p.ID);

                    LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromProductecaProductsCheckByNotifications");
                }
            }




        }

        public static void InicializarCatalogo_CategoriasForProducteca(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            string URL_Producteca = System.Configuration.ConfigurationSettings.AppSettings["URL_Producteca"];
            int TopeStockUrgente = 10;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            var magento = new Magento(URL_Producteca);
            int GenerarImagenesTodoelCatalogo = 0;
            int StoreID_Magento = -1;

            try
            {
                MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                StoreID_Magento = Store_Magento.idStoreMagento;
            }
            catch (Exception dd)
            {
                LogSystem.WriteLogDebugDirect("Parametro de Store MAgento no OBTENIDO + " + dd.Message, "StockCentralfromProductecaProductsCheck");
                return;
            }

            DateTime timeToken = DateTime.Now;

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);
            StockCentralToMagento.DataAccess.ProviderDALC p_dalc = new ProviderDALC();
            List<StockCentralToMagento.Entities.ProviderEntity> Providers = p_dalc.GetListByEnabledAndIntegratorAndCatalogID(true, 2, CatalogoID);

            foreach (StockCentralToMagento.Entities.ProviderEntity p in Providers)
            {

                LogSystem.WriteLogDebugDirect(">>>>>>>> Inicio Proceso Categorias - Proveedor :" + p.Name , "StockCentralfromProductecaProductsCheck");
                try
                {
                    string responseTr = "-1|ERROR";
                    responseTr = magento.PutCategoriasForProductTeca(p.Token , CatalogoID, "Productos");
                    LogSystem.WriteLogDebugDirect("Respuesta --> " + responseTr, "StockCentralfromProductecaProductsCheck");
                }
                catch (Exception pr)
                {
                    LogSystem.WriteLogDebugDirect("Respuesta ERRORRRRR --> " + pr.Message + "  ----  " + pr.InnerException.ToString() + " --- " + pr.StackTrace, "StockCentralfromProductecaProductsCheck");

                }

                LogSystem.WriteLogDebugDirect("<<<<<<< FIN Proceso Categoria ", "StockCentralfromProductecaProductsCheck");
            }


        }

        public static void InicializarCatalogo_CheckProductosFromProducteca(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            string URL_Producteca = System.Configuration.ConfigurationSettings.AppSettings["URL_Producteca"];
            int TopeStockUrgente = 10;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            var magento = new Magento(URL_Producteca);
            int GenerarImagenesTodoelCatalogo = 0;
            int StoreID_Magento = -1;

            try
            {
                MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                StoreID_Magento = Store_Magento.idStoreMagento;
            }
            catch (Exception dd)
            {
                LogSystem.WriteLogDebugDirect("Parametro de Store MAgento no OBTENIDO + " + dd.Message, "StockCentralfromProductecaProductsCheck");
                return;
            }

            DateTime timeToken = DateTime.Now;

            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro de generación de imagenes de todo el catalogo -> Desactivado", "StockCentralfromProductecaProductsCheck");
            }

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

            // REVISAR EL SP DEL METODO DE ABAJO
            StockCentralToMagento.DataAccess.ProviderDALC p_dalc = new ProviderDALC();
            List<StockCentralToMagento.Entities.ProviderEntity> Providers = p_dalc.GetListByEnabledAndIntegratorAndCatalogID(true, 2, CatalogoID);

            foreach(StockCentralToMagento.Entities.ProviderEntity p in Providers)
            {
                LogSystem.WriteLogDebugDirect("Inicio Proceso Proveedor -> " + p.Name, "StockCentralfromProductecaProductsCheck");

                string msg = magento.Get_ProductTeca_SkuList(p.Token, p.ID);
                string[] msg_arr = msg.Split('|');
                if (msg_arr[0] != "-1" && msg_arr[0] != "-99")
                {
                    JArray o = JArray.Parse(msg);
                    if (o != null)
                    {
                        if (o.Count > 0)
                        {
                            for (int j = 0; j < o.Count; j++)
                            {
                                LogSystem.WriteLogDebugDirect("------------------- Producto "+ j + "/" + o.Count + " del Seller " + p.Name +" --------------------", "StockCentralfromProductecaProductsCheck");
                                LogSystem.WriteLogDebugDirect("Producto encontrado en Producteca --> " + o[j].ToString() /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                try
                                {
                                    string CatName = "-1";
                                    try
                                    {
                                        string catSearch = (string)o[j].SelectToken("category");
                                        ItemCompleteSubCategoryEntity rub = scdal.GetSubCategoryByProviderCategoryName(p.ID, catSearch);
                                        CatName = rub.ItemCategoryID.ToString();
                                        LogSystem.WriteLogDebugDirect("Se convirtió y encontró categoria del producto de producteca a --> " + CatName /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                    }
                                    catch (Exception eenocat)
                                    {
                                        LogSystem.WriteLogDebugDirect("Categoria del producto de producteca no encontrada en mapeo" /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                        CatName = "-1";
                                    }

                                    if(CatName == "-1")
                                    {
                                        try
                                        {
                                            string BarCode = "00" + p.ID + (string)o[j].SelectToken("variations[0].sku");
                                            LogSystem.WriteLogDebugDirect("Intentado obtener Categoria en SC de "+ BarCode + " - Catalogo " + CatalogoID, "StockCentralfromProductecaProductsCheck");
                                            CatalogStockEntity prod = cDalc.GetProductStockByBarCode( BarCode, CatalogoID);
                                            CatName = prod.rubro.ItemCategoryID.ToString();
                                            LogSystem.WriteLogDebugDirect("Se encontró categoria del producto en CTC --> " + CatName /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                        }
                                        catch (Exception sct)
                                        {
                                            LogSystem.WriteLogDebugDirect("Producto no categorizado en CTC" /* .Substring(0,100) */ + "...", "StockCentralfromProductecaProductsCheck");
                                            CatName = "-1";
                                        }
                                    }


                                    string responseTr = magento.GetBy_SkuProductTeca_Product(o[j].ToObject<JObject>(), p.ID, CatName);
                                    LogSystem.WriteLogDebugDirect("Respuesta --> " + responseTr, "StockCentralfromProductecaProductsCheck");
                                }catch(Exception pr)
                                {
                                    LogSystem.WriteLogDebugDirect("Respuesta ERRORRRRR --> " + pr.Message + "  ----  " + pr.InnerException.ToString() + " --- " + pr.StackTrace  , "StockCentralfromProductecaProductsCheck");

                                }
                            }
                        }
                    }
                }
                //string responseProvider = magento.GetBy_SkuProductTeca_List(p.Token, p.ID);
                
                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromProductecaProductsCheck");

            }



        }


        // To File of images
        public static void ExportProductosToImagesFile(int CatalogoID, string Token, ref string log)
        {
            int i = 0;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            string URL_FJ = System.Configuration.ConfigurationSettings.AppSettings["URL_API_CTC"];

            int GenerarImagenesTodoelCatalogo = 1;
            string ProviderId = "297";
            var CTCtoFJ = new Magento(URL_FJ);
            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

            LogSystem.WriteLogDebugDirect("Cantidad de Productos obtenidas con esta parametrización : " + sprod.Count.ToString(), "ProductsTools");

            string fileContent = "AV SKU;Stock;imagen_1;imagen_2;imagen_3;imagen_4;imagen_5;imagen_6;imagen_7;imagen_8\n";
            /* PUBLICACION EN CATALOGO */
            i = 0;
            foreach (CatalogStockEntity c in sprod)
            {
                i++;

                LogSystem.WriteLogDebugDirect(" UPDATE IMAGES ", "ProductsTools");
                LogSystem.WriteLogDebugDirect(" ====== ====== ", "ProductsTools");
                LogSystem.WriteLogDebugDirect("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "ProductsTools");
                //
                string response3 = CTCtoFJ.AddImageProduct_ToFile(c.codigobarra, ProviderId, Token);
                fileContent += response3 + '\n';
                
                LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_ToFj Imagen de productos --> " + response3, "ProductsTools");

            }
            File.WriteAllText("ImagesFile.csv", fileContent);

            // imagenes
            LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");


        }



        // To FJ as VENDOR
        public static void InicializarCatalogo_CheckProductosToFJasVendor(int CatalogoID, ref string log)
        {
            int i = 0;
            string URL_FJ = System.Configuration.ConfigurationSettings.AppSettings["URL_FJ"];
            int milisegundos_entre_paginas = 120000;

            int TopeStockUrgente = 10;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            int GenerarImagenesTodoelCatalogo = 0;
            int Pagina = 1;
            int ProductType = 0;
            int CantidadFilas = 100;
            string ProviderId = "297";
            string Brand = "";
            int Active = -1;
            int inStock = -1;

            var CTCtoFJ = new Magento(URL_FJ);

            DateTime timeToken = DateTime.Now;

            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);


            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro de generación de imagenes de todo el catalogo -> Desactivado", "StockCentralToFJasVendorProductsCheck");
            }


            try
            {
                CantidadFilas = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["CantidadProductosATransmitirxEnvio"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro de Cantidad de productos a transmitir por envio en default 100", "StockCentralToFJasVendorProductsCheck");
            }

            try
            {
                milisegundos_entre_paginas  = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Milisegundos_de_espera_entre_paginas_a_enviar_a_FJ"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro milisegundos_entre_paginas no configurado.", "StockCentralToFJasVendorProductsCheck");
            }


            LogSystem.WriteLogDebugDirect("Cantidad de Productos obtenidas con esta parametrización : " + sprod.Count.ToString(), "StockCentralToFJasVendorProductsCheck");
            LogSystem.WriteLogDebugDirect(" ************************** A C T I V A N D O      P R O D U C T O S ***************************** ", "StockCentralToFJasVendorProductsCheck");

            bool Salir = false;
            while (!Salir)
            {
                i++;
                LogSystem.WriteLogDebugDirect(" ", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect("  --------------  Obteniendo de SC la Pagina Nro " + i.ToString(), "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect("  -----------------------------------------------", "StockCentralToFJasVendorProductsCheck");
               
                string response = CTCtoFJ.GetProductsFromCatalogoCTC(CatalogoID, i, ProductType, CantidadFilas, ProviderId, Brand, Active, inStock);

                //LogSystem.WriteLogDebugDirect("Respuesta de SC --> " + response, "StockCentralToFJasVendorProductsCheck");
                //string[] res = response.Split(; // pos 0 = resultado , pos 1 = id producto si ok
                if (response.Substring(0,5) != "ERROR")
                {
                    // Enviar actualización de productos
                    LogSystem.WriteLogDebugDirect("------------------- Enviando actualización producto .... ", "StockCentralToFJasVendorProductsCheck");
                    try
                    {
                        string response2 = CTCtoFJ.UpdateProduct_ToFj(response);
                        LogSystem.WriteLogDebugDirect("------------------- Respuesta UpdateProduct_ToFj --> " + response2, "StockCentralToFJasVendorProductsCheck");
                    }
                    catch (Exception f)
                    {

                        LogSystem.WriteLogDebugDirect("------------------- Error " + f.Message + f.InnerException + f.Source, "StockCentralToFJasVendorProductsCheck");
                        LogSystem.WriteLogDebugDirect("------------------- Paquete con error -> " + response, "StockCentralToFJasVendorProductsCheck");
                    }

                }
                else
                    Salir = true;
                LogSystem.WriteLogDebugDirect("****************************** Esperando  " + milisegundos_entre_paginas + " milisegundos para que FJ no de error. *********************************", "StockCentralToFJasVendorProductsCheck");

                Thread.Sleep(milisegundos_entre_paginas);
            }

            LogSystem.WriteLogDebugDirect(" ************************** D E S A C T I V A N D O      P R O D U C T O S ***************************** ", "StockCentralToFJasVendorProductsCheck");
            /*
            sprod = cDalc.GetAllCatalogStockDisabled(CatalogoID, 0);

            foreach (CatalogStockEntity c in sprod)
            {
                i++;
                LogSystem.WriteLogDebugDirect(" ", "StockCentralfromVTEXProductsCheck");
                LogSystem.WriteLogDebugDirect("DISABLE PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralfromVTEXProductsCheck");
                //
                //string response = notification.PutNotificationBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                string response = notification.GetProductBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);

                LogSystem.WriteLogDebugDirect("Respuesta --> " + response, "StockCentralfromVTEXProductsCheck");
                string[] res = response.Split('|'); // pos 0 = resultado , pos 1 = id producto si ok
                if (res[0] == "OK")
                {
                    // Enviar actualización de precios

                    LogSystem.WriteLogDebugDirect("Enviando actualización producto .... ", "StockCentralfromVTEXProductsCheck");
                    string response2 = notification.UpdateProduct_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1]);
                    LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");


                }
                else if (res[0] == "NOT FOUND")
                {
                    LogSystem.WriteLogDebugDirect("Producto NO PUBLICADO - ESTA Deshabilitado y no se publica .... ", "StockCentralfromVTEXProductsCheck");
                }
                else
                {
                    LogSystem.WriteLogDebugDirect("  ERROR xxxxxxxxxxxxxxxxx " + response, "StockCentralfromVTEXProductsCheck");
                }

                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromVTEXProductsCheck");

            }
            */



            LogSystem.WriteLogDebugDirect("---------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");
            LogSystem.WriteLogDebugDirect("oooooooo PUBLICACION DE PRODUCTOS", "StockCentralToFJasVendorProductsCheck");
            LogSystem.WriteLogDebugDirect("---------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");



            /* PUBLICACION EN CATALOGO */
            i = 0;
            foreach (CatalogStockEntity c in sprod)
            {
                i++;

                LogSystem.WriteLogDebugDirect(" UPDATE IMAGES - GOLDEN RULES - PUBLICATIONS", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect(" ====== ====== = ====== ===== = ============", "StockCentralToFJasVendorProductsCheck");
                LogSystem.WriteLogDebugDirect("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralToFJasVendorProductsCheck");
                //
                LogSystem.WriteLogDebugDirect("Enviando actualización Imagen de productos .... ", "StockCentralToFJasVendorProductsCheck");
                string response3 = CTCtoFJ.UpdateImageProduct_ToFj(c.codigobarra, ProviderId, CatalogoID);
                LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_ToFj Imagen de productos --> " + response3, "StockCentralToFJasVendorProductsCheck");




                //
                // Activación de GOLDEN RULES
                // --------------------------


                // Tenemos un problema con los GR ya creados.
                // Pra resolverlos tenemos que ir a FJ y traer todos los GR de los productos. 
                // Una vez que los traemos, tenemos que verificar a que MKP está apuntado y si es el que estamos evaluando, asociar el ID del GR al Codigo SKU en un diccionario clave=valor

                /*
                 Dictionary<string, string> idGRinFJ =  new Dictionary<string, string>();

                 iterar entre todos los registros y si este corresponde al Canal evaluado hacer

                    idGRinFJ.Add(SKU, IDFJGR);

                 Luego para acceder al GR si es que existe se evalua como
                   
                 si existe 
                  idGRinFJ[SKU]
                obtengo valor y actualizo el GR por 
                put https://api.fulljaus.com/v1/golden_rule_product/{id}

                si no existe lo creo como es el codigo debajo

                 */




                LogSystem.WriteLogDebugDirect("Enviando Golden Rules de productos .... ", "StockCentralToFJasVendorProductsCheck");
                string response33 = CTCtoFJ.SetGoldenruleProduct_ToFj(c.codigobarra, cat.idCatalogoMagento, Convert.ToDecimal(c.precioventa));
                LogSystem.WriteLogDebugDirect("Respuesta Golden Rules de productos --> " + response33, "StockCentralToFJasVendorProductsCheck");
                string[] res3 = response33.Split('|');
                if (res3[0] == "0")
                {
                    LogSystem.WriteLogDebugDirect("golden Rules Ok ", "StockCentralToFJasVendorProductsCheck");
                }
                else
                {
                    LogSystem.WriteLogDebugDirect("XXXXXX  Error Golden Rules", "StockCentralToFJasVendorProductsCheck");
                }


                // PUBLICACION
                LogSystem.WriteLogDebugDirect("Enviando Publicación de productos .... ", "StockCentralToFJasVendorProductsCheck");
                string response34 = "";
                if (c.enable )
                    response34 = CTCtoFJ.PublicateProduct_ToFj(c.codigobarra, cat.idCatalogoMagento, c.stock, c.enable  );
               // else
               //     response34 = CTCtoFJ.PublicationUpdateProduct_ToFj(c.codigobarra, cat.idCatalogoMagento);
                LogSystem.WriteLogDebugDirect("Respuesta Publicación de productos --> " + response34, "StockCentralToFJasVendorProductsCheck");
                string[] res = response34.Split('|');
                if (res[0] == "0")
                {
                    LogSystem.WriteLogDebugDirect(" Publicación de productos OK ", "StockCentralToFJasVendorProductsCheck");
                }
                else
                {
                    LogSystem.WriteLogDebugDirect("XXXXXX  Error Publicación de productos", "StockCentralToFJasVendorProductsCheck");
                }
            }

            // imagenes
            LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralToFJasVendorProductsCheck");


        }


        // VTEX
        public static void InicializarCatalogo_CheckProductosFromVTEX(int CatalogoID,  ref string log)
        {
            int i = 0;
            string URL_VTEX_notification_Product = System.Configuration.ConfigurationSettings.AppSettings["URL_VTEX_notification_Product"];
            string URL_VTEX_suggestion_Product = System.Configuration.ConfigurationSettings.AppSettings["URL_VTEX_suggestion_Product"];
            string X_VTEX_API_AppToken = System.Configuration.ConfigurationSettings.AppSettings["X-VTEX-API-AppToken"];
            string X_VTEX_API_AppKey = System.Configuration.ConfigurationSettings.AppSettings["X-VTEX-API-AppKey"];
            string MkpSellerID = (System.Configuration.ConfigurationSettings.AppSettings["Vtex_Seller_Id"]);

            int TopeStockUrgente = 10;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            var notification = new Magento(URL_VTEX_notification_Product);
            var suggestion = new Magento(URL_VTEX_suggestion_Product);
            int GenerarImagenesTodoelCatalogo = 0;
 
  
            DateTime timeToken = DateTime.Now;
           
            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);
           
            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro de generación de imagenes de todo el catalogo -> Desactivado", "StockCentralfromVTEXProductsCheck");
            }

            LogSystem.WriteLogDebugDirect("Cantidad de Productos obtenidas con esta parametrización : " + sprod.Count.ToString(), "StockCentralfromVTEXProductsCheck");

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);



            foreach (CatalogStockEntity c in sprod)
            {
                i++;
                LogSystem.WriteLogDebugDirect(" ", "StockCentralfromVTEXProductsCheck");
                LogSystem.WriteLogDebugDirect("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralfromVTEXProductsCheck");
                //
                // Modificación API VTX 2024-02-15
                //string response = notification.GetProductBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                string response = notification.GetProductDetailBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);

                LogSystem.WriteLogDebugDirect("Respuesta --> " + response, "StockCentralfromVTEXProductsCheck"); 
                string[] res = response.Split('|'); // pos 0 = resultado , pos 1 = id producto si ok
                if (res[0] == "OK")
                {
                    // Enviar actualización de precios

                    LogSystem.WriteLogDebugDirect("Enviando actualización producto .... ", "StockCentralfromVTEXProductsCheck");

                    // Modificación API VTX 2024-02-15
                    //string response2 = notification.UpdateProduct_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1]);
                    string response2 = notification.UpdateProductwDetail_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1], res[2]);
                    LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");


                } else if(res[0] == "NOT FOUND")
                {
                    LogSystem.WriteLogDebugDirect("Enviando nuevo producto .... ", "StockCentralfromVTEXProductsCheck");
                    string response2 = notification.NewProduct_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID);
                    LogSystem.WriteLogDebugDirect("Respuesta NewProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");
 

                }
                else
                {
                    LogSystem.WriteLogDebugDirect("  ERROR xxxxxxxxxxxxxxxxx " + response, "StockCentralfromVTEXProductsCheck");
                }
                
                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromVTEXProductsCheck");

            }
            LogSystem.WriteLogDebugDirect(" ************************** D E S A C T I V A N D O      P R O D U C T O S ***************************** ", "StockCentralfromVTEXProductsCheck");
            i = 0;
            sprod = cDalc.GetAllCatalogStockDisabled (CatalogoID, 0);

            foreach (CatalogStockEntity c in sprod)
            {
                i++;
                LogSystem.WriteLogDebugDirect(" ", "StockCentralfromVTEXProductsCheck");
                LogSystem.WriteLogDebugDirect("DISABLE PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralfromVTEXProductsCheck");
                //
                //string response = notification.PutNotificationBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                //string response = notification.GetProductBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                string response = notification.GetProductDetailBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);

                LogSystem.WriteLogDebugDirect("Respuesta --> " + response, "StockCentralfromVTEXProductsCheck");
                string[] res = response.Split('|'); // pos 0 = resultado , pos 1 = id producto si ok
                if (res[0] == "OK")
                {
                    // Enviar actualización de precios

                    LogSystem.WriteLogDebugDirect("Enviando actualización producto .... ", "StockCentralfromVTEXProductsCheck");
                    //string response2 = notification.UpdateProduct_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1]);
                    string response2 = notification.UpdateProductwDetail_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1], res[2]);
                    LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");


                }
                else if (res[0] == "NOT FOUND")
                {
                    LogSystem.WriteLogDebugDirect("Producto NO PUBLICADO - ESTA Deshabilitado y no se publica .... ", "StockCentralfromVTEXProductsCheck");
                }
                else
                {
                    LogSystem.WriteLogDebugDirect("  ERROR xxxxxxxxxxxxxxxxx " + response, "StockCentralfromVTEXProductsCheck");
                }

                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromVTEXProductsCheck");

            }




        }
        public static void InicializarCatalogo_CheckProductosToVTEXbySeller(int CatalogoID, ref string log)
        {
            int i = 0;
            string URL_VTEX_notification_Product = System.Configuration.ConfigurationSettings.AppSettings["URL_VTEX_notification_Product"];
            string URL_VTEX_suggestion_Product = System.Configuration.ConfigurationSettings.AppSettings["URL_VTEX_suggestion_Product"];
            string X_VTEX_API_AppToken = System.Configuration.ConfigurationSettings.AppSettings["X-VTEX-API-AppToken"];
            string X_VTEX_API_AppKey = System.Configuration.ConfigurationSettings.AppSettings["X-VTEX-API-AppKey"];
            string MkpSellerID = (System.Configuration.ConfigurationSettings.AppSettings["Vtex_Seller_Id"]);

            int TopeStockUrgente = 10;

            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.SubCategoryDALC scdal = new SubCategoryDALC();
            var notification = new Magento(URL_VTEX_notification_Product);
            var suggestion = new Magento(URL_VTEX_suggestion_Product);
            int GenerarImagenesTodoelCatalogo = 0;


            DateTime timeToken = DateTime.Now;

            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);

            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebugDirect("Parametro de generación de imagenes de todo el catalogo -> Desactivado", "StockCentralfromVTEXProductsCheck");
            }

            LogSystem.WriteLogDebugDirect("Cantidad de Productos obtenidas con esta parametrización : " + sprod.Count.ToString(), "StockCentralfromVTEXProductsCheck");

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);



            foreach (CatalogStockEntity c in sprod)
            {
                i++;
                LogSystem.WriteLogDebugDirect(" ", "StockCentralfromVTEXProductsCheck");
                LogSystem.WriteLogDebugDirect("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralfromVTEXProductsCheck");
                //
                // Modificación API VTX 2024-02-15
                //string response = notification.GetProductBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                string response = notification.GetProductDetailBy_Sku_VTEXbySeller(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);

                LogSystem.WriteLogDebugDirect("Respuesta --> " + response, "StockCentralfromVTEXProductsCheck");
                string[] res = response.Split('|'); // pos 0 = resultado , pos 1 = id producto si ok
                if (res[0] == "OK")
                {
                    // Enviar actualización de precios

                    LogSystem.WriteLogDebugDirect("Enviando actualización producto .... ", "StockCentralfromVTEXProductsCheck");

                    // Modificación API VTX 2024-02-15
                    //string response2 = notification.UpdateProduct_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1]);
                    string response2 = notification.UpdateProductwDetail_VTEXbySeller(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1], res[2]);
                    LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");


                }
                else if (res[0] == "NOT FOUND")
                {
                    LogSystem.WriteLogDebugDirect("Enviando nuevo producto .... ", "StockCentralfromVTEXProductsCheck");
                    string response2 = notification.NewProduct_VTEXbySeller(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID);
                    LogSystem.WriteLogDebugDirect("Respuesta NewProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");


                }
                else
                {
                    LogSystem.WriteLogDebugDirect("  ERROR xxxxxxxxxxxxxxxxx " + response, "StockCentralfromVTEXProductsCheck");
                }

                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromVTEXProductsCheck");

            }
            LogSystem.WriteLogDebugDirect(" ************************** D E S A C T I V A N D O      P R O D U C T O S ***************************** ", "StockCentralfromVTEXProductsCheck");
            i = 0;
            sprod = cDalc.GetAllCatalogStockDisabled(CatalogoID, 0);

            foreach (CatalogStockEntity c in sprod)
            {
                i++;
                LogSystem.WriteLogDebugDirect(" ", "StockCentralfromVTEXProductsCheck");
                LogSystem.WriteLogDebugDirect("DISABLE PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", "StockCentralfromVTEXProductsCheck");
                //
                //string response = notification.PutNotificationBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                //string response = notification.GetProductBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);
                string response = notification.GetProductDetailBy_Sku_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, c.codigobarra, MkpSellerID);

                LogSystem.WriteLogDebugDirect("Respuesta --> " + response, "StockCentralfromVTEXProductsCheck");
                string[] res = response.Split('|'); // pos 0 = resultado , pos 1 = id producto si ok
                if (res[0] == "OK")
                {
                    // Enviar actualización de precios

                    LogSystem.WriteLogDebugDirect("Enviando actualización producto .... ", "StockCentralfromVTEXProductsCheck");
                    //string response2 = notification.UpdateProduct_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1]);
                    string response2 = notification.UpdateProductwDetail_VTEX(X_VTEX_API_AppToken, X_VTEX_API_AppKey, "297", c.codigobarra, MkpSellerID, res[1], res[2]);
                    LogSystem.WriteLogDebugDirect("Respuesta UpdateProduct_VTEX --> " + response2, "StockCentralfromVTEXProductsCheck");


                }
                else if (res[0] == "NOT FOUND")
                {
                    LogSystem.WriteLogDebugDirect("Producto NO PUBLICADO - ESTA Deshabilitado y no se publica .... ", "StockCentralfromVTEXProductsCheck");
                }
                else
                {
                    LogSystem.WriteLogDebugDirect("  ERROR xxxxxxxxxxxxxxxxx " + response, "StockCentralfromVTEXProductsCheck");
                }

                LogSystem.WriteLogDebugDirect("-------------------------------------------------------------------------------", "StockCentralfromVTEXProductsCheck");

            }




        }
        // StockCentraltoMagentoServices
        public static void InicializarCatalogo_ImportProductos(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);
            var magento = new Magento(URL);
            int GenerarImagenesTodoelCatalogo = 0;
            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de imagenes de todo el catalogo -> Desactivado", ref log); ;
            }
            int StoreID_Magento = -1;
            try
            {
                MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                StoreID_Magento = Store_Magento.idStoreMagento;
            }
            catch (Exception dd)
            {
                LogSystem.WriteLogDebug("Parametro de Store MAgento no OBTENIDO + " + dd.Message, ref log);
                return;
            }

            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

            /*

            */

            // RESETEO PARA GENERAR UNA IMPORTACION de los productos nuevos
            string ListaProductos = "-1";
            LogSystem.WriteLogDebug("===== Creando Listado completo de Productos ---NEWS--- de los catalogos a incluir en MAGENTO --> STARTUP PROGRAMA o RESET LISTADO DE PRODUCTOS", ref log);
            //ProcessExportImages(sprod_w_errors, CatalogoID, ref log);
            string[] ListaCSV;
            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( 0 , " + ListaProductos + ")", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoNEW(0, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_TODOS_NEWS");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }


            // RESETEO PARA GENERAR UNA IMPORTACION GLOBAL
            ListaProductos = "-1";
            LogSystem.WriteLogDebug("===== Creando Listado completo de Productos del Catalogo " + cat.MagentoWebSite + "  a incluir en MAGENTO --> STARTUP PROGRAMA o RESET LISTADO DE PRODUCTOS", ref log);
            //ProcessExportImages(sprod_w_errors, CatalogoID, ref log);
            ListaCSV = null;
            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + ")", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagento(CatalogoID, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Global");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagentoUPD( " + CatalogoID + " , " + ListaProductos + ")", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoUPD (CatalogoID, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_UPD");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            try
            {
                LogSystem.WriteLogDebug("===== GetPriceDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + ")", ref log);
                ListaCSV = cDalc.GetPriceDataToImportListProductsToMagento(CatalogoID, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Price");

                LogSystem.WriteLogDebug("===== GetPriceDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + " UPDATE)", ref log);
                ListaCSV = cDalc.GetPriceDataToImportListProductsToMagentoUpdate(CatalogoID, ListaProductos);
                LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Price_Update");


                if (CatalogoID == 30)
                {
                    int IdEmpresaMaximo = 5;
                    try
                    {
                        IdEmpresaMaximo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["NroStoreViewMaximoCuponstar"]);
                    }
                    catch (Exception d)
                    {
                        LogSystem.WriteLogDebug("Parametro de StoreView máximo de Cuponstar no presente", ref log); ;
                    }

                    for (int ii = 2; ii <= IdEmpresaMaximo; ii++)
                    {
                        LogSystem.WriteLogDebug("===== GetPriceDataToImportListProductsToMagento( " + CatalogoID + " , StoreView " + ii + " , " + ListaProductos + ")", ref log);
                        ListaCSV = cDalc.GetPriceDataToImportListProductsToMagento(CatalogoID, ListaProductos, ii);
                        LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_" + cat.MagentoWebSite + "_ST_" + ii + "_" + CatalogoID.ToString() + "_Price");

                        LogSystem.WriteLogDebug("===== GetPriceDataToImportListProductsToMagento( " + CatalogoID + " , StoreView " + ii + " , " + ListaProductos + " UPDATE)", ref log);
                        ListaCSV = cDalc.GetPriceDataToImportListProductsToMagentoUpdate(CatalogoID, ListaProductos, ii);
                        LogSystem.WriteListToFile(ListaCSV, "ImportToMagento_" + cat.MagentoWebSite + "_ST_" + ii + "_" + CatalogoID.ToString() + "_Price_UPDATE");
                    }
                }
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            if (GenerarImagenesTodoelCatalogo > 0)
                ProcessExportImages(sprod, CatalogoID, ref log);
            /*
            if (CatalogoID == 1)
            {
                ListaCSV = cDalc.GetDataToImportListProductsToPIN_COLLINSON(CatalogoID, ListaProductos);
                ProcessExportToExcel("ImportToMagento_COLLINSON_" + CatalogoID.ToString() + "_Global", ".xls", ListaCSV, ref log);
            }
            */
        }

        // StockCentralToMagentoServicesMKP
        public static void InicializarCatalogo_ImportProductosMkp(int CatalogoID, int MagentoWebSiteId, ref string log)
        {
            int i = 0;
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);
            var magento = new Magento(URL);
            int GenerarImagenesTodoelCatalogo = 0;
            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de imagenes de todo el catalogo -> Desactivado", ref log); ;
            }

            int StoreID_Magento = -1;
            try
            {
                MagentoStoreEntity Store_Magento = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(MagentoWebSiteId, CatalogoID);
                StoreID_Magento = Store_Magento.idStoreMagento;
            }
            catch (Exception dd)
            {
                LogSystem.WriteLogDebug("Parametro de Store MAgento no OBTENIDO + " + dd.Message, ref log);
                return;
            }



            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);

            /*

            */

            // RESETEO PARA GENERAR UNA IMPORTACION GLOBAL
            string ListaProductos = "-1";
            LogSystem.WriteLogDebug("===== Creando Listado completo de Productos del Catalogo " + cat.MagentoWebSite + "  a incluir en MAGENTO --> STARTUP PROGRAMA o RESET LISTADO DE PRODUCTOS", ref log);
            //ProcessExportImages(sprod_w_errors, CatalogoID, ref log);
            string[] ListaCSV;
            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + ")", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkp(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length , ref log);

                LogSystem.WriteListToFile(ListaCSV, "InToMgt_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Global");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento_Global ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }

            try
            {

                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + " UPDATE)", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkpUpd(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMgt_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Global_UPD");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento_Global_UPD ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            try
            {

                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + " NEW)", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkpNew(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMgt_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Global_NEW");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento_Global_NEW ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            try
            {

                LogSystem.WriteLogDebug("===== GetDataToImportListProductsPricesToMagento( " + CatalogoID + " , " + ListaProductos + " UPDATE)", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsPricesToMagentoMkpUpd(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMgt_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Global_UPD_Precios");

            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento_Global_UPD_Precios ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            try
            {

                LogSystem.WriteLogDebug("===== GetDataToImportListProductsPricesToMagento( " + CatalogoID + " , " + ListaProductos + " UPDATE PROMO)", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsPricesToMagentoMkpUpdPromo(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMgt_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Global_UPD_Precios_Promo");

            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento_Global_UPD_Precios ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }

            /*
            if (CatalogoID == 1)
            {
                ListaCSV = cDalc.GetDataToImportListProductsToPIN_COLLINSON(CatalogoID, ListaProductos);
                ProcessExportToExcel("ImportToMagento_COLLINSON_" + CatalogoID.ToString() + "_Global", ".xls", ListaCSV, ref log);
            }
            */
            StockCentralToMagento.DataAccess.ProviderDALC pDalc = new ProviderDALC();
            List<StockCentralToMagento.Entities.ProviderEntity> Prov = pDalc.GetListByEnabledAnVendorAndCatalogID(true, true, CatalogoID);

            foreach (StockCentralToMagento.Entities.ProviderEntity p in Prov)
            {
                /*
                ListaProductos = "";
                List<CatalogStockEntity> sprod_Prov = cDalc.GetAllCatalogStock(CatalogoID, 0,  p.ID);
                foreach(CatalogStockEntity c in sprod_Prov)
                {
                    ListaProductos += c.codigobarra + ",";
                }
                ListaProductos = ListaProductos.Substring(0, ListaProductos.Length - 1);
                */
                try
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + p.Name + ")", ref log);
                    ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkp(CatalogoID, p.ID);
                    LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                    LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_" + p.Name);
                }
                catch (Exception li)
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                }
                try
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + p.Name + " UPDATE)", ref log);
                    ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkpUpd(CatalogoID, p.ID);
                    LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                    LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_" + p.Name + "_UPD");
                }
                catch (Exception li)
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                }
                try
                {

                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsPRICESToMagento( " + CatalogoID + " , " + p.Name + " PRICES UPDATE)", ref log);
                    ListaCSV = cDalc.GetDataToImportListProductsPricesToMagentoMkpUpd(CatalogoID, p.ID);
                    LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                    LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_" + p.Name + "_UPD_Precios");
                }
                catch (Exception li)
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                }
                try
                {

                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsPRICESToMagento( " + CatalogoID + " , " + p.Name + " PRICES UPDATE PROMO)", ref log);
                    ListaCSV = cDalc.GetDataToImportListProductsPricesToMagentoMkpUpdPromo(CatalogoID, p.ID);
                    LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                    LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_" + p.Name + "_UPD_Precios_Promo");
                }
                catch (Exception li)
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                }
                try
                {

                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + p.Name + " NEW)", ref log);
                    ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkpNew(CatalogoID, p.ID);
                    LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                    LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_" + p.Name + "_NEW");
                }
                catch (Exception li)
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                }

            }


            ListaProductos = "-2";

            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + ") CTCGROUP", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkp(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_CTCGROUP_CP");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }

            try
            {

                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + ListaProductos + ") CTCGROUP", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkpUpd(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_CTCGROUP_CP_UPD");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }

            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsPricesToMagento( " + CatalogoID + " , " + ListaProductos + ") CTCGROUP", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsPricesToMagentoMkpUpd(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_CTCGROUP_CP_UPD_Precios");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }

            try
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsPricesToMagento( " + CatalogoID + " , " + ListaProductos + ") CTCGROUP PROMO", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsPricesToMagentoMkpUpdPromo(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_CTCGROUP_CP_UPD_Precios_Promo");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            try
            {

                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " ,  " + ListaProductos + " NEW)", ref log);
                ListaCSV = cDalc.GetDataToImportListProductsToMagentoMkpNew(CatalogoID, ListaProductos);
                LogSystem.WriteLogDebug("===== Resultado Cantidad de items -> " + ListaCSV.Length, ref log);
                LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "__CTCGROUP__NEW");
            }
            catch (Exception li)
            {
                LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
            }
            int GenerarArchivosCategorias = 0;
            try
            {
                GenerarArchivosCategorias = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarArchivosCategorias"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de archivos x categoria -> Desactivado", ref log); ;
            }

            if (GenerarArchivosCategorias>0)
            {
                CategoryDALC catdalc = new CategoryDALC();
                List<ItemCategoryEntity> Cat = catdalc.GetCategoriesByMenuIDAndCatalogId(60, CatalogoID);
                foreach (StockCentralToMagento.Entities.ItemCategoryEntity c in Cat)
                {
                    /*
                    ListaProductos = "";
                    List<CatalogStockEntity> sprod_Prov = cDalc.GetAllCatalogStock(CatalogoID, 0,  p.ID);
                    foreach(CatalogStockEntity c in sprod_Prov)
                    {
                        ListaProductos += c.codigobarra + ",";
                    }
                    ListaProductos = ListaProductos.Substring(0, ListaProductos.Length - 1);
                    */
                    try
                    {
                        LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , " + c.Name + ")", ref log);
                        ListaCSV = cDalc.GetDataToImportListProductsToMagentoCatMkp(CatalogoID, c.ItemCategoryID);
                        LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_Cat_" + c.Name);

                    }
                    catch (Exception li)
                    {
                        LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                    }
                }
            }
            int GenerarArchivosSubCategorias = 0;
            try
            {
                GenerarArchivosSubCategorias = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ID_de_Archivo_De_Subcategoria_a_Generar"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de archivos x una Subcategoria -> Desactivado", ref log); ;
            }

            if (GenerarArchivosSubCategorias > 0)
            {
                SubCategoryDALC Scatdalc = new SubCategoryDALC();
                ItemCompleteSubCategoryEntity sc = Scatdalc.GetSubCategoryByID(GenerarArchivosSubCategorias);
                
                try
                {
                    if (sc != null && sc.ItemCategoryID > 0)
                    {
                        LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento( " + CatalogoID + " , Rubro - " + sc.Name2 + ")", ref log);
                        ListaCSV = cDalc.GetDataToImportListProductsToMagentoSubCatMkp(CatalogoID, GenerarArchivosSubCategorias);
                        LogSystem.WriteListToFile(ListaCSV, "InToMkp_" + cat.MagentoWebSite + "_" + CatalogoID.ToString() + "_SUBCategoria_" + GenerarArchivosSubCategorias);
                    }
                }
                catch (Exception li)
                {
                    LogSystem.WriteLogDebug("===== GetDataToImportListProductsToMagento ----> ERROR : " + li.Message + li.StackTrace + li.Source, ref log);
                }
                
            }
            if (GenerarImagenesTodoelCatalogo > 0)
                ProcessExportImagesFTP(sprod, CatalogoID, ref log);


        }

        static private string accesskey = "";
        static private string secretkey = "";

        private static void ProcessExportToExcel(string Path, string extension, String[] lineList, ref string log)
        {

            string productPathFile = ConfigurationSettings.AppSettings["PathProductsFile"];
            string fullPath = productPathFile + DateTime.Now.ToString("yyyy-MM-dd") + "_" +  Path + extension;
            
            IWorkbook workbook;
            if (extension == ".xls")
                workbook = new HSSFWorkbook();//Para archivo ".xls" se usa la clase HSSFWorkbook
            else
                workbook = new XSSFWorkbook();//Para archivo ".xlsx" se usa la clase XSSFWorkbook

            var sheet = workbook.CreateSheet("Product Info");
            LogSystem.WriteLogDebug("Exportación a Excel - Libro y hoja instanciados", ref log);

            for (int lineNumber = 0; lineNumber < lineList.Length; lineNumber++)
            {
                string[] colArray = lineList[lineNumber].Split(';');

                var row = sheet.CreateRow(lineNumber);

                for (int colNumber = 0; colNumber < colArray.Length; colNumber++)
                {
                    var cell = row.CreateCell(colNumber);
                    cell.SetCellValue(colArray[colNumber]);

                    //Header en negrita (En la primer línea siempre vienen los nombres de las columnas)
                    if (lineNumber == 0)
                    {
                        var headerCellStyle = workbook.CreateCellStyle();
                        var headerFont = workbook.CreateFont();
                        headerFont.Boldweight = (short)FontBoldWeight.Bold;
                        headerCellStyle.SetFont(headerFont);
                        cell.CellStyle = headerCellStyle;
                    }
                }
            }
            LogSystem.WriteLogDebug("Exportación a Excel - Valores asignados a celdas", ref log);

            using (var fileData = new FileStream(fullPath, FileMode.Create))
            {
                workbook.Write(fileData);
            }
        }


        public static void SetCategoriesStoreView(string StoreView, string rootCategory, ref string log)
        {
            var filter = new Filter();
            filter.FilterExpressions.Add(new FilterExpression("name", ExpressionOperator.eq, rootCategory));
            LogSystem.WriteLogDebug("===== Iniciando análisis de Categorias. Categoria raiz : "+rootCategory+". Store View : " + StoreView, ref log);
            
            CategoryList cat = GetCategoriesByStoreView(filter, Token, StoreView);
            
            if (cat.Name != null)
            {
                string name = cat.Name;
                int catID = cat.Id;
                ViewCategoryChildrens(cat, name, StoreView, ref log);
            }  
            else
            {
                LogSystem.WriteLogDebug("===== Categorias NO encontradas ", ref log);
            }
        }
        public static decimal processEventsStockProducts(int ID, ref string log)
        {
            LogSystem.WriteLogDebug(" ======== INICIO ITERACION ===========", ref log);
            decimal LastMov = Convert.ToDecimal(ID);
            LogSystem.WriteLogDebug(" ======== Movimiento inicial ---> " + LastMov, ref log);

            string[] CatalogoExplicito  ;
            bool esMarketplace = true;
            try
            {
                CatalogoExplicito = (System.Configuration.ConfigurationSettings.AppSettings["ActivarSoloCatalogoNro"]).Split(',');
            }
            catch (Exception d)
            {
                CatalogoExplicito = new string[] { "0" };
                LogSystem.WriteLogDebug("Parametro de Catalogo particular -> Desactivado", ref log); ;
            }

            try
            {
                esMarketplace = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["esMarketplace"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug(" ====== Parametro de Marketplace no presente ------", ref log); ;
            }
            LogSystem.WriteLogDebug(" Es Modo MARKETPLACE -> " + esMarketplace, ref log); ;
            string ListaMovimientosAEnviar = "";
            try
            {
                ListaMovimientosAEnviar = System.Configuration.ConfigurationSettings.AppSettings["ListaMovimientosAEnviar"];
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug(" ====== Parametro de ListaMovimientosAEnviar no presente ------", ref log); ;
            }

            LogSystem.WriteLogDebug(" ====== Parametro de ListaMovimientosAEnviar   ------ " + ListaMovimientosAEnviar, ref log); ;

            StockCentralToMagento.DataAccess.MovementDALC Mdalc = new MovementDALC();
            StockCentralToMagento.Entities.MovementEntity[] movs;
            try
            {
                if (ListaMovimientosAEnviar == "")
                    movs = Mdalc.GetMovementsFromID(ID);
                else
                    movs = Mdalc.GetMovementsFromID(ID, ListaMovimientosAEnviar);

                LogSystem.WriteLogDebug(" =============================  Movimientos a Procesar..." + movs.Length, ref log);

            }
            catch (Exception err)
            {
                LogSystem.WriteLogDebug("Error al obtener movimientos..." + err.Message + "\n" + err.StackTrace, ref log);
                return LastMov;
            }

            GetToken(User, Pass);


            MagentoCatalogEntity cat = new StockCentralToMagento.Entities.MagentoCatalogEntity();
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();


            ProviderDALC pDalc = new ProviderDALC();
            ProviderVoucherDALC pvDalc = new ProviderVoucherDALC();

            foreach (MovementEntity m in movs)
            {
                try
                {
                    var magento = new Magento(URL, Token);
                    LogSystem.WriteLogDebug("--------------------------------------------------------- Movimiento [" + m.MovimientoTipoID  + "]. Catalogo [" + m.CatalogId + "].  Valor a setear = " + m.Final  + ", Mov = " + m.MovimientoID.ToString() + ", PROD = " + m.ArticuloCodigo , ref log);
                    bool AplicaCatalogo = false;

                    foreach(string dat in CatalogoExplicito)
                    {
                        if (dat == m.CatalogId.ToString())
                        {
                            AplicaCatalogo = true;
                            break;
                        }
                    }

                    CatalogStockEntity ilsProd = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, m.CatalogId);
                    
                    StockCentralToMagento.Entities.ProviderEntity Pe = pDalc.GetByID(ilsProd.providerid);
                    LogSystem.WriteLogDebug("----------------------------------- ["+ ilsProd.nombre +"] ["+Pe.Name+"]" , ref log);
                    
                    if (!esMarketplace && Pe.Vendor)
                    {
                        LogSystem.WriteLogDebug("--------------------------------------------------------- SE DESCARTA : Este Magento No es MKP y el producto es del VENDOR --> "+ Pe.Name  , ref log);
                        continue;
                    }

                    string res = "";
                    switch (m.MovimientoTipoID)
                    {
                        case 2:
                        case 8:
                        case 11:
                        case 5:
                            int StkQty = cDalc.GetStockByBarCode(m.ArticuloCodigo, m.CatalogId);
                            LogSystem.WriteLogDebug("------ Actualización de Stock [" + m.ArticuloCodigo + "]. Valor a setear = " + StkQty.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                            
                            if (m.MovimientoTipoID == 11)
                            {

                                // CANCELAR ORDENES de MAGENTO
                                string Op = m.AdicionalData.Split('-')[0];
                                if (Op.Substring(0, 3) != "LBR")
                                {
                                    var filter2 = new Filter();
                                    filter2.FilterExpressions.Add(new FilterExpression("reserved_order_id", ExpressionOperator.eq, Op));

                                    Quote Cart = magento.GetQuotesByFilter(Token, filter2);
                                    OrderList Order;
                                    if (Cart != null)
                                    {
                                        var filter3 = new Filter();
                                        filter3.FilterExpressions.Add(new FilterExpression("quote_id", ExpressionOperator.eq, Cart.Item[0].Id.ToString()));
                                        Order = magento.GetOrder(Token, filter3);
                                        if (Order != null && Order.Order[0].Status != "canceled")
                                        {
                                            string RespO = magento.OrderCancelbyID(Token, Order.Order[0].Id.ToString());
                                            if (RespO.Split('|')[0] == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooooo Orden " + Op + " CANCELADA." + " Mov = " + m.MovimientoID.ToString() + " - Resp Cancelación = " + RespO, ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL CANCELAR Orden " + Op + ":  " + RespO + ", Mov = " + m.MovimientoID.ToString(), ref log);

                                            }
                                        }
                                        else
                                            LogSystem.WriteLogDebug("ooooooooooooo Orden " + Op + " CANCELADA PREVIAMENTE. Mov = " + m.MovimientoID.ToString(), ref log);

                                    }
                                }

                            }
                            try
                            {
                                M2Product Prod_s = magento.GetSku(Token, m.ArticuloCodigo);


                                res = magento.SetStockItemsSku(Token, StkQty, m.ArticuloCodigo);
                                if (res == "0")
                                {
                                    LogSystem.WriteLogDebug("ooooooooooooo Stock actualizado exitósamente", ref log);
                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res, ref log);
                                    GetToken(User, Pass);
                                    magento = new Magento(URL, Token);
                                }

                                // Desactivamos la posibilidad de quitar el producto de los catalogos para evitar que luego desaparezcan de los catalgos. Esto tiene que estar controlado con el stock en cero
                                // ***************************************************************************************************************************************************************************
                                // 2024-08-06 DWP
                                /*
                                 
                                if (Prod_s != null)
                                {
                                    foreach (string dat in CatalogoExplicito)
                                    {

                                        cat = cDalc.GetCatalogMagentoByILSCatalogID(Convert.ToInt32(dat));
                                        ilsProd = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, Convert.ToInt32(dat));

                                        if (m.Final <= 0)
                                        {
                                            res = magento.SetStatusSku(Token, 0, Prod_s, cat.idCatalogoMagento);
                                            LogSystem.WriteLogDebug("ooooooooooooo Status  -> " + m.Final.ToString() + ", Catalogo " + cat.idCatalogoILS + ", Resp Magento = " + res, ref log);
                                        }
                                        else
                                        {
                                            if (m.MovimientoTipoID == 5)
                                                if (ilsProd != null && ilsProd.enable)
                                                {
                                                    // res = magento.SetStatusSku(Token, 1, Prod_s, cat.idCatalogoMagento);
                                                    LogSystem.WriteLogDebug("ooooooooooooo Status  -> 1 PERO NO SE ACTUALIZA POR PROBLEMAS DE INFLACION. EL ALTA ES POR PROCESO DE CONCILIACION O ARCHIVO, Catalogo " + cat.idCatalogoILS + ", Resp Magento = " + res, ref log);
                                                }
                                                else
                                                {
                                                    res = magento.SetStatusSku(Token, 0, Prod_s, cat.idCatalogoMagento);
                                                    LogSystem.WriteLogDebug("ooooooooooooo Status  -> 0, Catalogo " + cat.idCatalogoILS + ", Resp Magento = " + res, ref log);
                                                }
                                        }
                                        

                                    }
                                }

                                */
                            }   
                            catch (Exception st)
                            {
                                LogSystem.WriteLogDebug("xxxxxxxxxx ERROR - No pudo actualizar STATUS [" + m.ArticuloCodigo + "] ", ref log);
                                GetToken(User, Pass);
                                magento = new Magento(URL, Token);
                            }

                            break;
                        case 4:
                        case 1:
                        case 103:
                            LogSystem.WriteLogDebug("------  Actualización de Precios [" + m.ArticuloCodigo + "]. " , ref log);
                            cat = cDalc.GetCatalogMagentoByILSCatalogID(m.CatalogId);
                            LogSystem.WriteLogDebug("------  Catalogo  [" + cat.UrlCatalogo + "]. ", ref log);
                            magento = new Magento("http://" + cat.UrlCatalogo , Token);

                            if (AplicaCatalogo)
                            {
                                if (m.CatalogId == 31)
                                {
                                    CatalogStockEntity c = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, Convert.ToInt32(m.CatalogId));
                                    
                                    MagentoStoreEntity mag = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(cat.idCatalogoMagento, cat.idCatalogoILS);


                                    //float conv = (float)0.025;
                                    float conv = c.preciounidad;
                                    int precioL = Convert.ToInt32((c.preciolista / (Convert.ToSingle((100 + c.alicuotaIVA)) / 100)) / conv);

                                    LogSystem.WriteLogDebug("ooooooo Precio de LISTA de REferencia para " + m.ArticuloCodigo + ". Valor referencia LISTA = " + precioL.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString() + ", Precio de LISTA REAL en PESOS = " + c.preciolista.ToString(), ref log);

                                    if (precioL < Convert.ToInt32(c.precioreal))
                                        precioL = Convert.ToInt32(c.precioreal);
                                    LogSystem.WriteLogDebug("ooooooo Precio de LISTA definitivo para " + m.ArticuloCodigo + ". Valor referencia LISTA = " + precioL.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString() , ref log);


                                    LogSystem.WriteLogDebug("------ Cambio de Precio en SKU " + m.ArticuloCodigo + ". Valor a setear precio de LISTA = " + precioL.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                    string res1 = magento.SetPriceSkuStore(Token, mag.idStoreMagento, c.codigobarra, precioL.ToString());
                                    if (res1 == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res1, ref log);
                                        GetToken(User, Pass);
                                        magento = new Magento(URL, Token);
                                    }

                                    LogSystem.WriteLogDebug("------ Cambio de Precio Especial en SKU " + c.codigobarra + ". Valor a setear actual del StockCentral = " + Convert.ToInt32(c.precioreal).ToString(), ref log);

                                    List<Price> ePrice = new List<Price>();
                                    ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = c.codigobarra, store_id = mag.idStoreMagento });

                                    res = magento.SetEspecialPrice(ePrice, mag.idStoreMagento.ToString(), Token);
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio especial actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial :" + res, ref log);
                                        GetToken(User, Pass);
                                        magento = new Magento(URL, Token);
                                    }

                                    int CargaDescuento = 0;
                                    try
                                    {
                                        CargaDescuento = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ActualizaDescuento"].ToString());
                                    }
                                    catch (Exception f)
                                    {
                                        CargaDescuento = 0;
                                    }

                                    if (CargaDescuento == 1)
                                    {
                                        // Actualizar variable descuento
                                        M2Product PD = new M2Product();
                                        PD.Sku = c.codigobarra;


                                        LogSystem.WriteLogDebug("------  AGREGANDO Descuento a Prod  ", ref log);
                                        CustomAttribute attr_d = new CustomAttribute();

                                        attr_d.AttributeCode = "descuento";
                                        attr_d.Value = Convert.ToInt32(Math.Truncate((Convert.ToSingle(precioL) - c.precioreal) / Convert.ToSingle(precioL))).ToString();
                                        PD.CustomAttributes = new List<CustomAttribute>();
                                        PD.CustomAttributes.Add(attr_d);
                                        // LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                        LogSystem.WriteLogDebug("[ " + attr_d.AttributeCode + "] \n" + attr_d.Value, ref log);


                                        res = magento.UpdateProductoTyC(Token, PD, cat.idCatalogoMagento.ToString());
                                        if (res == "OK")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooooo Descuento actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Descuento :" + res, ref log);
                                        }
                                    }
                                }
                                else
                                {

                                    LogSystem.WriteLogDebug("------ Cambio de Precio en SKU " + m.ArticuloCodigo + ". Valor a setear = " + m.Final.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                    CatalogStockEntity c = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, Convert.ToInt32(m.CatalogId));
                                   
                                    MagentoStoreEntity mag = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(cat.idCatalogoMagento, cat.idCatalogoILS);
                                    //float conv = (float)0.025;
                                    float conv = c.preciounidad;
                                    int precioL = Convert.ToInt32((c.preciolista / (Convert.ToSingle((100 + c.alicuotaIVA)) / 100)) / conv);


                                    LogSystem.WriteLogDebug("------ Precio de LISTA de REferencia para " + m.ArticuloCodigo + ". Valor referencia LISTA = " + precioL.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString() + ", Precio de LISTA REAL en PESOS = " + c.preciolista.ToString(), ref log);

                                    if (precioL < Convert.ToInt32(c.precioreal))
                                        precioL = Convert.ToInt32(c.precioreal);
                                    LogSystem.WriteLogDebug("------ Precio de LISTA definitivo para " + m.ArticuloCodigo + ". Valor referencia LISTA = " + precioL.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);


                                    res = magento.SetPriceSkuStore(Token, mag.idStoreMagento, m.ArticuloCodigo, precioL.ToString());
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                        GetToken(User, Pass);
                                        magento = new Magento(URL, Token);
                                    }

                                    LogSystem.WriteLogDebug("------ Cambio de Precio Especial en SKU " + m.ArticuloCodigo + ". Valor a setear = " + m.Final.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);

                                    List<Price> ePrice = new List<Price>();
                                    ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(c.precioreal)), sku = m.ArticuloCodigo, store_id = mag.idStoreMagento });

                                    res = magento.SetEspecialPrice(ePrice, mag.idStoreMagento.ToString(), Token);
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio Especial actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio Especial :" + res, ref log);
                                        res = magento.SetEspecialPriceAlt(Token, mag.idStoreMagento, m.ArticuloCodigo, Convert.ToInt32(c.precioreal).ToString(), cat.MagentoWebSite);
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial Alternativo :" + res, ref log);
                                            List<string> SkuL = new List<string>();
                                            SkuL.Add(m.ArticuloCodigo);
                                            List<Price> EspPriceList = magento.getEspecialPrice(SkuL, Token);
                                            foreach (Price p in EspPriceList)
                                            {
                                                LogSystem.WriteLogDebug("----------- Especial Price existente : store=" + p.store_id + ", price=" + p.price + ", from=" + p.price_from, ref log);
                                                if (p.store_id == mag.idStoreMagento)
                                                {
                                                    res = magento.EspecialPriceDelete(Token, mag.idStoreMagento, m.ArticuloCodigo, p.price.ToString(), cat.MagentoWebSite, p.price_from, p.price_to);
                                                    if (res == "0")
                                                    {
                                                        LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo DEFAULT eliminado exitósamente", ref log);
                                                    }
                                                    else
                                                    {
                                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ELIMINAR Precio especial DEFAULT Alternativo :" + res, ref log);
                                                    }
                                                }


                                            }

                                        }

                                    }
                                }
                            }
                            break;
                        case 25:

                            LogSystem.WriteLogDebug("-----  Actualización de ESTADOS [" + m.ArticuloCodigo + "]. ", ref log);
                            
                            cat = cDalc.GetCatalogMagentoByILSCatalogID(m.CatalogId);

                            magento = new Magento(URL, Token);
                            if (AplicaCatalogo)
                            {

                                LogSystem.WriteLogDebug("------ Habilitación de Canje producto [" + m.ArticuloCodigo + "]. Valor a setear = " + Convert.ToInt32(m.Final).ToString() + ", CatalogID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                M2Product Prod = magento.GetSku(Token, m.ArticuloCodigo);
                                StockCentralToMagento.Entities.ProviderVoucherEntity PVE = pvDalc.GetProviderVoucherByArt(m.CatalogId, m.ArticuloCodigo, -1);
                                int StkQty25 = cDalc.GetStockByBarCode(m.ArticuloCodigo, m.CatalogId);
                                ilsProd = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, m.CatalogId);

                                if (Prod != null)
                                {
                                    if (StkQty25 == 0)
                                        m.Final = 0;

                                    LogSystem.WriteLogDebug(">>>>>>>>>>> Iniciando cambio de Status  -> " + m.Final.ToString() + ", SKU: " + Prod.Sku + ", Catalogo : " + cat.idCatalogoMagento, ref log);

                                    // Cambiamos modalidad de quitado del producto del site x colocarlo con status = 0 - 20240806
                                    // ******************************************************************************************
                                    if(m.Final == 1)
                                        res = magento.SetStatusSku(Token, Convert.ToInt32(m.Final), Prod, cat.idCatalogoMagento);

                                    magento = new Magento("http://" + cat.UrlCatalogo, Token);

                                    res = magento.SetStatusSku(Token, Convert.ToInt32(m.Final), Prod);
                                    // ******************************************************************************************

                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooooo Status actualizado exitósamente  -> " + m.Final.ToString(), ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Status :" + res, ref log);
                                        GetToken(User, Pass);
                                        magento = new Magento(URL, Token);
                                    }

                                    
                                    if (m.Final > 0)
                                    {
                                        MagentoStoreEntity mag = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(cat.idCatalogoMagento, cat.idCatalogoILS);
                                        if (m.CatalogId == 31)
                                        {

                                            //float conv = (float)0.025;
                                            float conv = ilsProd.preciounidad;
                                            int precioL = Convert.ToInt32((ilsProd.preciolista / (Convert.ToSingle((100 + ilsProd.alicuotaIVA)) / 100)) / conv);

                                            if (precioL < Convert.ToInt32(ilsProd.precioreal))
                                                precioL = Convert.ToInt32(ilsProd.precioreal);


                                            LogSystem.WriteLogDebug("------ Cambio de Precio en SKU " + m.ArticuloCodigo + ". Valor a setear precio de LISTA = " + precioL.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                            string res1 = magento.SetPriceSkuStore(Token, mag.idStoreMagento, ilsProd.codigobarra, precioL.ToString());
                                            if (res1 == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res1, ref log);

                                            }

                                            LogSystem.WriteLogDebug("------ Cambio de Precio Especial en SKU " + ilsProd.codigobarra + ". Valor a setear actual del StockCentral = " + Convert.ToInt32(ilsProd.precioreal).ToString(), ref log);

                                            List<Price> ePrice = new List<Price>();
                                            ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(ilsProd.precioreal)), sku = ilsProd.codigobarra, store_id = mag.idStoreMagento });

                                            res = magento.SetEspecialPrice(ePrice, mag.idStoreMagento.ToString(), Token);
                                            if (res == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooo Precio especial actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial :" + res, ref log);
                                                res = magento.SetEspecialPriceAlt(Token, mag.idStoreMagento, m.ArticuloCodigo, Convert.ToInt32(ilsProd.precioreal).ToString(), cat.MagentoWebSite);
                                                if (res == "0")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio especial Alternativo :" + res, ref log);
                                                    List<string> SkuL = new List<string>();
                                                    SkuL.Add(m.ArticuloCodigo);
                                                    List<Price> EspPriceList = magento.getEspecialPrice(SkuL, Token);
                                                    foreach (Price p in EspPriceList)
                                                    {
                                                        LogSystem.WriteLogDebug("----------- Especial Price existente : store=" + p.store_id + ", price=" + p.price + ", from=" + p.price_from, ref log);
                                                        if (p.store_id == mag.idStoreMagento)
                                                        {
                                                            res = magento.EspecialPriceDelete(Token, mag.idStoreMagento, m.ArticuloCodigo, p.price.ToString(), cat.MagentoWebSite, p.price_from, p.price_to);
                                                            if (res == "0")
                                                            {
                                                                LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo DEFAULT eliminado exitósamente", ref log);
                                                            }
                                                            else
                                                            {
                                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ELIMINAR Precio especial DEFAULT Alternativo :" + res, ref log);
                                                            }
                                                        }


                                                    }

                                                }
                                            }
                                            int CargaDescuento = 0;
                                            try
                                            {
                                                CargaDescuento = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ActualizaDescuento"].ToString());
                                            }
                                            catch (Exception f)
                                            {
                                                CargaDescuento = 0;
                                            }

                                            if (CargaDescuento == 1)
                                            {

                                                // Actualizar variable descuento
                                                M2Product PD = new M2Product();
                                                PD.Sku = Prod.Sku;


                                                LogSystem.WriteLogDebug("------ AGREGANDO Descuento a Prod  ", ref log);
                                                CustomAttribute attr_d = new CustomAttribute();

                                                attr_d.AttributeCode = "descuento";
                                                attr_d.Value = Convert.ToInt32(Math.Truncate((Convert.ToSingle(precioL) - ilsProd.precioreal) / Convert.ToSingle(precioL))).ToString();
                                                PD.CustomAttributes = new List<CustomAttribute>();
                                                PD.CustomAttributes.Add(attr_d);
                                                // LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                                LogSystem.WriteLogDebug("[ " + attr_d.AttributeCode + "] \n" + attr_d.Value, ref log);


                                                res = magento.UpdateProductoTyC(Token, PD, cat.idCatalogoMagento.ToString());
                                                if (res == "OK")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooooo Descuento actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Descuento :" + res, ref log);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("------ Cambio de Precio en SKU " + m.ArticuloCodigo + ". Valor a setear = " + ilsProd.precioreal.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                            res = magento.SetPriceSkuStore(Token, mag.idStoreMagento, m.ArticuloCodigo, Convert.ToInt32(ilsProd.precioreal).ToString());
                                            if (res == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                            }

                                            LogSystem.WriteLogDebug("------ Cambio de Precio Especial en SKU " + m.ArticuloCodigo + ". Valor a setear = " + ilsProd.precioreal.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);

                                            List<Price> ePrice = new List<Price>();
                                            ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(ilsProd.precioreal)), sku = m.ArticuloCodigo, store_id = mag.idStoreMagento });

                                            res = magento.SetEspecialPrice(ePrice, mag.idStoreMagento.ToString(), Token);
                                            if (res == "0")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooo Precio Especial actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio ESPECIAL :" + res, ref log);
                                                List<string> SkuL = new List<string>();
                                                SkuL.Add(m.ArticuloCodigo);
                                                List<Price> EspPriceList = magento.getEspecialPrice(SkuL, Token);
                                                foreach (Price p in EspPriceList)
                                                {
                                                    LogSystem.WriteLogDebug("----------- Especial Price existente : store=" + p.store_id + ", price=" + p.price + ", from=" + p.price_from, ref log);
                                                    if (p.store_id == mag.idStoreMagento)
                                                    {
                                                        res = magento.EspecialPriceDelete(Token, mag.idStoreMagento, m.ArticuloCodigo, p.price.ToString(), cat.MagentoWebSite, p.price_from, p.price_to);
                                                        if (res == "0")
                                                        {
                                                            LogSystem.WriteLogDebug("ooooooooooo Precio especial Alternativo DEFAULT eliminado exitósamente", ref log);
                                                        }
                                                        else
                                                        {
                                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ELIMINAR Precio especial DEFAULT Alternativo :" + res, ref log);
                                                        }
                                                    }


                                                }
                                            }
                                        }
                                        LogSystem.WriteLogDebug("------ Cambio de Precio Costo en SKU " + m.ArticuloCodigo + ". Valor a setear = " + Convert.ToInt32(ilsProd.precioventa).ToString(), ref log);
                                        res = magento.SetCostPrice(Token, m.ArticuloCodigo, ilsProd.precioventa.ToString(), mag.idStoreMagento.ToString());
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooo Precio costo actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio costo :" + res, ref log);

                                        }

                                        M2Product P2 = new M2Product();
                                        P2.Sku = m.ArticuloCodigo;


                                        LogSystem.WriteLogDebug("------ AGREGANDO Precio a Prod  ", ref log);
                                        CustomAttribute attr = new CustomAttribute();

                                        attr.AttributeCode = "precio_en_pesos";
                                        attr.Value = Convert.ToDecimal(ilsProd.precioventa).ToString();
                                        P2.CustomAttributes = new List<CustomAttribute>();
                                        P2.CustomAttributes.Add(attr);
                                        LogSystem.WriteLogDebug("[ " + attr.AttributeCode + "] \n" + attr.Value, ref log);


                                        res = magento.UpdateProductoGlobalAttribute(Token, P2, mag.idStoreMagento.ToString());
                                        if (res.Split('|')[0] == "OK")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos :" + res, ref log);
                                            
                                        }


                                    }
                                    if (PVE != null)
                                    {
                                        M2Product P2 = new M2Product();
                                        P2.Sku = Prod.Sku;

                                        bool existTyC = false;
                                        LogSystem.WriteLogDebug("------ AGREGANDO TyC a Prod  ", ref log);
                                        CustomAttribute attr = new CustomAttribute();

                                        attr.AttributeCode = "terminos_y_condiciones";
                                        attr.Value = (PVE.tycVoucher + " " + PVE.tycVirtual + " " + PVE.tycHotel + " " + PVE.meVoucher + " " + PVE.meVirtual + " " + PVE.meHotel).Replace("\n", "\\n").Replace("\r", "\\r");
                                        P2.CustomAttributes = new List<CustomAttribute>();
                                        P2.CustomAttributes.Add(attr);
                                        // LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                        LogSystem.WriteLogDebug("[ " + attr.AttributeCode + "] \n" + attr.Value, ref log);


                                        res = magento.UpdateProductoTyC(Token, P2, cat.idCatalogoMagento.ToString());
                                        if (res == "OK")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooooo TyC actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR TyC :" + res, ref log);
                                            GetToken(User, Pass);
                                            magento = new Magento(URL, Token);
                                        }
                                    }
                                     
                                }
                                else
                                {
                                    // Veamos si tenemos que crear un producto.
                                    MagentoAPI.ApiService ap = new ApiService();
                                    MagentoAPI.MuResults mre = new MagentoAPI.MuResults(false, "");
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx PRODUCTO NO ENCONTRADO xxxxxxxxxxx", ref log);
                                    //mre = ap.Crear_Producto()

                                }


                            }
                            break;

                        case 525:


                            LogSystem.WriteLogDebug("-----  Nuevo PRODUCTO [" + m.ArticuloCodigo + "]. ", ref log);

                            if (AplicaCatalogo)
                            {

                                LogSystem.WriteLogDebug("ooooooo Habilitación de Canje producto [" + m.ArticuloCodigo + "]. Valor a setear = " + Convert.ToInt32(m.Final).ToString() + ", CatalogID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                M2Product Prod = magento.GetSku(Token, m.ArticuloCodigo);
                                cat = cDalc.GetCatalogMagentoByILSCatalogID(m.CatalogId);
                                StockCentralToMagento.Entities.ProviderVoucherEntity PVE = pvDalc.GetProviderVoucherByArt(m.CatalogId, m.ArticuloCodigo, -1);
                                int StkQty25 = cDalc.GetStockByBarCode(m.ArticuloCodigo, m.CatalogId);
                                ilsProd = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, m.CatalogId);
                                MagentoStoreEntity mag = cDalc.GetMagentoStoreIdByCatalogMagentoAndILSCatalogID(cat.idCatalogoMagento, cat.idCatalogoILS);

                                if (Prod != null)
                                {

                                    if (PVE != null)
                                    {
                                        M2Product P2 = new M2Product();
                                        P2.Sku = Prod.Sku;

                                        bool existTyC = false;
                                        LogSystem.WriteLogDebug("===== ::::: AGREGANDO TyC a Prod  ", ref log);
                                        CustomAttribute attr = new CustomAttribute();

                                        attr.AttributeCode = "terminos_y_condiciones";
                                        attr.Value = (PVE.tycVoucher + " " + PVE.tycVirtual + " " + PVE.tycHotel + " " + PVE.meVoucher + " " + PVE.meVirtual + " " + PVE.meHotel).Replace("\n", "\\n").Replace("\r", "\\r");
                                        P2.CustomAttributes = new List<CustomAttribute>();
                                        P2.CustomAttributes.Add(attr);
                                        // LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                        LogSystem.WriteLogDebug("[ " + attr.AttributeCode + "] \n" + attr.Value, ref log);


                                        res = magento.UpdateProductoTyC(Token, P2, cat.idCatalogoMagento.ToString());
                                        if (res == "OK")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooooo TyC actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR TyC :" + res, ref log);
                                        }
                                    }
                                }
                                else
                                {
                                    // Veamos si tenemos que crear un producto.
                                    MagentoAPI.ApiService ap = new ApiService();
                                    MagentoAPI.MuResults mre = new MagentoAPI.MuResults(false, "");
                                    Pe = pDalc.GetByID(ilsProd.providerid);
                                    mre = ap.Crear_Producto(cat.idCatalogoMagento.ToString(), 
                                        ilsProd.codigobarra, 
                                        ilsProd.nombre, 
                                        ilsProd.DeliveryType, 
                                        false, 
                                        DateTime.Now, 
                                        ilsProd.ArtDescription, 
                                        "", 
                                        "10", 
                                        1, 
                                        ilsProd.DeliveryType, 
                                        Convert.ToDecimal(Convert.ToInt32(ilsProd.precioreal)), 
                                        Convert.ToDecimal(ilsProd.precioventa), 
                                        Convert.ToDecimal(ilsProd.precioventa),
                                        ///replace(Lower(replace(A.codigobarra + ' ' + P.Name + ' ' + replace(replace(A.nombre, '"', ''), ';', '-'), ' ', '-')), '"', ''),
                                        (((ilsProd.codigobarra + " " + Pe.Name + " " + ilsProd.nombre).ToLower().Replace("\"", "")).Replace(';', '-')).Replace(' ', '-'),
                                        ((Pe.Name + " - " + ilsProd.nombre).Replace("\"", "")).Replace(';', '-'),
                                        ((Pe.Name + " - " + ilsProd.nombre).Replace("\"", "")).Replace(';', '-'),
                                        ((Pe.Name + " - " + ilsProd.nombre).Replace("\"", "")).Replace(';', '-')
                                        );

                                }
                                if (StkQty25 == 0)
                                    m.Final = 0;
                                res = magento.SetStatusSku(Token, Convert.ToInt32(m.Final), Prod, cat.idCatalogoMagento);
                                if (res == "0")
                                {
                                    LogSystem.WriteLogDebug("ooooooooooooo Status actualizado exitósamente  -> " + m.Final.ToString(), ref log);
                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Status :" + res, ref log);
                                }

                                if (m.Final > 0)
                                {
                                    LogSystem.WriteLogDebug("ooooooo Cambio de Precio en SKU " + m.ArticuloCodigo + ". Valor a setear = " + ilsProd.precioreal.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                    res = magento.SetPriceSkuStore(Token, mag.idStoreMagento, m.ArticuloCodigo, Convert.ToInt32(ilsProd.precioreal).ToString());
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                    }

                                    LogSystem.WriteLogDebug("ooooooo Cambio de Precio Especial en SKU " + m.ArticuloCodigo + ". Valor a setear = " + ilsProd.precioreal.ToString() + ", CatalogoID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);

                                    List<Price> ePrice = new List<Price>();
                                    ePrice.Add(new Price() { price = Convert.ToDecimal(Convert.ToInt32(ilsProd.precioreal)), sku = m.ArticuloCodigo, store_id = mag.idStoreMagento });

                                    res = magento.SetEspecialPrice(ePrice, mag.idStoreMagento.ToString());
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio :" + res, ref log);
                                    }

                                    LogSystem.WriteLogDebug("ooooooo Cambio de Precio Costo en SKU " + m.ArticuloCodigo + ". Valor a setear = " + Convert.ToInt32(ilsProd.precioventa).ToString(), ref log);
                                    res = magento.SetCostPrice(Token, m.ArticuloCodigo, ilsProd.precioventa.ToString(), mag.idStoreMagento.ToString());
                                    if (res == "0")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooo Precio costo actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxx ERROR AL ACTUALIZAR Precio costo :" + res, ref log);

                                    }

                                    M2Product P2 = new M2Product();
                                    P2.Sku = m.ArticuloCodigo;


                                    LogSystem.WriteLogDebug("===== ::::: AGREGANDO Precio a Prod  ", ref log);
                                    CustomAttribute attr = new CustomAttribute();

                                    attr.AttributeCode = "precio_en_pesos";
                                    attr.Value = Convert.ToDecimal(ilsProd.precioventa).ToString();
                                    P2.CustomAttributes = new List<CustomAttribute>();
                                    P2.CustomAttributes.Add(attr);
                                    LogSystem.WriteLogDebug("[ " + attr.AttributeCode + "] \n" + attr.Value, ref log);


                                    res = magento.UpdateProductoGlobalAttribute(Token, P2, mag.idStoreMagento.ToString());
                                    if (res.Split('|')[0] == "OK")
                                    {
                                        LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos :" + res, ref log);
                                        //res = magento.UpdateProductoAttributeStoreViewPut(Token, P2, /* cat.idCatalogoMagento.ToString() */ "all");
                                    }

                                }

                            }
                            break;
                        case 43:

                            LogSystem.WriteLogDebug("------  Actualización de Precios PESOS VENTA [" + m.ArticuloCodigo + "]. ", ref log);
                            
                            cat = cDalc.GetCatalogMagentoByILSCatalogID(m.CatalogId);
                            LogSystem.WriteLogDebug("------  Catalogo  [" + cat.UrlCatalogo + "]. ", ref log);

                            magento = new Magento("http://" + cat.UrlCatalogo, Token);

                            if (AplicaCatalogo)
                            {
                                LogSystem.WriteLogDebug("------ Cambio de precio en pesos [" + m.ArticuloCodigo + "]. Valor a setear = " + Convert.ToInt32(m.Final).ToString() + ", CatalogID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                try
                                {
                                    M2Product ProdPrice = magento.GetSku(Token, m.ArticuloCodigo);

                                    if (ProdPrice != null)
                                    {
                                        M2Product P2 = new M2Product();
                                        P2.Sku = ProdPrice.Sku;


                                        LogSystem.WriteLogDebug("------ AGREGANDO Precio a Prod  ", ref log);
                                        CustomAttribute attr = new CustomAttribute();

                                        attr.AttributeCode = "precio_en_pesos";
                                        attr.Value = Convert.ToDecimal(m.Final).ToString().Replace(",", ".");
                                        P2.CustomAttributes = new List<CustomAttribute>();
                                        P2.CustomAttributes.Add(attr);
                                        LogSystem.WriteLogDebug("[ " + attr.AttributeCode + "] --->  " + attr.Value.ToString().Replace(",", "."), ref log);

                                        if (m.CatalogId == 31)
                                        {
                                            //res = magento.UpdateProductoAttributeStoreViewNumber(Token, P2, cat.idCatalogoMagento.ToString());
                                            res = magento.UpdateProductoGlobalAttribute(Token, P2, cat.idCatalogoMagento.ToString());
                                            if (res.Split('|')[0] == "OK")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx Intento por STORE VIEW FALLIDO, Intento Global :" + res, ref log);
                                                
                                            }
                                        }
                                        else
                                        {
                                            res = magento.UpdateProductoAttributeStoreViewNumber(Token, P2, cat.idCatalogoMagento.ToString());
                                            if (res == "OK")
                                            {
                                                LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos , intentando de nuevo :" + res, ref log);
                                                res = magento.UpdateProductoAttributeStoreViewPut(Token, P2, cat.idCatalogoMagento.ToString());
                                                if (res == "OK")
                                                {
                                                    LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos , intentando de nuevo :" + res, ref log);
                                                    res = magento.UpdateProductoGlobalAttributePOST_Store(Token, P2, cat.idCatalogoMagento.ToString());
                                                    if (res == "OK")
                                                    {
                                                        LogSystem.WriteLogDebug("ooooooooooooo Precio en Pesos actualizado exitósamente", ref log);
                                                    }
                                                    else
                                                    {
                                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio en Pesos , intentando de nuevo :" + res, ref log);
                                                        res = magento.UpdateProductoGlobalAttribute(Token, P2, cat.idCatalogoMagento.ToString());
                                                        LogSystem.WriteLogDebug("xxxxxxxxxxxxx Respuesta UpdateProductoGlobalAttribute AL ACTUALIZAR Precio en Pesos :" + res, ref log);

                                                    }
                                                }
                                            }

                                        }

                                        res = magento.SetCostPrice(Token, P2.Sku, Convert.ToDecimal(m.Final).ToString().Replace(",", "."), cat.idCatalogoMagento.ToString());
                                        if (res == "0")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooooo Precio de Costo actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Precio de Costo :" + res, ref log);
                                        }
                                    }
                                    else
                                    {
                                        LogSystem.WriteLogDebug(" xxxxxxxxxxxxxxxxx    No encuentra el producto en el catalogo activo. xxxxxxxxxxxxxxxxx", ref log);
                                    }
                                }catch(Exception NoEsta)
                                {
                                    LogSystem.WriteLogDebug(" xxxxxxxxxxxxxxxxx    No encuentra el producto en el catalogo activo. xxxxxxxxxxxxxxxxx", ref log);
                                }
                            }
                            break;

                        case 101:
                            LogSystem.WriteLogDebug("------  Actualización de PESO PROD [" + m.ArticuloCodigo + "]. ", ref log);

                            if (AplicaCatalogo)
                            {
                                int PermiteActualizarPeso = 0;
                                try
                                {
                                    PermiteActualizarPeso = Convert.ToInt32((System.Configuration.ConfigurationSettings.AppSettings["PermiteActualizarPesoProductos"]).ToString());
                                }
                                catch (Exception d)
                                {
                                    PermiteActualizarPeso = 0;
                                    LogSystem.WriteLogDebug("Parametro de Actualización de PESO -> Desactivado", ref log); ;
                                }

                                if (PermiteActualizarPeso != 0)
                                {

                                    LogSystem.WriteLogDebug("------ Cambio de PESO [" + m.ArticuloCodigo + "]. Valor a setear = " + Convert.ToInt32(m.Final).ToString() + ", CatalogID = " + m.CatalogId.ToString() + ", Mov = " + m.MovimientoID.ToString(), ref log);
                                    M2Product_PayLoad Prod = magento.GetSku_PP(Token, m.ArticuloCodigo);
                                    cat = cDalc.GetCatalogMagentoByILSCatalogID(m.CatalogId);

                                    if (Prod != null)
                                    {
                                        M2Product_PayLoad P2 = Prod; //new M2Product_PayLoad();
                                                                     //P2.Sku = Prod.Sku;
                                        P2.Weigth = Convert.ToInt32(m.Final);
                                        LogSystem.WriteLogDebug("------ Actualizando PESO a Prod  ", ref log);
                                        string ProdUpt = "{ \"sku\":\"" + Prod.Sku + "\",\"name\":\"" + Prod.Name + "\", \"attribute_set_id\":" + Prod.AttributeSetId + ",\"weight\":" + P2.Weigth + "}";
                                        res = magento.PutProductW(Token, ProdUpt, Prod.Sku, /* cat.idCatalogoMagento.ToString() */ "all");
                                        if (res == "OK")
                                        {
                                            LogSystem.WriteLogDebug("ooooooooooooo weight actualizado exitósamente", ref log);
                                        }
                                        else
                                        {
                                            res = magento.PutProductPL(Token, P2, cat.idCatalogoMagento.ToString());
                                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR weight :" + res, ref log);
                                        }


                                    }

                                }
                            }
                            break;
                         default:
                            LogSystem.WriteLogDebug("-----  NO APLICA EVALUACIÓN [" + m.ArticuloCodigo + "]. ", ref log);
                            break;
                    }
                    LogSystem.WriteLogDebug("-----  Saliendo evaluación [" + m.ArticuloCodigo + "]. ", ref log);

                }
                catch (Exception mm)
                {
                    LogSystem.WriteLogDebug("xxxxxxxxx Error en la interpretación del movimiento que afecta al PRODUCTO. Err : " + mm.Message + ", Mov = " + m.MovimientoID.ToString(), ref log);
                }
                LastMov = m.MovimientoID;

            }

            //ET.Comun.Business.Parametros param = new ET.Comun.Business.Parametros();
            //param.SetValor("StockToMagentoLastMovProcessed", LastMov.ToString());
            return LastMov;

        }
        public static decimal processEventsStockProductsToCollinson(int ID, ref string log)
        {
            StockCentralToMagento.DataAccess.MovementDALC Mdalc = new MovementDALC();
            StockCentralToMagento.Entities.MovementEntity[] movs = Mdalc.GetMovementsFromID(ID);
            var magento = new Magento(URL);
            decimal LastMov = Convert.ToDecimal(ID);
            MagentoCatalogEntity cat = new StockCentralToMagento.Entities.MagentoCatalogEntity();
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();

            int ControlReservaStock = 3;
            try
            {
                ControlReservaStock = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ControlReservaStock"]);
            }
            catch (Exception d)
            {
                ControlReservaStock = 3;
                LogSystem.WriteLogDebug("Parametro de Control Reserva Stock -> No configurado  - Se asume el default = 3", ref log); ;
            }
            LogSystem.WriteLogDebug("Parametro  de Control Reserva Stock -> " + ControlReservaStock.ToString(), ref log); 

            int CollinsonCatalog = 1;
            try
            {
                CollinsonCatalog = Convert.ToInt32(ConfigurationSettings.AppSettings["CollinsonCatalogInILS"]);
            }
            catch(Exception T)
            { }

            foreach (MovementEntity m in movs)
            {
                try
                {
                    LogSystem.WriteLogDebug("ooov201201oooo Nuevo Evento para [" + m.ArticuloCodigo + "]. Catalogo = " + m.CatalogId + " , Mov = " + m.MovimientoTipoID, ref log);
                     
                    int StkQty = 0;
                    string res = "";
                    switch (m.MovimientoTipoID)
                    {
                        case 2:
                        case 8:
                        case 11:
                        case 5:
                            CatalogStockEntity p = cDalc.GetProductStockByBarCode(m.ArticuloCodigo, CollinsonCatalog);
                            if (p.enable)
                            {
                                StkQty = cDalc.GetStockByBarCodeCollinson(m.ArticuloCodigo, m.CatalogId);
                                LogSystem.WriteLogDebug("ooooooo Actualización de Stock [" + m.ArticuloCodigo + "]. Valor a setear = " + ((StkQty - ControlReservaStock) > 0 ? (StkQty - ControlReservaStock) : 0).ToString(), ref log);
                                res = magento.SetStockItemsSkuCollinson(accesskey, secretkey, ((StkQty - ControlReservaStock) > 0 ? (StkQty - ControlReservaStock) : 0), m.ArticuloCodigo);
                                string[] res2 = res.Split('|');
                                if (res2[0] == "0")
                                {
                                    LogSystem.WriteLogDebug("ooooooooooooo Respuesta Metodo Stock: " + res2[1], ref log);

                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res2[0], ref log);
                                }

                            }
                            break;
                        case 25:
                            LogSystem.WriteLogDebug("ooooooo Habilitación de Canje producto [" + m.ArticuloCodigo + "]. Valor a setear = " + Convert.ToInt32(m.Final).ToString() + ", CatalogID = " + m.CatalogId.ToString(), ref log);

                            if (m.CatalogId == CollinsonCatalog)
                            {
                                if (m.Final > 0)
                                {
                                    StkQty = cDalc.GetStockByBarCodeCollinson(m.ArticuloCodigo, m.CatalogId);
                                }

                                LogSystem.WriteLogDebug("ooooooo Actualización de Stock [" + m.ArticuloCodigo + "]. Valor a setear = " + ((StkQty - ControlReservaStock) > 0 ? (StkQty - ControlReservaStock) : 0).ToString(), ref log);
                                res = magento.SetStockItemsSkuCollinson(accesskey, secretkey, ((StkQty - ControlReservaStock) > 0 ? (StkQty - ControlReservaStock) : 0), m.ArticuloCodigo);
                                string[] res22 = res.Split('|');
                                if (res22[0] == "0")
                                {
                                    LogSystem.WriteLogDebug("ooooooooooooo Respuesta Metodo Stock: " + res22[1], ref log);

                                }
                                else
                                {
                                    LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res22[0], ref log);
                                }
                            }
                            break;

                    }
                }
                catch (Exception mm)
                {
                    LogSystem.WriteLogDebug("xxxxxxxxx PRODUCTO NO CARGADO EN MAGENTO o error. Err : " + mm.Message + " | " + mm.Source , ref log);
                }
                LastMov = m.MovimientoID;

            }

            //ET.Comun.Business.Parametros param = new ET.Comun.Business.Parametros();
            //param.SetValor("StockToMagentoLastMovProcessed", LastMov.ToString());
            return LastMov;

        }

        public static void InicializarCatalogo_ProductosCollinson(int CatalogoID, int StoreID_Magento, ref string log)
        {
            int i = 0;
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            List<CatalogStockEntity> sprod = cDalc.GetAllCatalogStock(CatalogoID, 0);
            var magento = new Magento(URL);
            int GenerarImagenesTodoelCatalogo = 0;
            try
            {
                GenerarImagenesTodoelCatalogo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["GenerarImagenesTodoelCatalogo"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de imagenes de todo el catalogo -> Desactivado", ref log); ;
            }
            List<CatalogStockEntity> sprod_w_errors = new List<CatalogStockEntity>();
            List<CatalogStockEntity> sprod_enabled = new List<CatalogStockEntity>();

            int SoloGenerarArchivoGlobal = 0;
            try
            {
                SoloGenerarArchivoGlobal = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["SoloGenerarArchivoGlobal"]);
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de generación de Solo Archivo Global -> Desactivado", ref log); ;
            }

            int ControlReservaStock = 3;
            try
            {
                ControlReservaStock = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ControlReservaStock"]);
                LogSystem.WriteLogDebug("Parametro  de Control Reserva Stock -> " + ControlReservaStock, ref log); ;
            }
            catch (Exception d)
            {
                LogSystem.WriteLogDebug("Parametro de Control Reserva Stock -> No configurado - Se asume el default = 3", ref log); ;
            }
            string ListaProductos = "";
            string[] ListaCSV;
            MagentoCatalogEntity cat = cDalc.GetCatalogMagentoByILSCatalogID(CatalogoID);
            int StkQty;
            if (SoloGenerarArchivoGlobal == 0)
            {

                foreach (CatalogStockEntity c in sprod)
                {
                    i++;

                    try
                    {

                        LogSystem.WriteLogDebug("PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "]", ref log);

                        StkQty = cDalc.GetStockByBarCodeCollinson(c.codigobarra, CatalogoID);

                        LogSystem.WriteLogDebug("ooooooo Actualización de Stock del producto Nro " + i.ToString() + " [" + c.codigobarra + "]. Valor Actual = " + Convert.ToInt32(((StkQty - ControlReservaStock) > 0 ? (StkQty - ControlReservaStock) : 0)).ToString(), ref log);
                        string res = magento.SetStockItemsSkuCollinson(accesskey, secretkey, ((StkQty - ControlReservaStock) > 0 ? (StkQty - ControlReservaStock) : 0), c.codigobarra);
                        string[] res2 = res.Split('|');
                        if (res2[0] == "0")
                        {
                            int ind = -1;
                            ind = res2[1].IndexOf("were successfully updated");

                            LogSystem.WriteLogDebug("ooooooooooooo Respuesta Metodo Stock: " + res2[1], ref log);

                            if (ind < 0)
                            {
                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res, ref log);
                                if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                    sprod_w_errors.Add(c);
                            }
                            else
                            {
                                if (!sprod_enabled.Exists(x => x.codigobarra == c.codigobarra))
                                    sprod_enabled.Add(c);

                            }
                        }
                        else
                        {
                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res, ref log);
                            if (!sprod_w_errors.Exists(x => x.codigobarra == c.codigobarra))
                                sprod_w_errors.Add(c);
                        }
                    }
                    catch(Exception ff)
                    {
                        LogSystem.WriteLogDebug("ERROR  PRODUCTO Nro " + i.ToString() + " [" + c.codigobarra + "] ---- " + ff.Message + ff.StackTrace  , ref log);
                    }

                }

                // Desactivar productos que no pudieron ser informados.
                List<CatalogStockEntity> sprod_disable = cDalc.GetAllCatalogStockDisabled(CatalogoID, 0);
                LogSystem.WriteLogDebug("ANALIZAR PRODUCTOS DEL CATALOGO DESACTIVADOS", ref log);
                i = 0;

                if (CatalogoID == 1)
                {
                    foreach (CatalogStockEntity c in sprod_disable)
                    {
                        i++;
                        LogSystem.WriteLogDebug("ooooooo Quitando producto [" + c.codigobarra + "] del Catálogo " + CatalogoID.ToString(), ref log);
                        try
                        {
                            string res = magento.SetStockItemsSkuCollinson(accesskey, secretkey, 0, c.codigobarra);
                            string[] res22 = res.Split('|');
                            if (res22[0] == "0")
                            {
                                LogSystem.WriteLogDebug("ooooooooooooo Respuesta Metodo Stock: " + res22[1], ref log);

                            }
                            else
                            {
                                LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR Stock :" + res22[0], ref log);
                            }
                        }
                        catch (Exception dis)
                        {
                            LogSystem.WriteLogDebug("xxxxxxxxxxxxx ERROR AL ACTUALIZAR :" + dis.Message, ref log);
                        }

                    }
                }

               
                i = 0;
                foreach (CatalogStockEntity c in sprod_w_errors)
                {
                    LogSystem.WriteLogDebug("== " + i++.ToString() + " === Producto con error en MAGENTO --> " + c.codigobarra, ref log);
                    ListaProductos += c.codigobarra + ",";
                    /*
                    MG2Connector.M2Product p = new M2Product();
                    p.Name = c.nombre;
                    p.Sku = c.codigobarra;
                    p.Price = Convert.ToInt32(c.precioreal);
                    p.Status = 0;
                    p.TypeId = "simple";
                    p.Visibility = 0;
                    */
                }

                if (GenerarImagenesTodoelCatalogo > 0)
                    ProcessExportImages(sprod, CatalogoID, ref log);
                else
                    ProcessExportImages(sprod_w_errors, CatalogoID, ref log);

                ListaCSV = cDalc.GetDataToImportListProductsToPIN_COLLINSON(CatalogoID, ListaProductos);
                ProcessExportToExcel("ImportStockCentralToMagentoCollinsonProducts_Cat_" + CatalogoID.ToString() + "_NewProducts", ".xls", ListaCSV, ref log);


                //-----------------------------------------
                // Reseteo Lista de productos para poder armar archivo de productos actuales e informar los precios actuales.
                ListaProductos = "";
                i = 0;
                foreach (CatalogStockEntity c in sprod_enabled)
                {
                    LogSystem.WriteLogDebug("== " + i++.ToString() + " === Producto habilitado en MAGENTO --> " + c.codigobarra, ref log);
                    ListaProductos += c.codigobarra + ",";

                }
                // Lista para actualziación deprecios de productos actuales.
                ListaCSV = cDalc.GetDataToImportListProductsToPIN_COLLINSON(CatalogoID, ListaProductos);
                ProcessExportToExcel("ImportStockCentralToMagentoCollinsonProducts_Cat_" + CatalogoID.ToString() + "_Actualizacion_productos_habilitados", ".xls", ListaCSV, ref log);



                //-----------------------------------------
                // Generar lista de productos que cambiaron de precio .
                List<CatalogStockEntity> sprod_price_updated = cDalc.GetAllCatalogPriceUpdated(CatalogoID, 0);
                LogSystem.WriteLogDebug("Inicio Análisis de productos que cambiaron de precio.", ref log);
                // Reseteo Lista de productos para poder armar archivo de productos actuales e informar los precios actuales.
                ListaProductos = "";
                i = 0;
                foreach (CatalogStockEntity c in sprod_price_updated)
                {
                    LogSystem.WriteLogDebug("== " + i++.ToString() + " === Producto a informar Cambio de precio --> " + c.codigobarra, ref log);
                    ListaProductos += c.codigobarra + ",";

                }
                // Lista para actualziación deprecios de productos actuales.
                ListaCSV = cDalc.GetDataToImportListProductsPriceUpdatedToPIN_COLLINSON(CatalogoID, ListaProductos);
                ProcessExportToExcel("ImportStockCentralToMagentoCollinsonProducts_Cat_" + CatalogoID.ToString() + "_Actualizacion_Precios_Productos", ".xls", ListaCSV, ref log);

            }

            //--------------------------------------------
            // RESETEO PARA GENERAR UNA IMPORTACION GLOBAL
            ListaProductos = "-1";
            LogSystem.WriteLogDebug("===== Creando Listado completo de Productos del Catalogo " + cat.MagentoWebSite + "  a incluir en MAGENTO --> STARTUP PROGRAMA o RESET LISTADO DE PRODUCTOS", ref log);

            ListaCSV = cDalc.GetDataToImportListProductsToPIN_COLLINSON(CatalogoID, ListaProductos);
            ProcessExportToExcel("ImportStockCentralToMagentoCollinsonProducts_Cat_" + CatalogoID.ToString() + "_Global", ".xls", ListaCSV, ref log);

            LogSystem.WriteLogDebug("===== Creando Listado completo de Productos del Catalogo " + cat.MagentoWebSite + " en PROMOCION a incluir en MAGENTO --> STARTUP PROGRAMA o RESET LISTADO DE PRODUCTOS", ref log);

            ListaCSV = cDalc.GetDataToImportListProductsToPIN_COLLINSON_PROMOTIONS(CatalogoID, ListaProductos);
            ProcessExportToExcel("ImportStockCentralToMagentoCollinsonProducts_Cat_" + CatalogoID.ToString() + "_PROMOTIONS", ".xls", ListaCSV, ref log);
        }


        public static void GetToken(string userName, string passWord)
        {
            var m2 = new Magento(URL);
            Token = m2.GetAdminToken(userName, passWord);
            User = userName;
            Pass = passWord;


        }

        public static void SetAWS(string access_key, string secret_key)
        {
            accesskey = access_key;
            secretkey = secret_key;
        }
        static string SetStatusSku(M2Product skuName, int Status, string token)
        {
            var magento = new Magento(URL);
            return magento.SetStatusSku(token, Status, skuName);
        }


        public static void ViewCategoryChildrens(CategoryList cat, string arbol)
        {
            for (int i = 0; i < cat.ChildrenData.Count; i++)
            {
                CategoryList c = cat.ChildrenData[i];
                string name = c.Name;
                int catID = c.Id;
                ViewCategoryChildrens(c, arbol + "/" + name);
            }

        }
        static void ViewCategoryChildrens_old(CategoryList cat, string arbol, string StoreView, ref string log)
        {
            string resp = "";
            ProviderVoucherDALC pvDalc = new ProviderVoucherDALC();

            for (int i = 0; i < cat.ChildrenData.Count; i++)
            {
                CategoryList c = cat.ChildrenData[i];
                string name = c.Name;
                int catID = c.Id;
                bool ActualizarCat = false;
                Category Cat = GetCategoryByIDByStore(c.Id.ToString(), Token, StoreView);
                if(c.ChildrenData.Count == 0)
                    if ( c.ProductCount == "0" && Cat.IncludeInMenu)
                    {
                        LogSystem.WriteLogDebug("===== ::::: Desactivando categoria al StoreView : " + arbol + "/" + name , ref log);
                        Cat.IncludeInMenu = false;
                        ActualizarCat = true;
                    }
                    else
                    {
                        if (c.ProductCount != "0")
                        {
                            StockCentralToMagento.Entities.ProviderVoucherEntity PVE = pvDalc.GetProviderVoucherBySubCategoryName(name, 0);
                            if (!Cat.IncludeInMenu)
                            {
                                LogSystem.WriteLogDebug("===== ::::: Activando categoria al StoreView : " + arbol + "/" + name, ref log);
                                Cat.IncludeInMenu = true;
                                ActualizarCat = true;        
                            }
                    
                            if (PVE != null)
                            {
                                bool existTyC = false;
                                for (int t = 0; t < Cat.CustomAttributes.Count; t++)
                                {
                                    if (Cat.CustomAttributes[t].AttributeCode == "terms_and_conditions")
                                    {
                                        existTyC = true;
                                        Cat.CustomAttributes[t].Value = PVE.tycVoucher + " " + PVE.tycVirtual + " " + PVE.tycHotel + " " + PVE.meVoucher + " " + PVE.meVirtual + " " + PVE.meHotel;
                                        LogSystem.WriteLogDebug("===== ::::: Modificando TyC a categoria : " + arbol + "/" + name, ref log);
                                        LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + Cat.CustomAttributes[t].Value, ref log);
                                        
                                        ActualizarCat = true;
                                        break;
                                    }
                                }
                                if (!existTyC)
                                {
                                    LogSystem.WriteLogDebug("===== ::::: Agregando TyC a categoria : " + arbol + "/" + name, ref log);
                                    CustomAttribute attr = new CustomAttribute();
                                    attr.AttributeCode = "terms_and_conditions";
                                    attr.Value = PVE.tycVoucher + " " + PVE.tycVirtual + " " + PVE.tycHotel + " " + PVE.meVoucher + " " + PVE.meVirtual + " " + PVE.meHotel;
                                    Cat.CustomAttributes.Add(attr);
                                    LogSystem.WriteLogDebug("===== ::::: :::::::  TyC  : " + attr.Value, ref log);
                                    ActualizarCat = true;
                                }
                            }
                        }
                    }
                if (ActualizarCat)
                {
                    LogSystem.WriteLogDebug("===== ::::: Comenzando actualización Categoria : " + Cat.Id.ToString()  + ", Nombre = "  + Cat.Name + ", Path: " + Cat.Path , ref log);
                    resp = ModifyCategoryByStoreView(Cat, Token, StoreView);
                    LogSystem.WriteLogDebug("===== ::::: FIN Actualización Categoria : " + arbol + "/" + name +  " [" + resp + "]", ref log);
                    
                }    
                ViewCategoryChildrens(c, arbol + "/" + name, StoreView, ref log);
            }

        }

        static void ViewCategoryChildrens(CategoryList cat, string arbol, string StoreView, ref string log)
        {
            string resp = "";
  
            for (int i = 0; i < cat.ChildrenData.Count; i++)
            {
                CategoryList c = cat.ChildrenData[i];
                string name = c.Name;
                int catID = c.Id;
                bool ActualizarCat = false;
                Category Cat = GetCategoryByIDByStore(c.Id.ToString(), Token, StoreView);
                if (c.ChildrenData.Count == 0)
                    if (c.ProductCount == "0" && Cat.IncludeInMenu)
                    {
                        LogSystem.WriteLogDebug("===== ::::: Desactivando categoria al StoreView : " + arbol + "/" + name, ref log);
                        Cat.IncludeInMenu = false;
                        ActualizarCat = true;
                    }
                    else
                    {
                        if (c.ProductCount != "0")
                        {
                            if (!Cat.IncludeInMenu)
                            {
                                LogSystem.WriteLogDebug("===== ::::: Activando categoria al StoreView : " + arbol + "/" + name, ref log);
                                Cat.IncludeInMenu = true;
                                ActualizarCat = true;
                            }
                        }
                    }
                if (ActualizarCat)
                {
                    LogSystem.WriteLogDebug("===== ::::: Comenzando actualización Categoria : " + Cat.Id.ToString() + ", Nombre = " + Cat.Name + ", Path: " + Cat.Path, ref log);
                    resp = ModifyCategoryByStoreView(Cat, Token, StoreView);
                    LogSystem.WriteLogDebug("===== ::::: FIN Actualización Categoria : " + arbol + "/" + name + " [" + resp + "]", ref log);

                }
                ViewCategoryChildrens(c, arbol + "/" + name, StoreView, ref log);
            }

        }

        public static void GetSku(string skuName, string token)
        {
            var magento = new Magento(URL);
            magento.GetSku(token, skuName);
        }

        public static ProductPriceSku[] GetPricesSku(string skuName, string token)
        {
            var magento = new Magento(URL);
            return magento.GetPriceSku(token, skuName);
        }

        static string GetStockSku(string skuName, string token)
        {
            var magento = new Magento(URL);
            return magento.GetStockSku(token, skuName);
        }

        static string SetStockItemsSku(string skuName, int Qty, string token)
        {
            var magento = new Magento(URL);
            return magento.SetStockItemsSku(token, Qty, skuName);
        }

        static string SetPriceSkuStore(string skuName, int StoreId, string Price, string token)
        {
            var magento = new Magento(URL);
            return magento.SetPriceSkuStore(token, StoreId, skuName, Price);
        }

        static string GetModules(string token)
        {
            var magento = new Magento(URL);
            return magento.GetModules(token);
        }

        static void GetCategory(string Cat, string token)
        {
            var magento = new Magento(URL);
            magento.GetCategory(token, Cat);
        }

        static CategoryList GetCategories(Filter filter, string token)
        {
            var magento = new Magento(URL);
            return magento.GetCategories(token, filter);
        }
        static Category GetCategoryByIDByStore(string Cat, string token, string StoreView)
        {
            var magento = new Magento(URL);
            return magento.GetCategoryByIDByStore(token, Cat, StoreView);
        }
        static CategoryList GetCategoriesByStoreView(Filter filter, string token, string StoreView)
        {
            var magento = new Magento(URL);
            return magento.GetCategoriesByStoreView(token, filter, StoreView);
        }

        static void CreateCategory(ProductCategory name, string StoreView)
        {
            var magento = new Magento("http://34.211.82.219", Token);
            magento.CreateCategory(name, StoreView);
        }
        static void ModifyCategory(Category cat, string token)
        {
            var magento = new Magento(URL);
            magento.ModifyCategory(token, cat);
        }

        static string ModifyCategoryByStoreView(Category cat, string token, string StoreView)
        {
            var magento = new Magento(URL);
            return magento.ModifyCategoryByStoreView(token, cat, StoreView);
        }
        static void CreateProduct(ProductCategory name, string StoreView)
        {
            var magento = new Magento("http://34.211.82.219", Token);
            magento.CreateCategory(name, StoreView);
        }

        public static void ProcessExportImages(List<CatalogStockEntity> sprod_w_errors, int catalogo, ref string log)
        {
            string PathIm = System.Configuration.ConfigurationSettings.AppSettings["PathImagenes"]; ;
            CatalogDALC cDalc = new CatalogDALC();

            bool exists = System.IO.Directory.Exists(PathIm);

            if (!exists)
                System.IO.Directory.CreateDirectory(PathIm);

            // Display the file contents by using a foreach loop.
            //System.Console.WriteLine("Contents of WriteLines2.txt = ");
            foreach (CatalogStockEntity lineS in sprod_w_errors)
            {
                try
                {
                    // Use a tab to indent each line of the file.
                    //ProductInfoEntity productInfo = ProductInfoBusiness.GetProductInfo(lineS.codigobarra, catalogo);
                    string Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+","-").Replace(" ","").Replace(",", "") + "_b.jpg";

                    //ImageEntity img = cDalc.GetLargeImageByBarCode(lineS.codigobarra);
                    List<ImageEntity> imgL = cDalc.GetLargeImageByBarCodeList(lineS.codigobarra);
                    int ind = 0;
                    LogSystem.WriteLogDebug("Extrayendo Imagenes de codigo: " + lineS.codigobarra, ref log);
                    foreach (ImageEntity img in imgL)
                    {
                        
                        ind++;
                        if (ind == 1)
                        {
                            Bitmap i = new Bitmap(new MemoryStream(img.Image));
                            if (System.IO.File.Exists(Path))
                            {
                                LogSystem.WriteLogDebug("Imagen ya existe para exportar : " + Path, ref log);
                                continue;
                            }

                            if (i.Width > 1100 || i.Height > 800)
                                Save(i, 1920, 1200, 100, Path);
                            else if (i.Width > 500 || i.Height > 500)
                                Save(i, 1100, 1100, 100, Path);
                            else
                                Save(i, 500, 500, 100, Path);
                            Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_t.jpg";
                            Save(i, 50, 50, 100, Path);
                            Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_s.jpg";
                            Save(i, 470, 470, 100, Path);
                        }
                        else
                        {
                            Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_b_" + ind + ".jpg";
                            if (System.IO.File.Exists(Path))
                            {
                                LogSystem.WriteLogDebug("Imagen ya existe para exportar : " + Path, ref log);
                                continue;
                            }
                            Bitmap i = new Bitmap(new MemoryStream(img.Image));
                            if (i.Width > 1100 || i.Height > 800)
                                Save(i, 1920, 1200, 100, Path);
                            else if (i.Width > 500 || i.Height > 500)
                                Save(i, 1100, 1100, 100, Path);
                            else
                                Save(i, 500, 500, 100, Path);
                        }


                    }
                    if(ind==0)
                        LogSystem.WriteLogDebug("No encontré imagen del producto: " + lineS.codigobarra + " - " + lineS.nombre, ref log);
                }
                catch(Exception p)
                {
                    LogSystem.WriteLogDebug("Error en imagenes del producto: " + lineS.codigobarra + " - " + lineS.nombre + " - Error: " + p.Message , ref log);
                }
            }

            LogSystem.WriteLogClose(" ------  Fin exportación imágenes", log);
        }

        public static void ProcessExportImagesFTP(List<CatalogStockEntity> sprod_w_errors, int catalogo, ref string log)
        {

            int ImageTimeRevision = -1;

            try
            {
                ImageTimeRevision =  Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ImageTimeRevision"]) * -1;
            }
            catch(Exception errImg)
            {
                LogSystem.WriteLogDebug("No encuentro parametro de seteo de dias de revisión para imagenes. Default = 1 dias" , ref log);
            }
            string PathIm = System.Configuration.ConfigurationSettings.AppSettings["PathImagenes"]; ;
            string PathImFTP = System.Configuration.ConfigurationSettings.AppSettings["PathImagenes"] + "FTP" ;
            CatalogDALC cDalc = new CatalogDALC();

            bool exists = System.IO.Directory.Exists(PathIm);

            if (!exists)
                System.IO.Directory.CreateDirectory(PathIm);

            exists = System.IO.Directory.Exists(PathImFTP);

            if (!exists)
                System.IO.Directory.CreateDirectory(PathImFTP);

            int ctrl = 0;
            // Display the file contents by using a foreach loop.
            //System.Console.WriteLine("Contents of WriteLines2.txt = ");

            //DateTime Control_Mov_imagen_forzar_grabación_en_dias = DateTime.Now.AddDays(ImageTimeRevision);
            DateTime Control_Mov_imagen_forzar_grabación_en_dias = ControlFechaUltProceso;

            foreach (CatalogStockEntity lineS in sprod_w_errors)
            {
                ctrl++;
                LogSystem.WriteLogDebug(">>>>>>>>>>> Imagen : " + ctrl, ref log);

                try
                {
                    // Use a tab to indent each line of the file.
                    //ProductInfoEntity productInfo = ProductInfoBusiness.GetProductInfo(lineS.codigobarra, catalogo);
                    string Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_b.jpg";
                    string PathFTP = PathImFTP + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_b.jpg";

                    //ImageEntity img = cDalc.GetLargeImageByBarCode(lineS.codigobarra);
                    List<ImageEntity> imgL = cDalc.GetLargeImageByBarCodeList(lineS.codigobarra);
                    int ind = 0;
                    MovementDALC mdalc = new MovementDALC();
                    DateTime f;
                    DateTime f1;
                    DateTime f2;


                    LogSystem.WriteLogDebug("Extrayendo Imagenes de codigo: " + lineS.codigobarra, ref log);
                    foreach (ImageEntity img in imgL)
                    {

                        ind++;
                        if (ind == 1)
                        {
                            Bitmap i = new Bitmap(new MemoryStream(img.Image));
                            if (System.IO.File.Exists(Path))
                            {
                                f = System.IO.File.GetCreationTime(Path);
                                LogSystem.WriteLogDebug("---------------------- Fecha de creación imagen : " + f.ToString("yyyy-MM-dd HH:mm:ss"), ref log);
                                f2 = f;
                                f1 = f;
                                try
                                {
                                    f1 = System.IO.File.GetLastWriteTime (Path);
                                }
                                catch {
                                    f1 = f;
                                }

                                LogSystem.WriteLogDebug("---------------------- Fecha de modificación imagen : " + f1.ToString("yyyy-MM-dd HH:mm:ss"), ref log);

                                if (DateTime.Compare(f1, f) > 0)
                                    f = f1;

                                LogSystem.WriteLogDebug("---------------------- Fecha de control imagen : " + f.ToString("yyyy-MM-dd HH:mm:ss"), ref log);


                                MovementEntity mm = mdalc.GetLastByTypeIDAndBarCode(117, lineS.codigobarra);
                                if (mm == null)
                                {
                                   // LogSystem.WriteLogDebug("Sin mov 117 - " + Path, ref log);

                                    mm = mdalc.GetLastByTypeIDAndBarCode(116, lineS.codigobarra);
                                    if (mm == null)
                                    {
                                        //LogSystem.WriteLogDebug("Imagen ya existe para exportar y no hay movimiento 116 : " + Path, ref log);
                                        if (DateTime.Compare(f2, DateTime.Now.AddMonths(-12)) < 0)
                                        {
                                            LogSystem.WriteLogDebug("Imagen ya existe para exportar y esta guardada desde hace más de un año : " + Path, ref log);
                                            continue;

                                        }

                                        //continue;
                                    }
                                    else
                                    {
                                        //LogSystem.WriteLogDebug("Fecha de mov 116  imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);



                                        if (DateTime.Compare(mm.FechaAlta, f) < 0)
                                        {
                                            if (DateTime.Compare(mm.FechaAlta, Control_Mov_imagen_forzar_grabación_en_dias) < 0)
                                            {
                                                LogSystem.WriteLogDebug("Imagen ya existe para exportar y mov 116 < File y es mov mayor a 14 dias " + Path, ref log);
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("Imagen ya existe para exportar pero exite mov 116 mas nuevo : " + Path, ref log);


                                        }
                                    }

                                    /*
                                    if (DateTime.Compare(f, DateTime.Now.AddMonths(-17)) < 0)
                                    {
                                        LogSystem.WriteLogDebug("Imagen ya existe para exportar y esta guardada desde hace más de un año : " + Path, ref log);
                                        continue;

                                    }
                                    */





                                }
                                else
                                {
                                    //LogSystem.WriteLogDebug("Fecha de mov 117  imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);

                                    if (DateTime.Compare(mm.FechaAlta, f) < 0)
                                    {

                                        if (DateTime.Compare(mm.FechaAlta, Control_Mov_imagen_forzar_grabación_en_dias) < 0)
                                        {

                                            mm = mdalc.GetLastByTypeIDAndBarCode(116, lineS.codigobarra);
                                            if (mm == null)
                                            {
                                                LogSystem.WriteLogDebug("Imagen ya existe para exportar y no hay movimiento 116: " + Path, ref log);
                                                continue;
                                            }
                                            else
                                            {
                                                LogSystem.WriteLogDebug("Fecha de mov 116  imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);
                                                if (DateTime.Compare(mm.FechaAlta, f) < 0)
                                                {
                                                    if (DateTime.Compare(mm.FechaAlta, Control_Mov_imagen_forzar_grabación_en_dias) < 0)
                                                    {
                                                        LogSystem.WriteLogDebug("Imagen ya existe para exportar y mov 116 < File y mov menor a 14 dias " + Path, ref log);
                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    LogSystem.WriteLogDebug("Imagen ya existe para exportar pero exite mov 116 mas nuevo : " + Path, ref log);

                                                }

                                            }
                                            /*
                                            if (DateTime.Compare(f, DateTime.Now.AddMonths(-17)) < 0)
                                            {
                                                LogSystem.WriteLogDebug("Imagen ya existe para exportar y esta guardada desde hace más de un año : " + Path, ref log);
                                                continue;

                                            }*/

                                        }


                                    }
                                    else
                                        LogSystem.WriteLogDebug("Actualizar por nueva imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);

                                }
                            }
                            else
                                LogSystem.WriteLogDebug(" xxxxxxxxxx  Imagen no encontrada en Almacen - Se crea : " + Path, ref log);

                            LogSystem.WriteLogDebug("--------------------------  Imagen en preparación para exportar : " + Path, ref log);

                            if (i.Width > 1100 || i.Height > 800)
                                SaveMkp(i, 1920, 1200, 100, Path, PathFTP);
                            else if (i.Width > 500 || i.Height > 500)
                                SaveMkp(i, 1100, 1100, 100, Path, PathFTP);
                            else
                                SaveMkp(i, 500, 500, 100, Path, PathFTP);
                            Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_t.jpg";
                            PathFTP = PathImFTP + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_t.jpg";
                            SaveMkp(i, 50, 50, 100, Path, PathFTP);
                            Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_s.jpg";
                            PathFTP = PathImFTP + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_s.jpg";
                            SaveMkp(i, 470, 470, 100, Path, PathFTP);
                            LogSystem.WriteLogDebug(" ooooooooooooooooooooooooooooo  LOTE BASICO DE IMAGENES CREADA : " + Path, ref log);

                        }
                        else
                        {
                            Path = PathIm + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_b_" + ind + ".jpg";
                            PathFTP = PathImFTP + "\\" + lineS.codigobarra.ToLower().Replace("+", "-").Replace(" ", "").Replace(",", "") + "_b_" + ind + ".jpg";
                            if (System.IO.File.Exists(Path))
                            {
                                f = System.IO.File.GetCreationTime(Path);
                                LogSystem.WriteLogDebug("---------------------- Fecha de creación imagen : " + f.ToString("yyyy-MM-dd HH:mm:ss"), ref log);

                                f1 = f;
                                f2 = f;
                                try
                                {
                                    f1 = System.IO.File.GetLastWriteTime(Path);
                                }
                                catch
                                {
                                    f1 = f;
                                }

                                LogSystem.WriteLogDebug("---------------------- Fecha de modificación imagen : " + f1.ToString("yyyy-MM-dd HH:mm:ss"), ref log);

                                if (DateTime.Compare(f1, f) > 0)
                                    f = f1;

                                LogSystem.WriteLogDebug("---------------------- Fecha de control imagen : " + f.ToString("yyyy-MM-dd HH:mm:ss"), ref log);


                                MovementEntity mm = mdalc.GetLastByTypeIDAndBarCode(117, lineS.codigobarra);
                                if (mm == null)
                                {
                                    //LogSystem.WriteLogDebug("Sin mov 117 - " + Path, ref log);
                                    mm = mdalc.GetLastByTypeIDAndBarCode(116, lineS.codigobarra);
                                    if (mm == null)
                                    {
                                        if (DateTime.Compare(f2, DateTime.Now.AddMonths(-12)) < 0)
                                        {
                                            LogSystem.WriteLogDebug("Imagen ya existe para exportar y esta guardada desde hace más de un año : " + Path, ref log);
                                            continue;

                                        }
                                        else
                                            LogSystem.WriteLogDebug("Imagen ya existe para exportar y no hay movimiento 116: " + Path, ref log);
                                        //continue;
                                    }
                                    else
                                    {
                                        //LogSystem.WriteLogDebug("Fecha de mov 116  imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);



                                        if (DateTime.Compare(mm.FechaAlta, f) < 0)
                                        {
                                            if (DateTime.Compare(mm.FechaAlta, Control_Mov_imagen_forzar_grabación_en_dias) < 0)
                                            {
                                                LogSystem.WriteLogDebug("Imagen ya existe para exportar y mm 116 < File y fecha mov menor a 14 dias - " + Path, ref log);
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            LogSystem.WriteLogDebug("Imagen ya existe para exportar pero exite mov 116 mas nuevo : " + Path, ref log);

                                        }
                                    }
                                    /*
                                    if (DateTime.Compare(f, DateTime.Now.AddMonths(-17)) < 0)
                                    {
                                        LogSystem.WriteLogDebug("Imagen ya existe para exportar y esta guardada desde hace más de un año : " + Path, ref log);
                                        continue;

                                    }
                                    */


                                }
                                else
                                {
                                    //LogSystem.WriteLogDebug("Fecha de mov 117  imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);
                                    if (DateTime.Compare(mm.FechaAlta, Control_Mov_imagen_forzar_grabación_en_dias) < 0)
                                    {

                                        if (DateTime.Compare(mm.FechaAlta, f) < 0)
                                        {
                                            mm = mdalc.GetLastByTypeIDAndBarCode(116, lineS.codigobarra);
                                            if (mm == null)
                                            {
                                                LogSystem.WriteLogDebug("Imagen ya existe para exportar y no hay movimiento 116 : " + Path, ref log);
                                                continue;
                                            }
                                            else
                                            {
                                                //LogSystem.WriteLogDebug("Fecha de mov 116  imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);
                                                if (DateTime.Compare(mm.FechaAlta, f) < 0)
                                                {
                                                    if (DateTime.Compare(mm.FechaAlta, Control_Mov_imagen_forzar_grabación_en_dias) < 0)
                                                    {
                                                        LogSystem.WriteLogDebug("Imagen ya existe para exportar y mm 116 < F y mov menor a 14 dias: " + Path, ref log);
                                                        continue;
                                                    }
                                                }

                                            }


                                        }
                                        else
                                            LogSystem.WriteLogDebug("Actualizar por nueva imagen : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);
                                    }
                                    else
                                        LogSystem.WriteLogDebug("Actualizar por nueva imagen dentro de los 14 dias : " + mm.FechaAlta.ToString("yyyy-MM-dd HH:mm:ss"), ref log);

                                }
                            }
                            LogSystem.WriteLogDebug("--------------------------  Imagen en preparación para exportar : " + Path, ref log);

                            Bitmap i = new Bitmap(new MemoryStream(img.Image));
                            if (i.Width > 1100 || i.Height > 800)
                                SaveMkp(i, 1920, 1200, 100, Path, PathFTP);
                            else if (i.Width > 500 || i.Height > 500)
                                SaveMkp(i, 1100, 1100, 100, Path, PathFTP);
                            else
                                SaveMkp(i, 500, 500, 100, Path, PathFTP);

                            LogSystem.WriteLogDebug(" ooooooooooooooooooooooooooooo IMAGEN CREADA : " + Path, ref log);

                        }


                    }
                    if (ind == 0)
                        LogSystem.WriteLogDebug("No encontré imagen del producto: " + lineS.codigobarra + " - " + lineS.nombre, ref log);
                }
                catch (Exception p)
                {
                    LogSystem.WriteLogDebug("Error en imagenes del producto: " + lineS.codigobarra + " - " + lineS.nombre + " - Error: " + p.Message, ref log);
                }
            }

            LogSystem.WriteLogClose(" <<<<<<<<<<<<<<<<<<<<<<<<<<  Fin exportación imágenes", log);
        }


        public static void Save(Bitmap image, int maxWidth, int maxHeight, int quality, string filePath)
        {
            // Get the image's original width and height
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // To preserve the aspect ratio
            float ratioX = (float)maxWidth / (float)originalWidth;
            float ratioY = (float)maxHeight / (float)originalHeight;
            float ratio = Math.Min(ratioX, ratioY);

            // New width and height based on aspect ratio
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            // Convert other formats (including CMYK) to RGB.
            Bitmap newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            // Draws the image in the specified size with quality mode set to HighQuality
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }


            // Get an ImageCodecInfo object that represents the JPEG codec.
            ImageCodecInfo imageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg);

            // Create an Encoder object for the Quality parameter.
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object. 
            EncoderParameters encoderParameters = new EncoderParameters(1);

            // Save the image as a JPEG file with quality level.
            EncoderParameter encoderParameter = new EncoderParameter(encoder, quality);
            encoderParameters.Param[0] = encoderParameter;
            newImage.Save(filePath, imageCodecInfo, encoderParameters);
        }

        public static void SaveMkp(Bitmap image, int maxWidth, int maxHeight, int quality, string filePath, string filePathFtp)
        {
            // Get the image's original width and height
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // To preserve the aspect ratio
            float ratioX = (float)maxWidth / (float)originalWidth;
            float ratioY = (float)maxHeight / (float)originalHeight;
            float ratio = Math.Min(ratioX, ratioY);

            // New width and height based on aspect ratio
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            // Convert other formats (including CMYK) to RGB.
            Bitmap newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            // Draws the image in the specified size with quality mode set to HighQuality
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }


            // Get an ImageCodecInfo object that represents the JPEG codec.
            ImageCodecInfo imageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg);

            // Create an Encoder object for the Quality parameter.
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object. 
            EncoderParameters encoderParameters = new EncoderParameters(1);

            // Save the image as a JPEG file with quality level.
            EncoderParameter encoderParameter = new EncoderParameter(encoder, quality);
            encoderParameters.Param[0] = encoderParameter;
            newImage.Save(filePath, imageCodecInfo, encoderParameters);
            newImage.Save(filePathFtp, imageCodecInfo, encoderParameters);
        }
        /// <summary>
        /// Method to get encoder infor for given image format.
        /// </summary>
        /// <param name="format">Image format</param>
        /// <returns>image codec info.</returns>
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == format.Guid);
        }

         public static void SynchronizeImagesBySFTP(ref string log)
        {
            #region SUBIR ARCHIVOS A SFTP
            LogSystem.WriteLogDebug("Inicio SynchronizeImagesBySFTP ", ref log);

            string toFile = ConfigurationManager.AppSettings["SftpPath"];
            string fullPath = Path.Combine(ConfigurationManager.AppSettings["PathImagenes"].ToString(), "*");

            
            try
            {
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = ConfigurationManager.AppSettings["HostName"],
                    UserName = ConfigurationManager.AppSettings["UserName"],
                    Password = ConfigurationManager.AppSettings["Password"],
                    SshHostKeyFingerprint = ConfigurationManager.AppSettings["SshHostKeyFingerprint"],
                    SshPrivateKeyPath = ConfigurationManager.AppSettings["SshPrivateKeyPath"]


                };

                sessionOptions.AddRawSettings("FSProtocol", "2");

                using (Session session = new Session())
                {
                    session.ExecutablePath = ConfigurationManager.AppSettings["WinCspExe"];
                    session.Open(sessionOptions);
                    LogSystem.WriteLogDebug("** Conección a SFTP establecida **", ref log);
            
                    // Archivos de Imagenes
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;
                    transferOptions.PreserveTimestamp = false;
                    
                    TransferOperationResult transferResult;
                    transferResult = session.PutFiles(fullPath, toFile, true, transferOptions);

                    transferResult.Check();

                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        LogSystem.WriteLogDebug("Status del archivo subido: " + transfer.FileName.ToString() + " - Status:" + transfer.Error.Message , ref log);

                    }

                    
                }
            }
            catch (Exception e)
            {
                LogSystem.WriteLogDebug("xxxx ERROR Conección a SFTP xxxx  Error: " + e.Message, ref log);
               
            }
            finally
            {
                LogSystem.WriteLogDebug("FIN SynchronizeImagesBySFTP ", ref log);
            }

            #endregion

        }
        public static int ContarArchivos(string ruta)
        {
            int contador = 0;

            DirectoryInfo directorio = new DirectoryInfo(ruta);

            // Contar archivos en el directorio actual
            contador += directorio.GetFiles().Length;

            // Contar archivos en subdirectorios
            foreach (DirectoryInfo subDirectorio in directorio.GetDirectories())
            {
                contador += ContarArchivos(subDirectorio.FullName); // Recursivamente contar en subdirectorios
            }

            return contador;
        }
        public static void SynchronizeImagesBySFTPMkp(ref string log)
        {
            #region SUBIR ARCHIVOS A SFTP
            LogSystem.WriteLogDebug("Inicio SynchronizeImagesBySFTP ", ref log);

            string toFile = ConfigurationManager.AppSettings["SftpPath"];
            string fullPath = Path.Combine(ConfigurationManager.AppSettings["PathImagenes"].ToString() + "FTP", "*");

            LogSystem.WriteLogDebug("Desde  " + fullPath, ref log);
            LogSystem.WriteLogDebug("Hacia " + toFile, ref log);
            try
            {
                int iteraciones = 0;
                bool Salir = false;
                while (!Salir && iteraciones < 10)
                {
                    iteraciones++;
                    LogSystem.WriteLogDebug("Inicio intento de envio por SFTP nro: " + iteraciones, ref log);

                    try
                    {
                        LogSystem.WriteLogDebug("Cantidad de archivos para transferir: " + ContarArchivos(fullPath), ref log);

                    }
                    catch
                    {
                        LogSystem.WriteLogDebug("xxx No se pudo contar los archivos", ref log);
                    }
                    try
                    {
                        LogSystem.WriteLogDebug("Iniciando seteo de AWS S3", ref log);
                        var settings = new AwsSettings();
                        var s3 = new S3Service(settings);
                        /*
                        // Listar archivos
                        var archivos = s3.ListFiles("reportes/");
                        foreach (var a in archivos)
                        {
                            Console.WriteLine(a);
                        }
                        
                        // Subir archivo
                        s3.UploadFile(@"C:\tmp\prueba.xlsx", "reportes/prueba.xlsx");

                        // Descargar archivo
                        s3.DownloadFile("reportes/prueba.xlsx", @"C:\tmp\prueba_bajada.xlsx");

                        // Leer contenido como texto
                        string contenido = s3.GetTextFile("data/test.txt");
                        Console.WriteLine(contenido);
                        */
                        // Subir todo un directorio
                        int count = s3.UploadDirectory(
                            fullPath.Replace("\\*",""),
                            ""   // carpeta destino en S3 (opcional)
                        );
                        LogSystem.WriteLogDebug("Proceso de carga finalizado en AWS S3. Cantidad de productos subidos: " + count, ref log);


                    }
                    catch (Exception aws3)
                    {
                        LogSystem.WriteLogDebug("xxxx ERROR intentando subir los archivos a AWS.  Error: " + aws3.Message, ref log);

                    }


                    try
                    {
                        SessionOptions sessionOptions = new SessionOptions
                        {

                            Protocol = Protocol.Sftp,
                            HostName = ConfigurationManager.AppSettings["HostName"],
                            UserName = ConfigurationManager.AppSettings["UserName"],
                            //Password = ConfigurationManager.AppSettings["Password"],
                            SshHostKeyFingerprint = ConfigurationManager.AppSettings["SshHostKeyFingerprint"],
                            SshPrivateKeyPath = ConfigurationManager.AppSettings["SshPrivateKeyPath"]


                        };

                        sessionOptions.AddRawSettings("FSProtocol", "2");

                        using (Session session = new Session())
                        {
                            session.ExecutablePath = ConfigurationManager.AppSettings["WinCspExe"];
                            session.SessionLogPath = ConfigurationSettings.AppSettings["PathLog"] + DateTime.Now.ToString("yyyy-MM-dd") + "_SyncFTP.log";
                            session.Open(sessionOptions);
                            LogSystem.WriteLogDebug("** Conección a SFTP establecida **", ref log);

                            // Archivos de Imagenes
                            TransferOptions transferOptions = new TransferOptions();
                            transferOptions.TransferMode = TransferMode.Binary;
                            transferOptions.PreserveTimestamp = false;


                            TransferOperationResult transferResult;
                            transferResult = session.PutFiles(fullPath, toFile, true, transferOptions);

                            transferResult.Check();

                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                LogSystem.WriteLogDebug("Status del archivo subido: " + transfer.FileName.ToString(), ref log);

                            }
                            string pattern = "dd-MM-yyyy HH:mm:ss";
                            LogSystem.WriteLogDebug("** Conección a SFTP FINALIZADA **", ref log);
                            LogSystem.WriteLogDebug("** Intentando actualizar fecha de proceso en parametros **", ref log);

                            ET.Comun.Business.Parametros param = new ET.Comun.Business.Parametros();
                            param.SetValor("FechaControlActualizacionImagenesMagento", FechaInicioProceso);
                            param.SetParametros();

                            LogSystem.WriteLogDebug("** FechaControlActualizacionImagenesMagento --> "  + FechaInicioProceso, ref log);

                            Salir = true;

                        }
                    }
                    catch (Exception e)
                    {

                        LogSystem.WriteLogDebug("xxxx ERROR Conección a SFTP xxxx  Error: " + e.Message, ref log);
                        LogSystem.WriteLogDebug("xxxx Esperamos 2 minutos y vemos si intentamos de nuevo. " , ref log);
                        Thread.Sleep(120000);
                    }

                }
            }
            catch (Exception e)
            {
                LogSystem.WriteLogDebug("xxxx ERROR Conección a SFTP xxxx  Error: " + e.Message, ref log);

            }
            finally
            {
                LogSystem.WriteLogDebug("FIN SynchronizeImagesBySFTP ", ref log);
            }

            #endregion

        }
        public static void ImportProductsfromExcel(string Path, string ProveedorID, ref string log)
        {
            var magento = new Magento(URL);
            string resp = magento.ImportProductfromFileLine(Path, ProveedorID);
            LogSystem.WriteLogDebug("Fin proceso : " + resp, ref log);

        }
        /// <summary>
        /// Retorna una lista de archivos para procesar
        /// </summary>
        /// <param name="sourceDirectory">Path del directorio donde se desea realizar la búsqueda</param>
        /// <returns>List[FileInfo]</returns>
        public static List<FileInfo> GetFileInfoListToProcess(string storeDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(storeDirectory);
            return directoryInfo.GetFiles().ToList<FileInfo>();
        }
        /// <summary>
        /// Retorna una lista de directorios para procesar
        /// </summary>
        /// <param name="sourceDirectory">Path del directorio donde se desea realizar la búsqueda</param>
        /// <returns>List[FileInfo]</returns>
        public static List<DirectoryInfo> GetFolderInfoListToProcess(string storeDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(storeDirectory);
            return directoryInfo.GetDirectories().ToList<DirectoryInfo>();
        }
    }
}

