using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Helpers
{
    public static class Logger
    {
        private static string logFilePath = Path.Combine(Properties.Settings.Default.DBLocation, "Logs",
                                                         string.Format("log{0:MM-dd-yyyy}.txt", DateTime.Today));
        private static object lockObj = new object();

        public static void Info(string msg)
        {
            lock(lockObj)
            {
                if (Directory.Exists(Path.GetDirectoryName(logFilePath)))
                {
                    string line = string.Format("{0:MM/dd/yy - HH:mm:ss} - INFO - {1}" + Environment.NewLine, DateTime.Now, msg);
                    File.AppendAllText(logFilePath, line);
                }
            }
        }

        public static void Error(string msg)
        {
            lock (lockObj)
            {

                if (Directory.Exists(Path.GetDirectoryName(logFilePath)))
                {
                    string line = string.Format("{0:MM/dd/yy - HH:mm:ss} - ERROR - {1}" + Environment.NewLine, DateTime.Now, msg);
                    File.AppendAllText(logFilePath, line);
                }
            }
        }
    }
}
