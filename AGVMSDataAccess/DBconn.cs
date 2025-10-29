using AGVMSUtility;
using System.Configuration;

namespace AGVMSDataAccess
{
    public class DBconn
    {
        public static string strProdDB = ConfigurationManager.ConnectionStrings["AGVDBconn"].ConnectionString;
        public static MSSqlHelper DB;

        public static void setDBconnection(string loginType)
        {
            if (DB == null)
            {
                switch (loginType)
                {
                    default:
                        DB = new MSSqlHelper(strProdDB);
                        break;
                }
            }

        }
    }
}
