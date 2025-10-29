using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows.Forms;
using Modbus.Device;

namespace AGVMSModel
{

    [Serializable]
    public class DeviceInfoModel
    {
        public string ConnectMachineID { get; set; } //ASM01, ASM02, AGVM01 (一律大寫)
        public string ConnectMachineType { get; set; } //1:autostock management, 2:AGV management, 3:welding machine
        public Socket clientSocket { get; set; } //for 自動倉儲管理機 & AGV PLC(MELSEC)

        //public TcpClient TcpClient { get; set; } //for AGV PLC

        //public ModbusIpMaster MBusM { get; set; }//for AGV PLC

        public TextBox tbxConnIP { get; set; } //connect IP
        public TextBox tbxConnPort { get; set; } //connect Port

        public Label lblStatus { get; set; } //connect status display

        public Label lblClientIP_Port { get; set; } //connect IP Port display

        public Button btnConnService { get; set; } //connect button

        public Button btnDisconn { get; set; } //disconnect button

        public Label lblServerConnectLight { get; set; } //connect status light

        public string ConnStatus { get; set; } //connect status log

        //public Dictionary<int, ushort> dictDWordValue { get; set; } //PLC DWord key & value

        public Dictionary<int, int> dictAGVDword { get; set; } //MELSEC communcation protocol

        public dsAutoStockTransferJsonModel dsAutostockClient { get; set; } //上位PC傳送訊息

        public dsAutoStockTransferJsonModel dsAutostockServer { get; set; } //接收管理機訊息

        public dsAutoStockTransferJsonModel dsAutostockServerKeepData { get; set; } //接收管理機訊息後的訊息記錄
        public dsAutoStockTransferJsonModel dsModify { get; set; } //RFID rewrite傳送訊息
    }

    /// <summary>
    /// 焊接機器HMI傳送資料, 含大轉盤及鏈條區
    /// </summary>
    [Serializable]
    public class MachineTransferData
    {
        public string StationNo { get; set; }
        public string ItemNo { get; set; }
        public string InOutFlag { get; set; }
        public string PriorityArea { get; set; }

    }

    [Serializable]
    public class BufferStatus
    {
        public string StationNo { get; set; }
        public string IsEmpty { get; set; }
        public string ArrangedTask { get; set; }
        public string ItemNo { get; set; }
        public string InOutFlag { get; set; }
        public string PriorityArea { get; set; }
    }

    [Serializable]
    public class RotateStatus
    {
        public string StationNo { get; set; }
        public string IsEmpty { get; set; }
        public string ItemNo { get; set; }
    }

}
