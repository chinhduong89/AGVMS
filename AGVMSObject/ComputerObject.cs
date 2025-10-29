using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using AGVMSModel;
using AGVMSUtility;
using AGVMSDataAccess;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;

namespace AGVMSObject
{
    public class ComputerObject
    {
        private DataGridView dgvItem;
        //private List<AGVTaskModel> liComputer;
        private BindingList<BufferStatus> listBufferStatus;
        private BindingList<RotateStatus> listRotateStatus;
        //private List<BufferStatus> listBufferStatus;
        //private List<RotateStatus> listRotateStatus;
        //private List<DeviceInfoModel> listDeviceConnInfo;
        private DeviceInfoModel agvEntity;
        private DeviceInfoModel autostockEntity;
        private int AGVReadtimeCycle = 300;
        //private DaoBuffer daoBuffer = new DaoBuffer();

        BindingList<AGVTaskModel> liExecuteSource;
        //List<AGVTaskModel> liExecuteSource;
        int runningData = 0;
        AGVTaskModel executeItem;

        public delegate void DgvItemDataRefreshEventHandler();
        public DgvItemDataRefreshEventHandler dgvItemRefresh;

        public delegate void DgvItemDataRemoveEventHandler(AGVTaskModel removeItem);
        public DgvItemDataRemoveEventHandler dgvItemRowsRemoveAt;

        public delegate void TbxLogMessageEventHandler(dsLOG_MESSAGE entity);
        public TbxLogMessageEventHandler tbxLogAddMessageTrigger;

        public delegate void lblBufferSetArrangedTaskEventHandler(BufferStatus transData);
        public lblBufferSetArrangedTaskEventHandler lblBufferSetArrangedTaskTrigger;

        public delegate void lblBufferSetItemNoEventHandler(BufferStatus transData);
        public lblBufferSetItemNoEventHandler lblBufferItemNoTrigger;

        public delegate void RFIDCheckFormOpenEventHandler();
        public RFIDCheckFormOpenEventHandler RFIDCheckFormOpenTrigger;

        public delegate void AGVDisconnectEventHandler();
        public AGVDisconnectEventHandler AGVDisconnectTrigger;

        public delegate void MessageBoxEventHandler(string msg, AGVTaskModel item);
        public MessageBoxEventHandler MessageBoxTrigger;

        public ComputerObject()
        {

        }

        public void executeAGVTaskSetData(DeviceInfoModel _autostockEntity, DeviceInfoModel _agvEntity, DataGridView _dgvItem, BindingList<AGVTaskModel> _liComputer)
        {
            autostockEntity = _autostockEntity;
            agvEntity = _agvEntity;

            //liComputer = _liComputer;
            dgvItem = _dgvItem;
        }
        public void setBufferStatus(BindingList<BufferStatus> _listBufferStatus)
        {
            listBufferStatus = _listBufferStatus;
        }

        public void setRotateStatus(BindingList<RotateStatus> _listRotateStatus)
        {
            listRotateStatus = _listRotateStatus;
        }

        #region AGV life cycle cannot use, because use thread function
        //public virtual void LifeCycle()
        //{
        //    try
        //    {
        //        PLCObject agvObjectLifeCycle = new PLCObject();
        //        int DWordLifeCycle = 0; //D0

        //        while (true)
        //        {
        //            Thread.Sleep(1000);

        //            switch (agvEntity.dictAGVDword[DWordLifeCycle]) //D0
        //            {
        //                case (ushort)AGVNormalFeedback.OFF:
        //                    ModifyDWord(agvObjectLifeCycle, DWordLifeCycle, AGVNormalFeedback.ON);
        //                    break;

        //                case (ushort)AGVNormalFeedback.ON:
        //                    ModifyDWord(agvObjectLifeCycle, DWordLifeCycle, AGVNormalFeedback.OFF);
        //                    break;

        //                default:
        //                    break;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string issueMessage = ex.Message.ToString();
        //        throw;
        //    }
        //}
        #endregion

