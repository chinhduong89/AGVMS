using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using AGVMSModel;


namespace AGVMSDataAccess
{
    public class DaoCode
    {             
        public List<dsCODE_DETAIL> getAGVStationToList()
        {
            string sqlCmd = @"SELECT 
                            CODE_NO, PARA_1, PARA_2, PARA_3, STATION, MEMO, PARA_NAME, PARA_NAME_ENG, PARA_NAME_LOCAL, PARA_NAME_OTHER, SORT
                            FROM CODE_DETAIL
                            WHERE CODE_NO IN ('AUTOSTOCK','BUFFER','MACHINE')
                            ORDER BY CODE_NO,SORT ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            return (List<dsCODE_DETAIL>)DBconn.DB.GetEntityList<dsCODE_DETAIL>(sqlCmd, CommandType.Text, paras.ToArray());
        }

        public List<dsCODE_DETAIL> getInOutToList()
        {
            string sqlCmd = @"SELECT 
                            CODE_NO, PARA_1, PARA_2, PARA_3, STATION, MEMO, PARA_NAME, PARA_NAME_ENG, PARA_NAME_LOCAL, PARA_NAME_OTHER, SORT
                            FROM CODE_DETAIL
                            WHERE CODE_NO IN ('INOUT')
                            ORDER BY SORT ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            return (List<dsCODE_DETAIL>)DBconn.DB.GetEntityList<dsCODE_DETAIL>(sqlCmd, CommandType.Text, paras.ToArray());
        }

        public List<dsCODE_DETAIL> getPriorityAreaToList()
        {
            string sqlCmd = @"SELECT 
                            CODE_NO, PARA_1, PARA_2, PARA_3, STATION, MEMO, PARA_NAME, PARA_NAME_ENG, PARA_NAME_LOCAL, PARA_NAME_OTHER, SORT
                            FROM CODE_DETAIL
                            WHERE CODE_NO IN ('PRIORITY_AREA')
                            ORDER BY SORT ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            return (List<dsCODE_DETAIL>)DBconn.DB.GetEntityList<dsCODE_DETAIL>(sqlCmd, CommandType.Text, paras.ToArray());
        }

    }
}
