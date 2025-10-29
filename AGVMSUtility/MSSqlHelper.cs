using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using AGVMSModel;

namespace AGVMSUtility
{
  public  class MSSqlHelper
    {
        protected string _ConString = null;
        SqlConnection _ConDB = null;

        /// <summary>
        /// 不需參數的建構式, 但需要在Configuration中有連線字串設定
        /// </summary>
        /// <param name="constr">連線字串</param>
        public MSSqlHelper(string constr)
        {
            _ConString = constr;
        }

        /// <summary>
        /// 需連線字串參數的建構式
        /// </summary>
        /// <param name="datasource">Host IP or (local)\MSSQLSERVER</param>
        /// <param name="database">Database Name</param>
        /// <param name="account">DB Account ID</param>
        /// <param name="pwd">DB Account Password</param>
        public MSSqlHelper(string datasource, string account)
        {
            _ConString = datasource + account;
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~MSSqlHelper()
        {
            Close();
            Dispose();
        }

        /// <summary>
        ///  開啟資料庫連線
        /// </summary>
        protected void Open()
        {
            if (_ConDB == null)
            {
                _ConDB = new SqlConnection(_ConString);
            }
            if (_ConDB.State != ConnectionState.Open)
            {
                _ConDB.Open();
            }
        }

        /// <summary>
        /// 關閉資料庫連線
        /// </summary>
        public void Close()
        {
            if (_ConDB != null)
            {
                if (_ConDB.State != ConnectionState.Closed)
                {
                    try
                    {
                        _ConDB.Close();
                    }
                    catch
                    {
                        //
                    }
                }
            }
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            //throw new NotImplementedException();
            if (_ConDB != null)
            {
                _ConDB.Dispose();
                _ConDB = null;
            }
        }
            
        /// <summary>
        /// 批次執行insert, update, and delete等SQL語句
        /// </summary>
        /// <param name="arrsqlstring"></param>
        /// <param name="commandType"></param>
        /// <param name="arrparas"></param>
        /// <returns></returns>
        public int BatchExecuteSQL(List<string> arrsqlstring, CommandType commandType, List<object> arrparas)
        {
            try
            {
                int Count = 0;
                Open();
                using (var transaction = _ConDB.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < arrparas.Count; i++)
                        {
                            List<SqlParameter> paras = (List<SqlParameter>)arrparas[i];

                            SqlCommand cmd = new SqlCommand(arrsqlstring[i].ToString(), _ConDB);
                            cmd.CommandType = commandType;
                            if (paras != null)
                            {
                                cmd.Parameters.AddRange(paras.ToArray());
                            }
                            cmd.Transaction = transaction;
                            Count += cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }

                }

                return Count;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Close();
            }
        }
             
        /// <summary>
        /// 獲取資料轉存成DataTable
        /// </summary>
        /// <param name="sqlString">SQL字串</param>
        /// <param name="commandType">SQL字串是StoreProcedure/Table/SQL文字</param>
        /// <param name="parameters">SQL參數</param>
        /// <returns>執行成功後，回傳DataTable，否則回傳空值</returns>
        public DataTable GetDataTable(string sqlString, CommandType commandType, SqlParameter[] parameters)
        {
            try
            {
                Open();
                SqlDataAdapter adapter = new SqlDataAdapter(sqlString, _ConDB);
                adapter.SelectCommand.CommandType = commandType;
                if (parameters != null)
                {
                    adapter.SelectCommand.Parameters.AddRange(parameters);
                }
                DataTable dtData = new DataTable();
                adapter.Fill(dtData);

                return dtData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Close();
            }
        }

        /// <summary>
        /// 獲取資料轉存成DataReader
        /// </summary>
        /// <param name="sqlString">SQL字串</param>
        /// <param name="commandType">SQL字串是StoreProcedure/Table/SQL文字</param>
        /// <param name="parameters">SQL參數</param>
        /// <returns>執行成功後，回傳DataReader，否則回傳空值</returns>
        public IDataReader GetDataReader(string sqlString, CommandType commandType, SqlParameter[] parameters)
        {
            try
            {
                Open();
                SqlCommand cmd = new SqlCommand(sqlString, _ConDB);
                cmd.CommandType = commandType;

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                // 如果關閉DataReader, 則關連的Connection也會關閉
                // IDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                IDataReader reader = cmd.ExecuteReader();

                return reader;
            }
            catch (Exception ex)
            {
                throw ex;
            }      
        }
              
        /// <summary>
        /// 獲取資料轉存成實體集合
        /// </summary>
        /// <param name="sqlString">SQL字串</param>
        /// <param name="commandType">SQL字串是StoreProcedure/Table/SQL文字</param>
        /// <param name="parameters">SQL參數</param>
        /// <returns>執行成功後，回傳實體集合，否則回傳空值</returns>
        public IList<T> GetEntityList<T>(string sqlString, CommandType commandType, SqlParameter[] parameters)
        {
            try
            {
                IDataReader reader = GetDataReader(sqlString, commandType, parameters);
                IList<T> list = ReaderToList<T>(reader);
                reader.Close();

                return list;
            }
            catch (Exception ex)
            {
                throw ex;
                // return null;
            }           
        }
            
