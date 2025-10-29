using AGVMSModel;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AGVMSDataAccess
{
    public class DaoBuffer
    {
        public DataTable QueryBUFFER_DATA(dsBUFFER_DATA entity)
        {
            string sqlCmd = @" SELECT BUFFER_NO, STATION_NO, ITEM_NO FROM BUFFER_DATA WHERE 1=1 ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            if (!string.IsNullOrEmpty(entity.BUFFER_NO))
            {
                sqlCmd += " AND BUFFER_NO = @BUFFER_NO ";
                paras.Add(new SqlParameter("@BUFFER_NO", entity.BUFFER_NO));
            }
            if (!string.IsNullOrEmpty(entity.STATION_NO))
            {
                sqlCmd += " AND STATION_NO = @STATION_NO ";
                paras.Add(new SqlParameter("@STATION_NO", entity.STATION_NO));
            }
            if (!string.IsNullOrEmpty(entity.ITEM_NO))
            {
                sqlCmd += " AND ITEM_NO = @ITEM_NO ";
                paras.Add(new SqlParameter("@ITEM_NO", entity.ITEM_NO));
            }

            sqlCmd += @" ORDER BY BUFFER_NO ";

            return DBconn.DB.GetDataTable(sqlCmd, CommandType.Text, paras.ToArray());
        }

        public int InsertBUFFER_DATA(dsBUFFER_DATA entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;

            sqlCmd = @"INSERT INTO BUFFER_DATA (BUFFER_NO, STATION_NO, ITEM_NO)
                        VALUES 
                        ( @BUFFER_NO, @STATION_NO, @ITEM_NO) ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@BUFFER_NO", entity.BUFFER_NO));
            paras.Add(new SqlParameter("@STATION_NO", entity.STATION_NO));
            paras.Add(new SqlParameter("@ITEM_NO", entity.ITEM_NO));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }

        public int UpdateBUFFER_DATA(dsBUFFER_DATA entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;
            string sqlUpdate = string.Empty;

            sqlCmd = @"UPDATE BUFFER_DATA SET ITEM_NO = @ITEM_NO  WHERE STATION_NO = @STATION_NO ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@STATION_NO", entity.STATION_NO));
            paras.Add(new SqlParameter("@ITEM_NO", entity.ITEM_NO));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }

        public int DeleteBUFFER_DATA(dsBUFFER_DATA entity)
        {
            List<object> arrParas = new List<object>();
            List<string> arrSql = new List<string>();

            string sqlCmd = string.Empty;
            string sqlUpdate = string.Empty;

            sqlCmd = @"DELETE BUFFER_DATA WHERE BUFFER_NO = @BUFFER_NO ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            paras.Add(new SqlParameter("@BUFFER_NO", entity.BUFFER_NO));

            arrSql.Add(sqlCmd);
            arrParas.Add(paras);

            return DBconn.DB.BatchExecuteSQL(arrSql, CommandType.Text, arrParas);
        }
    }
}
