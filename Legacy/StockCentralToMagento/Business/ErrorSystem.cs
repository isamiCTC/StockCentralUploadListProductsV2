using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
//using Microsoft.Practices.EnterpriseLibrary.Common;
//using Microsoft.Practices.EnterpriseLibrary.Data;

using System.Configuration;
using System.Diagnostics;


namespace StockCentralToMagento
{
    public static class ErrorSystem
    {
        static string logFile = ConfigurationSettings.AppSettings["logFile"];

 
        public static void LogExceptionAndError(Exception ex, string customMessage, EventLogEntryType eventLogEntryType)
        {
            //Se loguea la excepcion en el EventLog
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("[Error]:  " + Error);
            sb.AppendLine(customMessage);
            //EventLog.WriteEntry("TcpIpProcessService", sb.ToString(), eventLogEntryType);
            try
            {
                System.IO.File.AppendAllText(logFile, sb.ToString());
            }
            catch (Exception f)
            {
                System.IO.File.AppendAllText(logFile + ".ExceptionError.log", sb.ToString());
            }

            if (ex != null)
            {
                sb = new StringBuilder();
                sb.AppendLine(string.Format("[Exception]: {0}", ex.Message));
                if (ex.InnerException != null)
                    sb.AppendLine(string.Format("[InnerException]: {0}", ex.InnerException.Source));
                if (ex.Source != null)
                    sb.AppendLine(string.Format("[Source]: {0}", ex.Source.ToString()));
                if (ex.StackTrace != null)
                    sb.AppendLine(string.Format("[StackTrace]: {0}", ex.StackTrace.ToString()));
                if (ex.TargetSite != null)
                    sb.AppendLine(string.Format("[TargetSite]: {0}", ex.TargetSite.ToString()));

                //EventLog.WriteEntry("TcpIpProcessService", sb.ToString(), eventLogEntryType);
                try
                {
                    System.IO.File.AppendAllText(logFile, sb.ToString());
                }
                catch (Exception f)
                {
                    System.IO.File.AppendAllText(logFile + ".ExceptionError.log", sb.ToString());
                }
            }
        }

    }
}