        /// <summary>
        /// DataReader 轉存成實體集合
        /// </summary>
        /// <typeparam name="T">實體類型</typeparam>
        /// <param name="dr">IDataReader介面</param>
        /// <returns>實體類型集合</returns>
        public static IList<T> ReaderToList<T>(IDataReader drData)
        {
            using (drData)
            {
                List<T> list = new List<T>();
                Type modelType = typeof(T);
                int count = drData.FieldCount;
                while (drData.Read())
                {
                    T model = Activator.CreateInstance<T>();
                    for (int i = 0; i < count; i++)
                    {
                        if (!IsNullOrDBNull(drData[i]))
                        {
                            PropertyInfo pi = modelType.GetProperty(GetPropertyName(drData.GetName(i)), BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            if (pi != null)
                            {
                                pi.SetValue(model, HackType(drData[i], pi.PropertyType), null);
                            }
                        }
                    }
                    list.Add(model);
                }
                return list;
            }
        }

        /// <summary>
        /// 物件是否為空?
        /// </summary>
        /// <param name="obj">待判斷的物件</param>
        /// <returns>是否為空?</returns>
        private static bool IsNullOrDBNull(object obj)
        {
            return (obj == null || (obj is DBNull)) ? true : false;
        }

        /// <summary>
        /// 資料庫類型轉換成C#類型
        /// </summary>
        /// <param name="value">待轉換的資料庫類型物件</param>
        /// <param name="conversionType">物件類型</param>
        /// <returns>轉換後的C#物件</returns>
        private static object HackType(object value, Type conversionType)
        {
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                    return null;
                System.ComponentModel.NullableConverter nullableConverter = new System.ComponentModel.NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            }
            return Convert.ChangeType(value, conversionType);
        }

        /// <summary>
        /// 取得DB欄位對應的屬性
        /// </summary>
        /// <param name="column">DB欄位</param>
        /// <returns>屬性</returns>
        private static string GetPropertyName(string column)
        {
            //column = column.ToLower();
            //string[] narr = column.Split('_');
            //column = "";
            //for (int i = 0; i < narr.Length; i++)
            //{
            //    if (narr[i].Length > 1)
            //    {
            //        column += narr[i].Substring(0, 1).ToUpper() + narr[i].Substring(1);
            //    }
            //    else
            //    {
            //        column += narr[i].Substring(0, 1).ToUpper();
            //    }
            //}
            return column;
        }

        //public DataTable ExecuteFunction(SP_FUN_Model entity)
        //{
        //    DataTable dtResult = new DataTable();

        //    if (entity != null)
        //    {
        //        try
        //        {
        //            Open();
        //            SqlCommand cmd = new SqlCommand(entity.SP_FUN_NAME, _ConDB);

        //            if (entity.INPUT_PARAM.Rows.Count > 0)
        //            {
        //                for (int i = 0; i < entity.INPUT_PARAM.Rows.Count; i++)
        //                {
        //                    cmd.Parameters.AddWithValue();
        //                }
        //                adapter.SelectCommand.Parameters.AddRange(parameters);
        //            }
        //            SqlDataAdapter adapter = new SqlDataAdapter();

        //            DataTable dtData = new DataTable();
        //            adapter.Fill(dtData);

        //            return dtData;
        //        }
        //        catch (Exception ex)
        //        {
        //            Close();
        //            //throw ex.ToString();
        //            throw ex.GetBaseException();
        //        }
        //        finally
        //        {
        //            Close();
        //        }
        //    }
        //    else
        //    {
        //        return dtResult;
        //    }
        //}

        public DataTable ExecuteStoredProcedure(SP_FUN_Model entity)
        {
            DataTable dtResult = new DataTable();

            if (entity != null)
            {
                try
                {
                    Open();

                    SqlCommand cmd = new SqlCommand(entity.SP_FUN_NAME, _ConDB);

                    if (entity.IS_SP)
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                    }

                    SqlParameter paramItem = new SqlParameter();//cmd.Parameters.AddWithValue(inputName, inputParam);

                    if (!string.IsNullOrWhiteSpace(entity.INPUT_NAME) && entity.INPUT_PARAM.Rows.Count > 0)
                    {
                        paramItem.ParameterName = entity.INPUT_NAME; //SP需帶入參數名稱
                        paramItem.SqlDbType = SqlDbType.Structured;
                        paramItem.Value = entity.INPUT_PARAM; //SP需帶入參數資料
                    }

                    cmd.Parameters.Add(paramItem);

                    if (entity.IS_OUTPUT)
                    {
                        var reader = cmd.ExecuteReader();
                        dtResult.Load(reader);
                    }
                    else
                    {
                        int count = cmd.ExecuteNonQuery();
                    }

                    return dtResult;
                }
                catch (Exception ex)
                {
                    Close();
                    //throw ex.ToString();
                    throw ex.GetBaseException();
                }
                finally
                {
                    Close();
                }
            }
            else
            {
                return dtResult;
            }
        }
             
    }
}
