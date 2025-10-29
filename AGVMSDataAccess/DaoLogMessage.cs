using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using AGVMSModel;

namespace AGVMSDataAccess
{
    public class DaoLogMessage
    {
        public int InsertLOG_MESSAGE(dsLOG_MESSAGE entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;

            sqlCmd = @"INSERT INTO LOG_MESSAGE (CREATE_DATETIME, LOG_MESSAGE)
                        VALUES 
                       (@CREATE_DATETIME, @LOG_MESSAGE) ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@CREATE_DATETIME", entity.CREATE_DATETIME));
            paras.Add(new SqlParameter("@LOG_MESSAGE", entity.LOG_MESSAGE));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }

    }
}