        /// <summary>
        /// 執行任務排程
        /// </summary>
        public void executeAGVTask()
        {
            bool runMasterTaskWhile = true;
            bool blAutostockAutoMode = false;
            bool blRFIDTagRewrite = false;
            bool blMoveIDWriteFinish = false;
            bool blAGVMoveID_1stCheckIssue = false;

            try
            {
                //While:Master
                while (runMasterTaskWhile)
                {
                    //Thread.Sleep(300);

                    //某些階段需要確認AGV及autostock是否連線及狀態
                    //確認AGV 是否連線(connected=true & D5108:1)&自動(D5109:1)模式且無異常(D51011:0)
                    //確認autostock 是否連線(connected=true), 是否為單獨(LIFT)或連線(TFR)模式

                    //AGV跟autostock 未連線, 不做任何事
                    if (!getAGVConnectStatus() && !getAutostockConnectStatus())
                    {
                        //Thread.Sleep(2000);
                        //var msg = string.Format("{0}：step 0-1:agv & autostock can not connected, please check it! \r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        //Console.WriteLine(msg);
                        continue;
                    }

                    //dgv no data
                    if (dgvItem.Rows.Count == 0)
                    {
                        //var msg = string.Format("{0}：step 0:dgv no data \r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        //Console.WriteLine(msg);
                        continue;
                    }

                    //dgv has data
                    Console.WriteLine("{0}：step 1:dgv has data \r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    executeItem = new AGVTaskModel();

                    //**啟動任務
                    //step 1:開始執行cmd, begin Execute cmd
                    //檢查執行清單中是否有資料, 如有資料則檢查是否有正在執行的任務, 有：繼續執行, 無：將第一筆資料變更為執行狀態
                    getExecuteCmd();

                    //step 2:判斷InOutFlag, 再接續執行該項目,
                    //**step 2-1:In(autostock入庫)：從焊接、Buffer站台取出, 放入自動動倉儲站台

                    //step 2-1-1:檢查AGV是否連線(可正常運作)：是
                    //step 2-1-1-1:檢查是否為大轉盤的站台：是：
                    //step 2-1-1-1-1:檢查buffer站台是否有空位置:是：執行AGV移動將治具放到buffer站台
                    //step 2-1-1-1-2:檢查buffer站台是否有空位置:否：將指令取消
                    //step 2-1-1-2:檢查是否為大轉盤的站台：否：執行發給自動倉儲入庫指令, 再執行AGV移動

                    //step 2-1-2:檢查AGV是否連線(可正常運作)：否
                    //step 2-1-2-1:不呼叫AGV, 只執行發給自動倉儲的入庫指令

                    //**step 2-2:Out(autostock出庫)：從自動動倉儲、Buffer站台取出, 放入焊接站台
                    //step 2-2-1:檢查是否為大轉盤:是
                    //step 2-2-1-1:檢查大轉盤是否有空位置:否, 取消指令
                    //step 2-2-1-2:檢查大轉盤是否有空位置:是
                    //step 2-2-1-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                    //step 2-2-1-2-2:檢查buffer站台找尋Item NO: 無Item No:
                    //step 2-2-1-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                    //step 2-2-1-2-2-2:執行自動倉儲出庫指令, 檢查是否有Item No:無Item No:須記錄log並取消cmd
                    //step 2-2-2:檢查是否為大轉盤:否 (以下同2-2-1-2指令)
                    //step 2-2-2-1:檢查是否為Buffer站台：是
                    //step 2-2-2-1-1:檢查buffer站台是否有空位置：否, 取消指令
                    //step 2-2-2-1-2:檢查buffer站台是否有空位置：是
                    //step 2-2-2-1-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                    //step 2-2-2-1-2-2:檢查buffer站台找尋Item NO: 無Item No:
                    //step 2-2-2-1-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                    //step 2-2-2-1-2-2-2:執行自動倉儲出庫指令, 檢查是否有Item No:無Item No:須記錄log並取消cmd
                    //step 2-2-2-2:檢查是否為Buffer區：否                
                    //step 2-2-2-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                    //step 2-2-2-2-2:檢查buffer站台找尋Item NO: 無Item No:
                    //step 2-2-2-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                    //step 2-2-2-2-2-2:執行自動倉儲出庫指令, 檢查是否有Item No:無Item No:須記錄log並取消cmd

                    //**step 2-3:Dispatch(調度)：執行AGV移動, 站台到站台, 不須執行自動倉儲指令

                    //**執行任務前置作業
                    if (runningData > 0 && executeItem != null
                       && executeItem.EXECUTE_STATUS == StatusFlag.Executing.ToString())
                    {
                        executeCmdOfInOutFlag();
                    }

                    //**執行AGV MoveID寫入
                    //AGV MOVE ID 寫入 - 這一階段由上位PC發起請求(Computer-Request), AGV監視盤回應(AGV-Response)
                    if (runningData > 0 && executeItem != null
                        && executeItem.EXECUTE_STATUS == StatusFlag.AGVing.ToString()
                        && !(executeItem.CMD_AGV_EXECUTING_STATUS == AGVExecutingStatusFlag.AGVOperationFinish.ToString() || executeItem.CMD_AGV_EXECUTING_STATUS == AGVExecutingStatusFlag.AGVOperationAbnormal.ToString())
                        && executeItem.CMD_TRANS_TO_AGV_FLAG == AGVNormalFeedback.OFF.ToString()
                        && executeItem.CMD_AGV_EXECUTING_STATUS == AGVExecutingStatusFlag.Start.ToString()
                        )
                    {
                        var msg = string.Empty;
                        DateTime dateTimeAGVMoveID = DateTime.Now;
                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();

                        //AGV 未連線, 不做任何事
                        if (!getAGVConnectStatus())
                        {
                            //Thread.Sleep(1000);

                            msg = string.Format("{0}：step AGVing-Move ID:AGV沒有連線(AGV disconnect), seq:{1} \r\n", dateTimeAGVMoveID.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            Console.WriteLine(msg);

                            //重新執行自動倉儲的工作
                            if (executeItem.INOUT_FLAG == InOutStockFlag.In.ToString()
                                && executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.WaitingAGV.ToString())
                            {
                                executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString();
                                dgvItemRefresh();

                                msg = string.Format("{0}：因AGV未連線, 換執行自動倉儲命令(入庫), Because the AGV is not connected, change execution of autostock command(in stock) , seq:{1} \r\n", dateTimeAGVMoveID.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());

                                entityMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                                entityMsg.LOG_MESSAGE = msg;

                                Console.WriteLine(msg);
                                tbxLogAddMessageTrigger(entityMsg);
                            }
                            //因AGV無法連線, 刪除指令
                            else if ((executeItem.INOUT_FLAG == InOutStockFlag.Out.ToString()
                                     && executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockOperationFinish.ToString()) || executeItem.INOUT_FLAG == InOutStockFlag.Dispatch.ToString())
                            {
                                //暫時先不刪
                                //bool killCmd = false;
                                ////DateTime dateTimeAutostocKillCmdkLog = DateTime.Now;

                                //msg = string.Format("因AGV無法連線, 且自動倉儲命令已完成, 刪除此命令");
                                //killCmd = CancelExecuteSEQ(msg); //msg, dateTimeAutostocKillCmdkLog

                                //if (killCmd)
                                //{
                                //    //do nothing 
                                //    //AutostockOperating = false;
                                //}

                                //blAutostockAutoMode = false;
                                //blRFIDTagRewrite = false;
                                //blMoveIDWriteFinish = false;
                            }
                        }
                        else
                        {
                            if (!blMoveIDWriteFinish)
                            {
                                MELSECAGVObject agvObjectCleanAllMoveID = new MELSECAGVObject();
                                //agv write move id 
                                agvObjectCleanAllMoveID.setLoopCleanMoveIDData(agvEntity);
                                agvObjectCleanAllMoveID.MoveIDLoopWriteData();

                                MELSECAGVObject agvObjectMoveID = new MELSECAGVObject();
                                //agvObjectMoveID.setMoveIDWriteData(agvEntity, executeItem);
                                //agvObjectMoveID.MoveIDWriteData();
                                agvObjectMoveID.setMoveIDLoopWriteData(agvEntity, executeItem);
                                agvObjectMoveID.MoveIDLoopWriteData();

                                ////上位PC
                                //int D72 = agvEntity.dictAGVDword[72]; //72;                          

                                blMoveIDWriteFinish = true;
                            }

                            executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.ON.ToString();
                            executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.TransCmd.ToString();
                            executeItem.CMD_AGV_TRANS_DATETIME = dateTimeAGVMoveID.ToString("yyyy/MM/dd HH:mm:ss");
                            dgvItemRefresh();

                            msg = string.Format("{0}：step AGVing-Move ID:狀態為AGV執行中, 先寫入AGV Move ID, The status is AGV executing, first write the AGV Move ID, seq:{1} \r\n", dateTimeAGVMoveID.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            entityMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                            entityMsg.LOG_MESSAGE = msg;
                            Console.WriteLine(msg);
                            tbxLogAddMessageTrigger(entityMsg);

                            bool runDetailTaskWhile = true;
                            bool IsFirstComeIn = true;
                            bool blSection1 = true;
                            bool blSection2 = true;
                            bool blSection3 = true;
                            bool IsClose = false;
                            bool IsNG = false;
                            //bool IsFinish = false;

                            //While:Detail Move ID flow
                            while (runDetailTaskWhile)
                            {
                                //Thread.Sleep(300);

                                if (!getAGVConnectStatus())
                                {
                                    //Thread.Sleep(1000);
                                    bool cannotFindExecuteSeqAGVMoveIDDetail = findExecuteSeq();

                                    //找不到執行序號, 有可能被刪除了
                                    if (cannotFindExecuteSeqAGVMoveIDDetail)
                                    {
                                        runDetailTaskWhile = false;
                                        blRFIDTagRewrite = false;
                                        blAutostockAutoMode = false;
                                        blMoveIDWriteFinish = false;
                                    }

                                    continue;
                                }

                                //string MoveIDForAGV_test = getAGVPLCMoveID(2);

                                //AGV
                                int D472_0_moveid = agvEntity.dictAGVDword[4720]; //4720;
                                int D472_1_moveid = agvEntity.dictAGVDword[4721]; //4721;
                                int D472_2_moveid = agvEntity.dictAGVDword[4722]; //4722;
                                int D510_8_moveid = agvEntity.dictAGVDword[5108]; //5108;
                                int D510_9_moveid = agvEntity.dictAGVDword[5109]; //5109;
                                int D510_11_moveid = agvEntity.dictAGVDword[51011]; //51011;

                                DateTime dateTimeLogDetailStage = DateTime.Now;
                                dsLOG_MESSAGE entityAGVMoveIDMsg = new dsLOG_MESSAGE();

                                MELSECAGVObject agvObjectMoveIDResponse = new MELSECAGVObject(agvEntity);

                                //D472_0 回應
                                if (blSection1 && D472_0_moveid == 1 && D472_1_moveid == 0 && D472_2_moveid == 0)
                                {
                                    agvObjectMoveIDResponse.setWriteData(72, 0);
                                    agvObjectMoveIDResponse.SingleWriteData();
                                    IsFirstComeIn = false;
                                    blSection1 = false;

                                    msg = string.Format("{0}：step AGVing-Move ID:D472_0={1}, D472_1={2}, D472_2={3}, D510_8={4}, D510_9={5}, D510_11={6}, 上位PC已回應, 等待AGV回覆D472_1或D472_2, AGVMS has reponseded, waiting for AGV to reply D472_1 or D472_2, seq:{7} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D472_0_moveid, D472_1_moveid, D472_2_moveid, D510_8_moveid, D510_9_moveid, D510_11_moveid, executeItem.EXECUTE_SEQ);

                                    entityMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                                    entityMsg.LOG_MESSAGE = msg;

                                    Console.WriteLine(msg);
                                    tbxLogAddMessageTrigger(entityMsg);
                                }
                                //OK:D472_1 回應 or D472_0 & D472_1 回應
                                else if ((blSection2 && D472_0_moveid == 0 && D472_1_moveid == 1 && D472_2_moveid == 0) || (blSection2 && D472_0_moveid == 1 && D472_1_moveid == 1 && D472_2_moveid == 0))
                                {
                                    agvObjectMoveIDResponse.setWriteData(72, 2);
                                    agvObjectMoveIDResponse.SingleWriteData();
                                    IsFirstComeIn = false;
                                    blSection2 = false;
                                    IsClose = true;

                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.MoveCmdTransOK.ToString();
                                    dgvItemRefresh();

                                    msg = string.Format("{0}：step AGVing-Move ID:D472_0={1}, D472_1={2}, D472_2={3}, D510_8={4}, D510_9={5}, D510_11={6}, 上位PC已回應 OK, AGVMS has reponseded OK, seq:{7} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D472_0_moveid, D472_1_moveid, D472_2_moveid, D510_8_moveid, D510_9_moveid, D510_11_moveid, executeItem.EXECUTE_SEQ);

                                    entityMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                                    entityMsg.LOG_MESSAGE = msg;

                                    Console.WriteLine(msg);
                                    tbxLogAddMessageTrigger(entityMsg);
                                }
                                //NG:D472_2 回應 or D472_0 & D472_2 回應
                                else if ((blSection3 && D472_0_moveid == 0 && D472_1_moveid == 0 && D472_2_moveid == 1) || (blSection3 && D472_0_moveid == 1 && D472_1_moveid == 0 && D472_2_moveid == 1))
                                {
                                    agvObjectMoveIDResponse.setWriteData(72, 4);
                                    agvObjectMoveIDResponse.SingleWriteData();
                                    IsFirstComeIn = false;
                                    blSection3 = false;
                                    IsNG = true;
                                    IsClose = true;

                                    msg = string.Format("{0}：step AGVing-Move ID:D472_0={1}, D472_1={2}, D472_2={3}, D510_8={4}, D510_9={5}, D510_11={6}, 上位PC已回應 NG, AGVMS has reponseded NG, seq:{7} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D472_0_moveid, D472_1_moveid, D472_2_moveid, D510_8_moveid, D510_9_moveid, D510_11_moveid, executeItem.EXECUTE_SEQ);

                                    entityMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                                    entityMsg.LOG_MESSAGE = msg;

                                    Console.WriteLine(msg);
                                    tbxLogAddMessageTrigger(entityMsg);
                                }
                                //在非第一次進入及move id作業結束, 
                                else if (D472_0_moveid == 0 && D472_1_moveid == 0 && D472_2_moveid == 0)
                                {
                                    if (!IsFirstComeIn && IsClose && (blSection2 || blSection3))
                                    {
                                        //Thread.Sleep(10000);
                                        //string MoveIDForAGV = getAGVPLCMoveID(2);

                                        MELSECAGVObject agvObjectCleanMoveID = new MELSECAGVObject();
                                        //clean agv move id                                     
                                        //agvObjectCleanMoveID.setCleanMoveIdData(agvEntity);
                                        //agvObjectCleanMoveID.MoveIDWriteData();
                                        //agvObjectCleanMoveID.setLoopCleanMoveIDData(agvEntity);
                                        //agvObjectCleanMoveID.MoveIDLoopWriteData();

                                        if (IsNG)
                                        {
                                            agvObjectMoveIDResponse.setWriteData(72, 0);
                                            agvObjectMoveIDResponse.SingleWriteData();

                                            msg = string.Format("{0}：step AGVing-Move ID:Move ID寫入失敗, 重新執行, Move id write fail, will re-executing, seq:{1} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                            entityAGVMoveIDMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                                            entityAGVMoveIDMsg.LOG_MESSAGE = msg;

                                            Console.WriteLine(msg);

                                            tbxLogAddMessageTrigger(entityAGVMoveIDMsg);

                                            runDetailTaskWhile = false;
                                            blMoveIDWriteFinish = false;

                                            MELSECAGVObject agvObjectCleanAllMoveID_NG = new MELSECAGVObject();
                                            //agv write move id 
                                            agvObjectCleanAllMoveID_NG.setLoopCleanMoveIDData(agvEntity);
                                            agvObjectCleanAllMoveID_NG.MoveIDLoopWriteData();

                                            //NG：重新執行
                                            //executeItem.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                                            //executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                            //Autostock
                                            //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                            //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                            //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                            //AGV
                                            //executeItem.CMD_AGV_PRIORITY = "1";
                                            executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                            executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AGV_TRANS_DATETIME

                                            dgvItemRefresh();
                                        }
                                        else
                                        {
                                            agvObjectMoveIDResponse.setWriteData(72, 0);
                                            agvObjectMoveIDResponse.SingleWriteData();

                                            executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.WaitAGVMove.ToString();
                                            dgvItemRefresh();

                                            msg = string.Format("{0}：step AGVing-Move ID:OK, seq:{1} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                            entityAGVMoveIDMsg.CREATE_DATETIME = dateTimeAGVMoveID;
                                            entityAGVMoveIDMsg.LOG_MESSAGE = msg;

                                            Console.WriteLine(msg);
                                            tbxLogAddMessageTrigger(entityAGVMoveIDMsg);

                                            runDetailTaskWhile = false;
                                            blMoveIDWriteFinish = false;

                                            MELSECAGVObject agvObjectCleanAllMoveID_OK = new MELSECAGVObject();
                                            //agv write move id 
                                            agvObjectCleanAllMoveID_OK.setLoopCleanMoveIDData(agvEntity);
                                            agvObjectCleanAllMoveID_OK.MoveIDLoopWriteData();
                                        }
                                    }
                                }

                                bool cannotFindExecuteSeql = findExecuteSeq();

                                //找不到執行序號, 有可能被刪除了
                                if (cannotFindExecuteSeql)
                                {
                                    runDetailTaskWhile = false;
                                    blRFIDTagRewrite = false;
                                    blAutostockAutoMode = false;
                                    blMoveIDWriteFinish = false;
                                }
                            }
                        }
                    }

                    //**執行AGV移動(AGV 配車)
                    ////配車 -> From ST -> 夾具拾取/完了 -> To ST -> 搬送完了
                    if (runningData > 0 && executeItem != null
                        && executeItem.EXECUTE_STATUS == StatusFlag.AGVing.ToString()
                        && !(executeItem.CMD_AGV_EXECUTING_STATUS == AGVExecutingStatusFlag.AGVOperationFinish.ToString() || executeItem.CMD_AGV_EXECUTING_STATUS == AGVExecutingStatusFlag.AGVOperationAbnormal.ToString())
                        && executeItem.CMD_TRANS_TO_AGV_FLAG == AGVNormalFeedback.ON.ToString()
                        && executeItem.CMD_AGV_EXECUTING_STATUS == AGVExecutingStatusFlag.WaitAGVMove.ToString()
                        )
                    {
                        var msg = string.Empty;
                        DateTime dateTimeAGVMoving = DateTime.Now;
                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                        bool IsAGVPLCMoveIDEqualAGVMMoveID = false;

                        bool cannotFindExecuteSeqAGVMoveingl = findExecuteSeq();

                        //找不到執行序號, 有可能被刪除了
                        if (cannotFindExecuteSeqAGVMoveingl)
                        {
                            //runningDetailTaskWhile = false;
                            blRFIDTagRewrite = false;
                            blAutostockAutoMode = false;
                            blMoveIDWriteFinish = false;
                            blAGVMoveID_1stCheckIssue = false;
                        }

                        //AGV 未連線, 不做任何事
                        if (!getAGVConnectStatus())
                        {
                            //Thread.Sleep(1000);
                            msg = string.Format("{0}：step AGVing-Moving:AGV沒有連線, AGV disconnected, seq:{1} \r\n", dateTimeAGVMoving.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            Console.WriteLine(msg);
                            continue;
                        }

                        //檢查執行序號與AGV PLC Move ID是否一樣, 目前只有一台AGV No:1
                        string MoveIDForAGV_1stCheck = getAGVPLCMoveID(1); //check D476, official

                        int D510_8_1st = agvEntity.dictAGVDword[5108]; //5108;
                        int D510_9_1st = agvEntity.dictAGVDword[5109]; //5109;
                        int D510_11_1st = agvEntity.dictAGVDword[51011]; //51011;
                        int D476_1st = agvEntity.dictAGVDword[476]; //476; //STATUS MSG

                        if (MoveIDForAGV_1stCheck.Equals(executeItem.EXECUTE_SEQ) && !IsAGVPLCMoveIDEqualAGVMMoveID)
                        {
                            IsAGVPLCMoveIDEqualAGVMMoveID = true;

                            dsLOG_MESSAGE entityMsg_1stCheckMoveID_Pass = new dsLOG_MESSAGE();

                            string msg_1stCheckMoveID_Pass = string.Format("{0}：AGVing-Moving:1st檢查執行序號與AGV PLC Move ID是否一樣(1st check whether the execute seq is the same as the AGV PLC Move ID):PASS, D510_8={1}, D510_9={2}, D510_11={3}, D476={4}, seq:{5} \r\n", dateTimeAGVMoving.ToString("yyyy-MM-dd HH:mm:ss"), D510_8_1st, D510_9_1st, D510_11_1st, D476_1st, executeItem.EXECUTE_SEQ);
                            entityMsg_1stCheckMoveID_Pass.CREATE_DATETIME = dateTimeAGVMoving;
                            entityMsg_1stCheckMoveID_Pass.LOG_MESSAGE = msg_1stCheckMoveID_Pass;

                            Console.WriteLine(msg_1stCheckMoveID_Pass);
                            tbxLogAddMessageTrigger(entityMsg_1stCheckMoveID_Pass);
                        }

                        if (!IsAGVPLCMoveIDEqualAGVMMoveID && !blAGVMoveID_1stCheckIssue)
                        {
                            dsLOG_MESSAGE entityMsg_1stCheckMoveID_NG = new dsLOG_MESSAGE();

                            string msg_1stCheckMoveID_NG = string.Format("{0}：AGVing-Moving:1st檢查執行序號與AGV PLC Move ID是否一樣(1st check whether the execute seq is the same as the AGV PLC Move ID):FAIL, AGV PLC Move ID:{1}, D510_8={2}, D510_9={3}, D510_11={4}, D476={5}, seq:{6} \r\n", dateTimeAGVMoving.ToString("yyyy-MM-dd HH:mm:ss"), MoveIDForAGV_1stCheck, D510_8_1st, D510_9_1st, D510_11_1st, D476_1st, executeItem.EXECUTE_SEQ);
                            entityMsg_1stCheckMoveID_NG.CREATE_DATETIME = dateTimeAGVMoving;
                            entityMsg_1stCheckMoveID_NG.LOG_MESSAGE = msg_1stCheckMoveID_NG;

                            Console.WriteLine(msg_1stCheckMoveID_NG);
                            tbxLogAddMessageTrigger(entityMsg_1stCheckMoveID_NG);
                            //blAGVMoveID_1stCheckIssue = true;

                            Thread.Sleep(5000);
                            continue;
                        }

                        string strMoveID = string.Empty;

                        ////上位PC
                        //int D110 = agvEntity.dictAGVDword[110]; //110;

                        msg = string.Format("{0}：AGVing-Moving:狀態為AGV 開始搬運中, The status is AGV start moving, seq:{1} \r\n", dateTimeAGVMoving.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                        entityMsg.CREATE_DATETIME = dateTimeAGVMoving;
                        entityMsg.LOG_MESSAGE = msg;

                        Console.WriteLine(msg);
                        tbxLogAddMessageTrigger(entityMsg);

                        MELSECAGVObject agvObjectMoving = new MELSECAGVObject();
                        bool runningDetailTaskWhile = true;
                        bool blMovingSection1 = true;
                        bool blMovingSection2 = true;
                        bool blMovingSection3 = true;
                        bool blMovingSection4 = true;
                        bool blMovingSection5 = true;
                        bool blMovingSection6 = true;
                        bool blMovingSection7 = true;
                        bool blMovingSection8 = true;
                        bool blMovingSection9 = true;
                        bool blMovingSection10 = true;
                        bool blMovingSection11 = true;
                        bool blMovingSection12 = true;
                        bool IsClose = false;
                        bool IsNG = false;
                        bool IsFinish = false;

                        //bool IsFeedbackAGV = false;

                        bool IsAGVPLCMoveIDEqualAGVMMoveID_2nd = false;
                        bool blCheck_2nd = false;

                        while (runningDetailTaskWhile)
                        {
                            //Thread.Sleep(1000);

                            bool cannotFindExecuteSeqAGVMoveingDetail = findExecuteSeq();

                            //找不到執行序號, 有可能被刪除了
                            if (cannotFindExecuteSeqAGVMoveingDetail)
                            {
                                runningDetailTaskWhile = false;
                                blRFIDTagRewrite = false;
                                blAutostockAutoMode = false;
                                blMoveIDWriteFinish = false;
                                blAGVMoveID_1stCheckIssue = false;
                            }

                            if (!getAGVConnectStatus())
                            {
                                //Thread.Sleep(1000);
                                continue;
                            }

                            //AGV 
                            int D510_0 = agvEntity.dictAGVDword[5100]; //5100; //AGV REQUEST
                            int D510_8_2nd = agvEntity.dictAGVDword[5108]; //5108;
                            int D510_9_2nd = agvEntity.dictAGVDword[5109]; //5109;
                            int D510_11_2nd = agvEntity.dictAGVDword[51011]; //51011;
                            int D476 = agvEntity.dictAGVDword[476]; //476; //STATUS MSG

                            DateTime dateTimeLogDetailStage = DateTime.Now;
                            dsLOG_MESSAGE entityAGVMovingMsg = new dsLOG_MESSAGE();

                            MELSECAGVObject agvObjectMovingResponse = new MELSECAGVObject(agvEntity);

                            //檢查執行序號與AGV PLC Move ID是否一樣, 目前只有一台AGV No:1
                            string MoveIDForAGV = getAGVPLCMoveID(1);

                            if (MoveIDForAGV.Equals(executeItem.EXECUTE_SEQ) && !IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                            {
                                IsAGVPLCMoveIDEqualAGVMMoveID_2nd = true;
                                blAGVMoveID_1stCheckIssue = false;

                                dsLOG_MESSAGE entityAGVMovingMsg_2ndCheckMoveID_Pass = new dsLOG_MESSAGE();

                                string msg_2ndCheckMoveID_Pass = string.Format("{0}：AGVing-Moving:2nd檢查執行序號與AGV PLC Move ID是否一樣(2nd check whether the execute seq is the same as the AGV PLC Move ID):PASS, D510_8={1}, D510_9={2}, D510_11={3}, D476={4}, seq:{5} \r\n", dateTimeAGVMoving.ToString("yyyy-MM-dd HH:mm:ss"), D510_8_2nd, D510_9_2nd, D510_11_2nd, D476, executeItem.EXECUTE_SEQ);
                                entityAGVMovingMsg_2ndCheckMoveID_Pass.CREATE_DATETIME = dateTimeAGVMoving;
                                entityAGVMovingMsg_2ndCheckMoveID_Pass.LOG_MESSAGE = msg_2ndCheckMoveID_Pass;

                                Console.WriteLine(msg_2ndCheckMoveID_Pass);
                                tbxLogAddMessageTrigger(entityAGVMovingMsg_2ndCheckMoveID_Pass);
                            }

                            if (!IsAGVPLCMoveIDEqualAGVMMoveID_2nd && !blCheck_2nd)
                            {
                                dsLOG_MESSAGE entityAGVMovingMsg_2ndCheckMoveID_NG = new dsLOG_MESSAGE();

                                string msg_2ndCheckMoveID_NG = string.Format("{0}：AGVing-Moving:2nd檢查執行序號與AGV PLC Move ID是否一樣(2nd check whether the execute seq is the same as the AGV PLC Move ID):FAIL, AGV PLC Move ID:{1}, D510_8={2}, D510_9={3}, D510_11={4}, D476={5}, seq:{6} \r\n", dateTimeAGVMoving.ToString("yyyy-MM-dd HH:mm:ss"), MoveIDForAGV_1stCheck, D510_8_2nd, D510_9_2nd, D510_11_2nd, D476, executeItem.EXECUTE_SEQ);
                                entityAGVMovingMsg_2ndCheckMoveID_NG.CREATE_DATETIME = dateTimeAGVMoving;
                                entityAGVMovingMsg_2ndCheckMoveID_NG.LOG_MESSAGE = msg_2ndCheckMoveID_NG;

                                Console.WriteLine(msg_2ndCheckMoveID_NG);
                                tbxLogAddMessageTrigger(entityAGVMovingMsg_2ndCheckMoveID_NG);

                                //blCheck_2nd = true;
                                Thread.Sleep(5000);
                                continue;
                            }

                            //正常狀態
                            //D510_0:發出讀取請求 && D476:配車
                            if (D510_0 == 1 && D476 == 194 && blMovingSection1)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_194_1 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110:回應請求
                                    agvObjectMovingResponse.setWriteData(110, 1);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection1 = false;
                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVMovingFromST.ToString();
                                    dgvItemRefresh();

                                    string msg_194_1 = string.Format("{0}：step AGVing-Moving:配車作業開始, 回應, AGV moving operation start, response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_194_1.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_194_1.LOG_MESSAGE = msg_194_1;

                                    Console.WriteLine(msg_194_1);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_194_1);
                                }
                            }
                            //D510_0:關閉讀取請求 && D476:配車
                            else if (D510_0 == 0 && D476 == 194 && blMovingSection2 && !blMovingSection1)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_194_0 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110: 關閉請求
                                    agvObjectMovingResponse.setWriteData(110, 0);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection2 = false;

                                    string msg_194_0 = string.Format("{0}：step AGVing-Moving:配車作業開始, 關閉回應, AGV moving operation start, close response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_194_0.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_194_0.LOG_MESSAGE = msg_194_0;

                                    Console.WriteLine(msg_194_0);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_194_0);
                                }

                            }
                            //D510_0:發出讀取請求 && D476:取得Item
                            else if (D510_0 == 1 && D476 == 195 && blMovingSection3 && !blMovingSection2)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_195_1 = new dsLOG_MESSAGE();

                                    //上位PC回應, D110:回應請求
                                    agvObjectMovingResponse.setWriteData(110, 1);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection3 = false;
                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVMovingGetItem.ToString();
                                    dgvItemRefresh();

                                    string msg_195_1 = string.Format("{0}：step AGVing-Moving:取得Item作業開始, 回應, pick up Item operation start, response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_195_1.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_195_1.LOG_MESSAGE = msg_195_1;

                                    Console.WriteLine(msg_195_1);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_195_1);
                                }
                            }
                            //D510_0:關閉讀取請求 && D476:取得Item
                            else if (D510_0 == 0 && D476 == 195 && blMovingSection4 && !blMovingSection3)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_195_0 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110:關閉請求
                                    agvObjectMovingResponse.setWriteData(110, 0);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection4 = false;
                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVMovingToST.ToString();
                                    dgvItemRefresh();

                                    string msg_195_0 = string.Format("{0}：step AGVing-Moving:取得Item作業開始, 關閉回應, pick up Item operation start, close response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_195_0.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_195_0.LOG_MESSAGE = msg_195_0;

                                    Console.WriteLine(msg_195_0);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_195_0);
                                }
                            }
                            //D510_0:發出讀取請求 && D476:自動搬送完了
                            else if (D510_0 == 1 && D476 == 199 && blMovingSection5 && !blMovingSection4)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_199_1 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110:回應請求
                                    agvObjectMovingResponse.setWriteData(110, 1);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection5 = false;
                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVOperationFinish.ToString();
                                    dgvItemRefresh();

                                    string msg_199_1 = string.Format("{0}：step AGVing-Moving:自動搬送完了, 回應, auto transfer finish, response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_199_1.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_199_1.LOG_MESSAGE = msg_199_1;

                                    Console.WriteLine(msg_199_1);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_199_1);
                                }
                            }
                            //D510_0:關閉讀取請求 && D476:自動搬送完了
                            else if (D510_0 == 0 && D476 == 199 && blMovingSection6 && !blMovingSection5)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_199_0 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110:關閉請求
                                    agvObjectMovingResponse.setWriteData(110, 0);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection6 = false;
                                    string msg_199_0 = string.Format("{0}：step AGVing-Moving:自動搬送完了, 關閉回應, auto transfer finish, close response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_199_0.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_199_0.LOG_MESSAGE = msg_199_0;

                                    Console.WriteLine(msg_199_0);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_199_0);

                                    //runningDetailTaskWhile = false;
                                    IsClose = true;
                                }
                            }
                            //D510_0:發出讀取請求 && D476:搬送中止
                            else if (D510_0 == 1 && D476 == 197 && blMovingSection9 && blMovingSection10)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_197_1 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110:回應請求
                                    agvObjectMovingResponse.setWriteData(110, 1);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection9 = false;
                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVOperationFinish.ToString();
                                    dgvItemRefresh();

                                    string msg_197_1 = string.Format("{0}：step AGVing-Moving:搬送中止, 回應, the move task stop, response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_197_1.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_197_1.LOG_MESSAGE = msg_197_1;

                                    Console.WriteLine(msg_197_1);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_197_1);
                                }
                            }
                            //D510_0:關閉讀取請求 && D476:搬送中止
                            else if (D510_0 == 0 && D476 == 197 && blMovingSection10 && !blMovingSection9)
                            {
                                //執行序號與AGV PLC Move ID一樣
                                if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_197_0 = new dsLOG_MESSAGE();
                                    //上位PC回應, D110:關閉請求
                                    agvObjectMovingResponse.setWriteData(110, 0);
                                    agvObjectMovingResponse.SingleWriteData();

                                    blMovingSection10 = false;
                                    string msg_197_0 = string.Format("{0}：step AGVing-Moving:搬送中止, 關閉回應, the move task stop, close response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_197_0.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_197_0.LOG_MESSAGE = msg_197_0;

                                    Console.WriteLine(msg_197_0);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_197_0);

                                    //runningDetailTaskWhile = false;
                                    IsClose = true;
                                }
                            }
                            //以下為有異常時
                            //D510_0:發出讀取請求 && D476:為上面以外的其它數值
                            else if (D510_0 == 1)
                            {
                                if (D476 == 194 || D476 == 195 || D476 == 199 || D476 == 197)
                                {
                                    //do nothing 
                                    continue;
                                }
                                //執行序號與AGV PLC Move ID一樣
                                //普通異常
                                else if (IsAGVNormalIssue(D476) && IsAGVPLCMoveIDEqualAGVMMoveID_2nd && blMovingSection11)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_NG_1 = new dsLOG_MESSAGE();
                                    blMovingSection11 = false;
                                    blMovingSection12 = true;
                                    //上位PC回應, D110:回應請求
                                    agvObjectMovingResponse.setWriteData(110, 1);
                                    agvObjectMovingResponse.SingleWriteData();

                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVOperationAbnormal.ToString();
                                    dgvItemRefresh();

                                    string msg_response_NG_1 = string.Format("{0}：step AGVing-Moving:AGV有普通異常, 回應, AGV normal issue, response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_NG_1.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_NG_1.LOG_MESSAGE = msg_response_NG_1;

                                    Console.WriteLine(msg_response_NG_1);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_NG_1);
                                }
                                //嚴重異常
                                else if (IsAGVImportantIssue(D476) && IsAGVPLCMoveIDEqualAGVMMoveID_2nd && blMovingSection7)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_NG_1 = new dsLOG_MESSAGE();
                                    blMovingSection7 = false;
                                    blMovingSection8 = true;
                                    //上位PC回應, D110:回應請求
                                    agvObjectMovingResponse.setWriteData(110, 1);
                                    agvObjectMovingResponse.SingleWriteData();

                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.AGVOperationAbnormal.ToString();
                                    dgvItemRefresh();

                                    string msg_response_NG_1 = string.Format("{0}：step AGVing-Moving:AGV有嚴重異常, 回應, AGV serious issue, response, NG, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_NG_1.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_NG_1.LOG_MESSAGE = msg_response_NG_1;

                                    Console.WriteLine(msg_response_NG_1);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_NG_1);

                                    //IsNG = true;
                                }

                            }
                            //D510_0:關閉讀取請求 && D476:為上面以外的其它數值
                            else if (D510_0 == 0)
                            {
                                if (D476 == 194 || D476 == 195 || D476 == 199 || D476 == 197)
                                {
                                    //do nothing 
                                    continue;
                                }
                                //執行序號與AGV PLC Move ID一樣
                                //普通異常
                                else if (IsAGVNormalIssue(D476) && IsAGVPLCMoveIDEqualAGVMMoveID_2nd && !blMovingSection11 && blMovingSection12)
                                {
                                    dsLOG_MESSAGE entityAGVMovingMsg_NG_0 = new dsLOG_MESSAGE();
                                    blMovingSection12 = false;
                                    blMovingSection11 = true;
                                    //上位PC回應, D110:關閉請求
                                    agvObjectMovingResponse.setWriteData(110, 0);
                                    agvObjectMovingResponse.SingleWriteData();

                                    string msg_response_NG_0 = string.Format("{0}：step AGVing-Moving:AGV有異常, 關閉回應, AGV normal issue, close response, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_NG_0.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_NG_0.LOG_MESSAGE = msg_response_NG_0;

                                    Console.WriteLine(msg_response_NG_0);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_NG_0);
                                }
                                //執行序號與AGV PLC Move ID一樣
                                //嚴重異常
                                else if (IsAGVImportantIssue(D476) && IsAGVPLCMoveIDEqualAGVMMoveID_2nd && !blMovingSection7 && blMovingSection8)
                                {

                                    dsLOG_MESSAGE entityAGVMovingMsg_NG_0 = new dsLOG_MESSAGE();
                                    blMovingSection8 = false;
                                    blMovingSection7 = true;
                                    //上位PC回應, D110:關閉請求
                                    agvObjectMovingResponse.setWriteData(110, 0);
                                    agvObjectMovingResponse.SingleWriteData();

                                    string msg_response_NG_0 = string.Format("{0}：step AGVing-Moving:AGV有嚴重異常, 關閉回應, AGV serious issue, close response, NG, D510_0={1}, D476={2}, seq:{3} \r\n", dateTimeLogDetailStage.ToString("yyyy-MM-dd HH:mm:ss"), D510_0, D476, executeItem.EXECUTE_SEQ);

                                    entityAGVMovingMsg_NG_0.CREATE_DATETIME = dateTimeLogDetailStage;
                                    entityAGVMovingMsg_NG_0.LOG_MESSAGE = msg_response_NG_0;

                                    Console.WriteLine(msg_response_NG_0);
                                    tbxLogAddMessageTrigger(entityAGVMovingMsg_NG_0);

                                    //runningDetailTaskWhile = false;
                                    //IsClose = true;
                                }
                            }

