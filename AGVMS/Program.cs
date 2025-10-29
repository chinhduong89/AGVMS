using System;
using System.Windows.Forms;

namespace AGVMS
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //處理未catch的異常
            //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            //處理UI執行序異常
            //Application.ThreadException += new System.Threading.ThreadExceptionEventHandler();
            //處理非UI執行序異常
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler();

            Application.Run(new LoginForm());
        }

        static bool glExitApp = false;

        static void CurrendDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //LogHelper.Save();
        }
    }
}
