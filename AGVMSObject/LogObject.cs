using System;
using System.IO;
using AGVMSUtility;

namespace AGVMSObject
{
    public class LogObject
    {
        private string LogPath;
        private string LogFile;

        public LogObject()
        {
            LogPath = System.Environment.CurrentDirectory + "\\Log";
        }

        public void setLogFileName(string _LogFile)
        {
            LogFile = Path.Combine(LogPath, _LogFile);// Path.Combine(LogPath, @"AGVM-" + DateTime.Now.ToString("yyyyMMdd") + "_LOG.txt");
        }

        public virtual void WriteLog(string LogString)
        {
            try
            {
                //UtilityHelper.checkPathExist(LogPath);

                File.AppendAllText(LogFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "：" + LogString); //+ "\r\n"
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }

        }
    }
}