                            if ((IsAGVPLCMoveIDEqualAGVMMoveID_2nd && IsClose && !IsNG) || (IsAGVPLCMoveIDEqualAGVMMoveID_2nd && IsClose && IsNG))
                            {
                                //檢測狀態階段
                                DateTime dateTimeLogDataAdjustStage = DateTime.Now;
                                dsLOG_MESSAGE entityDataAdjustkStage = new dsLOG_MESSAGE();

                                //檢查是否為buffer
                                //因buffer區會為預定位置, 如有預定, 冶具送到buffer區後需要取消預定的註記
                                if (IsBuffer(executeItem.AGV_TO_ST))
                                {
                                    //AGV將冶具送到buffer區後需要取消預定的註記, 並將Item No資料寫入DB
                                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                                    for (int i = 0; i < listBufferStatus.Count; i++)
                                    {
                                        BufferStatus item = new BufferStatus();
                                        item = listBufferStatus[i];

                                        if (item.StationNo.Equals(executeItem.AGV_TO_ST) && item.ArrangedTask.Equals("1"))
                                        {
                                            item.ArrangedTask = "9";
                                            lblBufferSetArrangedTaskTrigger(item);

                                            //寫入DB, 先暫時不寫, 之後再補
                                            //dsBUFFER_DATA entityBuffer = new dsBUFFER_DATA();
                                            //entityBuffer.STATION_NO = executeItem.AGV_TO_ST;
                                            //entityBuffer.ITEM_NO = executeItem.ITEM_NO;

                                            ////int result = daoBuffer.UpdateBUFFER_DATA(entityBuffer);

                                            //if (result > 0)
                                            //{
                                            //    msg = string.Format("{0}：AGV Finish: buffer區DB資料變動OK! \r\n", dateTimeLogDataAdjustStage.ToString("yyyy-MM-dd HH:mm:ss"));
                                            //}
                                            //else
                                            //{
                                            //    msg = string.Format("{0}：AGV Finish: buffer區DB資料變動Fail! \r\n", dateTimeLogDataAdjustStage.ToString("yyyy-MM-dd HH:mm:ss"));
                                            //}

                                            //entityDataAdjustkStage.CREATE_DATETIME = dateTimeLogDataAdjustStage;
                                            //entityDataAdjustkStage.LOG_MESSAGE = msg;

                                            //Console.WriteLine(msg);
                                            //tbxLogAddMessageTrigger(entityDataAdjustkStage);
                                        }
                                    }
                                }
                            }

                            //檢測狀態階段
                            DateTime dateTimeLogCheckStage = DateTime.Now;
                            dsLOG_MESSAGE entityCheckStage = new dsLOG_MESSAGE();

                            //NG
                            if (IsNG && IsClose && !blMovingSection10)
                            {
                                dsLOG_MESSAGE entityAGVMovingMsg_Finish_NG = new dsLOG_MESSAGE();

                                msg = string.Format("{0}：AGV Finish: NG, 請檢測AGV! check AGV \r\n", dateTimeLogCheckStage.ToString("yyyy-MM-dd HH:mm:ss"));

                                entityAGVMovingMsg_Finish_NG.CREATE_DATETIME = dateTimeLogCheckStage;
                                entityAGVMovingMsg_Finish_NG.LOG_MESSAGE = msg;

                                Console.WriteLine(msg);
                                tbxLogAddMessageTrigger(entityAGVMovingMsg_Finish_NG);

                                IsFinish = true;
                            }
                            //Finish
                            else if (IsClose && !IsNG && (!blMovingSection6 || !blMovingSection10))
                            {
                                dsLOG_MESSAGE entityAGVMovingMsg_Finish_PASS = new dsLOG_MESSAGE();

                                msg = string.Format("{0}：AGV Finish: AGV任務完成, AGV task finish seq:{1} \r\n", dateTimeLogCheckStage.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());

                                entityAGVMovingMsg_Finish_PASS.CREATE_DATETIME = dateTimeLogCheckStage;
                                entityAGVMovingMsg_Finish_PASS.LOG_MESSAGE = msg;

                                Console.WriteLine(msg);
                                tbxLogAddMessageTrigger(entityAGVMovingMsg_Finish_PASS);

                                IsFinish = true;
                            }

                            DateTime dateTimeLogChangeStage = DateTime.Now;
                            dsLOG_MESSAGE entityChangeStage = new dsLOG_MESSAGE();

