using AGVMSModel;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AGVMSDataAccess
{
    public class DaoLoginUser
    {
        public DataTable QueryUSER_DATA(dsLOGIN_USER entity)
        {
            string sqlCmd = @" SELECT SYS_ID, LOGIN_ID, LOGIN_PASSWORD, LOGIN_NAME, LOGIN_TYPE, MEMO FROM LOGIN_USER WHERE 1=1 ";

            List<SqlParameter> paras = new List<SqlParameter>();
            paras.Clear();

            if (!string.IsNullOrEmpty(entity.LOGIN_ID))
            {
                sqlCmd += " AND LOGIN_ID = @LOGIN_ID ";
                paras.Add(new SqlParameter("@LOGIN_ID", entity.LOGIN_ID));
            }
            if (!string.IsNullOrEmpty(entity.LOGIN_PASSWORD))
            {
                sqlCmd += " AND LOGIN_PASSWORD = @LOGIN_PASSWORD ";
                paras.Add(new SqlParameter("@LOGIN_PASSWORD", entity.LOGIN_PASSWORD));
            }

            return DBconn.DB.GetDataTable(sqlCmd, CommandType.Text, paras.ToArray());
        }
    }
}
