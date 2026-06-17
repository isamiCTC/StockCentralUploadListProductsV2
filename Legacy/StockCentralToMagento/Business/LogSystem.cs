using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace StockCentralToMagento.Business
{
    public static class LogSystem
    {
        public static int writeLog = 0;

        public static string logFile = ConfigurationSettings.AppSettings["PathLog"];
        public static string productPathFile = ConfigurationSettings.AppSettings["PathProductsFile"];
        public static string exten = ".log";
        public static void Inicializar( int writelogint)
        {
            logFile = ConfigurationSettings.AppSettings["PathLog"];
            productPathFile = ConfigurationSettings.AppSettings["PathProductsFile"];
            exten = ".log";
            writeLog = writelogint;
        }

        public static void WriteLogDebugDirect(string msg, string nameFile)
        {
            int ifile = 0;
            while (true)
            {
                try
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile + DateTime.Now.ToString("yyyy-MM-dd") + "_"+nameFile+"_" + ifile.ToString() + exten, true);
                    sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " - " + msg);
                    sw.Close();
                    break;
                }
                catch (Exception e)
                {
                    ifile++;
                    if (ifile > 32) break;
                }
            }
        }


        public static void WriteLogDebug(string message, ref string log)
        {
            try
            {
                if (writeLog != -1)
                {
                    if (writeLog == 1)
                    {
                        int ifile = 0;
                        while (true)
                        {
                            try
                            {
                                System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile + DateTime.Now.ToString("yyyy-MM-dd") + "_StockCentralToMagentoServices_" + ifile.ToString() + exten, true);
                                sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " - " + message);
                                sw.Close();
                                break;
                            }
                            catch (Exception e)
                            {
                                ifile++;
                                if (ifile > 32) break;
                            }
                        }
                    }
                    else
                    {
                        log +=  DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " -- " + message + "\n";

                    }
                }
            }
            catch
            {}
        }
        public static void WriteLogClose(string messageLog, string logMsg)
        {
            try
            {
                if (writeLog != -1)
                {
                    int ifile = 0;
                    while (true)
                    {
                        try
                        {
                            System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile + DateTime.Now.ToString("yyyy-MM-dd") + "_StockCentralToMagentoServices_" + ifile.ToString() + exten, true);
                            sw.WriteLine(logMsg);
                            sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " -- " + messageLog);
                            sw.Close();
                            break;
                        }
                        catch (Exception e)
                        {
                            ifile++;
                            if (ifile > 32) break;
                        }
                    }
                }
                //return true;
            }
            catch (Exception ex)
            {
//                string errorLog = ex.Message;
                //return false;
            }

        }

        public static void WriteLogDebug(string message, ref string log, string fileName)
        {
            try
            {
                if (writeLog != -1)
                {
                    if (writeLog == 1)
                    {
                        int ifile = 0;
                        while (true)
                        {
                            try
                            {
                                System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile + DateTime.Now.ToString("yyyy-MM-dd") + "_"+fileName+"_" + ifile.ToString() + exten, true);
                                sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " - " + message);
                                sw.Close();
                                break;
                            }
                            catch (Exception e)
                            {
                                ifile++;
                                if (ifile > 32) break;
                            }
                        }
                    }
                    else
                    {
                        log += DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " -- " + message + "\n";

                    }
                }
            }
            catch
            { }
        }
        public static bool WriteLogClose(string messageLog, string logMsg, string fileName)
        {
            try
            {
                if (writeLog != -1)
                {
                    int ifile = 0;
                    while (true)
                    {
                        try
                        {
                            System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile + DateTime.Now.ToString("yyyy-MM-dd") + "_"+fileName+"_" + ifile.ToString() + exten, true);
                            sw.WriteLine(logMsg);
                            sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff") + " -- " + messageLog);
                            sw.Close();
                            break;
                        }
                        catch (Exception e)
                        {
                            ifile++;
                            if (ifile > 32) break;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //                string errorLog = ex.Message;
                return false;
            }

        }
        public static bool WriteListToFile(string[] List, string fileName)
        {
            try
            {
                int ifile = 0;
                while (true)
                {
                    try
                    {
                        System.IO.File.WriteAllLines  ( productPathFile + DateTime.Now.ToString("yyyy-MM-dd") + "_" + fileName + "_" + ifile.ToString() + ".csv", List);
                        break;
                    }
                    catch (Exception e)
                    {
                        ifile++;
                        if (ifile > 32) break;
                    }
                }
            }
            catch (Exception ex)
            {
                //                string errorLog = ex.Message;
                return false;
            }
            return true;
        }

    }
}