                            //AGV執行結束後, 換執行自動倉儲指令
                            //AGV是最後執行的動作, 需要刪除指令
                            if (IsAGVPLCMoveIDEqualAGVMMoveID_2nd && IsFinish)
                            {
                                //出發站台：如果是暫存區, 需要清空item no資料
                                if (IsBuffer(executeItem.AGV_FROM_ST))
                                {
                                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                                    for (int i = 0; i < listBufferStatus.Count; i++)
                                    {
                                        BufferStatus item = new BufferStatus();

                                        item = listBufferStatus[i];

                                        if (item.StationNo.Equals(executeItem.AGV_FROM_ST))
                                        {
                                            item.ArrangedTask = "9";
                                            item.ItemNo = "none";
                                            item.InOutFlag = "";
                                            item.PriorityArea = "";

                                            lblBufferSetArrangedTaskTrigger(item);
                                            lblBufferItemNoTrigger(item);
                                            break;
                                        }
                                    }
                                }

                                //到達站台：如果是暫存區, 需要填入Item no資料; 
                                if (IsBuffer(executeItem.AGV_TO_ST))
                                {
                                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                                    for (int i = 0; i < listBufferStatus.Count; i++)
                                    {
                                        BufferStatus item = new BufferStatus();

                                        item = listBufferStatus[i];

                                        if (item.StationNo.Equals(executeItem.AGV_TO_ST))
                                        {
                                            //item.StationNo = executeItem.AGV_TO_ST;                                            
                                            item.ArrangedTask = "9";
                                            item.ItemNo = executeItem.ITEM_NO;
                                            item.InOutFlag = executeItem.INOUT_FLAG;
                                            item.PriorityArea = executeItem.PRIORITY_AREA;

                                            lblBufferSetArrangedTaskTrigger(item);
                                            lblBufferItemNoTrigger(item);

                                            break;
                                        }
                                    }
                                }

                                //AGV執行結束後, 換執行自動倉儲指令:
                                //step1:自動倉儲入庫會先執行autostock cmd, 當正常狀況下, step2:換執行AGV配車, AGV配車執行完後, step3:再繼續自動倉儲命令
                                if (executeItem.INOUT_FLAG == InOutStockFlag.In.ToString()
                                && executeItem.CMD_AUTOSTOCK_PRIORITY.Equals("1") && executeItem.CMD_AGV_PRIORITY.Equals("2")
                                )
                                {
                                    executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();
                                    executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString();
                                    dgvItemRefresh();
                                    msg = string.Format("{0}：換執行自動倉儲命令(入庫), change the execution of autostock command(in stock), seq:{1} \r\n", dateTimeLogChangeStage.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());

                                    entityChangeStage.CREATE_DATETIME = dateTimeLogChangeStage;
                                    entityChangeStage.LOG_MESSAGE = msg;

                                    Console.WriteLine(msg);
                                    tbxLogAddMessageTrigger(entityChangeStage);

                                    runningDetailTaskWhile = false;
                                }
                                //AGV是最後執行的動作, 需要刪除指令
                                else if (((executeItem.CMD_AGV_PRIORITY.Equals("1") && executeItem.CMD_AUTOSTOCK_PRIORITY.Equals("0")) || executeItem.CMD_AGV_PRIORITY.Equals("2")))
                                {
                                    bool killCmd = false;
                                    //DateTime dateTimeAutostocKillCmdkLog = DateTime.Now;

                                    //msg = string.Format("{0}：所有執行命令皆已完成, 刪除此命令, seq:{1} \r\n", dateTimeAutostocKillCmdkLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());
                                    killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                    if (killCmd)
                                    {
                                        //do nothing 
                                        //AutostockOperating = false;
                                        runningDetailTaskWhile = false;
                                    }
                                }
                            }
                        }

                    }

                    //**執行autostock 確認自動倉儲模式(自動：TFR or 單獨：LIFT)
                    if (runningData > 0 && executeItem != null
                        && executeItem.EXECUTE_STATUS == StatusFlag.Autostocking.ToString()
                        && !(executeItem.CMD_AGV_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockOperationFinish.ToString() || executeItem.CMD_AGV_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockOperationAbnormal.ToString())
                        && executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG == AutostockNormalFeedback.OFF.ToString()
                        && executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.Start.ToString()
                        )
                    {
                        var msg = string.Empty;
                        DateTime dateTimeAutostockLog = DateTime.Now;
                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();

                        //autostock 未連線, 不做任何事
                        if (!autostockEntity.clientSocket.Connected)
                        {
                            msg = string.Format("{0}：step Autostocking:Autostock沒有連線(autostock disconnected), seq:{1} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            Console.WriteLine(msg);
                            continue;
                        }

                        //發送訊息給自動倉儲, 確認現在自動倉儲ST5模式
                        dsAutoStockTransferJsonModel AutostockModeRequest = new dsAutoStockTransferJsonModel();

                        AutostockModeRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                        AutostockModeRequest.TRANS_NO = "0009-1";
                        AutostockModeRequest.ITEM_NO = "";
                        AutostockModeRequest.STOCK_NO = "SEIBU-AS01";
                        AutostockModeRequest.SHELF_NO = "";
                        AutostockModeRequest.INOUT_FLAG = "";
                        AutostockModeRequest.STATUS = "1";
                        AutostockModeRequest.AUTOSTOCK_MODE = "";
                        AutostockModeRequest.RFID_NO = "";
                        AutostockModeRequest.ERROR_CODE = "";
                        AutostockModeRequest.PRIORITY_AREA = "";

                        executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.ON.ToString();
                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.TransCmd.ToString();
                        executeItem.CMD_AUTOSTOCK_TRANS_DATETIME = dateTimeAutostockLog.ToString("yyyy/MM/dd HH:mm:ss");
                        dgvItemRefresh();

                        SendTransferDataToAutostock(AutostockModeRequest);

                        msg = string.Format("{0}：step Autostocking:Autostock發送指令(autostock send command):{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockModeRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                        entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                        entityMsg.LOG_MESSAGE = msg;

                        Console.WriteLine(msg);
                        tbxLogAddMessageTrigger(entityMsg);

                        Stopwatch stwResponseTime = new Stopwatch();
                        stwResponseTime.Start();

                        bool blWaitAutoModeResponse = true;
                        dsLOG_MESSAGE entityReceiveMsg = new dsLOG_MESSAGE();

                        //等待並取得自動倉儲回應
                        while (blWaitAutoModeResponse)
                        {
                            DateTime dateTimeAutostockReceiveLog = DateTime.Now;

                            bool cannotFindExecuteSeql = findExecuteSeq();

                            //找不到執行序號, 有可能被刪除了
                            if (cannotFindExecuteSeql)
                            {
                                blWaitAutoModeResponse = false;
                                blRFIDTagRewrite = false;
                                blAutostockAutoMode = false;
                                blMoveIDWriteFinish = false;
                                blAGVMoveID_1stCheckIssue = false;
                            }

                            if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                && autostockEntity.dsAutostockServer.AUTOSTOCK_MODE != null
                                && autostockEntity.dsAutostockServer.TRANS_NO.Equals("0009-2")
                                && (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("TFR") || autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("LIFT")))
                            {
                                ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                if (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("TFR"))
                                {
                                    blAutostockAutoMode = true;
                                }
                                else if (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("LIFT"))
                                {
                                    blAutostockAutoMode = false;
                                }

                                blWaitAutoModeResponse = false;

                                ReceiveAutostockMsgClear();
                            }

                            //等待回應時間大於60秒則強制結束
                            if (stwResponseTime.ElapsedMilliseconds > 60000)
                            {
                                blWaitAutoModeResponse = false;
                                msg = string.Format("{0}：step Autostocking:未接收到0009-2回覆, 強制LIFT mode, 0009-2 reply not received, forced LIFT mode, seq:{2} \r\n", dateTimeAutostockReceiveLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                entityReceiveMsg.CREATE_DATETIME = dateTimeAutostockReceiveLog;
                                entityReceiveMsg.LOG_MESSAGE = msg;

                                Console.WriteLine(msg);
                                tbxLogAddMessageTrigger(entityReceiveMsg);
                            }
                        }

                        //轉為自動倉儲正式作業
                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperating.ToString();
                        dgvItemRefresh();

                        stwResponseTime.Stop();
                    }

                    //**執行autostock 入/出庫指令 (TRANS_NO:1001(入)/1002(出))
                    if (runningData > 0 && executeItem != null
                       && executeItem.EXECUTE_STATUS == StatusFlag.Autostocking.ToString()
                       && !(executeItem.CMD_AGV_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockOperationFinish.ToString() || executeItem.CMD_AGV_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockOperationAbnormal.ToString())
                       && executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG == AutostockNormalFeedback.ON.ToString()
                       && (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockOperating.ToString()
                          || executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString())
                       )
                    {
                        //Stopwatch stwResponseTime = new Stopwatch();
                        //  stwResponseTime.Start();
                        //if (stwResponseTime.ElapsedMilliseconds > 60000) //等待回應時間大於60秒則強制結束
                        // stwResponseTime.Stop();

                        //發送訊息給自動倉儲, 確認現在自動倉儲ST5模式
                        dsAutoStockTransferJsonModel AutostockCmdRequest = new dsAutoStockTransferJsonModel();

                        bool AutostockOperating = true;

                        string msg = string.Empty;
                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();

                        //主項目
                        while (AutostockOperating)
                        {
                            DateTime dateTimeAutostockLog = DateTime.Now;

                            //入庫命令, TRANS_NO:1001
                            if (executeItem.INOUT_FLAG.ToUpper().Equals("IN"))
                            {
                                //入庫非繼續執行, step:1001-1 & 1001-2
                                if (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS != AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString()
                                    ) //&& executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS != AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString()
                                {
                                    AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                    AutostockCmdRequest.TRANS_NO = "1001-1";
                                    AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                    AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                    AutostockCmdRequest.SHELF_NO = "";
                                    AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                    AutostockCmdRequest.STATUS = "1";
                                    AutostockCmdRequest.AUTOSTOCK_MODE = blAutostockAutoMode ? "TFR" : "LIFT";
                                    AutostockCmdRequest.RFID_NO = "";
                                    AutostockCmdRequest.ERROR_CODE = "";
                                    AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                    SendTransferDataToAutostock(AutostockCmdRequest);

                                    msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                    entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                    entityMsg.LOG_MESSAGE = msg;

                                    Console.WriteLine(msg);
                                    tbxLogAddMessageTrigger(entityMsg);

                                    bool blWaitAutostockResponse = true;
                                    bool blAutostockResponseYes = false;
                                    dsLOG_MESSAGE entityReceiveMsg = new dsLOG_MESSAGE();

                                    //等待autostock 回應
                                    while (blWaitAutostockResponse)
                                    {
                                        DateTime dateTimeAutostockReceiveLog = DateTime.Now;

                                        bool cannotFindExecuteSeql = findExecuteSeq();

                                        //找不到執行序號, 有可能被刪除了
                                        if (cannotFindExecuteSeql)
                                        {
                                            blWaitAutostockResponse = false;
                                            blRFIDTagRewrite = false;
                                            blAutostockAutoMode = false;
                                            blMoveIDWriteFinish = false;
                                            blAGVMoveID_1stCheckIssue = false;
                                        }

                                        if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-2"))
                                        {
                                            ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                            // 異常：檢查自動倉儲模式是不是一樣：TFR or LIFT, 重新執行取得自動倉儲模式
                                            if (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("TFR") != blAutostockAutoMode)
                                            {
                                                //執行autostock cmd, 
                                                //executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                                //Autostock
                                                //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                                executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                                executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;                                          

                                                //AGV不動
                                                //executeItem.CMD_AGV_PRIORITY = "1";
                                                //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                                //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                                //executeItem.CMD_AGV_TRANS_DATETIME

                                                dgvItemRefresh();

                                                blWaitAutostockResponse = false;
                                                AutostockOperating = false; //結束作業, 重新執行取得自動倉儲模式作業
                                                ReceiveAutostockMsgClear();
                                            }
                                            else
                                            {
                                                blWaitAutostockResponse = false;
                                                blAutostockResponseYes = true;
                                            }
                                        }

                                    }

                                    //DateTime dateTimeAutostockDetailLog = DateTime.Now;

                                    //TRANS_NO:1001-2
                                    //自動倉儲回應 & 狀態1:正常, 自動倉儲模式：TFR(連動)
                                    //AGV派車作業
                                    if (blAutostockResponseYes
                                        && autostockEntity.dsAutostockServer.TRANS_NO != null
                                        && autostockEntity.dsAutostockServer.STATUS != null
                                        && autostockEntity.dsAutostockServer.AUTOSTOCK_MODE != null
                                        && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-2")
                                        && autostockEntity.dsAutostockServer.STATUS.Equals("1")
                                        && autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("TFR")
                                        )
                                    {
                                        //執行AGV cmd, 不執行autostock cmd
                                        executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                        //Autostock
                                        //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                        //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = "";
                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.WaitingAGV.ToString();
                                        //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                        //AGV
                                        //executeItem.CMD_AGV_PRIORITY = "1";
                                        executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                        executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                        //executeItem.CMD_AGV_TRANS_DATETIME

                                        dgvItemRefresh();

                                        AutostockOperating = false;
                                        ReceiveAutostockMsgClear();
                                    }
                                    //自動倉儲回應 & 狀態1:正常 & 自動倉儲模式：LIFT(單獨)
                                    //AGV不派車, 因娃娃機無法作動
                                    else if (blAutostockResponseYes
                                            && autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && autostockEntity.dsAutostockServer.AUTOSTOCK_MODE != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-2")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("1")
                                            && autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("LIFT"))
                                    {
                                        //執行AGV cmd, 不執行autostock cmd
                                        //executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                        //Autostock
                                        //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                        //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = "";
                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString();
                                        //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                        //AGV
                                        //executeItem.CMD_AGV_PRIORITY = "1";
                                        //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                        //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                        //executeItem.CMD_AGV_TRANS_DATETIME

                                        dgvItemRefresh();

                                        ReceiveAutostockMsgClear();

                                        continue;
                                    }
                                    //異常
                                    else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-2")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("9"))
                                    {
                                        bool blKillExecute = true;

                                        //入庫
                                        //error code:1001
                                        //2024/4/8 因入庫改為RFID品番一致後就結束上位PC任務(1001-13 & 1001-14), 但自動倉儲管理系統還在排隊等待執行, 當自動倉儲尚未結ST5任務時,
                                        //有可能上位PC會再發一筆入/出庫任務給自動倉儲管理系統, 此時就會出現1001(入庫) & 2001(出庫)的error code,
                                        //需重新執行取得自動倉儲模式(0009-1 & 0009-2), 直到正確執行或取消任務, 否則一直loop
                                        if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1001"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-複数入庫指示, issue, multiple in stock command");

                                            //執行autostock cmd, 
                                            //executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                            //Autostock
                                            //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                            executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;                                          

                                            //AGV不動
                                            //executeItem.CMD_AGV_PRIORITY = "1";
                                            //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                            //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AGV_TRANS_DATETIME

                                            dgvItemRefresh();

                                            blWaitAutostockResponse = false;
                                            AutostockOperating = false; //結束作業, 重新執行取得自動倉儲模式作業

                                            blKillExecute = false;

                                            Thread.Sleep(15000); //15秒等待自動倉儲結束作業
                                        }
                                        //error code:1002
                                        else if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1002"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-空棚不足, issue, not enough shelf");
                                        }
                                        //error code:1003
                                        else if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1003"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-在庫有り, issue, This item is already in stock");
                                        }
                                        //error code:1004
                                        else if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1004"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-品番無し, issue, this jig has no data in the seibu system");
                                        }
                                        //error code:1005
                                        //等待修正後再繼續執行
                                        else if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1005"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-データ修正中, issue, data is being modified");

                                            //執行autostock cmd, 
                                            //executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                            //Autostock
                                            //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                            executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;                                          

                                            //AGV不動
                                            //executeItem.CMD_AGV_PRIORITY = "1";
                                            //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                            //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AGV_TRANS_DATETIME

                                            dgvItemRefresh();

                                            blWaitAutostockResponse = false;
                                            AutostockOperating = false; //結束作業, 重新執行取得自動倉儲模式作業

                                            blKillExecute = false;

                                            Thread.Sleep(5000); //5秒等待自動倉儲結束作業
                                        }

                                        if (blKillExecute)
                                        {
                                            //刪除此命令
                                            bool killCmd = false;
                                            killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                            if (killCmd)
                                            {
                                                //do nothing 
                                                //AutostockOperating = false;
                                            }

                                        }

                                        ReceiveAutostockMsgClear();
                                    }
                                }
                                //AGV配車後, 換自動倉儲作業, 或 1001-3 & LIFT模式後繼續自動倉儲作業 或 異常問題
                                else
                                {
                                    //自動倉儲回應, 狀態1:正常, 刪除指令
                                    if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                        && autostockEntity.dsAutostockServer.STATUS != null
                                        && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-3")
                                        && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                    {
                                        //SendTransferDataToAutostock(AutostockCmdRequest);
                                        ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                        AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                        AutostockCmdRequest.TRANS_NO = "1001-4";
                                        AutostockCmdRequest.ITEM_NO = autostockEntity.dsAutostockServer.ITEM_NO;
                                        AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                        AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                        AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                        AutostockCmdRequest.STATUS = "1";
                                        AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                        AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        AutostockCmdRequest.ERROR_CODE = "";
                                        AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                        SendTransferDataToAutostock(AutostockCmdRequest);

                                        msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);

                                        entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                        entityMsg.LOG_MESSAGE = msg;

                                        Console.WriteLine(msg);
                                        tbxLogAddMessageTrigger(entityMsg);

                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                        executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        dgvItemRefresh();

                                        //Autostock是最後執行的動作, 需要刪除指令
                                        bool killCmd = false;
                                        //DateTime dateTimeAutostocKillCmdkLog = DateTime.Now;
                                        //msg = string.Format("{0}：所有執行命令皆已完成, 刪除此命令, seq:{1} \r\n", dateTimeAutostocKillCmdkLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());
                                        killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                        if (killCmd)
                                        {
                                            AutostockOperating = false;
                                        }

                                        blRFIDTagRewrite = false;
                                        blAutostockAutoMode = false;
                                        blMoveIDWriteFinish = false;
                                        blAGVMoveID_1stCheckIssue = false;
                                        ReceiveAutostockMsgClear();
                                    }
                                    //自動倉儲回應, 狀態1:正常, 刪除指令
                                    else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-13")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                    {
                                        //SendTransferDataToAutostock(AutostockCmdRequest);
                                        ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                        AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                        AutostockCmdRequest.TRANS_NO = "1001-14";
                                        AutostockCmdRequest.ITEM_NO = autostockEntity.dsAutostockServer.ITEM_NO;
                                        AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                        AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                        AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                        AutostockCmdRequest.STATUS = "1";
                                        AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                        AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        AutostockCmdRequest.ERROR_CODE = "";
                                        AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                        SendTransferDataToAutostock(AutostockCmdRequest);


                                        msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);

                                        entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                        entityMsg.LOG_MESSAGE = msg;

                                        Console.WriteLine(msg);
                                        tbxLogAddMessageTrigger(entityMsg);

                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                        executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        dgvItemRefresh();

                                        //Autostock是最後執行的動作, 需要刪除指令
                                        bool killCmd = false;
                                        //DateTime dateTimeAutostocKillCmdkLog = DateTime.Now;
                                        //msg = string.Format("{0}：所有執行命令皆已完成, 刪除此命令, seq:{1} \r\n", dateTimeAutostocKillCmdkLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());
                                        killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                        if (killCmd)
                                        {
                                            AutostockOperating = false;
                                        }

                                        blRFIDTagRewrite = false;
                                        blAutostockAutoMode = false;
                                        blMoveIDWriteFinish = false;
                                        blAGVMoveID_1stCheckIssue = false;
                                        ReceiveAutostockMsgClear();
                                    }
                                    //異常問題處理
                                    else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && !autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-3")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("9"))
                                    {
                                        //TRANS_NO:1001-7, 入庫異常報告
                                        if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                           && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-7"))
                                        {
                                            if (string.IsNullOrWhiteSpace(autostockEntity.dsAutostockServer.checkPoint))//|| autostockEntity.dsAutostockResponse.checkPoint != autostockEntity.dsAutostockRequest.checkPoint )
                                            {
                                                //autostockEntity.dsAutostockServer.checkPoint = string.IsNullOrWhiteSpace(autostockEntity.dsAutostockServer.checkPoint) ? "1" : (Convert.ToInt32(autostockEntity.dsAutostockServer.checkPoint) + 1).ToString();
                                                ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);
                                                executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;

                                                dgvItemRefresh();

                                                //TRANS_NO:1001-8, 入庫異常報告應答
                                                AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                                AutostockCmdRequest.TRANS_NO = "1001-8";
                                                AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                                AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                                AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                                AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                                AutostockCmdRequest.STATUS = "9";
                                                AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                                AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                                AutostockCmdRequest.ERROR_CODE = "";
                                                AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                                SendTransferDataToAutostock(AutostockCmdRequest);

                                                //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);

                                                entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                entityMsg.LOG_MESSAGE = msg;

                                                Console.WriteLine(msg);
                                                tbxLogAddMessageTrigger(entityMsg);

                                                autostockEntity.dsAutostockServer.checkPoint = "1";
                                            }

                                            //1101:品番不一致	材料編號與RFID不符
                                            if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                                && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1101"))
                                            {
                                                //TRANS_NO:1001-5, RFID_NO rewrite 報告
                                                blRFIDTagRewrite = true;
                                                // executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString();
                                                //dgvItemRefresh();
                                            }
                                            //1102:ノーリード	無RFID
                                            else if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                                && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1102"))
                                            {
                                                //do nothing 
                                            }
                                            //1103:タグ故障	RFID故障
                                            else if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                              && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1103"))
                                            {
                                                // executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString();
                                                //dgvItemRefresh();
                                            }
                                            //1104:二重格	儲位已存在其它材料無法入庫
                                            else if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                              && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("1104"))
                                            {
                                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString();
                                                //dgvItemRefresh();
                                            }

                                            ReceiveAutostockMsgClear();
                                        }

                                        //RFID Tag要Rewrit, 跳出功能畫面
                                        if (blRFIDTagRewrite)
                                        {
                                            RFIDCheckFormOpenTrigger();
                                            bool blWaitUserRewriteReponse = true;

                                            while (blWaitUserRewriteReponse)
                                            {
                                                if (!string.IsNullOrWhiteSpace(autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS) && !autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("0"))
                                                {
                                                    //1:變更Item_NO
                                                    if (autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("1"))
                                                    {
                                                        executeItem.ITEM_NO = autostockEntity.dsModify.ITEM_NO;
                                                    }
                                                    //2:變更RFID_No
                                                    else if (autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("2"))
                                                    {
                                                        executeItem.RFID_NO = autostockEntity.dsModify.RFID_NO;
                                                    }
                                                    else if (autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("3"))
                                                    {
                                                        //do nothing                                                        
                                                    }

                                                    blWaitUserRewriteReponse = false;
                                                    blRFIDTagRewrite = false;
                                                }

                                                //已經執行過西部電機修改, 並收到出庫完了, 但上位PC回應要修改發送指令給管理機, 此時強制改為已由西部電機修改
                                                if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                                   && autostockEntity.dsAutostockServer.STATUS != null
                                                   && (autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-3") || autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-13"))
                                                   && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                                {
                                                    autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS = "3";
                                                }

                                                bool cannotFindExecuteSeqDetail = findExecuteSeq();

                                                //找不到執行序號, 有可能被刪除了
                                                if (cannotFindExecuteSeqDetail)
                                                {
                                                    blWaitUserRewriteReponse = false;
                                                    blRFIDTagRewrite = false;
                                                    blAutostockAutoMode = false;
                                                    blMoveIDWriteFinish = false;
                                                    blAGVMoveID_1stCheckIssue = false;
                                                }
                                            }

                                            if ((autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("1")
                                                || autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("2"))
                                                 && !(autostockEntity.dsAutostockServer.TRANS_NO != null
                                                   && autostockEntity.dsAutostockServer.STATUS != null
                                                   && (autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-3") || autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-13"))
                                                   && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                               )
                                            {
                                                //TRANS_NO:1001-5, RFID_NO rewrite 報告                                                
                                                AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                                AutostockCmdRequest.TRANS_NO = "1001-5";
                                                AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                                AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                                AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServerKeepData.SHELF_NO;
                                                AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                                AutostockCmdRequest.STATUS = "9";
                                                AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServerKeepData.AUTOSTOCK_MODE;
                                                AutostockCmdRequest.RFID_NO = executeItem.RFID_NO;
                                                AutostockCmdRequest.ERROR_CODE = "";
                                                AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                                SendTransferDataToAutostock(AutostockCmdRequest);

                                                //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);

                                                entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                entityMsg.LOG_MESSAGE = msg;

                                                Console.WriteLine(msg);
                                                tbxLogAddMessageTrigger(entityMsg);

                                                bool blWaitAutostockRewriteReponse = true;

                                                while (blWaitAutostockRewriteReponse)
                                                {
                                                    //TRANS_NO:1001-6, RFID_NO rewrite 報告應答
                                                    if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                                        && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-6"))
                                                    {
                                                        ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                                        blWaitAutostockRewriteReponse = false;

                                                        ReceiveAutostockMsgClear();
                                                    }

                                                    bool cannotFindExecuteSeqDetail = findExecuteSeq();

                                                    //找不到執行序號, 有可能被刪除了
                                                    if (cannotFindExecuteSeqDetail)
                                                    {
                                                        blWaitAutostockRewriteReponse = false;
                                                        blRFIDTagRewrite = false;
                                                        blAutostockAutoMode = false;
                                                        blMoveIDWriteFinish = false;
                                                        blAGVMoveID_1stCheckIssue = false;
                                                    }
                                                }
                                            }
                                        }
                                        //TRANS_NO:1001-9, 入庫異常完了報告-刪除
                                        else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                                && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1001-9"))
                                        {
                                            ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                            AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                            AutostockCmdRequest.TRANS_NO = "1001-10";
                                            AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                            AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                            AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                            AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                            AutostockCmdRequest.STATUS = "9";
                                            AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                            AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                            AutostockCmdRequest.ERROR_CODE = "";
                                            AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                            SendTransferDataToAutostock(AutostockCmdRequest);

                                            msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                            //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                            entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                            entityMsg.LOG_MESSAGE = msg;

                                            Console.WriteLine(msg);
                                            tbxLogAddMessageTrigger(entityMsg);

                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                            executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;

                                            dgvItemRefresh();

                                            //Autostock是最後執行的動作, 需要刪除指令
                                            bool killCmd = false;
                                            killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                            if (killCmd)
                                            {
                                                AutostockOperating = false;
                                            }

                                            blRFIDTagRewrite = false;
                                            blAutostockAutoMode = false;
                                            blMoveIDWriteFinish = false;
                                            blAGVMoveID_1stCheckIssue = false;

                                            ReceiveAutostockMsgClear();
                                        }
                                        ////第一次異常問題處理
                                        //if (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS != AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString())
                                        //{

                                        //}
                                        ////第二次後異常問題處理
                                        //else if (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString())
                                        //{

                                        //}
                                    }
                                }
                            }
                            //出庫命令 , TRANS_NO:1002
                            else if (executeItem.INOUT_FLAG.ToUpper().Equals("OUT"))
                            {
                                //出庫非繼續執行, step:1002-1 & 1002-2
                                if (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS != AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString()
                                    ) //&& executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS != AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString()
                                {
                                    AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                    AutostockCmdRequest.TRANS_NO = "1002-1";
                                    AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                    AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                    AutostockCmdRequest.SHELF_NO = "";
                                    AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                    AutostockCmdRequest.STATUS = "1";
                                    AutostockCmdRequest.AUTOSTOCK_MODE = blAutostockAutoMode ? "TFR" : "LIFT";
                                    AutostockCmdRequest.RFID_NO = "";
                                    AutostockCmdRequest.ERROR_CODE = "";
                                    AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                    SendTransferDataToAutostock(AutostockCmdRequest);

                                    msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                    //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                    entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                    entityMsg.LOG_MESSAGE = msg;

                                    Console.WriteLine(msg);
                                    tbxLogAddMessageTrigger(entityMsg);

                                    bool blWaitAutostockResponse = true;
                                    bool blAutostockResponseYes = false;
                                    dsLOG_MESSAGE entityReceiveMsg = new dsLOG_MESSAGE();

                                    //等待autostock 回應
                                    while (blWaitAutostockResponse)
                                    {
                                        if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-2"))
                                        {
                                            ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                            // 異常：檢查自動倉儲模式是不是一樣：TFR or LIFT, 重新執行取得自動倉儲模式
                                            if (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("TFR") != blAutostockAutoMode)
                                            {
                                                //執行autostock cmd, 
                                                //executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                                //Autostock
                                                //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                                executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                                executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;                                          

                                                //AGV不動
                                                //executeItem.CMD_AGV_PRIORITY = "1";
                                                //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                                //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                                //executeItem.CMD_AGV_TRANS_DATETIME

                                                dgvItemRefresh();

                                                blWaitAutostockResponse = false;
                                                AutostockOperating = false; //結束作業, 重新執行取得自動倉儲模式作業
                                                ReceiveAutostockMsgClear();
                                            }
                                            else
                                            {
                                                blWaitAutostockResponse = false;
                                                blAutostockResponseYes = true;
                                            }
                                        }
                                        else
                                        {
                                            bool cannotFindExecuteSeqDetail = findExecuteSeq();

                                            //找不到執行序號, 有可能被刪除了
                                            if (cannotFindExecuteSeqDetail)
                                            {
                                                blWaitAutostockResponse = false;
                                                blRFIDTagRewrite = false;
                                                blAutostockAutoMode = false;
                                                blMoveIDWriteFinish = false;
                                                blAGVMoveID_1stCheckIssue = false;
                                            }
                                        }
                                    }

                                    DateTime dateTimeAutostockDetailLog = DateTime.Now;

                                    //自動倉儲回應 & 狀態1:正常, 
                                    //跳出, 處理下一階段的回應
                                    if (blAutostockResponseYes
                                        && autostockEntity.dsAutostockServer.TRANS_NO != null
                                        && autostockEntity.dsAutostockServer.STATUS != null
                                        && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-2")
                                        && autostockEntity.dsAutostockServer.STATUS.Equals("1")
                                        )
                                    {
                                        //執行AGV cmd, 不執行autostock cmd
                                        //executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                        //Autostock
                                        //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                        //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = "";
                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockKeepingOperating.ToString();
                                        //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                        //AGV
                                        //executeItem.CMD_AGV_PRIORITY = "1";
                                        //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                        //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                        //executeItem.CMD_AGV_TRANS_DATETIME

                                        dgvItemRefresh();

                                        if (IsBuffer(executeItem.AGV_TO_ST))
                                        {
                                            bool blSetArrangedTask = false;

                                            //第一次針對設定站台判斷
                                            for (int i = 0; i < listBufferStatus.Count; i++)
                                            {
                                                if (listBufferStatus[i].StationNo.Equals(executeItem.AGV_TO_ST)
                                                    && listBufferStatus[i].IsEmpty == "9"
                                                    && listBufferStatus[i].ArrangedTask == "9")
                                                {
                                                    BufferStatus item = new BufferStatus();

                                                    item = listBufferStatus[i];

                                                    item.ArrangedTask = "1";
                                                    //item.ItemNo = executeItem.ITEM_NO;
                                                    //item.InOutFlag = executeItem.INOUT_FLAG;
                                                    //item.PriorityArea = executeItem.PRIORITY_AREA;

                                                    executeItem.AGV_TO_ST = item.StationNo;
                                                    lblBufferSetArrangedTaskTrigger(item);
                                                    blSetArrangedTask = true;

                                                    break;
                                                }
                                            }

                                            if (!blSetArrangedTask)
                                            {
                                                DateTime dateTimeAutostockBufferArranged = DateTime.Now;

                                                msg = string.Format("{0}：step Autostocking:1st buffer:{1}, 非空站台或站台已被預定, 找尋其它空站台, the station is not empty or the station has been arragned, find other unused station, seq:{2} \r\n", dateTimeAutostockBufferArranged.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.AGV_TO_ST, executeItem.EXECUTE_SEQ);
                                                //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                entityMsg.LOG_MESSAGE = msg;

                                                Console.WriteLine(msg);
                                                tbxLogAddMessageTrigger(entityMsg);

                                                //第二次站台判斷
                                                for (int i = 0; i < listBufferStatus.Count; i++)
                                                {
                                                    if (listBufferStatus[i].IsEmpty == "9"
                                                        && listBufferStatus[i].ArrangedTask == "9")
                                                    {
                                                        BufferStatus item = new BufferStatus();

                                                        item = listBufferStatus[i];

                                                        item.ArrangedTask = "1";
                                                        //item.ItemNo = executeItem.ITEM_NO;
                                                        //item.InOutFlag = executeItem.INOUT_FLAG;
                                                        //item.PriorityArea = executeItem.PRIORITY_AREA;

                                                        executeItem.AGV_TO_ST = item.StationNo;
                                                        lblBufferSetArrangedTaskTrigger(item);
                                                        blSetArrangedTask = true;

                                                        DateTime dateTimeAutostockBufferArranged2nd = DateTime.Now;

                                                        msg = string.Format("{0}：step Autostocking:1st buffer:{1}, 非空站台或站台已被預定, 找尋其它空站台, the station is not empty or the station has been arragned, find other unused station, seq:{2} \r\n", dateTimeAutostockBufferArranged2nd.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.AGV_TO_ST, executeItem.EXECUTE_SEQ);
                                                        //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                        entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                        entityMsg.LOG_MESSAGE = msg;

                                                        Console.WriteLine(msg);
                                                        tbxLogAddMessageTrigger(entityMsg);

                                                        break;
                                                    }
                                                }

                                                if (!blSetArrangedTask)
                                                {
                                                    DateTime dateTimeAutostockBufferArranged3rd = DateTime.Now;

                                                    msg = string.Format("{0}：step Autostocking:2nd 找不到其它空站台, 依照原目的地站台搬出, 2nd there is no empty station, Move the item out to the original destination, seq:{2} \r\n", dateTimeAutostockBufferArranged3rd.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.AGV_TO_ST, executeItem.EXECUTE_SEQ);
                                                    //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                    entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                    entityMsg.LOG_MESSAGE = msg;

                                                    Console.WriteLine(msg);
                                                    tbxLogAddMessageTrigger(entityMsg);
                                                }
                                            }
                                        }

                                        ReceiveAutostockMsgClear();

                                        continue;
                                    }
                                    //異常
                                    else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-2")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("9"))
                                    {
                                        bool blKillExecute = true;

                                        //出庫
                                        //error code:2001
                                        //2024/4/8 因入庫改為RFID品番一致後就結束上位PC任務(1001-13 & 1001-14), 但自動倉儲管理系統還在排隊等待執行, 當自動倉儲尚未結ST5任務時,
                                        //有可能上位PC會再發一筆入/出庫任務給自動倉儲管理系統, 此時就會出現1001(入庫) & 2001(出庫)的error code,
                                        //需重新執行取得自動倉儲模式(0009-1 & 0009-2), 直到正確執行或取消任務, 否則一直loop
                                        if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2001"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-複数出庫指示, issue, multiple out stock command");

                                            //執行autostock cmd, 
                                            //executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                            //Autostock
                                            //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                            executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;                                          

                                            //AGV不動
                                            //executeItem.CMD_AGV_PRIORITY = "1";
                                            //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                            //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AGV_TRANS_DATETIME

                                            dgvItemRefresh();

                                            blWaitAutostockResponse = false;
                                            AutostockOperating = false; //結束作業, 重新執行取得自動倉儲模式作業

                                            blKillExecute = false;

                                            Thread.Sleep(15000); //15秒等待自動倉儲結束作業
                                        }
                                        //error code:2002
                                        else if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2002"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-在庫無し, issue, this item is not in stock");
                                        }
                                        //error code:2003
                                        //等待修正後再繼續執行
                                        else if (autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2003"))
                                        {
                                            ErrorMsgLogRecord(autostockEntity.dsAutostockServer.ERROR_CODE, ":" + "異常-データ修正中, issue, data is being modified");

                                            //執行autostock cmd, 
                                            //executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                            //Autostock
                                            //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                            executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;                                          

                                            //AGV不動
                                            //executeItem.CMD_AGV_PRIORITY = "1";
                                            //executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                            //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AGV_TRANS_DATETIME

                                            dgvItemRefresh();

                                            blWaitAutostockResponse = false;
                                            AutostockOperating = false; //結束作業, 重新執行取得自動倉儲模式作業

                                            blKillExecute = false;

                                            Thread.Sleep(5000); //5秒等待自動倉儲結束作業
                                        }

                                        if (blKillExecute)
                                        {
                                            //刪除此命令
                                            bool killCmd = false;
                                            killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                            if (killCmd)
                                            {
                                                //do nothing 
                                                //AutostockOperating = false;
                                            }
                                        }

                                        ReceiveAutostockMsgClear();
                                    }
                                }
                                //繼續自動倉儲作業, 或 1002-2 & LIFT模式後繼續自動倉儲作業
                                else
                                {
                                    //自動倉儲回應, 狀態1:正常,
                                    if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                        && autostockEntity.dsAutostockServer.STATUS != null
                                        && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-3")
                                        && autostockEntity.dsAutostockServer.STATUS.Equals("1")
                                     )
                                    {
                                        ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                        AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                        AutostockCmdRequest.TRANS_NO = "1002-4";
                                        AutostockCmdRequest.ITEM_NO = autostockEntity.dsAutostockServer.ITEM_NO;
                                        AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                        AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                        AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                        AutostockCmdRequest.STATUS = "1";
                                        AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                        AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        AutostockCmdRequest.ERROR_CODE = "";
                                        AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                        SendTransferDataToAutostock(AutostockCmdRequest);

                                        msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                        //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                        entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                        entityMsg.LOG_MESSAGE = msg;

                                        Console.WriteLine(msg);
                                        tbxLogAddMessageTrigger(entityMsg);

                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                        executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        dgvItemRefresh();

                                        //自動倉儲出庫完成 & 自動倉儲模式：TFR(連動), 換執行AGV配車
                                        if (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("TFR"))
                                        {
                                            //執行AGV cmd, 不執行autostock cmd
                                            executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                            //Autostock
                                            //executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                            //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = "";
                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                            //AGV
                                            //executeItem.CMD_AGV_PRIORITY = "1";
                                            executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                            executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                            //executeItem.CMD_AGV_TRANS_DATETIME

                                            dgvItemRefresh();

                                            AutostockOperating = false;
                                        }
                                        //自動倉儲出庫完成 & 自動倉儲模式：LIFT(單獨), 執行刪除指令
                                        else if (autostockEntity.dsAutostockServer.AUTOSTOCK_MODE.Equals("LIFT"))
                                        {
                                            //Autostock是最後執行的動作, 需要刪除指令
                                            bool killCmd = false;
                                            //DateTime dateTimeAutostocKillCmdkLog = DateTime.Now;

                                            //msg = string.Format("{0}：所有執行命令皆已完成, 刪除此命令, seq:{1} \r\n", dateTimeAutostocKillCmdkLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());
                                            killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                            if (killCmd)
                                            {
                                                AutostockOperating = false;
                                            }
                                        }

                                        ReceiveAutostockMsgClear();
                                    }
                                    //TRANS_NO:1002-11, 出庫作業的入庫完了報告
                                    else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-11")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                    {
                                        ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                        AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                        AutostockCmdRequest.TRANS_NO = "1002-12";
                                        AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                        AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                        AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                        AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                        AutostockCmdRequest.STATUS = "1";
                                        AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                        AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        AutostockCmdRequest.ERROR_CODE = "";
                                        AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                        SendTransferDataToAutostock(AutostockCmdRequest);

                                        msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                        //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                        entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                        entityMsg.LOG_MESSAGE = msg;

                                        Console.WriteLine(msg);
                                        tbxLogAddMessageTrigger(entityMsg);

                                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                        executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                        dgvItemRefresh();

                                        //Autostock是最後執行的動作, 需要刪除指令
                                        bool killCmd = false;
                                        killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                        if (killCmd)
                                        {
                                            AutostockOperating = false;
                                        }

                                        blRFIDTagRewrite = false;
                                        blAutostockAutoMode = false;
                                        blMoveIDWriteFinish = false;
                                        blAGVMoveID_1stCheckIssue = false;

                                        ReceiveAutostockMsgClear();
                                    }
                                    //異常問題處理
                                    else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                            && autostockEntity.dsAutostockServer.STATUS != null
                                            && !autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-3")
                                            && autostockEntity.dsAutostockServer.STATUS.Equals("9"))
                                    {
                                        //TRANS_NO:1002-7, 出庫異常報告
                                        if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                               && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-7"))
                                        {
                                            if (string.IsNullOrWhiteSpace(autostockEntity.dsAutostockServer.checkPoint))
                                            {
                                                ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);
                                                executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;

                                                dgvItemRefresh();
                                                //TRANS_NO:1001-8, 入庫異常報告應答
                                                AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                                AutostockCmdRequest.TRANS_NO = "1002-8";
                                                AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                                AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                                AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                                AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                                AutostockCmdRequest.STATUS = "9";
                                                AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                                AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                                AutostockCmdRequest.ERROR_CODE = "";
                                                AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                                SendTransferDataToAutostock(AutostockCmdRequest);

                                                msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                                //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                entityMsg.LOG_MESSAGE = msg;

                                                Console.WriteLine(msg);
                                                tbxLogAddMessageTrigger(entityMsg);

                                                autostockEntity.dsAutostockServer.checkPoint = "1";
                                            }

                                            //2101:品番不一致	材料編號與RFID不符
                                            if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                                && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2101"))
                                            {
                                                //TRANS_NO:2001-5, RFID_NO rewrite 報告
                                                blRFIDTagRewrite = true;
                                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString();
                                                dgvItemRefresh();
                                            }
                                            //2102:ノーリード	無RFID
                                            else if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                                && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2102"))
                                            {
                                                //do nothing 
                                            }
                                            //2103:タグ故障	RFID故障
                                            else if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                              && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2103"))
                                            {
                                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString();
                                                dgvItemRefresh();
                                            }
                                            //2104:空出庫	系統資料有材料在儲位, 實際上沒有此材料
                                            else if (autostockEntity.dsAutostockServer.ERROR_CODE != null
                                            && autostockEntity.dsAutostockServer.ERROR_CODE.Equals("2104"))
                                            {
                                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString();
                                                dgvItemRefresh();
                                            }

                                            ReceiveAutostockMsgClear();
                                        }

                                        //RFID Tag要Rewrit, 跳出功能畫面
                                        if (blRFIDTagRewrite)
                                        {
                                            RFIDCheckFormOpenTrigger();
                                            bool blWaitUserRewriteReponse = true;

                                            while (blWaitUserRewriteReponse)
                                            {
                                                if (!string.IsNullOrWhiteSpace(autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS) && !autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("0"))
                                                {
                                                    //1:變更Item_NO
                                                    if (autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("1"))
                                                    {
                                                        executeItem.ITEM_NO = autostockEntity.dsModify.ITEM_NO;
                                                    }
                                                    //2:變更RFID_No
                                                    else if (autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("2"))
                                                    {
                                                        executeItem.RFID_NO = autostockEntity.dsModify.RFID_NO;
                                                    }
                                                    else if (autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("3"))
                                                    {
                                                        //do nothing                                                        
                                                    }

                                                    blWaitUserRewriteReponse = false;
                                                    blRFIDTagRewrite = false;
                                                }

                                                //已經執行過西部電機修改, 並收到出庫完了, 但上位PC回應要修改發送指令給管理機, 此時強制改為已由西部電機修改
                                                if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                                   && autostockEntity.dsAutostockServer.STATUS != null
                                                   && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-3")
                                                   && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                                {
                                                    autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS = "3";
                                                }

                                                bool cannotFindExecuteSeqDetail = findExecuteSeq();

                                                //找不到執行序號, 有可能被刪除了
                                                if (cannotFindExecuteSeqDetail)
                                                {
                                                    blWaitUserRewriteReponse = false;
                                                    blRFIDTagRewrite = false;
                                                    blAutostockAutoMode = false;
                                                    blMoveIDWriteFinish = false;
                                                    blAGVMoveID_1stCheckIssue = false;
                                                }
                                            }

                                            if ((autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("1")
                                                || autostockEntity.dsModify.RFID_REWRITE_RESPONSE_STATUS.Equals("2"))
                                                && !(autostockEntity.dsAutostockServer.TRANS_NO != null
                                                   && autostockEntity.dsAutostockServer.STATUS != null
                                                   && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-3")
                                                   && autostockEntity.dsAutostockServer.STATUS.Equals("1"))
                                               )
                                            {
                                                //TRANS_NO:1002-5, RFID_NO rewrite 報告                                                
                                                AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                                AutostockCmdRequest.TRANS_NO = "1002-5";
                                                AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                                AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                                AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServerKeepData.SHELF_NO;
                                                AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                                AutostockCmdRequest.STATUS = "9";
                                                AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServerKeepData.AUTOSTOCK_MODE;
                                                AutostockCmdRequest.RFID_NO = executeItem.RFID_NO;
                                                AutostockCmdRequest.ERROR_CODE = "";
                                                AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                                SendTransferDataToAutostock(AutostockCmdRequest);

                                                msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                                //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                                entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                                entityMsg.LOG_MESSAGE = msg;

                                                Console.WriteLine(msg);
                                                tbxLogAddMessageTrigger(entityMsg);

                                                bool blWaitAutostockRewriteReponse = true;

                                                while (blWaitAutostockRewriteReponse)
                                                {
                                                    //TRANS_NO:1002-6, RFID_NO rewrite 報告應答
                                                    if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                                        && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-6"))
                                                    {
                                                        ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                                        executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;

                                                        dgvItemRefresh();
                                                        blWaitAutostockRewriteReponse = false;

                                                        ReceiveAutostockMsgClear();
                                                    }

                                                    bool cannotFindExecuteSeqDetail = findExecuteSeq();

                                                    //找不到執行序號, 有可能被刪除了
                                                    if (cannotFindExecuteSeqDetail)
                                                    {
                                                        blWaitAutostockRewriteReponse = false;
                                                        blRFIDTagRewrite = false;
                                                        blAutostockAutoMode = false;
                                                        blMoveIDWriteFinish = false;
                                                        blAGVMoveID_1stCheckIssue = false;
                                                    }
                                                }
                                            }
                                        }
                                        //TRANS_NO:1002-9, 出庫異常完了報告-刪除
                                        else if (autostockEntity.dsAutostockServer.TRANS_NO != null
                                                && autostockEntity.dsAutostockServer.TRANS_NO.Equals("1002-9"))
                                        {
                                            ReceiveAutostockMsgRecord(autostockEntity.dsAutostockServer);

                                            AutostockCmdRequest.EXECUTE_SEQ = executeItem.EXECUTE_SEQ;
                                            AutostockCmdRequest.TRANS_NO = "1002-10";
                                            AutostockCmdRequest.ITEM_NO = executeItem.ITEM_NO;
                                            AutostockCmdRequest.STOCK_NO = "SEIBU-AS01";
                                            AutostockCmdRequest.SHELF_NO = autostockEntity.dsAutostockServer.SHELF_NO;
                                            AutostockCmdRequest.INOUT_FLAG = executeItem.INOUT_FLAG.ToUpper().Equals("IN") ? "1" : "2";
                                            AutostockCmdRequest.STATUS = "9";
                                            AutostockCmdRequest.AUTOSTOCK_MODE = autostockEntity.dsAutostockServer.AUTOSTOCK_MODE;
                                            AutostockCmdRequest.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;
                                            AutostockCmdRequest.ERROR_CODE = "";
                                            AutostockCmdRequest.PRIORITY_AREA = executeItem.PRIORITY_AREA;

                                            SendTransferDataToAutostock(AutostockCmdRequest);

                                            msg = string.Format("{0}：step Autostocking:發送指令(send command):{1}, Autostock Mode:{2}, Item No:{3}, seq:{4} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, AutostockCmdRequest.AUTOSTOCK_MODE, AutostockCmdRequest.ITEM_NO, executeItem.EXECUTE_SEQ);
                                            //msg = string.Format("{0}：step Autostocking:Autostock發送指令:{1}, seq:{2} \r\n", dateTimeAutostockLog.ToString("yyyy-MM-dd HH:mm:ss"), AutostockCmdRequest.TRANS_NO, executeItem.EXECUTE_SEQ);
                                            entityMsg.CREATE_DATETIME = dateTimeAutostockLog;
                                            entityMsg.LOG_MESSAGE = msg;

                                            Console.WriteLine(msg);
                                            tbxLogAddMessageTrigger(entityMsg);

                                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.AutostockOperationFinish.ToString();
                                            executeItem.RFID_NO = autostockEntity.dsAutostockServer.RFID_NO;

                                            dgvItemRefresh();

                                            //Autostock是最後執行的動作, 需要刪除指令
                                            bool killCmd = false;
                                            killCmd = CancelExecuteSEQ(); //msg, dateTimeAutostocKillCmdkLog

                                            if (killCmd)
                                            {
                                                AutostockOperating = false;
                                            }

                                            blRFIDTagRewrite = false;
                                            blAutostockAutoMode = false;
                                            blMoveIDWriteFinish = false;
                                            blAGVMoveID_1stCheckIssue = false;

                                            ReceiveAutostockMsgClear();
                                        }

                                        ////第一次異常問題處理
                                        //if (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS != AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString())
                                        //{

                                        //}
                                        ////第二次後異常問題處理
                                        //else if (executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS == AutostockExecutingStatusFlag.AutostockIssueKeepingOperating.ToString())
                                        //{


                                        //}
                                    }
                                }
                            }

                            bool cannotFindExecuteSeq = findExecuteSeq();

                            //找不到執行序號, 有可能被刪除了
                            if (cannotFindExecuteSeq)
                            {
                                AutostockOperating = false;
                                blRFIDTagRewrite = false;
                                blAutostockAutoMode = false;
                                blMoveIDWriteFinish = false;
                                blAGVMoveID_1stCheckIssue = false;
                                ReceiveAutostockMsgClear();
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();

                DateTime dateTimeLog = DateTime.Now;
                dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                string msg = string.Empty;

                msg = string.Format("{0}：exception msg:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), ex);

                entityMsg.CREATE_DATETIME = dateTimeLog;
                entityMsg.LOG_MESSAGE = msg;
                Console.WriteLine(msg);
                tbxLogAddMessageTrigger(entityMsg);
                //dgvItemRefresh();

                throw ex;
            }
        }

        /// <summary>
        /// step 1:開始執行cmd, begin Execute cmd
        ///檢查執行清單中是否有資料, 如有資料則檢查是否有正在執行的任務, 有：繼續執行, 無：將第一筆資料變更為執行狀態
        /// </summary>
        private void getExecuteCmd()
        {
            liExecuteSource = (BindingList<AGVTaskModel>)dgvItem.DataSource; //只取資料用, 沒有雙向繫結, 未同步變更資料
            runningData = liExecuteSource.Where(w => w.EXECUTE_STATUS != StatusFlag.Line.ToString()).Count();

            //AGVTaskModel executeItem = new AGVTaskModel();

            DateTime dateTimeLog = DateTime.Now;
            string msg = string.Empty;

            //step 1-1
            //執行清單有資料但沒有正在執行的任務, 將第一筆資料變更為執行狀態, 並確認是否有要執行項目
            //Executing
            if (runningData == 0)
            {
                //int AGVCurrentStatus = agvEntity.dictAGVDword[getMoveAreaDWord(1, AGVMoveAreaDetail.MovingCurrentStatus)]; //AGV 1號機, 狀態, D476
                //int AGVCurrentExecuteMoveID = agvEntity.dictAGVDword[getMoveAreaDWord(1, AGVMoveAreaDetail.MovingCurrentMoveID)];

                AGVTaskModel dgvFirstRowData = liExecuteSource.FirstOrDefault(); //從dgvItem取資料第一筆資料, 因沒有雙向繫結, 在下一步取原始資料                 
                executeItem = liExecuteSource.Where(w => w.EXECUTE_SEQ == dgvFirstRowData.EXECUTE_SEQ).FirstOrDefault(); //取得對應的原始資料, 用這做資料變更

                //step 1-1-1
                //AGV、autostock皆正常連線, 正常執行
                if (getAGVConnectStatus() && getAutostockConnectStatus())
                {
                    if (executeItem != null)
                    {
                        executeItem.EXECUTE_STATUS = StatusFlag.Executing.ToString();
                        //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                        msg = string.Format("{0}：step 1-1-1:runningData = 0, change EXECUTE_STATUS of first row data, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                    }
                }
                //step 1-1-2
                //AGV不連線, autostock連線, 僅做autostock入/出庫的指令, 但不做agv移動的指令
                else if (!getAGVConnectStatus() && getAutostockConnectStatus())
                {
                    //in & ount
                    if (executeItem != null
                        && executeItem.INOUT_FLAG == InOutStockFlag.In.ToString() || executeItem.INOUT_FLAG == InOutStockFlag.Out.ToString())
                    {
                        executeItem.EXECUTE_STATUS = StatusFlag.Executing.ToString();
                        //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                        msg = string.Format("{0}：step 1-1-2:runningData = 0, change EXECUTE_STATUS of first row data, seq:{1}, agv disconnect \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                    }
                    //dispatch (delete cmd)
                    else if (executeItem != null)
                    {
                        //decimal maxSortData = liComputer.Max(m => m.EXECUTE_SORT);
                        //executeItem.EXECUTE_SORT = maxSortData + 1;
                        msg = string.Format("{0}：step 1-1-2:runningData = 0, change EXECUTE_STATUS of first row data, seq:{1}, but agv disconnect, and this is in/out = dispatch, so remove current cmd, keeping execute next cmd \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ, executeItem.INOUT_FLAG);

                        dgvItemRowsRemoveAt(executeItem);
                    }
                }
                //step 1-1-3
                //AGV連線, autostock不連線, 僅做AGV調度(dispatch)的指令, 但不做autostock入出庫的指令
                else if (getAGVConnectStatus() && !getAutostockConnectStatus())
                {
                    if (executeItem != null)
                    {
                        //dispatch
                        if (executeItem.INOUT_FLAG == InOutStockFlag.Dispatch.ToString())
                        {
                            executeItem.EXECUTE_STATUS = StatusFlag.Executing.ToString();
                            //executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                            msg = string.Format("{0}：step 1-1-3:runningData = 0, change EXECUTE_STATUS of first row data, seq:{1}, autostock disconnect \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                        }
                        //in & ount
                        else
                        {
                            //decimal maxSortData = liComputer.Max(m => m.EXECUTE_SORT);
                            //executeItem.EXECUTE_SORT = maxSortData + 1;
                            executeItem.EXECUTE_STATUS = StatusFlag.Executing.ToString();
                            msg = string.Format("{0}：step 1-1-3:runningData = 0, change EXECUTE_STATUS of first row data, seq:{1}, but autostock disconnect, and this is in/out = {2} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ, executeItem.INOUT_FLAG);
                        }
                    }
                }

                dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                entityMsg.CREATE_DATETIME = dateTimeLog;
                entityMsg.LOG_MESSAGE = msg;
                Console.WriteLine(msg);
                tbxLogAddMessageTrigger(entityMsg);

                dgvItemRefresh();
            }
            //step 1-2
            //已有執行中資料
            else
            {
                if (liExecuteSource != null)
                {
                    executeItem = liExecuteSource.Where(w => w.EXECUTE_STATUS != StatusFlag.Line.ToString()).FirstOrDefault();

                    dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                    msg = string.Format("{0}：step 1-2:runningData = 1, get executing data, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                    entityMsg.CREATE_DATETIME = dateTimeLog;
                    entityMsg.LOG_MESSAGE = msg;

                    Console.WriteLine(msg);
                    tbxLogAddMessageTrigger(entityMsg);
                }
            }
        }

        private void executeCmdOfInOutFlag()
        {
            //step 2:判斷InOutFlag, 再接續執行該項目,          
            if (runningData > 0 && executeItem != null
               && executeItem.EXECUTE_STATUS == StatusFlag.Executing.ToString())
            {
                string msg = string.Empty;
                DateTime dateTimeLog = DateTime.Now;

                //**step 2-1:In(autostock入庫)：從焊接、Buffer站台取出, 放入自動動倉儲站台
                //step 2-1-1:檢查AGV是否連線(可正常運作)：是
                //step 2-1-1-1:檢查是否為大轉盤的站台：是：
                //step 2-1-1-1-1:檢查buffer站台是否有空位置:是：執行AGV移動將治具放到buffer站台
                //step 2-1-1-1-2:檢查buffer站台是否有空位置:否：將指令取消

                //step 2-1-1-2:檢查是否為鏈條區的站台：是：
                //step 2-1-1-2-1:檢查buffer站台是否有空位置:是：執行AGV移動將治具放到buffer站台
                //step 2-1-1-2-2:檢查buffer站台是否有空位置:否：將指令取消

                //step 2-1-1-3:檢查是否為大轉盤或鏈條區的站台：否：執行發給自動倉儲入庫指令, 再執行AGV移動
                //step 2-1-2:檢查AGV是否連線(可正常運作)：否
                //step 2-1-2-1:不呼叫AGV, 只執行發給自動倉儲的入庫指令

                if (executeItem.INOUT_FLAG == InOutStockFlag.In.ToString())
                {
                    //step 2-1-1:檢查AGV是否連線(可正常運作)：是
                    //if (getAGVConnectStatus())
                    //{
                    //step 2-1-1-1:檢查是否為大轉盤的站台：是 (大轉盤入庫需要先放到暫存區, 冶具整理後再入庫到自動倉儲)
                    //從站台入庫到自動倉儲、暫存區：檢查From ST
                    if (IsRotate(executeItem.AGV_FROM_ST))
                    {
                        //step 2-1-1-1-1:檢查buffer站台是否有空位置:
                        bool blBufferIsEmpty = false; //預設無空位置

                        //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                        for (int i = 0; i < listBufferStatus.Count; i++)
                        {
                            BufferStatus item = new BufferStatus();

                            item = listBufferStatus[i];

                            if (item.IsEmpty.Equals("9") && item.ArrangedTask.Equals("9") && !blBufferIsEmpty)
                            {
                                item.ArrangedTask = "1";
                                //item.ItemNo = executeItem.ITEM_NO;
                                //item.InOutFlag = executeItem.INOUT_FLAG;
                                //item.PriorityArea = executeItem.PRIORITY_AREA;

                                executeItem.AGV_TO_ST = item.StationNo;
                                lblBufferSetArrangedTaskTrigger(item);

                                //執行AGV cmd, 不執行autostock cmd
                                executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                //Autostock
                                executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = "";
                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = "";
                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                //AGV
                                executeItem.CMD_AGV_PRIORITY = "1";
                                executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                //executeItem.CMD_AGV_TRANS_DATETIME

                                dgvItemRefresh();

                                blBufferIsEmpty = true;

                                msg = string.Format("{0}：step 2-1-1-1-1:InOutFlag:In, 出發地是大轉盤, 且buffer站台有空位置, From ST is rotate, and there is an empty space on the buffer station, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                break;
                            }
                        }

                        //step 2-1-1-1-1:檢查buffer站台是否有空位置:是：執行AGV移動將治具放到buffer站台
                        if (blBufferIsEmpty)
                        {
                            //do nothing
                        }
                        //step 2-1-1-1-2:檢查buffer站台是否有空位置:否：將指令取消
                        if (!blBufferIsEmpty)
                        {
                            //msg = string.Format("{0}：step 2-1-1-1-2:InOutFlag:In, 出發地是大轉盤, 但buffer站台無位置, From ST is rotate, but there is no location on the buffer station , seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            //dgvItemRowsRemoveAt(executeItem);

                            msg = string.Format("{0}：step 2-1-1-1-2:InOutFlag:In, 出發地是大轉盤, 但buffer站台無位置, From ST is rotate, but there is no location on the buffer station , seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            MessageBoxTrigger(msg, executeItem);

                            Thread.Sleep(10000); //10秒等待buffer站台位置空出

                        }

                    }
                    //step 2-1-1-2:檢查是否為鏈條區的站台：是：(鏈條區入庫需要先放到暫存區, 冶具整理後再入庫到自動倉儲)
                    //從站台入庫到自動倉儲、暫存區：檢查From ST
                    else if (IsChainOut(executeItem.AGV_FROM_ST))
                    {
                        //step 2-1-1-2-1:檢查buffer站台是否有空位置:
                        bool blBufferIsEmpty = false; //預設無空位置

                        //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                        for (int i = 0; i < listBufferStatus.Count; i++)
                        {
                            BufferStatus item = new BufferStatus();

                            item = listBufferStatus[i];

                            if (item.IsEmpty.Equals("9") && item.ArrangedTask.Equals("9") && !blBufferIsEmpty)
                            {
                                item.ArrangedTask = "1";
                                //item.ItemNo = executeItem.ITEM_NO;
                                //item.InOutFlag = executeItem.INOUT_FLAG;
                                //item.PriorityArea = executeItem.PRIORITY_AREA;

                                executeItem.AGV_TO_ST = item.StationNo;
                                lblBufferSetArrangedTaskTrigger(item);

                                //執行AGV cmd, 不執行autostock cmd
                                executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                //Autostock
                                executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = "";
                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = "";
                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                //AGV
                                executeItem.CMD_AGV_PRIORITY = "1";
                                executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                //executeItem.CMD_AGV_TRANS_DATETIME

                                dgvItemRefresh();

                                blBufferIsEmpty = true;

                                msg = string.Format("{0}：step 2-1-1-2-1:InOutFlag:In, 出發地是鏈條區, 且buffer站台有空位置, From ST is chain zone, and there is an empty space on the buffer station, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                break;
                            }
                        }

                        //step 2-1-1-2-1:檢查buffer站台是否有空位置:是：執行AGV移動將治具放到buffer站台
                        if (blBufferIsEmpty)
                        {
                            //do nothing
                        }
                        //step 2-1-1-2-2:檢查buffer站台是否有空位置:否：將指令取消
                        if (!blBufferIsEmpty)
                        {
                            //msg = string.Format("{0}：step 2-1-1-2-2:InOutFlag:In, 出發地是鏈條區, 但buffer站台無位置, From ST is chain zone, but there is no location on the buffer station , seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            //dgvItemRowsRemoveAt(executeItem);

                            msg = string.Format("{0}：step 2-1-1-2-2:InOutFlag:In, 出發地是鏈條區, 但buffer站台無位置, From ST is chain zone, but there is no location on the buffer station , seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);
                            MessageBoxTrigger(msg, executeItem);
                            Thread.Sleep(10000); //10秒等待buffer站台位置空出

                        }
                    }
                    //step 2-1-1-2:檢查是否為大轉盤的站台：否：執行發給自動倉儲入庫指令, 再執行AGV移動
                    //step 2-1-1-3:檢查是否為大轉盤的站台：否：執行發給自動倉儲入庫指令, 再執行AGV移動
                    else
                    {
                        msg = string.Format("{0}：step 2-1-1-2:InOutFlag:In, 出發地非大轉盤或鏈條區, From ST is not rotate or chain zone, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                        executeItem.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                        executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                        //Autostock
                        executeItem.CMD_AUTOSTOCK_PRIORITY = "1";
                        executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                        executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                        //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                        //AGV
                        executeItem.CMD_AGV_PRIORITY = "2";
                        executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                        executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.WaitingAutostock.ToString();
                        //executeItem.CMD_AGV_TRANS_DATETIME

                        dgvItemRefresh();
                    }
                }

                //**step 2-2:Out(autostock出庫)：從自動動倉儲、Buffer站台取出, 放入焊接站台、Buffer站台
                //step 2-2-1:檢查是否為大轉盤:是
                //step 2-2-1-1:檢查大轉盤是否有空位置:否, 取消指令
                //step 2-2-1-2:檢查大轉盤是否有空位置:是
                //step 2-2-1-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                //step 2-2-1-2-2:檢查buffer站台找尋Item NO: 無Item No:
                //step 2-2-1-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                //step 2-2-1-2-2-2:執行自動倉儲出庫指令, 檢查是否有Item No:無Item No:須記錄log並取消cmd
                //step 2-2-2:檢查是否為大轉盤:否 (以下同2-2-1-2指令)
                //step 2-2-2-1:檢查是否為Buffer站台：是
                //step 2-2-2-1-1:檢查buffer站台是否有空位置：否, 取消指令
                //step 2-2-2-1-2:檢查buffer站台是否有空位置：是
                //step 2-2-2-1-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                //step 2-2-2-1-2-2:檢查buffer站台找尋Item NO: 無Item No:
                //step 2-2-2-1-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                //step 2-2-2-1-2-2-2:執行自動倉儲出庫指令, 檢查是否有Item No:無Item No:須記錄log並取消cmd
                //step 2-2-2-2:檢查是否為Buffer區：否                
                //step 2-2-2-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                //step 2-2-2-2-2:檢查buffer站台找尋Item NO: 無Item No:
                //step 2-2-2-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                //step 2-2-2-2-2-2:執行自動倉儲出庫指令, 檢查是否有Item No:無Item No:須記錄log並取消cmd
                else if (executeItem.INOUT_FLAG == InOutStockFlag.Out.ToString())
                {
                    //step 2-2-1:檢查是否為大轉盤:是
                    //從自動倉儲出庫到站台：檢查To ST
                    if (IsRotate(executeItem.AGV_TO_ST))
                    {
                        //step 2-2-1-1:檢查大轉盤是否有空位置:否, 取消指令
                        if (!checkRotateIsEmpty())
                        {
                            msg = string.Format("{0}：step 2-2-1-1:InOutFlag:Out, 目的地是大轉盤, 但大轉盤站台有Item, To ST is rotate, but Rotate has Item, seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            dgvItemRowsRemoveAt(executeItem);
                        }
                        //step 2-2-1-2:檢查大轉盤是否有空位置:是
                        else
                        {
                            //step 2-2-1-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                            if (findItemNoFromBuffer())
                            {
                                msg = string.Format("{0}：step 2-2-1-2-1:InOutFlag:Out, 目的地是大轉盤, 且大轉盤站台有空位置, 在buffer站台找到冶具, To ST is rotate, and Rotate has no Item, and find the Item on the buffer station, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                //executeItem.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                                executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                //Autostock
                                executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                //AGV
                                executeItem.CMD_AGV_PRIORITY = "1";
                                executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                //executeItem.CMD_AGV_TRANS_DATETIME

                                dgvItemRefresh();
                            }
                            //step 2-2-1-2-2:檢查buffer站台找尋Item NO: 無Item No:
                            else
                            {
                                //step 2-2-1-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                                msg = string.Format("{0}：step 2-2-1-2-2-1:InOutFlag:Out, 目的地是大轉盤, 且大轉盤站台有空位置, 在buffer站台找不到冶具, 執行自動倉儲出庫指令, To ST is rotate, and Rotate has no Item, and the item can not be found on the buffer station, execute autostock out stock command, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                executeItem.AGV_FROM_ST = ((int)AGVStation.ST1_Default).ToString();
                                executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                //Autostock
                                executeItem.CMD_AUTOSTOCK_PRIORITY = "1";
                                executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                //AGV
                                executeItem.CMD_AGV_PRIORITY = "2";
                                executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.WaitingAutostock.ToString();
                                //executeItem.CMD_AGV_TRANS_DATETIME

                                dgvItemRefresh();
                            }
                        }
                    }
                    //step 2-2-2-1:檢查是否為Buffer站台：是        
                    else if (IsBuffer(executeItem.AGV_TO_ST))
                    {
                        //step 2-2-2-1-1:檢查buffer站台是否有空位置：否, 取消指令
                        if (!checkBufferIsEmpty())
                        {
                            msg = string.Format("{0}：step 2-2-2-1-1:InOutFlag:Out, 目的地是Buffer站台, 但Buffer站台無空位置, TO ST is Buffer, but Buffer has Item, seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            dgvItemRowsRemoveAt(executeItem);
                        }
                        //step 2-2-2-1-2:檢查buffer站台是否有空位置：是
                        else
                        {
                            //step 2-2-2-1-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                            if (findItemNoFromBuffer())
                            {
                                if (executeItem.AGV_FROM_ST.Equals(executeItem.AGV_TO_ST))
                                {
                                    msg = string.Format("{0}：step 2-2-2-1-2-0:InOutFlag:Out, buffer站台找到冶具, 但出發地與目的地是同Buffer站台, find the Item on the buffer station, but From ST and To ST are on the same buffer station, seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                    dgvItemRowsRemoveAt(executeItem);

                                }
                                else
                                {

                                    msg = string.Format("{0}：step 2-2-2-1-2-1:InOutFlag:Out, 目的地是Buffer站台, 且Buffer站台有空位置, buffer站台找到冶具, To ST is Buffer, and buffer has no item, and find the Item on the buffer station, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                                    for (int i = 0; i < listBufferStatus.Count; i++)
                                    {
                                        BufferStatus item = new BufferStatus();

                                        item = listBufferStatus[i];

                                        if (item.StationNo.Equals(executeItem.AGV_TO_ST) && item.IsEmpty.Equals("9") && item.ArrangedTask.Equals("9"))
                                        {
                                            item.ArrangedTask = "1";
                                            //item.ItemNo = executeItem.ITEM_NO;
                                            //item.InOutFlag = executeItem.INOUT_FLAG;
                                            //item.PriorityArea = executeItem.PRIORITY_AREA;

                                            lblBufferSetArrangedTaskTrigger(item); //set buffer arranged task
                                            break;
                                        }
                                    }

                                    //executeItem.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                                    executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                                    //Autostock
                                    executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                                    //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                    //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                    //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                    //AGV
                                    executeItem.CMD_AGV_PRIORITY = "1";
                                    executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                    executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                                    //executeItem.CMD_AGV_TRANS_DATETIME

                                    dgvItemRefresh();
                                }
                            }
                            //step 2-2-2-1-2-2:檢查buffer站台找尋Item NO: 無Item No:
                            else
                            {
                                //step 2-2-2-1-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                                msg = string.Format("{0}：step 2-2-2-1-2-2-1:InOutFlag:Out, 目的地是Buffer站台, 且Buffer站台有空位置, 在buffer站台找不到冶具, 執行自動倉儲出庫指令, To ST is buffer, and buffer has no item, and the item can not be found on the buffer station, execute autostock out stock command, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                executeItem.AGV_FROM_ST = ((int)AGVStation.ST1_Default).ToString();
                                executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                                //Autostock
                                executeItem.CMD_AUTOSTOCK_PRIORITY = "1";
                                executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                                executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                                //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                                //AGV
                                executeItem.CMD_AGV_PRIORITY = "2";
                                executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                                executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.WaitingAutostock.ToString();
                                //executeItem.CMD_AGV_TRANS_DATETIME

                                dgvItemRefresh();
                            }

                        }
                    }
                    //step 2-2-2-2:檢查是否為Buffer區：否  
                    else
                    {
                        //step 2-2-2-2-1:檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                        if (findItemNoFromBuffer())
                        {
                            msg = string.Format("{0}：step 2-2-2-2-1:InOutFlag:Out, 目的地是其它站台, 在buffer站台找到冶具, To ST is not rotate, and find the Item on the buffer station, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            //executeItem.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                            executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                            //Autostock
                            executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                            //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                            //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                            //AGV
                            executeItem.CMD_AGV_PRIORITY = "1";
                            executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                            executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                            //executeItem.CMD_AGV_TRANS_DATETIME

                            dgvItemRefresh();
                        }
                        //step 2-2-2-2-2:檢查buffer站台找尋Item NO: 無Item No:
                        else
                        {
                            //step 2-2-2-2-2-1:執行自動倉儲出庫指令, 檢查是否有Item No:有Item No:接續執行, 並執行AGV移動
                            msg = string.Format("{0}：step 2-2-2-2-2-1:InOutFlag:Out, 目的地是其它站台, 在buffer站台找不到冶具, 執行自動倉儲出庫指令, To ST is not rotate, and the item can not be found on the buffer station, execute autostock out stock command, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            executeItem.AGV_FROM_ST = ((int)AGVStation.ST1_Default).ToString();
                            executeItem.EXECUTE_STATUS = StatusFlag.Autostocking.ToString();

                            //Autostock
                            executeItem.CMD_AUTOSTOCK_PRIORITY = "1";
                            executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                            executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                            //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                            //AGV
                            executeItem.CMD_AGV_PRIORITY = "2";
                            executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                            executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.WaitingAutostock.ToString();
                            //executeItem.CMD_AGV_TRANS_DATETIME

                            dgvItemRefresh();
                        }

                    }
                }
                //**step 2-3:Dispatch(調度)：執行AGV移動, 站台到站台, 不須執行自動倉儲指令
                else if (executeItem.INOUT_FLAG == InOutStockFlag.Dispatch.ToString())
                {
                    bool blDispatchExecuting = false;

                    //從非自動倉儲出庫到大轉盤站台：檢查To ST
                    if (IsRotate(executeItem.AGV_TO_ST))
                    {
                        //檢查大轉盤是否有空位置:否, 取消指令
                        if (!checkRotateIsEmpty())
                        {
                            msg = string.Format("{0}：step :InOutFlag:Dispatch, 目的地是大轉盤, 但大轉盤站台有Item, To ST is rotate, but Rotate has Item, seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            dgvItemRowsRemoveAt(executeItem);
                        }
                        //檢查大轉盤是否有空位置:是
                        else
                        {
                            blDispatchExecuting = true;
                        }
                    }
                    //step 2-2-2-1:檢查是否為Buffer站台：是        
                    else if (IsBuffer(executeItem.AGV_TO_ST))
                    {
                        //step 檢查buffer站台是否有空位置：否, 取消指令
                        if (!checkBufferIsEmpty())
                        {
                            msg = string.Format("{0}：step :InOutFlag:Dispatch, 目的地是Buffer站台, 但Buffer站台無空位置, To ST is Buffer, but Buffer has item, seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                            dgvItemRowsRemoveAt(executeItem);
                        }
                        //檢查buffer站台找尋Item NO:有Item No:執行AGV移動將治具放到需求(To ST)站台
                        else if (findItemNoFromBuffer())
                        {
                            if (executeItem.AGV_FROM_ST.Equals(executeItem.AGV_TO_ST))
                            {
                                msg = string.Format("{0}：step 2-2-2-1-2-0:InOutFlag:Dispatch, buffer站台找到冶具, 但出發地與目的地是同Buffer站台, find the Item on the buffer station, but From ST and To ST are on the same buffer station, seq:{1}, 刪除此任務, remove this execute seq \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                                dgvItemRowsRemoveAt(executeItem);
                            }
                            else
                            {
                                //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                                for (int i = 0; i < listBufferStatus.Count; i++)
                                {
                                    BufferStatus item = new BufferStatus();

                                    item = listBufferStatus[i];

                                    if (item.StationNo.Equals(executeItem.AGV_TO_ST) && item.IsEmpty.Equals("9") && item.ArrangedTask.Equals("9"))
                                    {
                                        item.ArrangedTask = "1";
                                        //item.ItemNo = executeItem.ITEM_NO;
                                        //item.InOutFlag = executeItem.INOUT_FLAG;
                                        //item.PriorityArea = executeItem.PRIORITY_AREA;

                                        lblBufferSetArrangedTaskTrigger(item); //set buffer arranged task
                                        break;
                                    }
                                }

                                blDispatchExecuting = true;
                            }
                        }
                        else
                        {
                            blDispatchExecuting = true;
                        }
                    }
                    //到鏈條區
                    else
                    {
                        blDispatchExecuting = true;
                    }

                    if (blDispatchExecuting)
                    {
                        msg = string.Format("{0}：step 2-3:InOutFlag:Dispatch, seq:{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ);

                        //executeItem.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                        executeItem.EXECUTE_STATUS = StatusFlag.AGVing.ToString();

                        //Autostock
                        executeItem.CMD_AUTOSTOCK_PRIORITY = "0";
                        //executeItem.CMD_TRANS_TO_AUTOSTOCK_FLAG = AutostockNormalFeedback.OFF.ToString();
                        //executeItem.CMD_AUTOSTOCK_EXECUTING_STATUS = AutostockExecutingStatusFlag.Start.ToString();
                        //executeItem.CMD_AUTOSTOCK_TRANS_DATETIME;

                        //AGV
                        executeItem.CMD_AGV_PRIORITY = "1";
                        executeItem.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                        executeItem.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Start.ToString();
                        //executeItem.CMD_AGV_TRANS_DATETIME

                        dgvItemRefresh();
                    }
                }

                dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();

                entityMsg.CREATE_DATETIME = dateTimeLog;
                entityMsg.LOG_MESSAGE = msg;
                Console.WriteLine(msg);
                tbxLogAddMessageTrigger(entityMsg);
            }
        }

        public virtual int getMoveAreaDWord(int _MoveIDAreaType, AGVMoveAreaDetail _AreaDetail)
        {
            return getMoveAreaStartDWord(_MoveIDAreaType, _AreaDetail);
        }

        private static int getMoveAreaStartDWord(int _MoveIDAreaType, AGVMoveAreaDetail _AreaDetail)
        {
            int iDWordStart = 0;

            switch (_MoveIDAreaType)
            {
                case 1: //1:move id 1 area
                    switch (_AreaDetail)
                    {
                        case AGVMoveAreaDetail.MoveID:
                            iDWordStart = 12;
                            break;
                        case AGVMoveAreaDetail.FromtST:
                            iDWordStart = 20;
                            break;
                        case AGVMoveAreaDetail.ToST:
                            iDWordStart = 21;
                            break;
                        case AGVMoveAreaDetail.MoveIDWriteToAGVRequest:
                            iDWordStart = 72;
                            break;
                        case AGVMoveAreaDetail.MoveIDWriteToAGVResponse:
                            iDWordStart = 472;
                            break;
                        case AGVMoveAreaDetail.MovingRequest:
                            iDWordStart = 510;
                            break;
                        case AGVMoveAreaDetail.MovingResponse:
                            iDWordStart = 110;
                            break;
                        case AGVMoveAreaDetail.MovingCurrentStatus:
                            iDWordStart = 476;
                            break;
                        case AGVMoveAreaDetail.MovingCurrentPlace:
                            iDWordStart = 511;
                            break;
                        case AGVMoveAreaDetail.MovingCurrentMoveID:
                            iDWordStart = 477;
                            break;
                        default:
                            iDWordStart = 0;
                            break;
                    }

                    break;
                default: //set move id 1 area
                    iDWordStart = 0;
                    break;
            }

            return iDWordStart;
        }

        private bool getAGVConnectStatus()
        {
            if (agvEntity.clientSocket.Connected)
            {
                return true;
            }
            else
            {
                Thread.Sleep(1000);
                if (!agvEntity.clientSocket.Connected)
                {
                    disconnectAGV();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private bool getAutostockConnectStatus()
        {
            if (autostockEntity.clientSocket.Connected)
            {
                return true;
            }
            else
            {
                disconnectAutostock();
                return false;
            }
        }

        private bool IsChainOut(string stationNo)
        {
            if ((int)AGVStation.ST10_ChainOut == int.Parse(stationNo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsChainIn(string stationNo)
        {
            if ((int)AGVStation.ST11_ChainIn == int.Parse(stationNo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRotate(string stationNo)
        {
            if ((int)AGVStation.ST30_Rotate1 == int.Parse(stationNo) ||
                (int)AGVStation.ST31_Rotate2 == int.Parse(stationNo) ||
                (int)AGVStation.ST32_Rotate3 == int.Parse(stationNo) ||
                (int)AGVStation.ST33_Rotate4 == int.Parse(stationNo) ||
                (int)AGVStation.ST34_Rotate5 == int.Parse(stationNo) ||
                (int)AGVStation.ST35_Rotate6 == int.Parse(stationNo) ||
                (int)AGVStation.ST36_Rotate7 == int.Parse(stationNo) ||
                (int)AGVStation.ST37_Rotate8 == int.Parse(stationNo) ||
                (int)AGVStation.ST50_Rotate9 == int.Parse(stationNo) ||
                (int)AGVStation.ST51_Rotate10 == int.Parse(stationNo) ||
                (int)AGVStation.ST52_Rotate11 == int.Parse(stationNo) ||
                (int)AGVStation.ST53_Rotate12 == int.Parse(stationNo) ||
                (int)AGVStation.ST54_Rotate13 == int.Parse(stationNo) ||
                (int)AGVStation.ST55_Rotate14 == int.Parse(stationNo) ||
                (int)AGVStation.ST56_Rotate15 == int.Parse(stationNo) ||
                (int)AGVStation.ST57_Rotate16 == int.Parse(stationNo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 要搬冶具到目的地(To ST), 檢查大轉盤站台是否為空
        /// </summary>
        /// <returns></returns>
        private bool checkRotateIsEmpty()
        {
            RotateStatus RotateItem = listRotateStatus.Where(w => w.StationNo.Equals(executeItem.AGV_TO_ST.ToString())).SingleOrDefault();

            if (RotateItem.IsEmpty.Equals("1"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 檢查是否為buffer站台, mainform 也有相同檢查, 如修改須一併調整
        /// </summary>
        /// <param name="stationNo"></param>
        /// <returns></returns>
        private bool IsBuffer(string stationNo)
        {
            if ((int)AGVStation.ST20_Buffer1 == int.Parse(stationNo) ||
                  (int)AGVStation.ST21_Buffer2 == int.Parse(stationNo) ||
                  (int)AGVStation.ST22_Buffer3 == int.Parse(stationNo) ||
                  (int)AGVStation.ST23_Buffer4 == int.Parse(stationNo) ||
                  (int)AGVStation.ST24_Buffer5 == int.Parse(stationNo) ||
                  (int)AGVStation.ST25_Buffer6 == int.Parse(stationNo) ||
                  (int)AGVStation.ST26_Buffer7 == int.Parse(stationNo) ||
                  (int)AGVStation.ST40_Buffer8 == int.Parse(stationNo) ||
                  (int)AGVStation.ST41_Buffer9 == int.Parse(stationNo) ||
                  (int)AGVStation.ST42_Buffer10 == int.Parse(stationNo) ||
                  (int)AGVStation.ST43_Buffer11 == int.Parse(stationNo) ||
                  (int)AGVStation.ST44_Buffer12 == int.Parse(stationNo) ||
                  (int)AGVStation.ST45_Buffer13 == int.Parse(stationNo) ||
                  (int)AGVStation.ST46_Buffer14 == int.Parse(stationNo) ||
                  (int)AGVStation.ST47_Buffer15 == int.Parse(stationNo) ||
                  (int)AGVStation.ST48_Buffer16 == int.Parse(stationNo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 要搬冶具到目的地(To ST), 檢查buffer站台是否為空, 且沒被預訂任務
        /// </summary>
        /// <returns></returns>
        private bool checkBufferIsEmpty()
        {
            BufferStatus BufferItem = listBufferStatus.Where(w => w.StationNo.Equals(executeItem.AGV_TO_ST.ToString())).SingleOrDefault();

            //檢查buffer站台是否為空位置 and 沒有被預訂任務
            if (BufferItem.IsEmpty.Equals("9") && BufferItem.ArrangedTask.Equals("9"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool findItemNoFromBuffer()
        {
            bool result = false;

            //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
            for (int i = 0; i < listBufferStatus.Count; i++)
            {
                BufferStatus item = new BufferStatus();
                item = listBufferStatus[i];

                //找到治具將從此Buffer站台開始搬送(From ST)至目的地站台
                if (item.ItemNo != null && item.ItemNo.Equals(executeItem.ITEM_NO))
                {
                    executeItem.AGV_FROM_ST = item.StationNo;
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 取得AGV PLC的MOVE ID
        /// </summary>
        /// <returns></returns>
        private string getAGVPLCMoveID(int AGVNo)
        {
            string AGVPLC_MoveID = string.Empty;
            int beginDword = 0;

            //if (AGVNo == 1)
            //{
            //    beginDword = 477;
            //}
            //else if (AGVNo == 2)
            //{
            //    beginDword = 12;
            //}
            //else if (AGVNo == 3)
            //{
            //    beginDword = 412;
            //}

            //MOVE ID:Dword:D12 D13 D14 D15 D16, block 1 ~ 5
            //Move id value example: 2401060001 (yymmdd+sequence no)
            //for (int i = 0; i < 5; i++)
            //{
            //    AGVPLC_MoveID += getCompileMoveIDDword(beginDword, AGVNo);

            //    beginDword += 1;
            //}

            DateTime dateTimeLog = DateTime.Now;
            dsLOG_MESSAGE entityChangeStage = new dsLOG_MESSAGE();

            string D477 = getCompileMoveIDDword(477, AGVNo);
            string D478 = getCompileMoveIDDword(478, AGVNo);
            string D479 = getCompileMoveIDDword(479, AGVNo);
            string D480 = getCompileMoveIDDword(480, AGVNo);
            string D481 = getCompileMoveIDDword(481, AGVNo);

            AGVPLC_MoveID = D477 + D478 + D479 + D480 + D481;
            //AGVPLC_MoveID = getCompileMoveIDDword(477, AGVNo) + getCompileMoveIDDword(478, AGVNo) + getCompileMoveIDDword(479, AGVNo) + getCompileMoveIDDword(480, AGVNo) + getCompileMoveIDDword(481, AGVNo);

            //string msg = string.Empty;

            // msg = string.Format("{0}：move id:{1} ,D477:{2}, D478:{3}, D479:{4}, D480:{5}, D481:{6} , seq:{7} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), AGVPLC_MoveID, D477, D478, D479, D480, D481, executeItem.EXECUTE_SEQ.ToString());

            //entityChangeStage.CREATE_DATETIME = dateTimeLog;
            //entityChangeStage.LOG_MESSAGE = msg;

            // Console.WriteLine(msg);
            //tbxLogAddMessageTrigger(entityChangeStage);

            return AGVPLC_MoveID;
        }

        /// <summary>
        /// 解析Move ID
        /// </summary>
        /// <param name="dword"></param>
        /// <returns></returns>
        private string getCompileMoveIDDword(int dword, int AGVNo)
        {
            string compileMoveID = string.Empty;
            string resource = string.Empty;

            //Thread.Sleep(10000);

            //for test
            //int D12 = agvEntity.dictAGVDword[12]; //12;
            //int D13 = agvEntity.dictAGVDword[13]; //13;
            //int D14 = agvEntity.dictAGVDword[14]; //14;
            //int D15 = agvEntity.dictAGVDword[15]; //15;
            //int D16 = agvEntity.dictAGVDword[16]; //16;

            //int D412 = agvEntity.dictAGVDword[412]; //412;
            //int D413 = agvEntity.dictAGVDword[413]; //413;
            //int D414 = agvEntity.dictAGVDword[414]; //414;
            //int D415 = agvEntity.dictAGVDword[415]; //415;
            //int D416 = agvEntity.dictAGVDword[416]; //416;

            //AGV MOVE ID
            int D477 = agvEntity.dictAGVDword[477]; //477;
            int D478 = agvEntity.dictAGVDword[478]; //478;
            int D479 = agvEntity.dictAGVDword[479]; //479;
            int D480 = agvEntity.dictAGVDword[480]; //480;
            int D481 = agvEntity.dictAGVDword[481]; //481;
            int D482 = agvEntity.dictAGVDword[482]; //482;
            int D483 = agvEntity.dictAGVDword[483]; //483;
            int D484 = agvEntity.dictAGVDword[484]; //484;

            switch (dword)
            {
                ////for test
                //case 12:
                //    resource = D12.ToString(); //D12 == 0 ? "3030" :
                //    break;
                //case 13:
                //    resource = D13.ToString();  //D13 == 0 ? "3030" :
                //    break;
                //case 14:
                //    resource = D14.ToString();  //D14 == 0 ? "3030" :
                //    break;
                //case 15:
                //    resource = D15.ToString();  //D15 == 0 ? "3030" :
                //    break;
                //case 16:
                //    resource = D16.ToString();  //D16 == 0 ? "3030" :
                //    break;

                //case 412:
                //    resource = D412.ToString(); //D412 == 0 ? "3030" :
                //    break;
                //case 413:
                //    resource = D413.ToString(); //D413 == 0 ? "3030" :
                //    break;
                //case 414:
                //    resource = D414.ToString(); //D414 == 0 ? "3030" :
                //    break;
                //case 415:
                //    resource = D415.ToString(); //D415 == 0 ? "3030" :
                //    break;
                //case 416:
                //    resource = D416.ToString(); //D416 == 0 ? "3030" :
                //    break;

                //official
                case 477:
                    resource = D477.ToString();
                    break;
                case 478:
                    resource = D478.ToString();
                    break;
                case 479:
                    resource = D479.ToString();
                    break;
                case 480:
                    resource = D480.ToString();
                    break;
                case 481:
                    resource = D481.ToString();
                    break;
                case 482:
                    resource = D482.ToString();
                    break;
                case 483:
                    resource = D483.ToString();
                    break;
                case 484:
                    resource = D484.ToString();
                    break;
                default:
                    break;
            }


            if (string.IsNullOrWhiteSpace(resource) || resource.Length != 4 || resource == "0")
            {
                //do nothing 
            }
            else
            {
                ////hex 反轉, ex:680D -> 0D68
                //string hexRotate = resource.Substring(2, 2) + resource.Substring(0, 2);
                ////hex 轉 dec, 16 轉 10 (ASCII代碼),0d68 -> 3432
                //string hexToDec = (Convert.ToInt32(hexRotate, 16)).ToString();
                //Console.WriteLine("move id hexToDec" + hexToDec + "\n");
                //dec 反轉, 3432 -> 3234
                //string decRotate = hexToDec.Substring(2, 2) + hexToDec.Substring(0, 2);
                string decRotate = resource.Substring(2, 2) + resource.Substring(0, 2);
                //Console.WriteLine("move id decRotate" + decRotate + "\n");
                //ascii代碼轉byte
                byte[] asciiToByte = Enumerable.Range(0, decRotate.Length).Where(x => x % 2 == 0).Select(s => Convert.ToByte(decRotate.Substring(s, 2), 16)).ToArray();
                //ascii byte 轉 dec
                string asciiToDec = Encoding.Default.GetString(asciiToByte);
                //Console.WriteLine("move id asciiToDec" + asciiToDec + "\n");

                if (asciiToDec.Length > 0)
                    compileMoveID = asciiToDec;
            }

            return compileMoveID;
        }

        /// <summary>
        /// 重送Json指令給自動倉儲系統
        /// </summary>
        /// <param name="TransferData"></param>
        private void SendTransferDataToAutostock(dsAutoStockTransferJsonModel TransferData)
        {
            string json = JsonConvert.SerializeObject(TransferData);

            DateTime dateTimeAutostockSendMsg = DateTime.Now;
            dsLOG_MESSAGE entitySendMsg = new dsLOG_MESSAGE();
            string receiveMsg = string.Empty;

            receiveMsg = string.Format("{0}：step Autostocking:Send Msg:{1} \r\n", dateTimeAutostockSendMsg.ToString("yyyy-MM-dd HH:mm:ss"), json);

            entitySendMsg.CREATE_DATETIME = dateTimeAutostockSendMsg;
            entitySendMsg.LOG_MESSAGE = receiveMsg;

            Console.WriteLine(receiveMsg);
            tbxLogAddMessageTrigger(entitySendMsg);

            autostockEntity.dsAutostockClient = TransferData;
            var sendJson = AutostockHelper.GetTransMsgByte(json, MessageTypeEnum.StringWord);
            int sendMsgJsonLength = autostockEntity.clientSocket.Send(sendJson);
        }

        private void ReceiveAutostockMsgRecord(dsAutoStockTransferJsonModel TransferData)
        {
            autostockEntity.dsAutostockServerKeepData = TransferData;

            string json = JsonConvert.SerializeObject(TransferData);

            DateTime dateTimeLog = DateTime.Now;
            dsLOG_MESSAGE entityReceiveMsg = new dsLOG_MESSAGE();
            string receiveMsg = string.Empty;

            receiveMsg = string.Format("{0}：step Autostocking:receive json record:{1}, seq:{2} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), json, executeItem.EXECUTE_SEQ);
            entityReceiveMsg.CREATE_DATETIME = dateTimeLog;
            entityReceiveMsg.LOG_MESSAGE = receiveMsg;

            Console.WriteLine(receiveMsg);
            tbxLogAddMessageTrigger(entityReceiveMsg);
        }

        private void ReceiveAutostockMsgClear()
        {
            autostockEntity.dsAutostockServer = new dsAutoStockTransferJsonModel();
        }

        private bool CancelExecuteSEQ(string otherMsg = "") //string msg, DateTime dateTimeAutostocKillCmdkLog
        {
            bool result = false;
            string msg = string.Empty;

            //Autostock是最後執行的動作, 需要刪除指令
            foreach (DataGridViewRow subItem in dgvItem.Rows)
            {
                if (subItem.Cells["EXECUTE_SEQ"].Value.ToString() == executeItem.EXECUTE_SEQ)
                {
                    dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                    DateTime dateTimeAutostocKillCmdkLog = DateTime.Now;

                    if (!string.IsNullOrWhiteSpace(otherMsg))
                    {
                        msg = string.Format("{0}：" + otherMsg + ", seq:{1} \r\n", dateTimeAutostocKillCmdkLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());
                    }
                    else
                    {
                        msg = string.Format("{0}：所有執行命令皆已完成, 刪除此命令, the task finish, delete this command , seq:{1} \r\n", dateTimeAutostocKillCmdkLog.ToString("yyyy-MM-dd HH:mm:ss"), executeItem.EXECUTE_SEQ.ToString());
                    }

                    entityMsg.CREATE_DATETIME = dateTimeAutostocKillCmdkLog;
                    entityMsg.LOG_MESSAGE = msg;

                    Console.WriteLine(msg);
                    tbxLogAddMessageTrigger(entityMsg);

                    dgvItemRowsRemoveAt(executeItem);

                    result = true;
                    break;
                }
            }

            return result;
        }

        private void ErrorMsgLogRecord(string Error_Code, string Error_Msg)
        {
            DateTime dateTimeAutostockErrorLog = DateTime.Now;
            dsLOG_MESSAGE entityErrorMsg = new dsLOG_MESSAGE();
            string msg = string.Empty;

            string error_msg = Error_Code + Error_Msg;

            msg = string.Format("{0}：step Autostocking:Error Code:{1}, seq:{2} \r\n", dateTimeAutostockErrorLog.ToString("yyyy-MM-dd HH:mm:ss"), error_msg, executeItem.EXECUTE_SEQ);
            entityErrorMsg.CREATE_DATETIME = dateTimeAutostockErrorLog;
            entityErrorMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
            tbxLogAddMessageTrigger(entityErrorMsg);
        }

        private bool findExecuteSeq()
        {
            bool cannotFindExecuteSeq = true;

            liExecuteSource = (BindingList<AGVTaskModel>)dgvItem.DataSource; //只取資料用, 沒有雙向繫結, 未同步變更資料

            //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
            for (int i = 0; i < liExecuteSource.Count; i++)
            {
                AGVTaskModel item = new AGVTaskModel();
                item = liExecuteSource[i];

                if (item != null && item.EXECUTE_SEQ.Equals(executeItem.EXECUTE_SEQ))
                {
                    cannotFindExecuteSeq = false;
                    break;
                }
            }

            return cannotFindExecuteSeq;
        }

        private void disconnectAGV()
        {
            AGVDisconnectTrigger();
            //agvEntity.lblClientIP_Port.Text = "";
            //agvEntity.tbxConnIP.Enabled = true;
            //agvEntity.tbxConnPort.Enabled = true;
            //agvEntity.lblStatus.Text = "Off line";
            //agvEntity.btnConnService.Enabled = true;
            //agvEntity.btnDisconn.Enabled = false;
            //agvEntity.lblServerConnectLight.ForeColor = Color.Gray;
            //agvEntity.ConnStatus = "0";

            //agvEntity.clientSocket.Dispose();
            //agvEntity.clientSocket.Close();
        }

        private void disconnectAutostock()
        {
            autostockEntity.lblClientIP_Port.Text = "";
            autostockEntity.tbxConnIP.Enabled = true;
            autostockEntity.tbxConnPort.Enabled = true;
            autostockEntity.lblStatus.Text = "Off line";
            autostockEntity.btnConnService.Enabled = true;
            autostockEntity.btnDisconn.Enabled = false;
            autostockEntity.lblServerConnectLight.ForeColor = Color.Gray;
            autostockEntity.ConnStatus = "0";

            autostockEntity.clientSocket.Dispose();
            autostockEntity.clientSocket.Close();
        }

        private bool IsAGVImportantIssue(int AGVResponseValue)
        {
            bool blImportantIssue = false;

            switch (AGVResponseValue)
            {
                case 3:   //AGV馬達異常
                case 4:   //偵測sensor異常
                case 5:   //磁條脫落, 或錯誤路徑
                case 6:   //PLC異常
                case 7:   //電量過低
                case 8:   //升降馬達異常
                case 11:  //升降超過預計時間timeout
                case 13:  //AGV充電器異常
                case 17:  //AGV發進資料接收異常, ex:ST error
                case 18:  //AGV發進資料要求異常
                case 198: //手動搬送完了
                    blImportantIssue = true;
                    break;
                default:
                    break;
            }

            return blImportantIssue;
        }

        private bool IsAGVNormalIssue(int AGVResponseValue)
        {
            bool blNormalIssue = false;

            switch (AGVResponseValue)
            {
                case 1:   //緊急停止
                case 2:   //AGV緩衝裝置撞到障礙物
                case 9:   //移載許可超過預定時間time out
                case 10:  //在席sensor異常
                case 12:  //障礙物未排除超過預定時間
                case 19:  //AGV WIFI異常
                case 191: //再実行（異常解除）
                case 196: //搬送中止（AGV切離）
                    blNormalIssue = true;
                    break;
                default:
                    break;
            }

            return blNormalIssue;
        }


    }
}
