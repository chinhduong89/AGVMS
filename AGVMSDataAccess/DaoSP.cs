using System.Data;
using AGVMSModel;

namespace AGVMSDataAccess
{
    public class DaoSP
    {
        public DataTable ExecStoredProcedure(SP_FUN_Model entity)
        {
            DataTable spResult = DBconn.DB.ExecuteStoredProcedure(entity);
            return spResult;
        }
    }
}
