using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Timers;
using System.Threading;
using System.Configuration;
using System.IO.Compression;
using StockCentralToMagento.Business;
using StockCentralToMagento.DataAccess;
using StockCentralToMagento.Entities;

namespace StockCentralUploadListProducts
{
    public partial class MainServices : ServiceBase
    {
        public MainServices()
        {
            InitializeComponent();
        }
        static int writeLog = 1;

        //Timer para la búsqueda de archivos en carpeta Almacen
        System.Timers.Timer timerStore;
        //Tiempo (mseg) que se espera para realizar una nueva búsqueda cuando no existen archivos para procesar
        int timeX = int.Parse(ConfigurationSettings.AppSettings["timeX"]);
        //Tiempo (mseg) que se espera para volver a reiniciar el proceso en horas.
        int timeZ = int.Parse(ConfigurationSettings.AppSettings["timeZ"]);
        int SincronizarImagenes = 1;

        protected override void OnStart(string[] args)
        {
            timerStore = new System.Timers.Timer();
            
            Redemption.Core.Common.SqlDataConfiguration.ConnectionString = ConfigurationSettings.AppSettings["StockCentral"];
 
            

            //Solo se ejecuta cada vez que se llama al metodo start
            timerStore.Interval = 30000;
            timerStore.AutoReset = false;

            timerStore.Elapsed += new ElapsedEventHandler(timerStore_Elapsed);

            try
            {
                SincronizarImagenes = int.Parse(ConfigurationSettings.AppSettings["SincronizarImagenes"]);
            }catch (Exception noway)
            { 
                SincronizarImagenes = 1; //Activo por default 
            }
            
            try
            {
                //Si no hay variable definida queda en 0
                // logType == 0; solo graba excepciones
                // logType == 1; graba en disco por cada WriteLog
                // logType == 2; graba una sola vez por cada TRN (más performante)
                writeLog = Convert.ToInt32(ConfigurationSettings.AppSettings["LogType"]);
                LogSystem.writeLog = writeLog;

                LogSystem.Inicializar(writeLog);
            }
            catch { }

            LogSystem.WriteLogDebugDirect("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx PROCESS StartUP StockCentralUploadListProducts V1.0 xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "StockCentralUploadListProducts");
           
            //Se inicializa el timer
            timerStore.Start();
        }

        protected override void OnStop()
        {
        }

        protected void timerStore_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timerStore.Stop();

            LogSystem.WriteLogDebugDirect("***************************************** INICIA PROCESO DE ITERACIONES  *****************************************", "StockCentralUploadListProducts");

            string user = "";
            string passWord = ""; 

            try
            {
                //foreach (ConnectionStringSettings connectionStringSettings in ConfigurationManager.ConnectionStrings)
                //{
                //    ConexionDataBase.connectionStringName = connectionStringSettings.Name;
                //}
                user = ConfigurationSettings.AppSettings["MagentoAdmin"]; //"admin";
                passWord = ConfigurationSettings.AppSettings["MagentoPass"]; //"ctc2018";
              
                
            }
            catch (Exception Init)
            {
                LogSystem.WriteLogDebugDirect("Error en inicialización de comunicaciones. " + Init.Message + " - \n\n" + Init.Source + "\n\n\n[" + Init.StackTrace + "]", "StockCentralUploadListProducts");
                Thread.Sleep(15000);
                user = "admin";
                passWord = "ctc2018";


            }
            while (true)
            {
                try
                {
                    Process(user, passWord);
                }
                catch (Exception f)
                {
                    //Se espera un tiempo "X" y se reanuda la búsqueda de archivos en el Almacen
                    LogSystem.WriteLogDebugDirect("BIG - BIG ERROR " + f.Message + " - \n\n" + f.Source + "\n\n\n[" + f.StackTrace + "]", "StockCentralUploadListProducts");
                    Thread.Sleep(15000);
                    //LogExceptionAndError(f, "BIG - BIG ERROR ", EventLogEntryType.Error);
                }
                LogSystem.WriteLogDebugDirect("Tiempo de Espera: " + timeZ.ToString() + " ms", "StockCentralUploadListProducts");
                Thread.Sleep(timeZ);
            }
            timerStore.Start();
        }
        private void Process(string User, string pass)
        {
            string logMsg = string.Empty;
            //ProcessSystem.GetToken(User, pass);
            DateTime InitToken = DateTime.Now;


            // **************************
            // Obtener los Catalogos configurados en MAGENTO desde la tabla nueva CatalogosMAgento en Stock Central
            int SolounCatalogo = 0;// Convert.ToInt32(ConfigurationSettings.AppSettings["ActivarSoloCatalogoNro"]);
            
            try
            {
                SolounCatalogo = Convert.ToInt32(ConfigurationSettings.AppSettings["ActivarSoloCatalogoNro"]);
            }
            catch(Exception t)
            {
                SolounCatalogo = 0;
            }
            LogSystem.WriteLogDebug("PARAMETRO SolounCatalogo = " + SolounCatalogo, ref logMsg);

            int CatalogoID = 1;
            List<MagentoCatalogEntity> cat = new List<StockCentralToMagento.Entities.MagentoCatalogEntity>();
            StockCentralToMagento.DataAccess.CatalogDALC cDalc = new CatalogDALC();
            StockCentralToMagento.DataAccess.ProviderDALC prDalc = new ProviderDALC();

            cat = cDalc.GetCatalogMagento();
            string fileNotFoundDirectory = "";
            string storeDirectory = "";
            string historyDirectory = "";

            //Directorio para la búsqueda de archivos
            try
            {
                 storeDirectory = ConfigurationSettings.AppSettings["StorePathUploadProducts"].ToString();
            }
            catch(Exception param)
            {
                LogSystem.WriteLogDebug("ERROR EN PARAMETRO StorePathUploadProducts -- ABORT", ref logMsg);
                return;
            }
            //Directorio al que se mueven los archivos
            try
            {
                historyDirectory = ConfigurationSettings.AppSettings["IFPS_HistoryDirectory"].ToString();
            }
            catch (Exception param)
            {
                LogSystem.WriteLogDebug("ERROR EN PARAMETRO historyDirectory -- ABORT", ref logMsg);
                return;
            }

            //Directorio al que se mueven los archivos no encontrados en la tabla FileConfig
            try
            {
                 fileNotFoundDirectory = ConfigurationSettings.AppSettings["IFPS_FileNotFoundDirectory"].ToString();
            }
            catch (Exception param)
            {
                LogSystem.WriteLogDebug("ERROR EN PARAMETRO fileNotFoundDirectory -- ABORT", ref logMsg);
                return;
            }

            LogSystem.WriteLogDebug("PARAMETRO storeDirectory -- > "+ storeDirectory, ref logMsg);
            LogSystem.WriteLogDebug("PARAMETRO historyDirectory -- > "+ historyDirectory, ref logMsg);
            LogSystem.WriteLogDebug("PARAMETRO fileNotFoundDirectory -- > "+ fileNotFoundDirectory, ref logMsg);

            List<StockCentralToMagento.Entities.ProviderEntity> providers = prDalc.GetListByEnabledAndIntegratorAndCatalogID(true, 3, SolounCatalogo);

            LogSystem.WriteLogDebug("Vendors encontrados -- > " + providers.Count , ref logMsg);

            foreach (ProviderEntity pr in providers)
            {
                LogSystem.WriteLogDebug("Evaluando a Proveedor = " + pr.ID + " [" + pr.Name + "]", ref logMsg);
                List<FileInfo> fileInfoList;
                //Se obtienen todos los archivos para procesar
                try
                {
                    fileInfoList = StockCentralToMagento.Business.ProcessSystem.GetFileInfoListToProcess(storeDirectory + "\\" + pr.ID);
                }
                catch(Exception dirProg)
                {
                    LogSystem.WriteLogDebug("ERROR EN Proveedor = " + pr.ID  + " [" + pr.Name + "]", ref logMsg);
                    continue;

                }
                LogSystem.WriteLogDebug("Archivos encontrados = " + fileInfoList.Count, ref logMsg);
                
                if (fileInfoList.Count > 0)
                {
                    foreach (FileInfo fileInfo in fileInfoList)
                    {
                        string historyFileName = FormatFileName(fileInfo);

                        Object[] fileNameComponents = GetFileNameComponets(fileInfo);

                        string fileName = (string)fileNameComponents[0];
                        //DateTime? uploadDate = (DateTime?)fileNameComponents[1];
                        int? ProvedorID = pr.ID;//(int?)fileNameComponents[2];
                        //string periodo = (fileNameComponents.Length > 3 ? (string)fileNameComponents[3] : "");

                        LogSystem.WriteLogDebug("Inicio proceso archivo.  File = " + fileInfo.FullName, ref logMsg);

                        if (ProvedorID != null)
                            if (isExcel(fileInfo))
                            {
                                StockCentralToMagento.Business.ProcessSystem.ImportProductsfromExcel(fileInfo.FullName, ProvedorID.ToString(), ref logMsg);
                                fileInfo.MoveTo(string.Format("{0}{1}", historyDirectory + "\\", historyFileName));
                                LogSystem.WriteLogDebug("Fin proceso archivo.  File = " + fileInfo.FullName, ref logMsg);

                            }
                            else
                            {
                                try
                                {
                                    fileInfo.MoveTo(string.Format("{0}{1}", fileNotFoundDirectory + "\\", FormatFileName(fileInfo)));
                                }
                                catch (Exception no_encontrado)
                                {
                                    LogSystem.WriteLogDebug("Archivo no encontrado. pudo ser movido por el proceso anterior ante una reproceso por error global.  File = " + fileName, ref logMsg);
                                }

                            }
                        else
                        {
                            try
                            {
                                fileInfo.MoveTo(string.Format("{0}{1}", fileNotFoundDirectory + "\\", FormatFileName(fileInfo)));
                            }
                            catch (Exception no_encontrado)
                            {
                                LogSystem.WriteLogDebug("Archivo no encontrado. pudo ser movido por el proceso anterior ante una reproceso por error global.  File = " + fileName, ref logMsg);
                            }
                            LogSystem.WriteLogDebug("Nombre archivo no reconocido.  File = " + fileName + ". La estrucutra del archivo es Nombre_Fecha_Proveedor.xlsx", ref logMsg);

                        }
                        //WriteLogDebug(" File = " + fileName);

                    }

                }

            }
            LogSystem.WriteLogClose(" =============================================== FIN Iteracion ======================================================= ", logMsg);
           
           

        }



