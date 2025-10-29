using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AGVMSModel
{
    [Serializable]
    public class AGVTaskModel
    {
        [Key]
        [DisplayName("Execute Seq")]
        public string EXECUTE_SEQ { get; set; } //上位電腦執行序號, 與**自動倉儲管理機相關

        [DisplayName("Execute Sort")]
        public decimal EXECUTE_SORT { get; set; } //上位電腦執行順序

        [DisplayName("Execute Status")]
        public string EXECUTE_STATUS { get; set; } //上位電腦全皆段執行狀態(包含AGV、自動倉儲)

        [DisplayName("AGV From ST")]
        public string AGV_FROM_ST { get; set; } //AGV FROM ST

        //[DisplayName("AGV From ST Name")]
        //public string AGV_FROM_PLACE { get; set; } //AGV FROM ST名稱

        [DisplayName("AGV To ST")]
        public string AGV_TO_ST { get; set; } //AGV TO ST

        //[DisplayName("AGV To ST Name")]
        //public string AGV_TO_PLACE { get; set; } //AGV TO ST名稱

        [DisplayName("In/Out")]
        public string INOUT_FLAG { get; set; } //入/出庫標示符, 與**自動倉儲管理機相關

        [DisplayName("Item No")]
        public string ITEM_NO { get; set; } //品號, 與**自動倉儲管理機相關

        [DisplayName("RFID No")]
        public string RFID_NO { get; set; } //RFID_NO, 與**自動倉儲管理機相關
            
        [DisplayName("cmd trans to autostock")]
        public string CMD_TRANS_TO_AUTOSTOCK_FLAG { get; set; } //指令傳送給AUTOSTOCK管理機標示符

        [DisplayName("autostock cmd exec status")]
        public string CMD_AUTOSTOCK_EXECUTING_STATUS { get; set; } //指令傳送給AGV控制盤執行流程階段

        [DisplayName("cmd trans to autostock time")]
        public string CMD_AUTOSTOCK_TRANS_DATETIME { get; set; } //指令傳送給AUTOSTOCK管理機日期時間

        [DisplayName("cmd autostock priority")]
        public string CMD_AUTOSTOCK_PRIORITY { get; set; }

        [DisplayName("cmd trans to AGV")]
        public string CMD_TRANS_TO_AGV_FLAG { get; set; } //指令傳送給AGV控制盤標示符

        [DisplayName("AGV cmd exec status")]
        public string CMD_AGV_EXECUTING_STATUS { get; set; } //指令傳送給AGV控制盤執行流程階段

        [DisplayName("cmd trans to AGV time")]
        public string CMD_AGV_TRANS_DATETIME { get; set; } //指令傳送給AGV控制盤日期時間

        [DisplayName("cmd AGV priority")] //確定AGV/autostock執行優先順序, 入庫:AGV=1, autostock=2; 出庫：Autostock=1, AGV=2
        public string CMD_AGV_PRIORITY { get; set; }

        [Browsable(false)]
        [DisplayName("Trans No")]
        public string TRANS_NO { get; set; } //傳輸指令編號, 與**自動倉儲管理機相關

        [Browsable(false)]
        [DisplayName("AGV Move ID")]
        public string AGV_MOVE_ID { get; set; } //AGV搬運ID     

        [Browsable(false)]
        [DisplayName("Stock No")]
        public string STOCK_NO { get; set; } //品號, 與**自動倉儲管理機相關

        [Browsable(false)]
        [DisplayName("Shelf No")]
        public string SHELF_NO { get; set; } //儲位, 與**自動倉儲管理機相關

        [Browsable(false)]
        [DisplayName("Autostock Mode")]
        public string AUTOSTOCK_MODE { get; set; } //自動倉儲ST5模式, 與**自動倉儲管理機相關

        [Browsable(false)]
        [DisplayName("Error Code")]
        public string ERROR_CODE { get; set; } //錯誤代碼, 與**自動倉儲管理機相關

        [DisplayName("Priority Area")]
        public string PRIORITY_AREA { get; set; } //優先區域, 與**自動倉儲管理機相關             

        [Browsable(false)]
        [DisplayName("Status")]
        public string STATUS { get; set; }//上位電腦與自動管理機的執行狀態, 與**自動倉儲管理機相關

        [DisplayName("Create DateTime")]
        public string CREATE_DATETIME { get; set; } //上位電腦執行序號建立日期時間

        // public List<MOVE_ID_DIGIT_HEX> liMoveID { get; set; } //AGV搬運ID list, 寫入AGV監控盤用

    }

   
}
