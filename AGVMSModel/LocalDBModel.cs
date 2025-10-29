using System;
using System.Collections.Generic;
using System.Data;

namespace AGVMSModel
{
   
    public class LocalDBModel
    {

    }
      

    [Serializable]
    public class dsCODE_DETAIL
    {
        public string CODE_NO { get; set; }
        public string PARA_1 { get; set; }
        public string PARA_2 { get; set; }
        public string PARA_3 { get; set; }
        public string STATION { get; set; }
        public string MEMO { get; set; }
        public string PARA_NAME { get; set; }
        public string PARA_NAME_ENG { get; set; }
        public string PARA_NAME_LOCAL { get; set; }
        public string PARA_NAME_OTHER { get; set; }
        public int SORT { get; set; }

    }       

    [Serializable]
    public class dsLOG_MESSAGE
    {
        public int SYS_ID { get; set; }
        public DateTime? CREATE_DATETIME { get; set; }
        public string LOG_MESSAGE { get; set; }
    }

    [Serializable]
    public class dsSYS_SEQUENCE
    {
        public string SEQUENCE_TYPE { get; set; }
        public string SEQUENCE_CODE { get; set; }
        public int SEQUENCE_NO { get; set; }

        public string MEMO { get; set; }
    }

    [Serializable]
    public class dsLOGIN_USER
    {
        public string SYS_ID { get; set; }
        public string LOGIN_ID { get; set; }
        public string LOGIN_PASSWORD { get; set; }
        public string LOGIN_NAME { get; set; }
        public string LOGIN_TYPE { get; set; }
        public string MEMO { get; set; }
    }

    [Serializable]
    public class SP_FUN_Model
    {
        public string SP_FUN_NAME { get; set; }
        public string INPUT_NAME { get; set; } //SP 參數名稱(table)
        public DataTable INPUT_PARAM { get; set; } //SP 參數資料(table)
        public List<string> MULTI_INPUT_NAME { get; set; }//FUN 多參數名稱
        public List<string> MULTI_INPUT_PARAM { get; set; }//FUN 多參數資料
        public bool IS_OUTPUT { get; set; } //是否輸出
        public bool IS_SP { get; set; } //是否為SP

        public SP_FUN_Model()
        {
            IS_OUTPUT = true;
            IS_SP = true;
        }
    }
}
