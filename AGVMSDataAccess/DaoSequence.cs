using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using AGVMSModel;

namespace AGVMSDataAccess
{
    public class DaoSequence
    {
        public DataTable QuerySYS_SEQUENCE(dsSYS_SEQUENCE entity)
        {
            string sqlCmd = @" SELECT SEQUENCE_TYPE, SEQUENCE_CODE, SEQUENCE_NO, MEMO FROM SYS_SEQUENCE WHERE 1=1 ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            if (!string.IsNullOrEmpty(entity.SEQUENCE_TYPE))
            {
                sqlCmd += " AND SEQUENCE_TYPE = @SEQUENCE_TYPE ";
                paras.Add(new SqlParameter("@SEQUENCE_TYPE", entity.SEQUENCE_TYPE));
            }
            if (!string.IsNullOrEmpty(entity.SEQUENCE_CODE))
            {
                sqlCmd += " AND SEQUENCE_CODE = @SEQUENCE_CODE ";
                paras.Add(new SqlParameter("@SEQUENCE_CODE", entity.SEQUENCE_CODE));
            }

            sqlCmd += @" ORDER BY SEQUENCE_TYPE ";

            return DBconn.DB.GetDataTable(sqlCmd, CommandType.Text, paras.ToArray());
        }

        public int InsertSYS_SEQUENCE(dsSYS_SEQUENCE entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;

            sqlCmd = @"INSERT INTO SYS_SEQUENCE (SEQUENCE_TYPE, SEQUENCE_CODE, SEQUENCE_NO, MEMO)
                        VALUES 
                        ( @SEQUENCE_TYPE, @SEQUENCE_CODE, @SEQUENCE_NO, @MEMO) ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@SEQUENCE_TYPE", entity.SEQUENCE_TYPE));
            paras.Add(new SqlParameter("@SEQUENCE_CODE", entity.SEQUENCE_CODE));
            paras.Add(new SqlParameter("@SEQUENCE_NO", entity.SEQUENCE_NO));
            paras.Add(new SqlParameter("@MEMO", entity.MEMO));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }

        public int UpdateSYS_SEQUENCE(dsSYS_SEQUENCE entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;
            string sqlUpdate = string.Empty;
            //update only MEMO, 
            sqlCmd = @"UPDATE SYS_SEQUENCE SET MEMO = @MEMO WHERE SEQUENCE_TYPE = @SEQUENCE_TYPE AND SEQUENCE_CODE = @SEQUENCE_CODE ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@SEQUENCE_TYPE", entity.SEQUENCE_TYPE));
            paras.Add(new SqlParameter("@SEQUENCE_CODE", entity.SEQUENCE_CODE));
            paras.Add(new SqlParameter("@SEQUENCE_CODE", entity.MEMO));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }

        public int DeleteSYS_SEQUENCE(dsSYS_SEQUENCE entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;
            string sqlUpdate = string.Empty;

            sqlCmd = @"DELETE SYS_SEQUENCE WHERE SEQUENCE_TYPE = @SEQUENCE_TYPE AND SEQUENCE_CODE = @SEQUENCE_CODE ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@SEQUENCE_TYPE", entity.SEQUENCE_TYPE));
            paras.Add(new SqlParameter("@SEQUENCE_CODE", entity.SEQUENCE_CODE));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }
    }
}
