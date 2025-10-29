using System;

namespace AGVMSModel
{
    [Serializable]
    public class dsAutoStockTransferJsonModel
    {
        public string EXECUTE_SEQ { get; set; }
        public string TRANS_NO { get; set; }
        public string ITEM_NO { get; set; }
        public string STOCK_NO { get; set; }
        public string SHELF_NO { get; set; }
        public string INOUT_FLAG { get; set; }
        public string STATUS { get; set; }
        public string AUTOSTOCK_MODE { get; set; }
        public string RFID_NO { get; set; }
        public string ERROR_CODE { get; set; }
        public string PRIORITY_AREA { get; set; }
        public string RFID_REWRITE_RESPONSE_STATUS { get; set; } //0:未動作(或空), 1:變更Item_NO, 2:變更RFID_No, 3:由HMI修改RFID_NO
        public string checkPoint { get; set; }
    }

    [Serializable]
    public class dsTaskAddTrans
    {
        public string INOUT_FLAG { get; set; }
        public string ITEM_NO { get; set; }
        public string FROM_ST { get; set; }
        public string TO_ST { get; set; }
        public string PRIORITY_AREA { get; set; }


    }
}
