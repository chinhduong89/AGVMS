using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using AGVMSDataAccess;
using AGVMSModel;
using AGVMSUtility;
using AGVMSObject;
using System.IO;
using System.Configuration;

namespace AGVMS
{
    public partial class MainForm : Form
    {
        public static string AGVMName = ConfigurationManager.AppSettings["SystemName"];
        public static string AGVMVer = ConfigurationManager.AppSettings["SystemVersion"];

        private BindingList<DeviceInfoModel> listDeviceConnInfo = new BindingList<DeviceInfoModel>();
        private Dictionary<int, int> agvDWord = new Dictionary<int, int>();
        //private Dictionary<int, ushort> agvDWord_old = new Dictionary<int, ushort>();

        private BindingList<BufferStatus> listBufferStatusData;
        private BindingList<RotateStatus> listRotateStatusData;
        //private List<BufferStatus> listBufferStatusData;
        //private List<RotateStatus> listRotateStatusData;
        private BindingList<AGVTaskModel> liComputerData;
        private static BindingList<AGVTaskModel> bdlAGVPlan;// = new BindingList<AGVTaskModel>();

        //private int AGVReadtimeCycle = 300;

        private Thread threadComputer;
        private string AGVLogFile = Path.Combine(System.Environment.CurrentDirectory + "\\Log", @"AGVM -" + DateTime.Now.ToString("yyyyMMdd") + "_LOG.txt");

        #region AGV PLC
        private Thread threadAGVM_D0;
        //private Thread threadAGVM_Read_D72;
        //private Thread threadAGVM_Read_D110;

        //private Thread threadAGVM_Read_D472; //AGV 1號機：(Response) move id寫入及請求發出後需監聽DWord 
        //private Thread threadAGVM_Read_D476; //AGV 1號機：現行狀態

        //private Thread threadAGVM_Read_D477; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D478; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D479; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D480; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D481; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D482; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D483; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D484; //AGV 1號機：搬送ID前2碼, 搬送ID D477~D484
        //private Thread threadAGVM_Read_D510; //AGV 1號機：(Request) 監聽DWord, 包含啟用配車、拾取完了(拾取夾具)、搬送完了(包含自動、手動)、搬送中止、異常問題
        //private Thread threadAGVM_Read_D511; //AGV 1號機：現行位置, 磁條上小圓標籤號碼

        private Thread threadAGVM_Read_DWord;

        //測試用Move ID
        //private Thread threadAGVM_Read_D412;
        //private Thread threadAGVM_Read_D413;
        //private Thread threadAGVM_Read_D414;
        //private Thread threadAGVM_Read_D415;
        //private Thread threadAGVM_Read_D416;

        //private Thread threadAGVM_Read_D12;
        //private Thread threadAGVM_Read_D13;
        //private Thread threadAGVM_Read_D14;
        //private Thread threadAGVM_Read_D15;
        //private Thread threadAGVM_Read_D16;

        private Thread threadGetHANPLCCmd;
        private Thread threadGetBufferStatus;
        private Thread threadGetRotateStatus;

        private string AGVClientIP = string.Empty;
        private string AGVClientPort = string.Empty;

        private string AutostockClientIP = string.Empty;
        private string AutostockClientPort = string.Empty;

        DaoLogMessage daoLogMessage = new DaoLogMessage();

        private int manualCancelCmdType = 0;
        private bool blExecuteAGVConnect = false;
        private bool blAGVManualConnect = false;
        private bool blAGVManualDisconnect = false;
        private bool blAGVAutoDisconnect = false;
        private bool blMessageboxShow = false;
        #endregion

        public MainForm()
        {
            InitializeComponent();
            this.Text = AGVMName + " " + AGVMVer;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            initial();
        }

        #region initial event

        private void initial()
        {
            //string LogAGVReadDWordFileName = @"AGV-Read-" + DateTime.Now.ToString("yyyyMMdd") + "_LOG.txt";
            //string LogAGVWriteDWordFileName = @"AGV-Write-" + DateTime.Now.ToString("yyyyMMdd") + "_LOG.txt";
            //LogAGVReadDWord.setLogFileName(LogAGVReadDWordFileName);
            //LogAGVWriteDWord.setLogFileName(LogAGVWriteDWordFileName);

            tbxAGVConnIP.Text = "172.0.0.1";
            tbxAGVConnPort.Text = "8001";
            btnAGVDisconnect.Enabled = false;

            tbxAutostockConnIP.Text = "172.0.0.2"; //"172.0.0.2" for online
            tbxAutostockConnPort.Text = "8003";
            btnAutostockDisconnect.Enabled = false;

            AGVClientIP = "172.0.0.2";
            AGVClientPort = "8001";

            //AutostockClientIP = "172.0.0.2";
            //AutostockClientPort = "8002";

            //for test
            AutostockClientIP = "127.0.0.1";
            AutostockClientPort = "8002";

            liComputerData = new BindingList<AGVTaskModel>();
            listBufferStatusData = new BindingList<BufferStatus>();
            listRotateStatusData = new BindingList<RotateStatus>();
            //listBufferStatusData = new List<BufferStatus>();
            //listRotateStatusData = new List<RotateStatus>();

            setAGVDWordKey();
            setAGVDatagridview();
            setLight();
            setBufferStatus();
            setRotateStatus();
        }

        private void setAGVDatagridview()
        {
            DataGridViewButtonColumn btnRemoveRow = new DataGridViewButtonColumn();
            btnRemoveRow.Name = "REMOVE_CMD";
            btnRemoveRow.UseColumnTextForButtonValue = true;
            btnRemoveRow.Text = "Remove";
            btnRemoveRow.HeaderText = "";
            dgvAGV_PlanList.Columns.Add(btnRemoveRow);

            DataGridViewButtonColumn btnAdjustSortUpRow = new DataGridViewButtonColumn();
            btnAdjustSortUpRow.Name = "ADJUST_SORT_MOVE_UP_CMD";
            btnAdjustSortUpRow.UseColumnTextForButtonValue = true;
            btnAdjustSortUpRow.Text = "Up";
            btnAdjustSortUpRow.HeaderText = "";
            dgvAGV_PlanList.Columns.Add(btnAdjustSortUpRow);

            DataGridViewButtonColumn btnAdjustSortDownRow = new DataGridViewButtonColumn();
            btnAdjustSortDownRow.Name = "ADJUST_SORT_MOVE_DOWN_CMD";
            btnAdjustSortDownRow.UseColumnTextForButtonValue = true;
            btnAdjustSortDownRow.Text = "Down";
            btnAdjustSortDownRow.HeaderText = "";
            dgvAGV_PlanList.Columns.Add(btnAdjustSortDownRow);

            dgvAGV_PlanList.AllowUserToAddRows = false;
            dgvAGV_PlanList.ReadOnly = true;
            dgvAGV_PlanList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //dgvAGV_PlanList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            //dgvAGV_PlanList.AutoGenerateColumns = false; //開啟會錯誤
            bdlAGVPlan = new BindingList<AGVTaskModel>(liComputerData);
            dgvAGV_PlanList.DataSource = bdlAGVPlan;

            SetDoubleBuffered(dgvAGV_PlanList);
        }

        private void autoConnect()
        {
            //ConnectExecuteOrDirect(liSocketConn_Data);
        }

        private void setAGVDWordKey()
        {
            //---- 上位PC控制, 包含寫入及清除 start ----
            agvDWord.Add(0, 0); //D0:AGV監視盤生存確認(上位PC寫入), 1:ON/0:OFF, 1秒間隔

            //搬送指示(上位PC寫入)
            agvDWord.Add(12, 0); //D12:搬送ID1, 1~2碼
            agvDWord.Add(13, 0); //D13:搬送ID1, 3~4碼
            agvDWord.Add(14, 0); //D14:搬送ID1, 5~6碼
            agvDWord.Add(15, 0); //D15:搬送ID1, 7~8碼
            agvDWord.Add(16, 0); //D16:搬送ID1, 9~10碼
            agvDWord.Add(17, 0); //D17:搬送ID1, 11~12碼
            agvDWord.Add(18, 0); //D18:搬送ID1, 13~14碼
            agvDWord.Add(19, 0); //D19:搬送ID1, 15~16碼
            agvDWord.Add(20, 0); //D20:搬送ID1, FROM ST
            agvDWord.Add(21, 0); //D21:搬送ID1, TO ST

            agvDWord.Add(22, 0); //D22:搬送ID2, 1~2碼
            agvDWord.Add(23, 0); //D23:搬送ID2, 3~4碼
            agvDWord.Add(24, 0); //D24:搬送ID2, 5~6碼
            agvDWord.Add(25, 0); //D25:搬送ID2, 7~8碼
            agvDWord.Add(26, 0); //D26:搬送ID2, 9~10碼
            agvDWord.Add(27, 0); //D27:搬送ID2, 11~12碼
            agvDWord.Add(28, 0); //D28:搬送ID2, 13~14碼
            agvDWord.Add(29, 0); //D29:搬送ID2, 15~16碼
            agvDWord.Add(30, 0); //D30:搬送ID2, FROM ST
            agvDWord.Add(31, 0); //D31:搬送ID2, TO ST

            agvDWord.Add(32, 0); //D32:搬送ID3, 1~2碼
            agvDWord.Add(33, 0); //D33:搬送ID3, 3~4碼
            agvDWord.Add(34, 0); //D34:搬送ID3, 5~6碼
            agvDWord.Add(35, 0); //D35:搬送ID3, 7~8碼
            agvDWord.Add(36, 0); //D36:搬送ID3, 9~10碼
            agvDWord.Add(37, 0); //D37:搬送ID3, 11~12碼
            agvDWord.Add(38, 0); //D38:搬送ID3, 13~14碼
            agvDWord.Add(39, 0); //D39:搬送ID3, 15~16碼
            agvDWord.Add(40, 0); //D40:搬送ID3, FROM ST
            agvDWord.Add(41, 0); //D41:搬送ID3, TO ST

            agvDWord.Add(72, 0); //D72:搬送指示(上位PC寫入), 1:讀取要求, 3:受付應答(OK), 5:NG應答(NG)
            agvDWord.Add(110, 0); //D110:agv 一號機 狀態讀取應答(上位PC寫入), 1:ON/0:OFF 
            agvDWord.Add(146, 0); //D146:agv 二號機 狀態讀取應答(上位PC寫入), 1:ON/0:OFF 
            //---- 上位PC控制, 包含寫入及清除 end ----

            //---- AGV監視盤控制, 上位PC只讀取資料 start ----
            agvDWord.Add(400, 0); //D400:AGV監視盤生存確認, 1:ON/0:OFF, 1秒間隔

            ////測試用讀取Move ID
            agvDWord.Add(412, 0);
            agvDWord.Add(413, 0);
            agvDWord.Add(414, 0);
            agvDWord.Add(415, 0);
            agvDWord.Add(416, 0);
            agvDWord.Add(417, 0);
            agvDWord.Add(418, 0);
            agvDWord.Add(419, 0);

            agvDWord.Add(472, 0); //D472:AGV-讀取-搬送指示読込応答; 1:搬送指示読込応答(ON), 2:搬送指示受付(OK), 4:搬送指示NG

            agvDWord.Add(4720, 0); //D472 bit 0:模式, 0:nothing/1:讀取要求 
            agvDWord.Add(4721, 0); //D472 bit 1:模式, 0:nothing/1:搬送指示接收 
            agvDWord.Add(4722, 0); //D472 bit 2:模式, 0:nothing/1:搬送指示NG

            //----- agv 一號機 -----
            agvDWord.Add(476, 0); //D476:1號機狀態
            agvDWord.Add(477, 0); //D477:目前搬送ID, 1~2碼
            agvDWord.Add(478, 0); //D478:目前搬送ID, 3~4碼
            agvDWord.Add(479, 0); //D479:目前搬送ID, 5~6碼
            agvDWord.Add(480, 0); //D480:目前搬送ID, 7~8碼
            agvDWord.Add(481, 0); //D481:目前搬送ID, 9~10碼
            agvDWord.Add(482, 0); //D482:目前搬送ID, 11~12碼
            agvDWord.Add(483, 0); //D483:目前搬送ID, 13~14碼
            agvDWord.Add(484, 0); //D484:目前搬送ID, 15~16碼
            agvDWord.Add(510, 0); //D510:模式讀取要求
            agvDWord.Add(511, 0); //D511:AGV目前所在定位點

            agvDWord.Add(5100, 0); //D510 bit 0:模式, 0:nothing/1:讀取要求 
            agvDWord.Add(5108, 0); //D510 bit 8:模式, 0:offline/1:online
            agvDWord.Add(5109, 0); //D510 bit 9:模式, 0:manual/1:auto
            agvDWord.Add(51011, 0); //D510 bit B:模式, 0:normal/1:abnormal

            //----- agv 二號機 -----
            agvDWord.Add(512, 0); //D512:2號機狀態
            agvDWord.Add(513, 0); //D513:目前搬送ID, 1~2碼
            agvDWord.Add(514, 0); //D514:目前搬送ID, 3~4碼
            agvDWord.Add(515, 0); //D515:目前搬送ID, 5~6碼
            agvDWord.Add(516, 0); //D516:目前搬送ID, 7~8碼
            agvDWord.Add(517, 0); //D517:目前搬送ID, 9~10碼
            agvDWord.Add(518, 0); //D518:目前搬送ID, 11~12碼
            agvDWord.Add(519, 0); //D519:目前搬送ID, 13~14碼
            agvDWord.Add(520, 0); //D520:目前搬送ID, 15~16碼
            agvDWord.Add(546, 0); //D546:狀態讀取要求
            agvDWord.Add(547, 0); //D547:AGV目前所在定位點

            agvDWord.Add(5460, 0); //D546 bit 0:模式, 0:nothing/1:讀取要求 
            agvDWord.Add(5468, 0); //D546 bit 8:模式, 0:offline/1:online
            agvDWord.Add(5469, 0); //D546 bit 9:模式, 0:manual/1:auto
            agvDWord.Add(54611, 0); //D546 bit B:模式, 0:normal/1:abnormal

            //---- AGV監視盤控制, 上位PC只讀取資料 end ----

        }

