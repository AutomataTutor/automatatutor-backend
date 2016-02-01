using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace AutomataPDL
{
    public static class Log
    {
        private static readonly object Locker = new object();

        const String nameFile = "log.txt";
        
        internal static void ClearLog()
        {
            lock (Locker)
            {
                File.Create(nameFile).Dispose();
            }
        }

        internal static void WriteLog(string message)
        {            
            //So far no threads
            lock (Locker)
            {
                using (StreamWriter sw = File.AppendText(nameFile))
                {
                    sw.WriteLine("{0}:{1}:{2} ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), message);
                    sw.Close();
                }
            }
        }
    }
}