        private string FormatFileName(FileInfo fileInfo)
        {
            string fileName;
            if (fileInfo.Extension != string.Empty)
            {
                fileName = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
                fileName = string.Format("{0} {1}", fileName, DateTime.Now.ToString("yyyy-MM-dd HHmmss"));
                fileName = string.Format("{0}{1}", fileName, fileInfo.Extension);
            }
            else
            {
                fileName = fileInfo.Name;
                fileName = string.Format("{0} {1}", fileName, DateTime.Now.ToString("yyyy-MM-dd HHmmss"));
            }

            return fileName;
        }
        private Object[] GetFileNameComponets(FileInfo fileInfo)
        {
            // Nombre archivo de importacion_Fecha Hora_Provedor ID.xlsx
            // Ejemplo: Diggit_2022-07-25 082000_342.xlsx
            string fileN;
            string fileExtension = fileInfo.Extension;
            if (fileExtension != string.Empty)
                fileN = fileInfo.Name.Replace(fileExtension, string.Empty);
            else
                fileN = fileInfo.Name;

            string[] fileNameComponentsArray = fileN.Split('_');

            string fileName = fileNameComponentsArray[0];
            DateTime? uploadDate = null;
            int? userID = null;
            string periodo = string.Empty;

            if (fileNameComponentsArray.Length == 3)
            {
                string[] dateHourComponents = fileNameComponentsArray[1].Split(' ');
                string[] dateComponents = dateHourComponents[0].Split('-');

                uploadDate = new DateTime(
                    int.Parse(dateComponents[0]),
                    int.Parse(dateComponents[1]),
                    int.Parse(dateComponents[2]),
                    int.Parse(dateHourComponents[1].Substring(0, 2)),
                    int.Parse(dateHourComponents[1].Substring(2, 2)),
                    int.Parse(dateHourComponents[1].Substring(4, 2)));

                userID = int.Parse(fileNameComponentsArray[2]);
            }

            if (fileNameComponentsArray.Length == 4)
            {
                string[] dateHourComponents = fileNameComponentsArray[1].Split(' ');
                string[] dateComponents = dateHourComponents[0].Split('-');

                uploadDate = new DateTime(
                    int.Parse(dateComponents[0]),
                    int.Parse(dateComponents[1]),
                    int.Parse(dateComponents[2]),
                    int.Parse(dateHourComponents[1].Substring(0, 2)),
                    int.Parse(dateHourComponents[1].Substring(2, 2)),
                    int.Parse(dateHourComponents[1].Substring(4, 2)));

                userID = int.Parse(fileNameComponentsArray[2]);

                periodo = fileNameComponentsArray[3];
            }

            //Object[] obj = new Object[3];
            //obj[0] = string.Format("{0}{1}",fileName, fileExtension);
            //obj[1] = uploadDate;
            //obj[2] = userID;

            //  Braian 150423 Agrego un nuevo objeto, para Período, cargado en archivo de Facturacion
            Object[] obj = new Object[4];
            obj[0] = string.Format("{0}{1}", fileName, fileExtension);
            obj[1] = uploadDate;
            obj[2] = userID;
            obj[3] = periodo;

            return obj;
        }
        private  bool isExcel(FileInfo file)
        {
            switch (file.Extension.ToLower())
            {
                case ".xls":
                case ".xlsx":
                    return true;
            }

            return false;
        }


    }
}