        private void setLight()
        {
            //autostock connect light
            lblAutostockConnLight.Visible = true;
            lblAutostockConnLight.Text = "●";
            lblAutostockConnLight.Font = new Font("Microsoft Sans Serif", 30);
            lblAutostockConnLight.ForeColor = Color.Gray;

            //AGV connect light
            lblAGVConnLight.Visible = true;
            lblAGVConnLight.Text = "●";
            lblAGVConnLight.Font = new Font("Microsoft Sans Serif", 30);
            lblAGVConnLight.ForeColor = Color.Gray;

            //buffer 1 Arranged Task light
            lblBuffer1ArrangedLight.Visible = true;
            lblBuffer1ArrangedLight.Text = "●";
            lblBuffer1ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer1ArrangedLight.ForeColor = Color.Gray;

            //buffer 1 isEmpty light
            lblBuffer1IsEmptyLight.Visible = true;
            lblBuffer1IsEmptyLight.Text = "●";
            lblBuffer1IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer1IsEmptyLight.ForeColor = Color.Gray;

            //buffer 2 Arranged Task light
            lblBuffer2ArrangedLight.Visible = true;
            lblBuffer2ArrangedLight.Text = "●";
            lblBuffer2ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer2ArrangedLight.ForeColor = Color.Gray;

            //buffer 2 isEmpty light
            lblBuffer2IsEmptyLight.Visible = true;
            lblBuffer2IsEmptyLight.Text = "●";
            lblBuffer2IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer2IsEmptyLight.ForeColor = Color.Gray;

            //buffer 3 Arranged Task light
            lblBuffer3ArrangedLight.Visible = true;
            lblBuffer3ArrangedLight.Text = "●";
            lblBuffer3ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer3ArrangedLight.ForeColor = Color.Gray;

            //buffer 3 isEmpty light
            lblBuffer3IsEmptyLight.Visible = true;
            lblBuffer3IsEmptyLight.Text = "●";
            lblBuffer3IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer3IsEmptyLight.ForeColor = Color.Gray;

            //buffer 4 Arranged Task light
            lblBuffer4ArrangedLight.Visible = true;
            lblBuffer4ArrangedLight.Text = "●";
            lblBuffer4ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer4ArrangedLight.ForeColor = Color.Gray;

            //buffer 4 isEmpty light
            lblBuffer4IsEmptyLight.Visible = true;
            lblBuffer4IsEmptyLight.Text = "●";
            lblBuffer4IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer4IsEmptyLight.ForeColor = Color.Gray;

            //buffer 5 Arranged Task light
            lblBuffer5ArrangedLight.Visible = true;
            lblBuffer5ArrangedLight.Text = "●";
            lblBuffer5ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer5ArrangedLight.ForeColor = Color.Gray;

            //buffer 5 isEmpty light
            lblBuffer5IsEmptyLight.Visible = true;
            lblBuffer5IsEmptyLight.Text = "●";
            lblBuffer5IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer5IsEmptyLight.ForeColor = Color.Gray;

            //buffer 6 Arranged Task light
            lblBuffer6ArrangedLight.Visible = true;
            lblBuffer6ArrangedLight.Text = "●";
            lblBuffer6ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer6ArrangedLight.ForeColor = Color.Gray;

            //buffer 6 isEmpty light
            lblBuffer6IsEmptyLight.Visible = true;
            lblBuffer6IsEmptyLight.Text = "●";
            lblBuffer6IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer6IsEmptyLight.ForeColor = Color.Gray;

            //buffer 7 Arranged Task light
            lblBuffer7ArrangedLight.Visible = true;
            lblBuffer7ArrangedLight.Text = "●";
            lblBuffer7ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer7ArrangedLight.ForeColor = Color.Gray;

            //buffer 7 isEmpty light
            lblBuffer7IsEmptyLight.Visible = true;
            lblBuffer7IsEmptyLight.Text = "●";
            lblBuffer7IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer7IsEmptyLight.ForeColor = Color.Gray;

            //buffer 8 Arranged Task light
            lblBuffer8ArrangedLight.Visible = true;
            lblBuffer8ArrangedLight.Text = "●";
            lblBuffer8ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer8ArrangedLight.ForeColor = Color.Gray;

            //buffer 8 isEmpty light
            lblBuffer8IsEmptyLight.Visible = true;
            lblBuffer8IsEmptyLight.Text = "●";
            lblBuffer8IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer8IsEmptyLight.ForeColor = Color.Gray;

            //buffer 9 Arranged Task light
            lblBuffer9ArrangedLight.Visible = true;
            lblBuffer9ArrangedLight.Text = "●";
            lblBuffer9ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer9ArrangedLight.ForeColor = Color.Gray;

            //buffer 9 isEmpty light
            lblBuffer9IsEmptyLight.Visible = true;
            lblBuffer9IsEmptyLight.Text = "●";
            lblBuffer9IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer9IsEmptyLight.ForeColor = Color.Gray;

            //buffer 10 Arranged Task light
            lblBuffer10ArrangedLight.Visible = true;
            lblBuffer10ArrangedLight.Text = "●";
            lblBuffer10ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer10ArrangedLight.ForeColor = Color.Gray;

            //buffer 10 isEmpty light
            lblBuffer10IsEmptyLight.Visible = true;
            lblBuffer10IsEmptyLight.Text = "●";
            lblBuffer10IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer10IsEmptyLight.ForeColor = Color.Gray;

            //buffer 11 Arranged Task light
            lblBuffer11ArrangedLight.Visible = true;
            lblBuffer11ArrangedLight.Text = "●";
            lblBuffer11ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer11ArrangedLight.ForeColor = Color.Gray;

            //buffer 11 isEmpty light
            lblBuffer11IsEmptyLight.Visible = true;
            lblBuffer11IsEmptyLight.Text = "●";
            lblBuffer11IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer11IsEmptyLight.ForeColor = Color.Gray;

            //buffer 12 Arranged Task light
            lblBuffer12ArrangedLight.Visible = true;
            lblBuffer12ArrangedLight.Text = "●";
            lblBuffer12ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer12ArrangedLight.ForeColor = Color.Gray;

            //buffer 12 isEmpty light
            lblBuffer12IsEmptyLight.Visible = true;
            lblBuffer12IsEmptyLight.Text = "●";
            lblBuffer12IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer12IsEmptyLight.ForeColor = Color.Gray;

            //buffer 13 Arranged Task light
            lblBuffer13ArrangedLight.Visible = true;
            lblBuffer13ArrangedLight.Text = "●";
            lblBuffer13ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer13ArrangedLight.ForeColor = Color.Gray;

            //buffer 13 isEmpty light
            lblBuffer13IsEmptyLight.Visible = true;
            lblBuffer13IsEmptyLight.Text = "●";
            lblBuffer13IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer13IsEmptyLight.ForeColor = Color.Gray;

            //buffer 14 Arranged Task light
            lblBuffer14ArrangedLight.Visible = true;
            lblBuffer14ArrangedLight.Text = "●";
            lblBuffer14ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer14ArrangedLight.ForeColor = Color.Gray;

            //buffer 14 isEmpty light
            lblBuffer14IsEmptyLight.Visible = true;
            lblBuffer14IsEmptyLight.Text = "●";
            lblBuffer14IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer14IsEmptyLight.ForeColor = Color.Gray;

            //buffer 15 Arranged Task light
            lblBuffer15ArrangedLight.Visible = true;
            lblBuffer15ArrangedLight.Text = "●";
            lblBuffer15ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer15ArrangedLight.ForeColor = Color.Gray;

            //buffer 15 isEmpty light
            lblBuffer15IsEmptyLight.Visible = true;
            lblBuffer15IsEmptyLight.Text = "●";
            lblBuffer15IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer15IsEmptyLight.ForeColor = Color.Gray;

            //buffer 16 Arranged Task light
            lblBuffer16ArrangedLight.Visible = true;
            lblBuffer16ArrangedLight.Text = "●";
            lblBuffer16ArrangedLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer16ArrangedLight.ForeColor = Color.Gray;

            //buffer 16 isEmpty light
            lblBuffer16IsEmptyLight.Visible = true;
            lblBuffer16IsEmptyLight.Text = "●";
            lblBuffer16IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblBuffer16IsEmptyLight.ForeColor = Color.Gray;


            //Rotate 1 isEmpty light
            lblRotate1IsEmptyLight.Visible = true;
            lblRotate1IsEmptyLight.Text = "●";
            lblRotate1IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate1IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 2 isEmpty light
            lblRotate2IsEmptyLight.Visible = true;
            lblRotate2IsEmptyLight.Text = "●";
            lblRotate2IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate2IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 3 isEmpty light
            lblRotate3IsEmptyLight.Visible = true;
            lblRotate3IsEmptyLight.Text = "●";
            lblRotate3IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate3IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 4isEmpty light
            lblRotate4IsEmptyLight.Visible = true;
            lblRotate4IsEmptyLight.Text = "●";
            lblRotate4IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate4IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 5 isEmpty light
            lblRotate5IsEmptyLight.Visible = true;
            lblRotate5IsEmptyLight.Text = "●";
            lblRotate5IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate5IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 6 isEmpty light
            lblRotate6IsEmptyLight.Visible = true;
            lblRotate6IsEmptyLight.Text = "●";
            lblRotate6IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate6IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 7 isEmpty light
            lblRotate7IsEmptyLight.Visible = true;
            lblRotate7IsEmptyLight.Text = "●";
            lblRotate7IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate7IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 8 isEmpty light
            lblRotate8IsEmptyLight.Visible = true;
            lblRotate8IsEmptyLight.Text = "●";
            lblRotate8IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate8IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 9 isEmpty light
            lblRotate9IsEmptyLight.Visible = true;
            lblRotate9IsEmptyLight.Text = "●";
            lblRotate9IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate9IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 10 isEmpty light
            lblRotate10IsEmptyLight.Visible = true;
            lblRotate10IsEmptyLight.Text = "●";
            lblRotate10IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate10IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 11 isEmpty light
            lblRotate11IsEmptyLight.Visible = true;
            lblRotate11IsEmptyLight.Text = "●";
            lblRotate11IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate11IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 12 isEmpty light
            lblRotate12IsEmptyLight.Visible = true;
            lblRotate12IsEmptyLight.Text = "●";
            lblRotate12IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate12IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 13 isEmpty light
            lblRotate13IsEmptyLight.Visible = true;
            lblRotate13IsEmptyLight.Text = "●";
            lblRotate13IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate13IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 14 isEmpty light
            lblRotate14IsEmptyLight.Visible = true;
            lblRotate14IsEmptyLight.Text = "●";
            lblRotate14IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate14IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 15 isEmpty light
            lblRotate15IsEmptyLight.Visible = true;
            lblRotate15IsEmptyLight.Text = "●";
            lblRotate15IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate15IsEmptyLight.ForeColor = Color.Gray;

            //Rotate 16 isEmpty light
            lblRotate16IsEmptyLight.Visible = true;
            lblRotate16IsEmptyLight.Text = "●";
            lblRotate16IsEmptyLight.Font = new Font("Microsoft Sans Serif", 30);
            lblRotate16IsEmptyLight.ForeColor = Color.Gray;
        }

