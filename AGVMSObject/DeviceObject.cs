using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AGVMSModel;
using System.Threading;
using AGVMSUtility;
using AGVMSDataAccess;
using System.Data;
using System.Windows.Forms;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace AGVMSObject
{
    public class DeviceObject
    {
        private List<string> listTempFiles;
        private string FolderPath = string.Empty;
        private BindingList<MachineTransferData> listMachineTransData;
        private BindingList<BufferStatus> listBufferStatusData;
        private BindingList<RotateStatus> listRotateStatusData;
        //private List<AGVTaskModel> liComputerData;

        private int ReadDataType = 0;
        private Thread threadObject;
        private int TimeCycle = 10000;
        public delegate void DgvItemDataSourceEventHandler(AGVTaskModel addData, decimal CutInLine);
        public DgvItemDataSourceEventHandler dgvItemDataSourceTrigger;

        public delegate void lblBufferIsEmptyEventHandler(BufferStatus transData);
        public lblBufferIsEmptyEventHandler lblBufferIsEmptyTrigger;

        public delegate void lblRotateIsEmptyEventHandler(RotateStatus transData);
        public lblRotateIsEmptyEventHandler lblRotateIsEmptyTrigger;

        private DaoSP daoSP = new DaoSP();

        public DeviceObject()
        {
            listMachineTransData = new BindingList<MachineTransferData>();
            listBufferStatusData = new BindingList<BufferStatus>();
            listRotateStatusData = new BindingList<RotateStatus>();
        }

        public void setFolderPath(string _FolderPath)
        {
            FolderPath = _FolderPath;
        }

        public void setReadDataType(int _ReadDataType)
        {
            ReadDataType = _ReadDataType;
        }

        public void setTimeCycle(int _TimeCycle)
        {
            if (_TimeCycle != 0)
            {
                TimeCycle = _TimeCycle;
            }
        }

        public void executeReadFile()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(TimeCycle);

                    getFiles();
                    readFileContentCmd();
                }
                catch (Exception ex)
                {
                    string issueMessage = ex.Message.ToString();
                    throw ex;
                }
            }
        }

        private void getFiles()
        {
            if (Directory.Exists(FolderPath))
            {
                listTempFiles = new List<string>();
                listTempFiles = Directory.GetFiles(FolderPath, "*.txt").ToList();
            }
        }

        private void readFileContentCmd()
        {
            if (listTempFiles?.Count > 0)
            {
                listMachineTransData = new BindingList<MachineTransferData>();
                listBufferStatusData = new BindingList<BufferStatus>();
                listRotateStatusData = new BindingList<RotateStatus>();

                listTempFiles.Sort();

                string dateFolder = DateTime.Now.ToString("yyyyMMdd");
                string tempPath = FolderPath + @"\temp\" + dateFolder;

                //已取得的檔案, 讀取內容資料暫存至list
                foreach (var file in listTempFiles)
                {
                    string strRowTxt = "";
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string StationNo = string.Empty;
                        string ItemNo = string.Empty;
                        string InOutFlag = string.Empty;
                        string PriorityArea = string.Empty;
                        string IsEmpty = string.Empty;

                        //HAN machine, rotate zone welding and chain zone welding 
                        if (ReadDataType == 1)
                        {
                            while ((strRowTxt = sr.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(strRowTxt) && strRowTxt.Length >= 49)
                                {
                                    StationNo = strRowTxt.Substring(1 - 1, 4);
                                    ItemNo = strRowTxt.Substring(6 - 1, 40).Trim();
                                    InOutFlag = strRowTxt.Substring(47 - 1, 1);
                                    PriorityArea = strRowTxt.Substring(49 - 1, 1);

                                    if (!string.IsNullOrWhiteSpace(ItemNo) && !string.IsNullOrWhiteSpace(InOutFlag) && !string.IsNullOrWhiteSpace(StationNo))
                                    {
                                        bool blStationNo_Pass = false;
                                        bool blItemNo_Pass = false;
                                        bool blInOutFlag_Pass = false;
                                        bool blPriorityArea_Pass = false;

                                        blStationNo_Pass = Regex.IsMatch(StationNo, @"^[0-9]+$");
                                        blItemNo_Pass = Regex.IsMatch(ItemNo, @"^[A-Za-z0-9]+$");
                                        blInOutFlag_Pass = Regex.IsMatch(InOutFlag, @"^[0-9]+$");
                                        blPriorityArea_Pass = Regex.IsMatch(PriorityArea, @"^[0-9]+$");

                                        if (ItemNo.Length > 16)
                                        {
                                            blItemNo_Pass = false;
                                        }

                                        if (blStationNo_Pass && blItemNo_Pass && blInOutFlag_Pass && blPriorityArea_Pass)
                                        {
                                            if (checkStation(StationNo))
                                            {
                                                MachineTransferData temp = new MachineTransferData();
                                                temp.StationNo = int.Parse(StationNo).ToString();
                                                temp.ItemNo = ItemNo;
                                                temp.InOutFlag = InOutFlag;
                                                temp.PriorityArea = PriorityArea;

                                                listMachineTransData.Add(temp);
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        //buffer zone
                        else if (ReadDataType == 2)
                        {
                            while ((strRowTxt = sr.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(strRowTxt) && strRowTxt.Length >= 6)
                                {
                                    StationNo = strRowTxt.Substring(1 - 1, 4);
                                    IsEmpty = strRowTxt.Substring(6 - 1, 1);

                                    if (!string.IsNullOrWhiteSpace(StationNo) && !string.IsNullOrWhiteSpace(IsEmpty))
                                    {
                                        bool blStationNo_Pass = false;
                                        bool blIsEmpty_Pass = false;

                                        blStationNo_Pass = Regex.IsMatch(StationNo, @"^[0-9]+$");
                                        blIsEmpty_Pass = Regex.IsMatch(IsEmpty, @"^[0-9]+$");

                                        if (blStationNo_Pass && blIsEmpty_Pass)
                                        {
                                            if (checkStation(StationNo))
                                            {
                                                BufferStatus temp = new BufferStatus();
                                                temp.StationNo = int.Parse(StationNo).ToString();
                                                temp.IsEmpty = IsEmpty;

                                                listBufferStatusData.Add(temp);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //Rotate zone
                        else if (ReadDataType == 3)
                        {
                            while ((strRowTxt = sr.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(strRowTxt) && strRowTxt.Length >= 6)
                                {
                                    StationNo = strRowTxt.Substring(1 - 1, 4);
                                    IsEmpty = strRowTxt.Substring(6 - 1, 1);

                                    if (!string.IsNullOrWhiteSpace(StationNo) && !string.IsNullOrWhiteSpace(IsEmpty))
                                    {
                                        bool blStationNo_Pass = false;
                                        bool blIsEmpty_Pass = false;

                                        blStationNo_Pass = Regex.IsMatch(StationNo, @"^[0-9]+$");
                                        blIsEmpty_Pass = Regex.IsMatch(IsEmpty, @"^[0-9]+$");

                                        if (blStationNo_Pass && blIsEmpty_Pass)
                                        {
                                            if (checkStation(StationNo))
                                            {
                                                RotateStatus temp = new RotateStatus();
                                                temp.StationNo = int.Parse(StationNo).ToString();
                                                temp.IsEmpty = IsEmpty;

                                                listRotateStatusData.Add(temp);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        sr.Close();
                    }

                    if (!Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }

                    //move file to temp folder
                    string destFile = Path.Combine(new string[] { tempPath, Path.GetFileName(file) });

                    if (File.Exists(destFile))
                    {
                        string newName = Path.Combine(new string[] { tempPath, Path.GetFileName(file.Replace(".txt", "") + "_" + DateTime.Now.ToString("HHmmssfff") + ".txt") });
                        File.Move(file, newName);
                    }
                    else
                    {
                        File.Move(file, destFile);
                    }
                }

                readFileProcessCmd();
            }
        }

        private void readFileProcessCmd(decimal CutInLine = 0)
        {
            //暫存後
            if (ReadDataType == 1 && listMachineTransData.Count > 0)
            {
                foreach (MachineTransferData item in listMachineTransData)
                {
                    DateTime dateNow = DateTime.Now;

                    AGVTaskModel AGVCmd_data = new AGVTaskModel();

                    AGVCmd_data.CREATE_DATETIME = dateNow.ToString("yyyy-MM-dd HH:mm:ss");
                    string strDate_yyyymmdd = dateNow.ToString("yyyyMMdd");

                    //get sequence,以年月日取流水號, 上位PC的執行序號與AGV Move ID相同
                    List<dsSYS_SEQUENCE> listData = new List<dsSYS_SEQUENCE>();
                    dsSYS_SEQUENCE data = new dsSYS_SEQUENCE();
                    data.SEQUENCE_TYPE = "1";
                    data.SEQUENCE_CODE = strDate_yyyymmdd;
                    listData.Add(data);

                    SP_FUN_Model entitySP = new SP_FUN_Model();
                    entitySP.SP_FUN_NAME = "usp_GetNewSYS_SEQUENCE";
                    entitySP.INPUT_NAME = "@paraSequence";
                    entitySP.INPUT_PARAM = DataToolHelper.ToDataTable(listData);
                    entitySP.IS_SP = true;
                    entitySP.IS_OUTPUT = true;

                    DataTable dtAGV_Sequence = new DataTable();
                    dtAGV_Sequence = daoSP.ExecStoredProcedure(entitySP);

                    //AGV MOVE ID 處理程序
                    if (dtAGV_Sequence.Rows.Count > 0)
                    {
                        //AGV 執行序號, 共10碼
                        string strMoveID_yymmdd = dtAGV_Sequence.Rows[0]["SEQUENCE_CODE"].ToString().Substring(2, 6); //取yyMMdd
                        string strMoveID_seq = dtAGV_Sequence.Rows[0]["SEQUENCE_NO"].ToString().PadLeft(4, '0'); //流水號4碼, 不足前面補0

                        AGVCmd_data.EXECUTE_SEQ = strMoveID_yymmdd + strMoveID_seq;  //上位電腦執行序號, 共10碼
                        AGVCmd_data.AGV_MOVE_ID = AGVCmd_data.EXECUTE_SEQ; //MOVE ID
                        AGVCmd_data.ITEM_NO = item.ItemNo;

                        //in stock
                        if (item.InOutFlag == "1")
                        {
                            AGVCmd_data.AGV_FROM_ST = item.StationNo;

                            //if ((int)AGVStation.ST10_ChainOut == int.Parse(item.StationNo))
                            //{
                            //    AGVCmd_data.AGV_TO_ST = ((int)AGVStation.ST1_Default).ToString();
                            //}
                            //else
                            //{
                            AGVCmd_data.AGV_TO_ST = ""; //20240529_lewis_大轉盤/鏈條區入庫都先放到暫存區, 故入庫皆不顯示目的地
                            //}

                            AGVCmd_data.INOUT_FLAG = InOutStockFlag.In.ToString();
                        }
                        //out stock
                        else if (item.InOutFlag == "2")
                        {
                            AGVCmd_data.AGV_FROM_ST = "";
                            AGVCmd_data.AGV_TO_ST = item.StationNo;
                            AGVCmd_data.INOUT_FLAG = InOutStockFlag.Out.ToString();
                        }

                        AGVCmd_data.PRIORITY_AREA = item.PriorityArea;

                        AGVCmd_data.EXECUTE_STATUS = StatusFlag.Line.ToString();

                        AGVCmd_data.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                        AGVCmd_data.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Line.ToString();

                        dgvItemDataSourceTrigger(AGVCmd_data, CutInLine); //mainform委派1 , 
                    }
                }
            }
            else if (ReadDataType == 2 && listBufferStatusData.Count > 0)
            {
                //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                for (int i = 0; i < listBufferStatusData.Count; i++)
                {
                    BufferStatus item = new BufferStatus();
                    item = listBufferStatusData[i];

                    lblBufferIsEmptyTrigger(item);
                }

            }
            else if (ReadDataType == 3 && listRotateStatusData.Count > 0)
            {
                //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                for (int i = 0; i < listRotateStatusData.Count; i++)
                {
                    RotateStatus item = new RotateStatus();
                    item = listRotateStatusData[i];

                    lblRotateIsEmptyTrigger(item);
                }
            }
        }

        private bool checkStation(string station)
        {
            bool result = true;

            if (!string.IsNullOrWhiteSpace(station))
            {
                if (station == "0001" || station == "0010" || station == "0011" ||
                    station == "0020" || station == "0021" || station == "0022" || station == "0023" || station == "0024" || station == "0025" || station == "0026" ||
                    station == "0030" || station == "0031" || station == "0032" || station == "0033" || station == "0034" || station == "0035" || station == "0036" || station == "0037" ||
                    station == "0040" || station == "0041" || station == "0042" || station == "0043" || station == "0044" || station == "0045" || station == "0046" || station == "0047" ||
                    station == "0050" || station == "0051" || station == "0052" || station == "0053" || station == "0054" || station == "0055" || station == "0056" || station == "0057" ||
                    station == "0048"
                    )
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

    }
}