        private void setBufferStatus()
        {
            int beginStationNo = 20;

            for (int i = beginStationNo; i < 27; i++)
            {
                BufferStatus item = new BufferStatus();
                item.StationNo = beginStationNo.ToString();
                item.IsEmpty = "9";
                item.ArrangedTask = "9";
                listBufferStatusData.Add(item);

                beginStationNo += 1;
            }

            int beginStationNo2 = 40;
            for (int i = beginStationNo2; i < 49; i++)
            {
                BufferStatus item = new BufferStatus();
                item.StationNo = beginStationNo2.ToString();
                item.IsEmpty = "9";
                item.ArrangedTask = "9";
                listBufferStatusData.Add(item);

                beginStationNo2 += 1;
            }
        }

        private void setRotateStatus()
        {
            int beginStationNo = 30;
            for (int i = beginStationNo; i < 38; i++)
            {
                RotateStatus item = new RotateStatus();
                item.StationNo = beginStationNo.ToString();
                item.IsEmpty = "9";
                listRotateStatusData.Add(item);

                beginStationNo += 1;
            }

            int beginStationNo2 = 50;
            for (int i = beginStationNo2; i < 58; i++)
            {
                RotateStatus item = new RotateStatus();
                item.StationNo = beginStationNo2.ToString();
                item.IsEmpty = "9";
                listRotateStatusData.Add(item);

                beginStationNo2 += 1;
            }
        }
        #endregion

        #region custom function event

        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }

        private void ConnectExecuteOrDirect(BindingList<DeviceInfoModel> liSocketConn_Data, string DirectConn = "ALL")
        {
            DirectConn.ToUpper(); //轉大寫

            if (liSocketConn_Data != null)
            {
                //all machine connect
                if (!string.IsNullOrWhiteSpace(DirectConn) && DirectConn == "ALL")
                {
                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                    for (int i = 0; i < liSocketConn_Data.Count; i++)
                    {
                        DeviceInfoModel item = new DeviceInfoModel();
                        item = liSocketConn_Data[i];
                        ConnectDeviceService(item);
                    }
                }
                //direct machine connect
                else if (!string.IsNullOrWhiteSpace(DirectConn) && DirectConn != "ALL")
                {
                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                    for (int i = 0; i < liSocketConn_Data.Count; i++)
                    {
                        DeviceInfoModel item = new DeviceInfoModel();
                        item = liSocketConn_Data[i];

                        if (item.ConnectMachineID == DirectConn)
                        {
                            ConnectDeviceService(item);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 啟用thread, 不需要連結裝置的
        /// </summary>
        private void BeginService()
        {
            string HANDataPath = @"D:\Autostock\HANPLC\CMD";
            string BufferDataPath = @"D:\Autostock\BUFFER\CMD";
            string RotateDataPath = @"D:\Autostock\ROTATE\CMD";

            //讀取焊接機台的請求(大轉盤、鏈條線的入/出庫請求)
            DeviceObject HANPLCFile = new DeviceObject();
            HANPLCFile.dgvItemDataSourceTrigger = new DeviceObject.DgvItemDataSourceEventHandler(dgvItemDataSourceTrigger);
            executeThreadFileRead(threadGetHANPLCCmd, HANPLCFile, HANDataPath, 1);

            //讀取暫存區是否有冶具存在
            DeviceObject BufferFile = new DeviceObject();
            BufferFile.lblBufferIsEmptyTrigger = new DeviceObject.lblBufferIsEmptyEventHandler(lblBufferIsEmptyTrigger);
            executeThreadFileRead(threadGetBufferStatus, BufferFile, BufferDataPath, 2);

            //讀取大轉盤是否有冶具存在
            DeviceObject RotateFile = new DeviceObject();
            RotateFile.lblRotateIsEmptyTrigger = new DeviceObject.lblRotateIsEmptyEventHandler(lblRotateIsEmptyTrigger);
            executeThreadFileRead(threadGetRotateStatus, RotateFile, RotateDataPath, 3);

            int type = 0;

            ////上位電腦
            ComputerObject coExecute = new ComputerObject();
            coExecute.dgvItemRefresh = new ComputerObject.DgvItemDataRefreshEventHandler(dgvItemRefresh);
            coExecute.dgvItemRowsRemoveAt = new ComputerObject.DgvItemDataRemoveEventHandler(dgvItemRowsRemoveAt);
            coExecute.tbxLogAddMessageTrigger = new ComputerObject.TbxLogMessageEventHandler(tbxLogAddMessageTrigger);
            coExecute.lblBufferSetArrangedTaskTrigger = new ComputerObject.lblBufferSetArrangedTaskEventHandler(lblBufferSetArrangedTaskTrigger);
            coExecute.lblBufferItemNoTrigger = new ComputerObject.lblBufferSetItemNoEventHandler(lblBufferItemNoTrigger);
            coExecute.RFIDCheckFormOpenTrigger = new ComputerObject.RFIDCheckFormOpenEventHandler(RFIDCheckFormOpenTrigger);
            coExecute.AGVDisconnectTrigger = new ComputerObject.AGVDisconnectEventHandler(AGVDisconnectTrigger);
            coExecute.MessageBoxTrigger = new ComputerObject.MessageBoxEventHandler(MessageBoxTrigger);

            executeThreadProcess(threadComputer, coExecute, listDeviceConnInfo, dgvAGV_PlanList);
        }

        private void OnToConnected(IAsyncResult async) //DeviceInfoModel entity,
        {
            try
            {
                DeviceInfoModel entity = listDeviceConnInfo.Where(w => w.ConnectMachineType.Equals("2")).SingleOrDefault();

                entity.clientSocket.EndConnect(async);

                Console.WriteLine("============ AGV connection ============\n");
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }

        }

        /// <summary>
        /// 啟用thread, 需要連結裝置的
        /// </summary>
        /// <param name="entity"></param>
        private void ConnectDeviceService(DeviceInfoModel entity)
        {
            try
            {
                //new object include autostock & AGV
                if (entity.clientSocket == null || !entity.clientSocket.Connected) //entity.ConnectMachineType == "1" &&
                {
                    entity.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }

                //check connect status
                //1:autostock
                if (entity.ConnectMachineType == "1" && !entity.clientSocket.Connected)
                {
                    //entity.clientSocket.Bind(new IPEndPoint(IPAddress.Parse(AutostockClientIP), int.Parse(AutostockClientPort)));
                    entity.clientSocket.Connect(IPAddress.Parse(entity.tbxConnIP.Text.Trim()), int.Parse(entity.tbxConnPort.Text.Trim()));

                    Console.WriteLine("============ Autostock connection ============\n");

                    //update object status
                    if (entity.clientSocket.Connected) //entity.ConnectMachineType == "2" &&  //(entity.ConnectMachineType == "1" && entity.clientSocket.Connected) ||
                    {
                        entity.tbxConnIP.Enabled = false;
                        entity.tbxConnPort.Enabled = false;
                        entity.lblStatus.Text = "Online";
                        entity.btnConnService.Enabled = false;
                        entity.btnDisconn.Enabled = true;
                        entity.lblServerConnectLight.ForeColor = Color.Green;
                        entity.ConnStatus = "1";

                        entity.lblClientIP_Port.Text = IPAddress.Parse(((IPEndPoint)entity.clientSocket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.clientSocket.RemoteEndPoint).Port.ToString();

                        string msg = string.Empty;

                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                        DateTime dateTimeAGVDisconnect = DateTime.Now;
                        msg = string.Format("{0}：Autostock 連線, Autostock connect \r\n", dateTimeAGVDisconnect.ToString("yyyy-MM-dd HH:mm:ss"));

                        entityMsg.CREATE_DATETIME = dateTimeAGVDisconnect;
                        entityMsg.LOG_MESSAGE = msg;

                        tbxLogAddMessageTrigger(entityMsg);

                        ReceiveMsg(entity);
                    }
                }
                //2:AGV PLC
                else if (entity.ConnectMachineType == "2" && !entity.clientSocket.Connected)
                {
                    blExecuteAGVConnect = true;

                    //re-connect
                    entity.clientSocket.ReceiveTimeout = 1000;
                    entity.clientSocket.SendTimeout = 1000;

                    //entity.clientSocket.Bind(new IPEndPoint(IPAddress.Parse(AGVClientIP), int.Parse(AGVClientPort)));
                    entity.clientSocket.Connect(IPAddress.Parse(entity.tbxConnIP.Text.Trim()), int.Parse(entity.tbxConnPort.Text.Trim()));

                    //entity.clientSocket.BeginConnect(IPAddress.Parse(entity.tbxConnIP.Text.Trim()), int.Parse(entity.tbxConnPort.Text.Trim()),new AsyncCallback(OnToConnected),null);

                    //update object status
                    if (entity.clientSocket.Connected) //entity.ConnectMachineType == "2" &&  //(entity.ConnectMachineType == "1" && entity.clientSocket.Connected) ||
                    {
                        entity.tbxConnIP.Enabled = false;
                        entity.tbxConnPort.Enabled = false;
                        entity.lblStatus.Text = "Online";
                        entity.btnConnService.Enabled = false;
                        entity.btnDisconn.Enabled = true;
                        entity.lblServerConnectLight.ForeColor = Color.Green;
                        entity.ConnStatus = "1";

                        if (entity.ConnectMachineType == "1")
                        {
                            entity.lblClientIP_Port.Text = IPAddress.Parse(((IPEndPoint)entity.clientSocket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.clientSocket.RemoteEndPoint).Port.ToString();
                        }
                        else
                        {
                            entity.lblClientIP_Port.Text = IPAddress.Parse(((IPEndPoint)entity.clientSocket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.clientSocket.RemoteEndPoint).Port.ToString();
                        }

                        MELSECAGVObject agvObjectCleanAllMoveID_init = new MELSECAGVObject();
                        //agv write move id 
                        agvObjectCleanAllMoveID_init.setLoopCleanMoveIDData(entity);
                        agvObjectCleanAllMoveID_init.MoveIDLoopWriteData();

                        string msg = string.Empty;

                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                        DateTime dateTimeAGVDisconnect = DateTime.Now;
                        msg = string.Format("{0}：AGV連線, AGV connect \r\n", dateTimeAGVDisconnect.ToString("yyyy-MM-dd HH:mm:ss"));

                        entityMsg.CREATE_DATETIME = dateTimeAGVDisconnect;
                        entityMsg.LOG_MESSAGE = msg;

                        tbxLogAddMessageTrigger(entityMsg);

                        blAGVManualConnect = true;
                        blAGVManualDisconnect = false;
                        blAGVAutoDisconnect = false;
                    }

                    // AGV 1號機：life cycle, 避免斷線無法讀寫資料
                    MELSECAGVObject AGV_D0 = new MELSECAGVObject();
                    executeThreadAGVLifeCycle(threadAGVM_D0, AGV_D0, entity);

                    //讀取DWord資料, 使用loop方式, 避免接收資料錯亂
                    //DWord: 472, 476, 477, 478, 479, 480, 481, 482, 483, 484, 510
                    MELSECAGVObject AGV_DWordLoop = new MELSECAGVObject();
                    executeThreadAGVReadLoop(threadAGVM_Read_DWord, AGV_DWordLoop, entity);

                    blExecuteAGVConnect = false;

                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                MessageBox.Show(issueMessage);

                entity.clientSocket.Close();
                entity.clientSocket.Dispose();

            }
        }

        /// <summary>
        /// AGV Read DWord, Delta PLC, 用不到
        /// </summary>
        /// <param name="objecThread"></param>
        /// <param name="objectDeviceOperation"></param>
        /// <param name="entity"></param>
        /// <param name="DWord"></param>
        /// <param name="TimeCycle"></param>
        //private void executeThreadProcess(Thread objecThread, PLCObject objectDeviceOperation, DeviceInfoModel entity, int DWord, int TimeCycle)
        //{
        //    //for AGV TEST 
        //    if (objecThread != null && objecThread.IsAlive)
        //        objecThread.Abort();

        //    objecThread = new Thread(new ThreadStart(objectDeviceOperation.ReadPLCRegister));
        //    objectDeviceOperation.ReadPLCSetData(entity, Convert.ToUInt16(DWord), objecThread, TimeCycle, tbxLog);
        //    //objectDeviceOperation.ReadPLCSetLogData(AGVLog);
        //    objecThread.IsBackground = true;
        //    objecThread.Start();
        //}

        /// <summary>
        /// AGV Read DWord, Delta PLC, 用不到
        /// </summary>
        /// <param name="objecThread"></param>
        /// <param name="objectDeviceOperation"></param>
        /// <param name="entity"></param>
        /// <param name="DWord"></param>
        /// <param name="TimeCycle"></param>
        //private void executeThreadPLCCoils(Thread objecThread, PLCObject objectDeviceOperation, DeviceInfoModel entity, int DWord, int TimeCycle)
        //{
        //    if (objecThread != null && objecThread.IsAlive)
        //        objecThread.Abort();

        //    objecThread = new Thread(new ThreadStart(objectDeviceOperation.ReadPLCCoils));
        //    objectDeviceOperation.ReadPLCSetData(entity, Convert.ToUInt16(DWord), objecThread, TimeCycle, tbxLog);
        //    //objectDeviceOperation.ReadPLCSetLogData(AGVLog);
        //    objecThread.IsBackground = true;
        //    objecThread.Start();
        //}

        /// <summary>
        /// AGV Life Cycle, for MELSEC PLC
        /// </summary>
        /// <param name="objecThread"></param>
        /// <param name="AGVObject"></param>
        /// <param name="entity"></param>
        private void executeThreadAGVLifeCycle(Thread objecThread, MELSECAGVObject AGVObject, DeviceInfoModel entity)
        {
            //for AGV 
            if (objecThread != null && objecThread.IsAlive)
                objecThread.Abort();

            objecThread = new Thread(new ThreadStart(AGVObject.executeLifeCycle));
            AGVObject.setLifeCycle(entity, objecThread);
            objecThread.IsBackground = true;
            objecThread.Start();
        }

        /// <summary>
        /// AGV Read Dword, for MELSEC PLC
        /// </summary>
        /// <param name="objecThread"></param>
        /// <param name="AGVObject"></param>
        /// <param name="entity"></param>
        /// <param name="Dword"></param>
        private void executeThreadAGVRead(Thread objecThread, MELSECAGVObject AGVObject, DeviceInfoModel entity, int Dword)
        {
            //for AGV
            if (objecThread != null && objecThread.IsAlive)
                objecThread.Abort();

            objecThread = new Thread(new ThreadStart(AGVObject.SingleReadData));
            AGVObject.setReadData(entity, Dword, objecThread);
            objecThread.IsBackground = true;
            objecThread.Start();
        }

        private void executeThreadAGVReadLoop(Thread objecThread, MELSECAGVObject AGVObject, DeviceInfoModel entity)
        {
            //for AGV
            if (objecThread != null && objecThread.IsAlive)
                objecThread.Abort();

            objecThread = new Thread(new ThreadStart(AGVObject.SingleReadDataLoop));
            AGVObject.setReadDataLoop(entity, objecThread);
            objecThread.IsBackground = true;
            objecThread.Start();
        }

        private void executeThreadFileRead(Thread objecThread, DeviceObject ReadFileObject, string FilePath, int ReadDataType)
        {
            //for AGV
            if (objecThread != null && objecThread.IsAlive)
                objecThread.Abort();

            objecThread = new Thread(new ThreadStart(ReadFileObject.executeReadFile));
            ReadFileObject.setFolderPath(FilePath);
            ReadFileObject.setReadDataType(ReadDataType);
            if (ReadDataType == 1)
            {
                ReadFileObject.setTimeCycle(5000);
            }
            else if (ReadDataType == 2)
            {
                ReadFileObject.setTimeCycle(1000);
            }
            else if (ReadDataType == 3)
            {
                ReadFileObject.setTimeCycle(1000);
            }

            objecThread.IsBackground = true;
            objecThread.Start();
        }

        /// <summary>
        /// 上位PC執行Task, execute Task
        /// </summary>
        /// <param name="objecThread"></param>
        /// <param name="coObject"></param>
        /// <param name="entity"></param>
        /// <param name="dgvItem"></param>
        private void executeThreadProcess(Thread objecThread, ComputerObject coObject, BindingList<DeviceInfoModel> liDeviceConnInfo, DataGridView dgvItem)
        {
            if (objecThread != null && objecThread.IsAlive)
                objecThread.Abort();


            DeviceInfoModel autostockEntity = new DeviceInfoModel();
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                DeviceInfoModel item = new DeviceInfoModel();
                item = listDeviceConnInfo[i];

                //1:autostock
                if (item.ConnectMachineType == "1")
                {
                    autostockEntity = item;
                }
                //2:AGV PLC
                else if (item.ConnectMachineType == "2")
                {
                    agvEntity = item;
                }
            }

            objecThread = new Thread(new ThreadStart(coObject.executeAGVTask));
            coObject.executeAGVTaskSetData(autostockEntity, agvEntity, dgvItem, liComputerData);
            coObject.setBufferStatus(listBufferStatusData);
            coObject.setRotateStatus(listRotateStatusData);
            objecThread.IsBackground = true;
            objecThread.Start();
        }

        private void ReceiveMsg(DeviceInfoModel entity)
        {
            Thread thread = new Thread(() =>
            {
                while (entity.clientSocket.Connected)
                {
                    try
                    {
                        Byte[] byteContainer = new Byte[1024 * 1024 * 4];
                        int getlength = entity.clientSocket.Receive(byteContainer);
                        if (getlength <= 0)
                        {
                            break;
                        }
                        var getType = byteContainer[0].ToString();
                        string getmsg = Encoding.UTF8.GetString(byteContainer, 1, getlength - 1);

                        GetMsgFromServer(getType, getmsg, entity);
                    }
                    catch (Exception ex)
                    {
                        string issueMessage = ex.Message.ToString();

                        entity.lblClientIP_Port.Text = "";
                        entity.tbxConnIP.Enabled = true;
                        entity.tbxConnPort.Enabled = true;
                        entity.lblStatus.Text = "Off line";
                        entity.btnConnService.Enabled = true;
                        entity.btnDisconn.Enabled = false;
                        entity.lblServerConnectLight.ForeColor = Color.Gray;
                        entity.ConnStatus = "0";

                        entity.clientSocket.Dispose();
                        entity.clientSocket.Close();
                        break;
                        //throw ex;
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void GetMsgFromServer(string strType, string msg, DeviceInfoModel entity)
        {
            //tbMsg.AppendText($"類型：{strType};{msg}(from" + recSocket.RemoteEndPoint.ToString() + ")\r\n");

            if (DataToolHelper.IsJsonFormat(msg))
            {
                DateTime dateTimeAutostockReceiveMsg = DateTime.Now;

                dsAutoStockTransferJsonModel jsonData = new dsAutoStockTransferJsonModel();
                jsonData = JsonConvert.DeserializeObject<dsAutoStockTransferJsonModel>(msg);

                entity.dsAutostockServer = jsonData;

                string receiveMsg = string.Empty;

                receiveMsg = string.Format("{0}：step Autostocking:Receive Msg:{1} \r\n", dateTimeAutostockReceiveMsg.ToString("yyyy-MM-dd HH:mm:ss"), msg);
                //接收資料顯示, 記錄接收資料(insert DB)在computerObject
                Console.WriteLine(receiveMsg);

            }
        }

        /// <summary>
        /// tbxLog 委任事件, 增加訊息
        /// </summary>
        /// <param name="msg"></param>
        public void tbxLogAddMessageTrigger(dsLOG_MESSAGE entity)
        {
            tbxLog.InvokeIfRequired(() =>
            {
                int result = daoLogMessage.InsertLOG_MESSAGE(entity);
                tbxLog.AppendText(entity.LOG_MESSAGE);
                tbxLog.ScrollToCaret();
            });
        }

        /// <summary>
        /// dgvAGV_PlanList 委任事件, 增加任務
        /// </summary>
        /// <param name="addData"></param>
        /// <param name="CutInLine"></param>
        public void dgvItemDataSourceTrigger(AGVTaskModel addData, decimal CutInLine)  //
        {
            try
            {
                AGVTaskModel checkValue = new AGVTaskModel();

                if (CutInLine != 0)
                {
                    checkValue = liComputerData.Where(w => w.EXECUTE_SORT == CutInLine).SingleOrDefault();
                }

                if (checkValue != null && checkValue.EXECUTE_SORT.ToString() != null && checkValue.EXECUTE_SORT != 0)
                {
                    MessageBox.Show("cut in line incorrect value");
                    return;
                }

                //排序, 在這階段給值
                addData.EXECUTE_SORT = getExecuteSortValue(CutInLine);
                liComputerData.Add(addData);

                dgvItemOrderBy();

                DateTime datetimeLog = DateTime.Now;

                string msg = string.Format("{0}：add new cmd, Execute Seq:{1}, in/out:{2}, Item:{3}, From ST:{4}, To ST:{5}, Priority:{6}  \r\n", datetimeLog.ToString("yyyy-MM-dd HH:mm:ss"), addData.EXECUTE_SEQ, addData.INOUT_FLAG, addData.ITEM_NO, addData.AGV_FROM_ST, addData.AGV_TO_ST, addData.PRIORITY_AREA);

                dsLOG_MESSAGE msgEntity = new dsLOG_MESSAGE();
                msgEntity.CREATE_DATETIME = datetimeLog;
                msgEntity.LOG_MESSAGE = msg;

                tbxLogAddMessageTrigger(msgEntity);
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        /// <summary>
        /// 取得TASK排序
        /// </summary>
        /// <param name="CutInLine"></param>
        /// <returns></returns>
        public decimal getExecuteSortValue(decimal CutInLine = 0)
        {
            decimal decSortValue = 0;

            BindingList<AGVTaskModel> liAGVComputerData = liComputerData; // this.ParentForm.getliComputerData();

            //有資料
            if (liAGVComputerData.Count > 0)
            {
                //插隊, 輸入插入點並且不能小於等於現在第一筆
                if (CutInLine != 0 && CutInLine > liAGVComputerData.Select(s => s.EXECUTE_SORT).First())
                {
                    decSortValue = CutInLine;
                }
                //正常排隊
                else
                {
                    decSortValue = (liAGVComputerData.Max(m => m.EXECUTE_SORT)) + 1;
                }
            }
            //無資料
            else
            {
                decSortValue = 1;
            }

            return decSortValue;
        }

        /// <summary>
        /// dgvAGV_PlanList 委任事件, 刷新DataGridView
        /// </summary>
        public void dgvItemRefresh()
        {
            dgvAGV_PlanList.InvokeIfRequired(() =>
            {
                dgvAGV_PlanList.Refresh();

            });
        }

        public void dgvItemOrderBy()
        {

            dgvAGV_PlanList.InvokeIfRequired(() =>
            {
                liComputerData = new BindingList<AGVTaskModel>(liComputerData.OrderBy(o => o.EXECUTE_SORT).ToList());
                bdlAGVPlan = new BindingList<AGVTaskModel>(liComputerData);

                dgvAGV_PlanList.DataSource = bdlAGVPlan;
                dgvAGV_PlanList.Refresh();
            });
        }

        /// <summary>
        /// dgvAGV_PlanList 委任事件, 移除任務
        /// </summary>
        /// <param name="Index"></param>
        private void dgvItemRowsRemoveAt(AGVTaskModel item) //, int type = 0
        {
            //**type(刪除類型): 自動：0, 手動：1

            dgvAGV_PlanList.InvokeIfRequired(() =>
            {
                foreach (DataGridViewRow subItem in dgvAGV_PlanList.Rows)
                {
                    if (subItem.Cells["EXECUTE_SEQ"].Value.ToString().Equals(item.EXECUTE_SEQ))
                    {
                        if (subItem.Cells["EXECUTE_STATUS"].Value.ToString() != "" && subItem.Cells["EXECUTE_STATUS"].Value.ToString().ToUpper() != "LINE")
                        {
                            //dev
                            if (manualCancelCmdType == 1)
                            {
                                DateTime dateTimeAutostockErrorLog = DateTime.Now;
                                dsLOG_MESSAGE entityErrorMsg = new dsLOG_MESSAGE();
                                string msg = string.Empty;
                                msg = string.Format("{0}：step cancel cmd:手動刪除執行中任務(Manual delete executing task), seq:{1} \r\n", dateTimeAutostockErrorLog.ToString("yyyy-MM-dd HH:mm:ss"), item.EXECUTE_SEQ);

                                entityErrorMsg.CREATE_DATETIME = dateTimeAutostockErrorLog;
                                entityErrorMsg.LOG_MESSAGE = msg;

                                Console.WriteLine(msg);
                                tbxLogAddMessageTrigger(entityErrorMsg);

                                if (IsBuffer(item.AGV_TO_ST))
                                {
                                    //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                                    for (int i = 0; i < listBufferStatusData.Count; i++)
                                    {
                                        BufferStatus BItem = new BufferStatus();
                                        BItem = listBufferStatusData[i];

                                        if (item.AGV_TO_ST.Equals(BItem.StationNo) && BItem.ArrangedTask.Equals("1"))
                                        {
                                            BItem.ArrangedTask = "9";
                                            //BItem.InOutFlag = "";
                                            //BItem.ItemNo = "";
                                            //BItem.PriorityArea = "";

                                            lblBufferSetArrangedTaskTrigger(BItem);

                                            break;
                                        }
                                    }
                                }

                                manualCancelCmdType = 0;

                                DeviceInfoModel autostockEntityTransData = new DeviceInfoModel();

                                for (int i = 0; i < listDeviceConnInfo.Count; i++)
                                {
                                    //刪除前, 重置資料
                                    if (listDeviceConnInfo[i].ConnectMachineID == "ASM01")
                                    {
                                        // autostockEntityTransData = listDeviceConnInfo[i];
                                        autostockEntityTransData.dsAutostockClient = new dsAutoStockTransferJsonModel();
                                        autostockEntityTransData.dsAutostockServer = new dsAutoStockTransferJsonModel();
                                        autostockEntityTransData.dsAutostockServerKeepData = new dsAutoStockTransferJsonModel();
                                        autostockEntityTransData.dsModify = new dsAutoStockTransferJsonModel();
                                    }
                                }

                                //dgvAGV_PlanList.Rows.RemoveAt(subItem.Index);
                                blMessageboxShow = false;

                            }
                            //任務已完成刪除任務
                            else
                            {
                                //dgvAGV_PlanList.Rows.RemoveAt(subItem.Index);
                                blMessageboxShow = false;
                            }
                            //else
                            //{
                            //    DateTime dateTimeAutostockErrorLog = DateTime.Now;
                            //    dsLOG_MESSAGE entityErrorMsg = new dsLOG_MESSAGE();
                            //    string msg = string.Empty;
                            //    msg = string.Format("{0}：step cancel cmd:自動刪除執行中任務(auto delete executing task), seq:{1} \r\n", dateTimeAutostockErrorLog.ToString("yyyy-MM-dd HH:mm:ss"), item.EXECUTE_SEQ);

                            //    entityErrorMsg.CREATE_DATETIME = dateTimeAutostockErrorLog;
                            //    entityErrorMsg.LOG_MESSAGE = msg;

                            //    Console.WriteLine(msg);
                            //    tbxLogAddMessageTrigger(entityErrorMsg);
                            //}
                        }
                        else if (subItem.Cells["EXECUTE_STATUS"].Value.ToString() != "" && subItem.Cells["EXECUTE_STATUS"].Value.ToString().ToUpper() == "LINE")
                        {
                            DateTime dateTimeAutostockErrorLog = DateTime.Now;
                            dsLOG_MESSAGE entityErrorMsg = new dsLOG_MESSAGE();
                            string msg = string.Empty;
                            msg = string.Format("{0}：step cancel cmd:手動刪除非執行中任務(Manual delete non-executing task), seq:{1} \r\n", dateTimeAutostockErrorLog.ToString("yyyy-MM-dd HH:mm:ss"), item.EXECUTE_SEQ);

                            entityErrorMsg.CREATE_DATETIME = dateTimeAutostockErrorLog;
                            entityErrorMsg.LOG_MESSAGE = msg;

                            Console.WriteLine(msg);
                            tbxLogAddMessageTrigger(entityErrorMsg);

                            //dgvAGV_PlanList.Rows.RemoveAt(subItem.Index);
                        }


                        dgvAGV_PlanList.Rows.RemoveAt(subItem.Index);
                        break;
                    }
                }
            });
        }

        private void dgvItemRowMoveUp(int index)
        {
            dgvAGV_PlanList.InvokeIfRequired(() =>
            {
                BindingList<AGVTaskModel> liExecuteSource = new BindingList<AGVTaskModel>((BindingList<AGVTaskModel>)dgvAGV_PlanList.DataSource);

                AGVTaskModel upBeforeItem = new AGVTaskModel();
                AGVTaskModel upItem = new AGVTaskModel();
                AGVTaskModel currentItem = new AGVTaskModel();

                upBeforeItem = liExecuteSource[index - 2];
                upItem = liExecuteSource[index - 1];
                currentItem = liExecuteSource[index];

                decimal diffValue = upItem.EXECUTE_SORT - upBeforeItem.EXECUTE_SORT;
                int diffValueLength = diffValue.ToString().Length - diffValue.ToString().IndexOf('.') - 1;

                decimal finalDiffValue = 0;

                finalDiffValue = getDiffValue(diffValueLength);

                if ((upItem.EXECUTE_SORT - finalDiffValue).Equals(upBeforeItem.EXECUTE_SORT))
                {
                    finalDiffValue = getDiffValue(diffValueLength, 1);
                }

                currentItem.EXECUTE_SORT = upItem.EXECUTE_SORT - Convert.ToDecimal(finalDiffValue);

            });

            dgvItemOrderBy();
        }

        public decimal getDiffValue(int diffValueLength, int addPlus = 0)
        {
            string finalDiffValue = "0.";
            diffValueLength += addPlus;

            for (int i = 0; i < diffValueLength; i++)
            {
                if (i == diffValueLength - 1)
                {
                    finalDiffValue = finalDiffValue + "1";
                }
                else
                {
                    finalDiffValue = finalDiffValue + "0";
                }
            }

            return Convert.ToDecimal(finalDiffValue);
        }

        private void dgvItemRowMoveDown(int index)
        {
            dgvAGV_PlanList.InvokeIfRequired(() =>
            {
                List<AGVTaskModel> liExecuteSource = new List<AGVTaskModel>((BindingList<AGVTaskModel>)dgvAGV_PlanList.DataSource);

                AGVTaskModel currentItem = new AGVTaskModel();
                AGVTaskModel downItem = new AGVTaskModel();
                AGVTaskModel downAfterItem = new AGVTaskModel();

                currentItem = liExecuteSource[index];
                downItem = liExecuteSource[index + 1];

                bool blDownLastItem = false;

                if (dgvAGV_PlanList.Rows.Count.Equals(index + 2))
                {
                    blDownLastItem = true;
                }
                else
                {
                    downAfterItem = liExecuteSource[index + 2];
                }

                decimal diffValue = 0;
                int diffValueLength = 0;
                decimal finalDiffValue = 0;

                if (!blDownLastItem)
                {
                    diffValue = downAfterItem.EXECUTE_SORT - downItem.EXECUTE_SORT;
                    diffValueLength = diffValue.ToString().Length - diffValue.ToString().IndexOf('.') - 1;
                    finalDiffValue = getDiffValue(diffValueLength);

                    if ((downItem.EXECUTE_SORT + finalDiffValue).Equals(downAfterItem.EXECUTE_SORT))
                    {
                        finalDiffValue = getDiffValue(diffValueLength, 1);
                    }
                }
                else
                {
                    finalDiffValue += 1;
                }

                currentItem.EXECUTE_SORT = downItem.EXECUTE_SORT + Convert.ToDecimal(finalDiffValue);

            });

            dgvItemOrderBy();
        }

        /// <summary>
        /// lblBuffer 委任事件, 判斷是否有治具或物品在載台上
        /// </summary>
        /// <param name="transData"></param>
        public void lblBufferIsEmptyTrigger(BufferStatus transData)
        {
            try
            {
                //ST20 buffer 1
                if ((int)AGVStation.ST20_Buffer1 == int.Parse(transData.StationNo))
                {
                    lblBuffer1IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST20_Buffer1))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer1IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer1IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST21 buffer 2
                else if ((int)AGVStation.ST21_Buffer2 == int.Parse(transData.StationNo))
                {
                    lblBuffer2IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST21_Buffer2))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer2IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer2IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST22 buffer 3
                else if ((int)AGVStation.ST22_Buffer3 == int.Parse(transData.StationNo))
                {
                    lblBuffer3IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST22_Buffer3))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer3IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer3IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST23 buffer 4
                else if ((int)AGVStation.ST23_Buffer4 == int.Parse(transData.StationNo))
                {
                    lblBuffer4IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST23_Buffer4))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer4IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer4IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST24 buffer 5
                else if ((int)AGVStation.ST24_Buffer5 == int.Parse(transData.StationNo))
                {
                    lblBuffer5IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST24_Buffer5))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer5IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer5IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST25 buffer 6
                else if ((int)AGVStation.ST25_Buffer6 == int.Parse(transData.StationNo))
                {
                    lblBuffer6IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST25_Buffer6))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer6IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer6IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST26 buffer 7
                else if ((int)AGVStation.ST26_Buffer7 == int.Parse(transData.StationNo))
                {
                    lblBuffer7IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST26_Buffer7))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer7IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer7IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST50 buffer8
                else if ((int)AGVStation.ST40_Buffer8 == int.Parse(transData.StationNo))
                {
                    lblBuffer8IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST40_Buffer8))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer8IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer8IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST51 buffer 9
                else if ((int)AGVStation.ST41_Buffer9 == int.Parse(transData.StationNo))
                {
                    lblBuffer9IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST41_Buffer9))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer9IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer9IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST52 buffer 10
                else if ((int)AGVStation.ST42_Buffer10 == int.Parse(transData.StationNo))
                {
                    lblBuffer10IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST42_Buffer10))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer10IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer10IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST53 buffer 11
                else if ((int)AGVStation.ST43_Buffer11 == int.Parse(transData.StationNo))
                {
                    lblBuffer11IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST43_Buffer11))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer11IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer11IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST54 buffer 12
                else if ((int)AGVStation.ST44_Buffer12 == int.Parse(transData.StationNo))
                {
                    lblBuffer12IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST44_Buffer12))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer12IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer12IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST55 buffer 13
                else if ((int)AGVStation.ST45_Buffer13 == int.Parse(transData.StationNo))
                {
                    lblBuffer13IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST45_Buffer13))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer13IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer13IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST56 buffer 14
                else if ((int)AGVStation.ST46_Buffer14 == int.Parse(transData.StationNo))
                {
                    lblBuffer14IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST46_Buffer14))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer14IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer14IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST57 buffer 15
                else if ((int)AGVStation.ST47_Buffer15 == int.Parse(transData.StationNo))
                {
                    lblBuffer15IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST47_Buffer15))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer15IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer15IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST58 buffer 16
                else if ((int)AGVStation.ST48_Buffer16 == int.Parse(transData.StationNo))
                {
                    lblBuffer15IsEmptyLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST48_Buffer16))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblBuffer16IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblBuffer16IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                DateTime dateTimeLog = DateTime.Now;

                string msg = string.Format("{0}：Buffer ST No:{1}, Item:{2}  \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), transData.StationNo, transData.IsEmpty == "1" ? "Yes" : transData.IsEmpty == "9" ? "No" : "incorrect value");

                dsLOG_MESSAGE entity = new dsLOG_MESSAGE();
                entity.CREATE_DATETIME = dateTimeLog;
                entity.LOG_MESSAGE = msg;

                tbxLogAddMessageTrigger(entity);
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        /// <summary>
        /// 1.大轉盤入庫冶具, 會將冶具放到暫存區(buffer zone), 如有空位置會先預定該站台
        /// </summary>
        /// <param name="transData"></param>
        public void lblBufferSetArrangedTaskTrigger(BufferStatus transData)
        {
            try
            {
                if ((int)AGVStation.ST20_Buffer1 == int.Parse(transData.StationNo))
                {
                    lblBuffer1ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST20_Buffer1))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer1ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer1ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST21_Buffer2 == int.Parse(transData.StationNo))
                {
                    lblBuffer2ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST21_Buffer2))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer2ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer2ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST22_Buffer3 == int.Parse(transData.StationNo))
                {
                    lblBuffer3ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST22_Buffer3))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer3ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer3ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST23_Buffer4 == int.Parse(transData.StationNo))
                {
                    lblBuffer4ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST23_Buffer4))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer4ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer4ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST24_Buffer5 == int.Parse(transData.StationNo))
                {
                    lblBuffer5ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST24_Buffer5))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer5ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer5ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST25_Buffer6 == int.Parse(transData.StationNo))
                {
                    lblBuffer6ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST25_Buffer6))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer6ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer6ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST26_Buffer7 == int.Parse(transData.StationNo))
                {
                    lblBuffer7ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST26_Buffer7))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer7ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer7ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST40_Buffer8 == int.Parse(transData.StationNo))
                {
                    lblBuffer8ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST40_Buffer8))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer8ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer8ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST41_Buffer9 == int.Parse(transData.StationNo))
                {
                    lblBuffer9ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST41_Buffer9))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer9ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer9ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST42_Buffer10 == int.Parse(transData.StationNo))
                {
                    lblBuffer10ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST42_Buffer10))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer10ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer10ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST43_Buffer11 == int.Parse(transData.StationNo))
                {
                    lblBuffer11ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST43_Buffer11))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer11ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer11ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST44_Buffer12 == int.Parse(transData.StationNo))
                {
                    lblBuffer12ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST44_Buffer12))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer12ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer12ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST45_Buffer13 == int.Parse(transData.StationNo))
                {
                    lblBuffer13ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST45_Buffer13))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer13ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer13ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST46_Buffer14 == int.Parse(transData.StationNo))
                {
                    lblBuffer14ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST46_Buffer14))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer14ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer14ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST47_Buffer15 == int.Parse(transData.StationNo))
                {
                    lblBuffer15ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST47_Buffer15))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer15ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer15ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
                else if ((int)AGVStation.ST48_Buffer16 == int.Parse(transData.StationNo))
                {
                    lblBuffer16ArrangedLight.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST48_Buffer16))).SingleOrDefault();

                        if (transData.ArrangedTask == "1")
                        {
                            lblBuffer16ArrangedLight.ForeColor = Color.Yellow;
                        }
                        else if (transData.ArrangedTask == "9")
                        {
                            lblBuffer16ArrangedLight.ForeColor = Color.Gray;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        //lblBuffer1ItemNo
        public void lblBufferItemNoTrigger(BufferStatus transData)
        {
            try
            {
                //ST20 buffer 1
                if ((int)AGVStation.ST20_Buffer1 == int.Parse(transData.StationNo))
                {
                    lblBuffer1ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST20_Buffer1))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer1ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer1ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST21 buffer 2
                else if ((int)AGVStation.ST21_Buffer2 == int.Parse(transData.StationNo))
                {
                    lblBuffer2ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST21_Buffer2))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer2ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer2ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST22 buffer 3
                else if ((int)AGVStation.ST22_Buffer3 == int.Parse(transData.StationNo))
                {
                    lblBuffer3ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST22_Buffer3))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer3ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer3ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST23 buffer 4
                else if ((int)AGVStation.ST23_Buffer4 == int.Parse(transData.StationNo))
                {
                    lblBuffer4ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST23_Buffer4))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer4ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer4ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST24 buffer 5
                else if ((int)AGVStation.ST24_Buffer5 == int.Parse(transData.StationNo))
                {
                    lblBuffer5ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST24_Buffer5))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer5ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer5ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST25 buffer 6
                else if ((int)AGVStation.ST25_Buffer6 == int.Parse(transData.StationNo))
                {
                    lblBuffer6ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST25_Buffer6))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer6ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer6ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST26 buffer 7
                else if ((int)AGVStation.ST26_Buffer7 == int.Parse(transData.StationNo))
                {
                    lblBuffer7ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST26_Buffer7))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer7ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer7ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST50 buffer 8
                else if ((int)AGVStation.ST40_Buffer8 == int.Parse(transData.StationNo))
                {
                    lblBuffer8ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST40_Buffer8))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer8ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer8ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST51 buffer 9
                else if ((int)AGVStation.ST41_Buffer9 == int.Parse(transData.StationNo))
                {
                    lblBuffer9ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST41_Buffer9))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer9ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer9ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST52 buffer 10
                else if ((int)AGVStation.ST42_Buffer10 == int.Parse(transData.StationNo))
                {
                    lblBuffer10ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST42_Buffer10))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer10ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer10ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST53 buffer 11
                else if ((int)AGVStation.ST43_Buffer11 == int.Parse(transData.StationNo))
                {
                    lblBuffer11ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST43_Buffer11))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer11ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer11ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST54 buffer 12
                else if ((int)AGVStation.ST44_Buffer12 == int.Parse(transData.StationNo))
                {
                    lblBuffer12ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST44_Buffer12))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer12ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer12ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST55 buffer 13
                else if ((int)AGVStation.ST45_Buffer13 == int.Parse(transData.StationNo))
                {
                    lblBuffer13ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST45_Buffer13))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer13ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer13ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST56 buffer 14
                else if ((int)AGVStation.ST46_Buffer14 == int.Parse(transData.StationNo))
                {
                    lblBuffer14ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST46_Buffer14))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer14ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer14ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST57 buffer 15
                else if ((int)AGVStation.ST47_Buffer15 == int.Parse(transData.StationNo))
                {
                    lblBuffer15ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST47_Buffer15))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer15ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer15ItemNo.Text = item.ItemNo;
                        }
                    });
                }
                //ST58 buffer 16
                else if ((int)AGVStation.ST48_Buffer16 == int.Parse(transData.StationNo))
                {
                    lblBuffer16ItemNo.InvokeIfRequired(() =>
                    {
                        BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST48_Buffer16))).SingleOrDefault();
                        item.ItemNo = transData.ItemNo;

                        if (string.IsNullOrWhiteSpace(item.ItemNo))
                        {
                            lblBuffer16ItemNo.Text = "none";
                        }
                        else
                        {
                            lblBuffer16ItemNo.Text = item.ItemNo;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        public void lblBufferDataClearTrigger(BufferStatus transData)
        {
            try
            {
                if (IsBuffer(transData.StationNo))
                {
                    BufferStatus item = listBufferStatusData.Where(w => w.StationNo.Equals(transData.StationNo)).SingleOrDefault();

                    item.IsEmpty = "9";
                    item.ArrangedTask = "9";
                    item.ItemNo = "";
                    item.InOutFlag = "";
                    item.PriorityArea = "";

                    lblBufferIsEmptyTrigger(item);
                    lblBufferSetArrangedTaskTrigger(item);
                    lblBufferItemNoTrigger(item);
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        public void lblRotateIsEmptyTrigger(RotateStatus transData)
        {
            try
            {
                //ST30 Rotate 1
                if ((int)AGVStation.ST30_Rotate1 == int.Parse(transData.StationNo))
                {
                    lblRotate1IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST30_Rotate1))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate1IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate1IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST31 Rotate 2
                else if ((int)AGVStation.ST31_Rotate2 == int.Parse(transData.StationNo))
                {
                    lblRotate2IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST31_Rotate2))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate2IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate2IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST32 Rotate 3
                else if ((int)AGVStation.ST32_Rotate3 == int.Parse(transData.StationNo))
                {
                    lblRotate3IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST32_Rotate3))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate3IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate3IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST33 Rotate 4
                else if ((int)AGVStation.ST33_Rotate4 == int.Parse(transData.StationNo))
                {
                    lblRotate4IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST33_Rotate4))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate4IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate4IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST34 Rotate 5
                else if ((int)AGVStation.ST34_Rotate5 == int.Parse(transData.StationNo))
                {
                    lblRotate5IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST34_Rotate5))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate5IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate5IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST35 Rotate 6
                else if ((int)AGVStation.ST35_Rotate6 == int.Parse(transData.StationNo))
                {
                    lblRotate6IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST35_Rotate6))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate6IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate6IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST36 Rotate 7
                else if ((int)AGVStation.ST36_Rotate7 == int.Parse(transData.StationNo))
                {
                    lblRotate7IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST36_Rotate7))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate7IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate7IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST37 Rotate 8
                else if ((int)AGVStation.ST37_Rotate8 == int.Parse(transData.StationNo))
                {
                    lblRotate8IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST37_Rotate8))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate8IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate8IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST40 Rotate 9
                else if ((int)AGVStation.ST50_Rotate9 == int.Parse(transData.StationNo))
                {
                    lblRotate9IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST50_Rotate9))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate9IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate9IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST41 Rotate 10
                else if ((int)AGVStation.ST51_Rotate10 == int.Parse(transData.StationNo))
                {
                    lblRotate10IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST51_Rotate10))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate10IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate10IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST42 Rotate 11
                else if ((int)AGVStation.ST52_Rotate11 == int.Parse(transData.StationNo))
                {
                    lblRotate11IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST52_Rotate11))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate11IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate11IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST43 Rotate 12
                else if ((int)AGVStation.ST53_Rotate12 == int.Parse(transData.StationNo))
                {
                    lblRotate12IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST53_Rotate12))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate12IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate12IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST44 Rotate 13
                else if ((int)AGVStation.ST54_Rotate13 == int.Parse(transData.StationNo))
                {
                    lblRotate13IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST54_Rotate13))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate13IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate13IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST45 Rotate 14
                else if ((int)AGVStation.ST55_Rotate14 == int.Parse(transData.StationNo))
                {
                    lblRotate14IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST55_Rotate14))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate14IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate14IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST46 Rotate 15
                else if ((int)AGVStation.ST56_Rotate15 == int.Parse(transData.StationNo))
                {
                    lblRotate15IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST56_Rotate15))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate15IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate15IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }
                //ST47 Rotate 16
                else if ((int)AGVStation.ST57_Rotate16 == int.Parse(transData.StationNo))
                {
                    lblRotate16IsEmptyLight.InvokeIfRequired(() =>
                    {
                        RotateStatus item = listRotateStatusData.Where(w => w.StationNo.Equals(Convert.ToString((int)AGVStation.ST57_Rotate16))).SingleOrDefault();
                        item.IsEmpty = transData.IsEmpty;

                        if (transData.IsEmpty == "1")
                        {
                            lblRotate16IsEmptyLight.ForeColor = Color.Red;
                        }
                        else if (transData.IsEmpty == "9")
                        {
                            lblRotate16IsEmptyLight.ForeColor = Color.Gray;
                        }
                    });
                }

                DateTime dateTimeLog = DateTime.Now;

                string msg = string.Format("{0}：Rotate ST No:{1}, Item:{2}  \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), transData.StationNo, transData.IsEmpty == "1" ? "Yes" : transData.IsEmpty == "9" ? "No" : "incorrect value");

                dsLOG_MESSAGE entity = new dsLOG_MESSAGE();
                entity.CREATE_DATETIME = dateTimeLog;
                entity.LOG_MESSAGE = msg;

                tbxLogAddMessageTrigger(entity);
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        public void RFIDCheckFormOpenTrigger()
        {
            DeviceInfoModel autostockEntityTransData = new DeviceInfoModel();
            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "ASM01")
                {
                    autostockEntityTransData = listDeviceConnInfo[i];
                    break;
                }
            }

            RFIDCheckForm subForm = new RFIDCheckForm();
            subForm.setData(autostockEntityTransData);

            DialogResult result = subForm.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.No)
            {
                //do nothing
            }
        }

        public void MessageBoxTrigger(string msg, AGVTaskModel item)
        {
            dgvAGV_PlanList.InvokeIfRequired(() =>
            {
                foreach (DataGridViewRow subItem in dgvAGV_PlanList.Rows)
                {
                    if (subItem.Cells["EXECUTE_SEQ"].Value.ToString().Equals(item.EXECUTE_SEQ))
                    {
                        if (!blMessageboxShow)
                        {
                            MessageBox.Show(msg, "AGVMS Important Message", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,MessageBoxOptions.DefaultDesktopOnly);
                            blMessageboxShow = true;
                        }
                    }
                }              
            });

            //DialogResult dialogResult = MessageBox.Show(msg, "Important Message", MessageBoxButtons.OK);

            //if (dialogResult == DialogResult.OK)
            //{
            //    //do nothing 
            //}
        }

        /// <summary>
        /// 檢查是否為buffer站台, ComputerObject 也有相同檢查, 如修改須一併調整
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
                  (int)AGVStation.ST26_Buffer7 == int.Parse(stationNo)||
                  (int)AGVStation.ST40_Buffer8 == int.Parse(stationNo)||
                  (int)AGVStation.ST41_Buffer9 == int.Parse(stationNo)||
                  (int)AGVStation.ST42_Buffer10 == int.Parse(stationNo)||
                  (int)AGVStation.ST43_Buffer11 == int.Parse(stationNo)||
                  (int)AGVStation.ST44_Buffer12 == int.Parse(stationNo)||
                  (int)AGVStation.ST45_Buffer13 == int.Parse(stationNo)||
                  (int)AGVStation.ST46_Buffer14 == int.Parse(stationNo)||
                  (int)AGVStation.ST47_Buffer15 == int.Parse(stationNo)||
                  (int)AGVStation.ST48_Buffer16 == int.Parse(stationNo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void AGVDisconnectTrigger()
        {
            //Thread.Sleep(1000);

            if (blExecuteAGVConnect)
            {
                //do nothing
            }
            else if (!blExecuteAGVConnect)
            {
                DeviceInfoModel agvEntity = listDeviceConnInfo.Where(w => w.ConnectMachineType.Equals("2")).SingleOrDefault();

                agvEntity.lblClientIP_Port.Text = "";
                agvEntity.tbxConnIP.Enabled = true;
                agvEntity.tbxConnPort.Enabled = true;
                agvEntity.lblStatus.Text = "Off line";
                agvEntity.btnConnService.Enabled = true;
                agvEntity.btnDisconn.Enabled = false;
                agvEntity.lblServerConnectLight.ForeColor = Color.Gray;
                agvEntity.ConnStatus = "0";

                agvEntity.clientSocket.Dispose();
                agvEntity.clientSocket.Close();

                if (!blExecuteAGVConnect && (!blAGVManualDisconnect && !blAGVAutoDisconnect) && blAGVManualConnect)
                {
                    string msg = string.Empty;

                    dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                    DateTime dateTimeAGVDisconnect = DateTime.Now;
                    msg = string.Format("{0}：AGV離線, AGV disconnect \r\n", dateTimeAGVDisconnect.ToString("yyyy-MM-dd HH:mm:ss"));

                    entityMsg.CREATE_DATETIME = dateTimeAGVDisconnect;
                    entityMsg.LOG_MESSAGE = msg;

                    tbxLogAddMessageTrigger(entityMsg);

                    blAGVAutoDisconnect = true;

                    Thread.Sleep(5000);
                }
            }
        }


        #endregion

        #region tools controller event

        private void MainForm_Shown(object sender, EventArgs e)
        {
            listDeviceConnInfo.Add(new DeviceInfoModel()
            {
                ConnectMachineID = "ASM01",
                ConnectMachineType = "1",
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                ConnStatus = "0",
                //dictAGVDword = agvDWord,
                tbxConnIP = tbxAutostockConnIP,
                tbxConnPort = tbxAutostockConnPort,
                lblStatus = lblAutostockConnStatus,
                lblClientIP_Port = lblAutostockIP_Port,
                lblServerConnectLight = lblAutostockConnLight,
                btnConnService = btnAutostockConnStart,
                btnDisconn = btnAutostockDisconnect,
                dsAutostockClient = new dsAutoStockTransferJsonModel(),
                dsAutostockServer = new dsAutoStockTransferJsonModel(),
                dsAutostockServerKeepData = new dsAutoStockTransferJsonModel(),
                dsModify = new dsAutoStockTransferJsonModel(),
            });
            //liDeviceConnInfo.Add(new DeviceInfoModel() { ConnectMachineID = "ASM02", ConnectMachineType = "1", clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), tbxConnIP = tbConnServiceIP2, tbxConnPort = tbConnServicePort2, lblStatus = labelStatus2, lblClientIP_Port = lbClientIP_Port2, btnConnService = btnConnService2, lblServerConnectLight = lblServerConnectLight2, ConnStatus = "0" });
            //liDeviceConnInfo.Add(new DeviceInfoModel() { ConnectMachineID = "AGVM01", ConnectMachineType = "2", TcpClient = new TcpClient(), tbxConnIP = tbxAGVTest_AGVConnIP, tbxConnPort = tbxAGVTest_AGVConnPort, lblStatus = lblAGVTest_AGVConnStatus, lblClientIP_Port = lblAGVTest_AGVIP_Port, btnConnService = btnAGVTest_ConnStart, btnDisconn = btnAGVTest_Disconnect, lblServerConnectLight = lblAGVTest_ConnLight, ConnStatus = "0", dictDWordValue = agvDWord });

            listDeviceConnInfo.Add(new DeviceInfoModel()
            {
                ConnectMachineID = "AGVM01",
                ConnectMachineType = "2",
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                ConnStatus = "0",
                dictAGVDword = agvDWord,
                tbxConnIP = tbxAGVConnIP,
                tbxConnPort = tbxAGVConnPort,
                lblStatus = lblAGVConnStatus,
                lblClientIP_Port = lblAGVIP_Port,
                lblServerConnectLight = lblAGVConnLight,
                btnConnService = btnAGVConnStart,
                btnDisconn = btnAGVDisconnect
            });

            autoConnect();
            BeginService();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            DialogResult dlResult = MessageBox.Show("Are you sure close AGV Management System?", "Close system", MessageBoxButtons.YesNo);

            //close
            if (dlResult == DialogResult.Yes)
            {
                e.Cancel = false;
            }
            //cannot close
            else
            {
                e.Cancel = true;
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                DeviceInfoModel item = new DeviceInfoModel();
                item = listDeviceConnInfo[i];

                if (item.ConnectMachineID == "AGVM01" || item.ConnectMachineID == "ASM01")
                {
                    item.clientSocket.Dispose();
                    item.clientSocket.Close();
                }
            }

            Application.Exit();
        }

        private void aGVTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //AGVTestForm subForm = new AGVTestForm(listDeviceConnInfo); //, liComputerData, dgvAGV_PlanList
            //subForm.dgvItemDataSourceTrigger = new AGVTestForm.DgvItemDataSourceEventHandler(dgvItemDataSourceTrigger);
            //subForm.Show(this);
        }

        private void taskAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //檢查視窗並只能開啟一次
            Form checkOpen = Application.OpenForms["TaskAddForm"];
            //目前沒有開啟
            if (checkOpen == null || checkOpen.IsDisposed)
            {
                TaskAddForm subForm = new TaskAddForm();
                subForm.dgvItemDataSourceTrigger = new TaskAddForm.DgvItemDataSourceEventHandler(dgvItemDataSourceTrigger);
                subForm.tbxLogAddMessageTrigger = new TaskAddForm.TbxLogMessageEventHandler(tbxLogAddMessageTrigger);
                subForm.Show();
            }
            //已開啟視窗移到最前面, 縮小視窗要正常顯示
            else
            {
                checkOpen.Activate();
                checkOpen.WindowState = FormWindowState.Normal;
            }
        }

        private void bufferDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BufferUpdateForm subForm = new BufferUpdateForm();
            subForm.setData(listBufferStatusData);
            subForm.lblBufferItemNoTrigger = new BufferUpdateForm.lblBufferSetItemNoEventHandler(lblBufferItemNoTrigger);
            subForm.tbxLogAddMessageTrigger = new BufferUpdateForm.TbxLogMessageEventHandler(tbxLogAddMessageTrigger);
            //subForm.Show();

            DialogResult result = subForm.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.No)
            {
                //do nothing
            }
        }

        private void btnAGVConnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbxAGVConnIP.Text.Trim()) || string.IsNullOrWhiteSpace(tbxAGVConnPort.Text.Trim()))
            {
                MessageBox.Show("欲連接的服務主機的IP和連接埠不能為空!The IP and port of the service host to be connected cannot be empty!");
                return;
            }

            ConnectExecuteOrDirect(listDeviceConnInfo, "AGVM01");
        }

        private void btnAGVDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                for (int i = 0; i < listDeviceConnInfo.Count; i++)
                {
                    DeviceInfoModel item = new DeviceInfoModel();
                    item = listDeviceConnInfo[i];

                    if (item.ConnectMachineID == "AGVM01")
                    {
                        item.clientSocket.Dispose();
                        item.clientSocket.Close();

                        item.lblClientIP_Port.Text = "";
                        item.tbxConnIP.Enabled = true;
                        item.tbxConnPort.Enabled = true;
                        item.lblStatus.Text = "Off line";
                        item.btnConnService.Enabled = true;
                        item.btnDisconn.Enabled = false;
                        item.lblServerConnectLight.ForeColor = Color.Gray;
                        item.ConnStatus = "0";

                        string msg = string.Empty;

                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                        DateTime dateTimeAGVDisconnect = DateTime.Now;
                        msg = string.Format("{0}：AGV手動離線, AGV Manual disconnect \r\n", dateTimeAGVDisconnect.ToString("yyyy-MM-dd HH:mm:ss"));

                        entityMsg.CREATE_DATETIME = dateTimeAGVDisconnect;
                        entityMsg.LOG_MESSAGE = msg;

                        tbxLogAddMessageTrigger(entityMsg);

                        blAGVManualConnect = false;
                        blAGVManualDisconnect = true;
                    }
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                MessageBox.Show(issueMessage);
                throw ex;
            }
        }

        private void btnAutostockConnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbxAutostockConnIP.Text.Trim()) || string.IsNullOrWhiteSpace(tbxAutostockConnPort.Text.Trim()))
            {
                MessageBox.Show("欲連接的服務主機的IP和連接埠不能為空!The IP and port of the service host to be connected cannot be empty!");
                return;
            }

            ConnectExecuteOrDirect(listDeviceConnInfo, "ASM01");
        }
        private void btnAutostockDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                //從foreach改for, 因List在FOREACH是ReadOnly的，你無法在迴圈裡改變來源Collection。
                for (int i = 0; i < listDeviceConnInfo.Count; i++)
                {
                    DeviceInfoModel item = new DeviceInfoModel();
                    item = listDeviceConnInfo[i];

                    if (item.ConnectMachineID == "ASM01")
                    {
                        //item.TcpClient.Client.Dispose();
                        //item.TcpClient.Client.Close();
                        item.clientSocket.Dispose();
                        item.clientSocket.Close();

                        item.lblClientIP_Port.Text = "";
                        item.tbxConnIP.Enabled = true;
                        item.tbxConnPort.Enabled = true;
                        item.lblStatus.Text = "Off line";
                        item.btnConnService.Enabled = true;
                        item.btnDisconn.Enabled = false;
                        item.lblServerConnectLight.ForeColor = Color.Gray;
                        item.ConnStatus = "0";

                        string msg = string.Empty;

                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                        DateTime dateTimeAGVDisconnect = DateTime.Now;
                        msg = string.Format("{0}：Autostock 手動離線, Autostock Manual disconnect \r\n", dateTimeAGVDisconnect.ToString("yyyy-MM-dd HH:mm:ss"));

                        entityMsg.CREATE_DATETIME = dateTimeAGVDisconnect;
                        entityMsg.LOG_MESSAGE = msg;

                        tbxLogAddMessageTrigger(entityMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                MessageBox.Show(issueMessage);
                throw ex;
            }
        }

        private void dgvAGV_PlanList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex > -1 && e.RowIndex > -1)
                {
                    if (dgvAGV_PlanList.Columns[e.ColumnIndex].Name == "REMOVE_CMD")
                    {
                        //if (e.RowIndex == 0)
                        //{
                        //    MessageBox.Show("First Data executing, cannnot remove ");
                        //    return;
                        //}

                        DialogResult dialogResult = MessageBox.Show("Are you sure to remove this command ?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (dialogResult.Equals(DialogResult.No))
                        {
                            return;
                        }

                        BindingList<AGVTaskModel> liData = (BindingList<AGVTaskModel>)dgvAGV_PlanList.DataSource;
                        AGVTaskModel removeItem = new AGVTaskModel();

                        removeItem = liData.Where(w => w.EXECUTE_SEQ == dgvAGV_PlanList.Rows[e.RowIndex].Cells["EXECUTE_SEQ"].Value.ToString()).SingleOrDefault();

                        //removeItem.EXECUTE_SEQ = dgvAGV_PlanList.Rows[e.RowIndex].Cells["EXECUTE_SEQ"].Value.ToString();
                        manualCancelCmdType = 1;
                        dgvItemRowsRemoveAt(removeItem);
                    }

                    if (dgvAGV_PlanList.Columns[e.ColumnIndex].Name == "ADJUST_SORT_MOVE_UP_CMD")
                    {
                        if (e.RowIndex == 0 || e.RowIndex == 1)
                        {
                            MessageBox.Show("First Data executing, cann not move up");
                            return;
                        }

                        dgvItemRowMoveUp(e.RowIndex);

                     }

                    if (dgvAGV_PlanList.Columns[e.ColumnIndex].Name == "ADJUST_SORT_MOVE_DOWN_CMD")
                    {
                        int dgvMaxIndex = dgvAGV_PlanList.Rows.Count - 1;

                        if (e.RowIndex == 0)
                        {
                            MessageBox.Show("First Data executing, cann not move down");
                            return;
                        }
                        else if (e.RowIndex == dgvMaxIndex)
                        {
                            MessageBox.Show("Last Data can nnot move down");
                            return;
                        }

                        dgvItemRowMoveDown(e.RowIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                throw ex;
            }
        }

        private void btnBuffer1ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "20";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer2ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "21";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer3ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "22";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer4ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "23";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer5ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "24";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer6ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "25";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer7ClearData_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "26";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnAGVD110ForceResponse_Click(object sender, EventArgs e)
        {
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "AGVM01")
                {
                    agvEntity = listDeviceConnInfo[i];
                    break;
                }
            }

            MELSECAGVObject agvObjectMovingResponse = new MELSECAGVObject(agvEntity);
            //上位PC回應, D110: 回應請求
            agvObjectMovingResponse.setWriteData(110, 1);
            agvObjectMovingResponse.SingleWriteData();

            DateTime dateTimeLog = DateTime.Now;

            string msg = string.Format("{0}：step 強制手動回應AGV(Force manual response AGV), D110=1 \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"));

            //entityAGVMsg.CREATE_DATETIME = dateTimeLog;
            //entityAGVMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
        }

        private void btnAGVD110ForceClose_Click(object sender, EventArgs e)
        {
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "AGVM01")
                {
                    agvEntity = listDeviceConnInfo[i];
                    break;
                }
            }

            MELSECAGVObject agvObjectMovingResponse = new MELSECAGVObject(agvEntity);
            //上位PC回應, D110: 關閉請求
            agvObjectMovingResponse.setWriteData(110, 0);
            agvObjectMovingResponse.SingleWriteData();

            DateTime dateTimeLog = DateTime.Now;

            string msg = string.Format("{0}：step 強制手動回應AGV(Force manual response AGV), D110=0 \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"));

            //entityAGVMsg.CREATE_DATETIME = dateTimeLog;
            //entityAGVMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
        }

        private void btnAGVD72ForceResponse1_Click(object sender, EventArgs e)
        {
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "AGVM01")
                {
                    agvEntity = listDeviceConnInfo[i];
                    break;
                }
            }

            MELSECAGVObject agvObjectMoveIDResponse = new MELSECAGVObject(agvEntity);
            //上位PC回應, D72: 回應請求
            agvObjectMoveIDResponse.setWriteData(72, 1);
            agvObjectMoveIDResponse.SingleWriteData();

            DateTime dateTimeLog = DateTime.Now;

            string msg = string.Format("{0}：step 強制手動回應AGV(Force manual response AGV), D72=1 \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"));

            //entityAGVMsg.CREATE_DATETIME = dateTimeLog;
            //entityAGVMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
        }

        private void btnAGVD72ForceResponse3_Click(object sender, EventArgs e)
        {
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "AGVM01")
                {
                    agvEntity = listDeviceConnInfo[i];
                    break;
                }
            }

            MELSECAGVObject agvObjectMoveIDResponse = new MELSECAGVObject(agvEntity);
            //上位PC回應, D72: 回應請求
            agvObjectMoveIDResponse.setWriteData(72, 3);
            agvObjectMoveIDResponse.SingleWriteData();

            DateTime dateTimeLog = DateTime.Now;

            string msg = string.Format("{0}：step 強制手動回應AGV(Force manual response AGV), D72=3 \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"));

            //entityAGVMsg.CREATE_DATETIME = dateTimeLog;
            //entityAGVMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
        }

        private void btnAGVD72ForceResponse5_Click(object sender, EventArgs e)
        {
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "AGVM01")
                {
                    agvEntity = listDeviceConnInfo[i];
                    break;
                }
            }

            MELSECAGVObject agvObjectMoveIDResponse = new MELSECAGVObject(agvEntity);
            //上位PC回應, D72: 回應請求
            agvObjectMoveIDResponse.setWriteData(72, 5);
            agvObjectMoveIDResponse.SingleWriteData();

            DateTime dateTimeLog = DateTime.Now;

            string msg = string.Format("{0}：step 強制手動回應AGV(Force manual response AGV), D72=5 \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"));

            //entityAGVMsg.CREATE_DATETIME = dateTimeLog;
            //entityAGVMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
        }

        private void btnAGVD72ForceClose_Click(object sender, EventArgs e)
        {
            DeviceInfoModel agvEntity = new DeviceInfoModel();

            for (int i = 0; i < listDeviceConnInfo.Count; i++)
            {
                if (listDeviceConnInfo[i].ConnectMachineID == "AGVM01")
                {
                    agvEntity = listDeviceConnInfo[i];
                    break;
                }
            }

            MELSECAGVObject agvObjectMoveIDResponse = new MELSECAGVObject(agvEntity);
            //上位PC回應, D72: 關閉請求
            agvObjectMoveIDResponse.setWriteData(72, 0);
            agvObjectMoveIDResponse.SingleWriteData();

            DateTime dateTimeLog = DateTime.Now;

            string msg = string.Format("{0}：step 強制手動回應AGV(Force manual response AGV), D72=0 \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"));

            //entityAGVMsg.CREATE_DATETIME = dateTimeLog;
            //entityAGVMsg.LOG_MESSAGE = msg;

            Console.WriteLine(msg);
        }






        #endregion

        private void btnBuffer8ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "40";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer9ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "41";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer10ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "42";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer11ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "43";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer12ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "44";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer13ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "45";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer14ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "46";

                lblBufferDataClearTrigger(item);
            }
        }

        private void btnBuffer15ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "47";

                lblBufferDataClearTrigger(item);
            }
        }
        private void btnBuffer16ClearData_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure clear data?", "Confirm clear data", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                BufferStatus item = new BufferStatus();

                item.StationNo = "48";

                lblBufferDataClearTrigger(item);
            }
        }
    }
}
