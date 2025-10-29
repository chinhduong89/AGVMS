using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using AGVMSModel;
using System.Windows.Forms;
using AGVMSUtility;
using System.Net.Sockets;
using System.Collections;
using System.Linq;

namespace AGVMSObject
{
    public class MELSECAGVObject
    {
        private DeviceInfoModel entity;
        private Thread threadObject;
        //private Dictionary<int, ushort> dictMoveIdDWord_1;
        private AGVTaskModel agvData;
        private int MoveIDAreaType;
        //private TextBox tbxLog;
        private int TimeCycle = 500;
        private int Dword = 0; //ex:D472 = 472
        private int DwordValue = 0; //ex:D472 value = 8
        private List<int> LoopData;

        public delegate void TbxLogMessageEventHandler(dsLOG_MESSAGE entity);
        public TbxLogMessageEventHandler tbxLogAddMessageTrigger;

        private bool blCleanMoveID = false;

        #region Command
        private const string _SUB_HEADER_SEND = "5000";//副頭部, 2 byte
        private const string _NETWORK_NO = "00";//網路編號, 1 byte
        private const string _PC_NO = "FF";//可編程控制器編號, 1 byte
        private const string _IO_NO = "FF03"; //請求目標模塊I/O編號, 2 byte
        private const string _DEVICE_NO = "00";//請求目標模塊站號, 1 byte
        private string _DATA_LENGTH = "0000";  //請求數據長度, 2 byte, 需反轉順序(從低到高的順序)
        private const string _CPU_TIMER = "0200";   //CPU監視定時器, 2 byte
        private string _MAIN_COMMAND = "0000"; //發起指令, 2 byte, 需反轉順序(從低到高的順序), read:0406 => 0604, write:1406 => 0614
        private const string _SUB_COMMAND = "0000"; //發起子指令, 2 byte
        private string _WORD_BLOCK_COUNT = "00";   //字軟元件塊數, 1 byte
        private string _BIT_BLOCK_COUNT = "00"; //位軟元件塊數, 1 byte, 目前沒用到, 但先設定可變值

        private string _DEVICE_ADDRESS = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //for move id and from ST and To ST
        //block 2 D13
        private string _DEVICE_ADDRESS_2 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_2 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_2 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_2 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //block 3 D14
        private string _DEVICE_ADDRESS_3 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_3 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_3 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_3 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //block 4 D15
        private string _DEVICE_ADDRESS_4 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_4 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_4 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_4 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //block 5 D16
        private string _DEVICE_ADDRESS_5 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_5 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_5 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_5 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //block 6 D20
        private string _DEVICE_ADDRESS_6 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_6 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_6 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_6 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //block 7 D21
        private string _DEVICE_ADDRESS_7 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_7 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_7 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_7 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        //block 8 D72
        private string _DEVICE_ADDRESS_8 = "000000"; //字軟元件編號, 3 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_CODE_8 = "A8"; //軟元件代碼, 1 byte
        private string _DEVICE_POINT_8 = "0100"; //軟元件點數長度, 2 byte, 需反轉順序(從低到高的順序)
        private string _DEVICE_WRITE_VALUE_8 = "0000"; //軟元件點數值, 2 byte , 寫入資料用, 需反轉順序(從低到高的順序)

        #endregion

        private readonly int singleReadDataSize = 23;
        private readonly int singleWriteDataSize = 25;
        private readonly int MoveIDWriteDataSize = 81;

        public MELSECAGVObject()
        {

        }

        public MELSECAGVObject(DeviceInfoModel _entity)
        {
            entity = _entity;
        }
           
        #region custom function event

        private byte[] setFromHexString(string sendCommand, int sizeLength)
        {

            byte[] CommandData = new byte[sizeLength];

            for (int i = 0; i < sizeLength; i++)
            {
                CommandData[i] = Convert.ToByte(sendCommand.Substring(i * 2, 2), 16);
            }

            return CommandData;
        }

        private void DWordValueToBitArray(int _DWord, int _value)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            result.Add(0, 0); //0
            result.Add(1, 0); //1
            result.Add(2, 0); //2
            result.Add(3, 0); //3
            result.Add(4, 0); //4
            result.Add(5, 0); //5
            result.Add(6, 0); //6
            result.Add(7, 0); //7
            result.Add(8, 0); //8
            result.Add(9, 0); //9
            result.Add(10, 0); //A
            result.Add(11, 0); //B
            result.Add(12, 0); //C
            result.Add(13, 0); //D
            result.Add(14, 0); //E
            result.Add(15, 0); //F

            string bit = Convert.ToString(_value, 2).PadLeft(16, '0');

            for (int i = 0; i < bit.Length; i++)
            {
                result[i] = int.Parse(bit.Substring(bit.Length - i - 1, 1));
            }

            if (_DWord == 510)
            {
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "0")] = result[0]; //bit:0
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "8")] = result[8]; //bit:8
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "9")] = result[9]; //bit:9    
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "11")] = result[11]; //bit:B
            }
            else if (_DWord == 472)
            {
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "0")] = result[0]; //bit:0
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "1")] = result[1]; //bit:1
                entity.dictAGVDword[int.Parse(_DWord.ToString() + "2")] = result[2]; //bit:2          
            }
        }
        #endregion

        #region single read funcation event

        public void setReadData(DeviceInfoModel _entity, int _Dword, Thread _threadObject)
        {
            entity = _entity;
            Dword = _Dword;
            threadObject = _threadObject;

            string address = Dword.ToString("X").PadLeft(6, '0');
            string addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            _DATA_LENGTH = "0E00";
            _MAIN_COMMAND = "0604";
            _WORD_BLOCK_COUNT = "01";
            _DEVICE_ADDRESS = addressData;
        }

        public void setReadDataLoop(DeviceInfoModel _entity, Thread _threadObject)
        {
            entity = _entity;
            threadObject = _threadObject;

            LoopData = new List<int>();

            LoopData.Add(472); //AGV 1號機：(Response)move id寫入及請求發出後 必須監聽DWord
            LoopData.Add(476); //AGV 1號機：現行狀態 必須監聽DWord, 需配合D510
            LoopData.Add(477); //AGV 1號機：目前搬送ID, D477~D484, 需自行組合, 必須配合D510 and D476 (一次全部讀取會出現問題, 後來改為單個Dword讀取)
            LoopData.Add(478);
            LoopData.Add(479);
            LoopData.Add(480);
            LoopData.Add(481);
            LoopData.Add(482);
            LoopData.Add(483);
            LoopData.Add(484);
            LoopData.Add(510); //AGV 1號機：現行模式 and 發出讀取要求(Request) 必須監聽DWord, 包含啟用配車、拾取完了(拾取夾具)、搬送完了(包含自動、手動)、搬送中止、異常問題

            _DATA_LENGTH = "0E00";
            _MAIN_COMMAND = "0604";
            _WORD_BLOCK_COUNT = "01";
        }

        public void SingleReadData()
        {
            byte[] requestReadData = new byte[singleReadDataSize];
            byte[] responseReadData = new byte[1024];
            int responseLength = 0;

            while (entity.clientSocket.Connected)
            {
                try
                {
                    Thread.Sleep(TimeCycle);

                    //convert to hex
                    requestReadData = setFromHexString(getSingleReadCommand(), singleReadDataSize);

                    //send cmd data to agv
                    //entity.clientSocket.Send(requestReadData);
                    entity.clientSocket.Send(requestReadData);
                    Console.WriteLine("single read:send D{0} data: {1}", Dword, BitConverter.ToString(requestReadData).Replace('-', ' '));

                    //receive data from agv
                    responseLength = entity.clientSocket.Receive(responseReadData);
                    Console.WriteLine("single read:receive D{0} all data: {1}", Dword, BitConverter.ToString(responseReadData, 0, responseLength).Replace('-', ' '));

                    //byte array convert to string
                    string responseData_convertString = BitConverter.ToString(responseReadData, 0, responseLength).Replace('-', ' ');

                    //get value
                    string[] spDword = responseData_convertString.Substring(responseData_convertString.Length - 5, 5).Split(' ');
                    int DwordValue = Convert.ToInt32(spDword[1] + spDword[0], 16);

                    //fill in dictionary
                    if (entity.dictAGVDword[Dword] != DwordValue)
                    {
                        entity.dictAGVDword[Dword] = DwordValue;

                        if (Dword == 472 || Dword == 510)
                        {
                            DWordValueToBitArray(Dword, DwordValue);
                        }

                        dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                        DateTime dateTimeLog = DateTime.Now;

                        //get info
                        string msg = string.Format("{0}：single read:thread ID:{1}, PLC:{2}, IP:{3}, DWord:{4} Value:{5}\r\n",
                                       dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"),
                                       Thread.CurrentThread.ManagedThreadId,
                                       entity.ConnectMachineID,
                                       IPAddress.Parse(((IPEndPoint)entity.clientSocket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.clientSocket.RemoteEndPoint).Port.ToString(),
                                       Dword,
                                       DwordValue);

                        entityMsg.CREATE_DATETIME = dateTimeLog;
                        entityMsg.LOG_MESSAGE = msg;
                        Console.WriteLine(msg);
                    }
                }
                catch (Exception ex)
                {
                    string issueMessage = ex.Message.ToString();
                    //throw ex;
                    //exceptionProcess();                 
                }
            }
        }

        public void SingleReadDataLoop()
        {
            byte[] requestReadData = new byte[singleReadDataSize];
            byte[] responseReadData = new byte[1024];
            int responseLength = 0;

            while (entity.clientSocket.Connected)
            {
                try
                {
                    //Thread.Sleep(300);

                    for (int i = 0; i < LoopData.Count; i++)
                    {
                        requestReadData = new byte[singleReadDataSize];
                        responseReadData = new byte[1024];
                        responseLength = 0;

                        string address = LoopData[i].ToString("X").PadLeft(6, '0');
                        string addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412
                        _DEVICE_ADDRESS = addressData;

                        //convert to hex
                        requestReadData = setFromHexString(getSingleReadCommand(), singleReadDataSize);

                        //send cmd data to agv
                        //entity.clientSocket.Send(requestReadData);
                        entity.clientSocket.Send(requestReadData);
                        Console.WriteLine("single read:send D{0} data: {1}", LoopData[i], BitConverter.ToString(requestReadData).Replace('-', ' '));

                        //receive data from agv
                        responseLength = entity.clientSocket.Receive(responseReadData);
                        Console.WriteLine("single read:receive D{0} all data: {1}", LoopData[i], BitConverter.ToString(responseReadData, 0, responseLength).Replace('-', ' '));

                        //byte array convert to string
                        string responseData_convertString = BitConverter.ToString(responseReadData, 0, responseLength).Replace('-', ' ');

                        //get value
                        string[] spDword = responseData_convertString.Substring(responseData_convertString.Length - 5, 5).Split(' ');
                        int DwordValue = Convert.ToInt32(spDword[1] + spDword[0], 16);

                        //fill in dictionary
                        if (entity.dictAGVDword[LoopData[i]] != DwordValue)
                        {
                            entity.dictAGVDword[LoopData[i]] = DwordValue;

                            if (LoopData[i] == 472 || LoopData[i] == 510)
                            {
                                DWordValueToBitArray(LoopData[i], DwordValue);
                            }

                            dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                            DateTime dateTimeLog = DateTime.Now;

                            //get info
                            string msg = string.Format("{0}：single read:thread ID:{1}, PLC:{2}, IP:{3}, DWord:{4} Value:{5}\r\n",
                                           dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"),
                                           Thread.CurrentThread.ManagedThreadId,
                                           entity.ConnectMachineID,
                                           IPAddress.Parse(((IPEndPoint)entity.clientSocket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.clientSocket.RemoteEndPoint).Port.ToString(),
                                           LoopData[i],
                                           DwordValue);

                            entityMsg.CREATE_DATETIME = dateTimeLog;
                            entityMsg.LOG_MESSAGE = msg;
                            Console.WriteLine(msg);
                        }
                    }                  
                }
                catch (Exception ex)
                {
                    string issueMessage = ex.Message.ToString();
                    //throw ex;
                    //exceptionProcess();                 
                }
            }
        }

        private string getSingleReadCommand()
        {
            string returnValue = string.Empty;

            returnValue += _SUB_HEADER_SEND;
            returnValue += _NETWORK_NO;
            returnValue += _PC_NO;
            returnValue += _IO_NO;
            returnValue += _DEVICE_NO;
            returnValue += _DATA_LENGTH;
            returnValue += _CPU_TIMER;
            returnValue += _MAIN_COMMAND;
            returnValue += _SUB_COMMAND;
            returnValue += _WORD_BLOCK_COUNT;
            returnValue += _BIT_BLOCK_COUNT;
            returnValue += _DEVICE_ADDRESS;
            returnValue += _DEVICE_CODE;
            returnValue += _DEVICE_POINT;

            return returnValue;
        }

        #endregion

        #region single write function event

        public void setWriteData(int _Dword, int _DwordValue)
        {

            Dword = _Dword;
            DwordValue = _DwordValue;

            string address = Dword.ToString("X").PadLeft(6, '0');
            string addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            string writeValue = DwordValue.ToString("X").PadLeft(4, '0');
            string writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DATA_LENGTH = "1000";
            _MAIN_COMMAND = "0614";
            _WORD_BLOCK_COUNT = "01";
            _DEVICE_ADDRESS = addressData;
            _DEVICE_WRITE_VALUE = writeValueData;
        }

        public void SingleWriteData()
        {
            byte[] requestWriteData = new byte[singleWriteDataSize];
            byte[] responseWriteData = new byte[1024];
            int responseLength = 0;

            try
            {
                requestWriteData = setFromHexString(getSingleWriteCommand(), singleWriteDataSize);

                //send cmd data to agv
                entity.clientSocket.Send(requestWriteData);
                Console.WriteLine("single write:send D{0} data: {1}", Dword, BitConverter.ToString(requestWriteData).Replace('-', ' '));

                //receive data from agv
                responseLength = entity.clientSocket.Receive(responseWriteData);
                Console.WriteLine("single write:receive D{0} all data: {1}", Dword, BitConverter.ToString(responseWriteData, 0, responseLength).Replace('-', ' '));

            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                //throw ex;
                //exceptionProcess();
            }
        }

        private string getSingleWriteCommand()
        {
            string returnValue = string.Empty;

            returnValue += _SUB_HEADER_SEND;
            returnValue += _NETWORK_NO;
            returnValue += _PC_NO;
            returnValue += _IO_NO;
            returnValue += _DEVICE_NO;
            returnValue += _DATA_LENGTH;
            returnValue += _CPU_TIMER;
            returnValue += _MAIN_COMMAND;
            returnValue += _SUB_COMMAND;
            returnValue += _WORD_BLOCK_COUNT;
            returnValue += _BIT_BLOCK_COUNT;
            returnValue += _DEVICE_ADDRESS;
            returnValue += _DEVICE_CODE;
            returnValue += _DEVICE_POINT;
            returnValue += _DEVICE_WRITE_VALUE;

            return returnValue;
        }

        #endregion

        #region Move ID and From To process function event

        public void setMoveIDWriteData(DeviceInfoModel _entity, AGVTaskModel _agvData)
        {
            entity = _entity;
            agvData = _agvData;
            Dword = 12;
            int getMoveIDPosition = 0;

            string address = string.Empty;
            string addressData = string.Empty;
            string writeValue = string.Empty;
            string writeValueData = string.Empty;

            _DATA_LENGTH = "4800"; //(8(block) * 8(array, 1 block 8 array)) + 8(array) => 72, convert to He => 48             _MAIN_COMMAND = "0614";
            _MAIN_COMMAND = "0614";
            _WORD_BLOCK_COUNT = "08"; //8 block

            //MOVE ID:Dword:D12 D13 D14 D15 D16, block 1 ~ 5
            //Move id value example: 2401060001 (yymmdd+sequence no)
            for (int i = 0; i < 5; i++)
            {
                int DwordTemp = Dword + i;
                address = string.Empty;
                addressData = string.Empty;
                writeValue = string.Empty;
                writeValueData = string.Empty;

                address = DwordTemp.ToString("X").PadLeft(6, '0');
                addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

                byte[] getASCII = ASCIIEncoding.Default.GetBytes(agvData.EXECUTE_SEQ.Substring(getMoveIDPosition, 2));
                string Temp = getASCII[1].ToString("X") + getASCII[0].ToString("X");

                writeValue = int.Parse(Temp).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
                writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

                if (i == 0)
                {
                    _DEVICE_ADDRESS = addressData;
                    _DEVICE_WRITE_VALUE = writeValueData;
                }
                else if (i == 1)
                {
                    _DEVICE_ADDRESS_2 = addressData;
                    _DEVICE_WRITE_VALUE_2 = writeValueData;
                }
                else if (i == 2)
                {
                    _DEVICE_ADDRESS_3 = addressData;
                    _DEVICE_WRITE_VALUE_3 = writeValueData;
                }
                else if (i == 3)
                {
                    _DEVICE_ADDRESS_4 = addressData;
                    _DEVICE_WRITE_VALUE_4 = writeValueData;
                }
                else if (i == 4)
                {
                    _DEVICE_ADDRESS_5 = addressData;
                    _DEVICE_WRITE_VALUE_5 = writeValueData;
                }

                getMoveIDPosition += 2;
            }

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D20:From ST, block 6
            int FromSTDword = 20;
            address = FromSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(agvData.AGV_FROM_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_6 = addressData;
            _DEVICE_WRITE_VALUE_6 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D21:To ST, block 7
            int ToSTDword = 21;
            address = ToSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(agvData.AGV_TO_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_7 = addressData;
            _DEVICE_WRITE_VALUE_7 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D72:request to agv monitor, block 8
            int RequestDword = 72;
            address = RequestDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse("1").ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_8 = addressData;
            _DEVICE_WRITE_VALUE_8 = writeValueData;
        }

        public void MoveIDWriteData()
        {
            byte[] requestWriteData = new byte[MoveIDWriteDataSize];
            byte[] responseWriteData = new byte[1024];
            int responseLength = 0;

            try
            {
                requestWriteData = setFromHexString(getMoveIDWriteCommand(), MoveIDWriteDataSize);

                //send cmd data to agv
                entity.clientSocket.Send(requestWriteData);
                Console.WriteLine("MoveID all write, Clean={0}:send data: {1}", blCleanMoveID, BitConverter.ToString(requestWriteData).Replace('-', ' '));

                //receive data from agv
                responseLength = entity.clientSocket.Receive(responseWriteData);
                Console.WriteLine("MoveID all write, Clean={0}:receive all data: {1}", blCleanMoveID, BitConverter.ToString(responseWriteData, 0, responseLength).Replace('-', ' '));

            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                //throw ex;
                // exceptionProcess();
            }
        }

        public void setMoveIDLoopWriteData(DeviceInfoModel _entity, AGVTaskModel _agvData)
        {
            entity = _entity;
            agvData = _agvData;
            Dword = 12;
            int getMoveIDPosition = 0;

            string address = string.Empty;
            string addressData = string.Empty;
            string writeValue = string.Empty;
            string writeValueData = string.Empty;

            //_DATA_LENGTH = "4800"; //(8(block) * 8(array, 1 block 8 array)) + 8(array) => 72, convert to He => 48             _MAIN_COMMAND = "0614";
            _DATA_LENGTH = "1000";
            _MAIN_COMMAND = "0614";
            _WORD_BLOCK_COUNT = "01"; //1 block

            //MOVE ID:Dword:D12 D13 D14 D15 D16, block 1 ~ 5
            //Move id value example: 2401060001 (yymmdd+sequence no)
            for (int i = 0; i < 5; i++)
            {
                int DwordTemp = Dword + i;
                address = string.Empty;
                addressData = string.Empty;
                writeValue = string.Empty;
                writeValueData = string.Empty;

                address = DwordTemp.ToString("X").PadLeft(6, '0');
                addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

                byte[] getASCII = ASCIIEncoding.Default.GetBytes(agvData.EXECUTE_SEQ.Substring(getMoveIDPosition, 2));
                string Temp = getASCII[1].ToString("X") + getASCII[0].ToString("X");

                writeValue = int.Parse(Temp).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
                writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

                if (i == 0)
                {
                    _DEVICE_ADDRESS = addressData;
                    _DEVICE_WRITE_VALUE = writeValueData;
                }
                else if (i == 1)
                {
                    _DEVICE_ADDRESS_2 = addressData;
                    _DEVICE_WRITE_VALUE_2 = writeValueData;
                }
                else if (i == 2)
                {
                    _DEVICE_ADDRESS_3 = addressData;
                    _DEVICE_WRITE_VALUE_3 = writeValueData;
                }
                else if (i == 3)
                {
                    _DEVICE_ADDRESS_4 = addressData;
                    _DEVICE_WRITE_VALUE_4 = writeValueData;
                }
                else if (i == 4)
                {
                    _DEVICE_ADDRESS_5 = addressData;
                    _DEVICE_WRITE_VALUE_5 = writeValueData;
                }

                getMoveIDPosition += 2;
            }

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D20:From ST, block 6
            int FromSTDword = 20;
            address = FromSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(agvData.AGV_FROM_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_6 = addressData;
            _DEVICE_WRITE_VALUE_6 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D21:To ST, block 7
            int ToSTDword = 21;
            address = ToSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(agvData.AGV_TO_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_7 = addressData;
            _DEVICE_WRITE_VALUE_7 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D72:request to agv monitor, block 8
            int RequestDword = 72;
            address = RequestDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse("1").ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_8 = addressData;
            _DEVICE_WRITE_VALUE_8 = writeValueData;
        }

        public void MoveIDLoopWriteData()
        {
            byte[] requestWriteData = new byte[singleWriteDataSize];
            byte[] responseWriteData = new byte[1024];
            int responseLength = 0;

            try
            {
                for (int i = 0; i < 8; i++)
                {
                    requestWriteData = setFromHexString(getMoveIDLoopWriteCommand(i), singleWriteDataSize);

                    //send cmd data to agv
                    entity.clientSocket.Send(requestWriteData);
                    Console.WriteLine("MoveID loop write, Clean={0}:send i={1} data: {2}", blCleanMoveID, i, BitConverter.ToString(requestWriteData).Replace('-', ' '));

                    //receive data from agv
                    responseLength = entity.clientSocket.Receive(responseWriteData);
                    Console.WriteLine("MoveID loop write, Clean={0}:receive i={1} all data: {2}", blCleanMoveID, i, BitConverter.ToString(responseWriteData, 0, responseLength).Replace('-', ' '));
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                //throw ex;
                // exceptionProcess();
            }
        }

        private string getMoveIDWriteCommand()
        {
            string returnValue = string.Empty;

            returnValue += _SUB_HEADER_SEND;
            returnValue += _NETWORK_NO;
            returnValue += _PC_NO;
            returnValue += _IO_NO;
            returnValue += _DEVICE_NO;
            returnValue += _DATA_LENGTH; //DATA LENGTH TOTAL(SUM OF THE FOLLOWING)：72
            returnValue += _CPU_TIMER; //LENGTH:2
            returnValue += _MAIN_COMMAND; //LENGTH:2
            returnValue += _SUB_COMMAND; //LENGTH:2
            returnValue += _WORD_BLOCK_COUNT; //LENGTH:1
            returnValue += _BIT_BLOCK_COUNT; //LENGTH:1
            //BLOCK 1 D12 - LENGTH:8
            returnValue += _DEVICE_ADDRESS; //LENGTH:3
            returnValue += _DEVICE_CODE; //LENGTH:1
            returnValue += _DEVICE_POINT; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE; //LENGTH:2

            //block 2 D13 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_2; //LENGTH:3
            returnValue += _DEVICE_CODE_2; //LENGTH:1
            returnValue += _DEVICE_POINT_2; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_2; //LENGTH:2

            //block 3 D14 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_3; //LENGTH:3
            returnValue += _DEVICE_CODE_3; //LENGTH:1
            returnValue += _DEVICE_POINT_3; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_3; //LENGTH:2

            //block 4 D15 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_4; //LENGTH:3
            returnValue += _DEVICE_CODE_4; //LENGTH:1
            returnValue += _DEVICE_POINT_4; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_4; //LENGTH:2

            //block 5 D16 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_5; //LENGTH:3
            returnValue += _DEVICE_CODE_5; //LENGTH:1
            returnValue += _DEVICE_POINT_5; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_5; //LENGTH:2

            //block 6 D20 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_6; //LENGTH:3
            returnValue += _DEVICE_CODE_6; //LENGTH:1
            returnValue += _DEVICE_POINT_6; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_6; //LENGTH:2

            //block 7 D21 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_7; //LENGTH:3
            returnValue += _DEVICE_CODE_7; //LENGTH:1
            returnValue += _DEVICE_POINT_7; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_7; //LENGTH:2

            //block 8 D72 - LENGTH:8
            returnValue += _DEVICE_ADDRESS_8; //LENGTH:3
            returnValue += _DEVICE_CODE_8; //LENGTH:1
            returnValue += _DEVICE_POINT_8; //LENGTH:2
            returnValue += _DEVICE_WRITE_VALUE_8; //LENGTH:2

            return returnValue;
        }

        private string getMoveIDLoopWriteCommand(int iLoop)
        {
            string returnValue = string.Empty;

            returnValue += _SUB_HEADER_SEND;
            returnValue += _NETWORK_NO;
            returnValue += _PC_NO;
            returnValue += _IO_NO;
            returnValue += _DEVICE_NO;
            returnValue += _DATA_LENGTH; //DATA LENGTH TOTAL(SUM OF THE FOLLOWING)：72
            returnValue += _CPU_TIMER; //LENGTH:2
            returnValue += _MAIN_COMMAND; //LENGTH:2
            returnValue += _SUB_COMMAND; //LENGTH:2
            returnValue += _WORD_BLOCK_COUNT; //LENGTH:1
            returnValue += _BIT_BLOCK_COUNT; //LENGTH:1

            if (iLoop == 0)
            {
                //BLOCK 1 D12 - LENGTH:8
                returnValue += _DEVICE_ADDRESS; //LENGTH:3
                returnValue += _DEVICE_CODE; //LENGTH:1
                returnValue += _DEVICE_POINT; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE; //LENGTH:2
            }
            else if (iLoop == 1)
            {
                //block 2 D13 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_2; //LENGTH:3
                returnValue += _DEVICE_CODE_2; //LENGTH:1
                returnValue += _DEVICE_POINT_2; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_2; //LENGTH:2
            }
            else if (iLoop == 2)
            {
                //block 3 D14 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_3; //LENGTH:3
                returnValue += _DEVICE_CODE_3; //LENGTH:1
                returnValue += _DEVICE_POINT_3; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_3; //LENGTH:2
            }
            else if (iLoop == 3)
            {
                //block 4 D15 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_4; //LENGTH:3
                returnValue += _DEVICE_CODE_4; //LENGTH:1
                returnValue += _DEVICE_POINT_4; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_4; //LENGTH:2
            }
            else if (iLoop == 4)
            {
                //block 5 D16 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_5; //LENGTH:3
                returnValue += _DEVICE_CODE_5; //LENGTH:1
                returnValue += _DEVICE_POINT_5; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_5; //LENGTH:2
            }
            else if (iLoop == 5)
            {
                //block 6 D20 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_6; //LENGTH:3
                returnValue += _DEVICE_CODE_6; //LENGTH:1
                returnValue += _DEVICE_POINT_6; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_6; //LENGTH:2
            }
            else if (iLoop == 6)
            {
                //block 7 D21 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_7; //LENGTH:3
                returnValue += _DEVICE_CODE_7; //LENGTH:1
                returnValue += _DEVICE_POINT_7; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_7; //LENGTH:2
            }
            else if (iLoop == 7)
            {
                //block 8 D72 - LENGTH:8
                returnValue += _DEVICE_ADDRESS_8; //LENGTH:3
                returnValue += _DEVICE_CODE_8; //LENGTH:1
                returnValue += _DEVICE_POINT_8; //LENGTH:2
                returnValue += _DEVICE_WRITE_VALUE_8; //LENGTH:2
            }

            return returnValue;
        }

        public void setCleanMoveIdData(DeviceInfoModel _entity)
        {
            entity = _entity;
            string cleanEXECUTE_SEQ = "0000000000";
            string cleanFROM_ST = "0";
            string cleanTO_ST = "0";
            string cleanValue = "0";

            Dword = 12;
            int getMoveIDPosition = 0;

            string address = string.Empty;
            string addressData = string.Empty;
            string writeValue = string.Empty;
            string writeValueData = string.Empty;

            _DATA_LENGTH = "4800"; //(8(block) * 8(array, 1 block 8 array)) + 8(array) => 72, convert to He => 48             _MAIN_COMMAND = "0614";
            _MAIN_COMMAND = "0614";
            _WORD_BLOCK_COUNT = "08"; //8 block

            //MOVE ID:Dword:D12 D13 D14 D15 D16, block 1 ~ 5
            //Move id value example: 2401060001 (yymmdd+sequence no)
            for (int i = 0; i < 5; i++)
            {
                int DwordTemp = Dword + i;
                address = string.Empty;
                addressData = string.Empty;
                writeValue = string.Empty;
                writeValueData = string.Empty;

                address = DwordTemp.ToString("X").PadLeft(6, '0');
                addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

                //byte[] getASCII = ASCIIEncoding.Default.GetBytes(cleanEXECUTE_SEQ.Substring(getMoveIDPosition, 2));
                //string Temp = getASCII[1].ToString("X") + getASCII[0].ToString("X");

                writeValue = int.Parse(cleanValue).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
                writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

                if (i == 0)
                {
                    _DEVICE_ADDRESS = addressData;
                    _DEVICE_WRITE_VALUE = writeValueData;
                }
                else if (i == 1)
                {
                    _DEVICE_ADDRESS_2 = addressData;
                    _DEVICE_WRITE_VALUE_2 = writeValueData;
                }
                else if (i == 2)
                {
                    _DEVICE_ADDRESS_3 = addressData;
                    _DEVICE_WRITE_VALUE_3 = writeValueData;
                }
                else if (i == 3)
                {
                    _DEVICE_ADDRESS_4 = addressData;
                    _DEVICE_WRITE_VALUE_4 = writeValueData;
                }
                else if (i == 4)
                {
                    _DEVICE_ADDRESS_5 = addressData;
                    _DEVICE_WRITE_VALUE_5 = writeValueData;
                }

                getMoveIDPosition += 2;
            }

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D20:From ST, block 6
            int FromSTDword = 20;
            address = FromSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(cleanFROM_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_6 = addressData;
            _DEVICE_WRITE_VALUE_6 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D21:To ST, block 7
            int ToSTDword = 21;
            address = ToSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(cleanTO_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_7 = addressData;
            _DEVICE_WRITE_VALUE_7 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D72:request to agv monitor, block 8
            int RequestDword = 72;
            address = RequestDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse("0").ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_8 = addressData;
            _DEVICE_WRITE_VALUE_8 = writeValueData;
        }

        public void setLoopCleanMoveIDData(DeviceInfoModel _entity)
        {
            entity = _entity;
            string cleanEXECUTE_SEQ = "0000000000";
            string cleanFROM_ST = "0";
            string cleanTO_ST = "0";
            string cleanValue = "0";
            Dword = 12;
            int getMoveIDPosition = 0;

            blCleanMoveID = true;

            string address = string.Empty;
            string addressData = string.Empty;
            string writeValue = string.Empty;
            string writeValueData = string.Empty;

            //_DATA_LENGTH = "4800"; //(8(block) * 8(array, 1 block 8 array)) + 8(array) => 72, convert to He => 48             _MAIN_COMMAND = "0614";
            _DATA_LENGTH = "1000";
            _MAIN_COMMAND = "0614";
            _WORD_BLOCK_COUNT = "01"; //8 block

            //MOVE ID:Dword:D12 D13 D14 D15 D16, block 1 ~ 5
            //Move id value example: 2401060001 (yymmdd+sequence no)
            for (int i = 0; i < 5; i++)
            {
                int DwordTemp = Dword + i;
                address = string.Empty;
                addressData = string.Empty;
                writeValue = string.Empty;
                writeValueData = string.Empty;

                address = DwordTemp.ToString("X").PadLeft(6, '0');
                addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

                //byte[] getASCII = ASCIIEncoding.Default.GetBytes(cleanEXECUTE_SEQ.Substring(getMoveIDPosition, 2));
                //string Temp = getASCII[1].ToString("X") + getASCII[0].ToString("X");

                writeValue = int.Parse(cleanValue).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
                writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

                if (i == 0)
                {
                    _DEVICE_ADDRESS = addressData;
                    _DEVICE_WRITE_VALUE = writeValueData;
                }
                else if (i == 1)
                {
                    _DEVICE_ADDRESS_2 = addressData;
                    _DEVICE_WRITE_VALUE_2 = writeValueData;
                }
                else if (i == 2)
                {
                    _DEVICE_ADDRESS_3 = addressData;
                    _DEVICE_WRITE_VALUE_3 = writeValueData;
                }
                else if (i == 3)
                {
                    _DEVICE_ADDRESS_4 = addressData;
                    _DEVICE_WRITE_VALUE_4 = writeValueData;
                }
                else if (i == 4)
                {
                    _DEVICE_ADDRESS_5 = addressData;
                    _DEVICE_WRITE_VALUE_5 = writeValueData;
                }

                getMoveIDPosition += 2;
            }

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D20:From ST, block 6
            int FromSTDword = 20;
            address = FromSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(cleanFROM_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_6 = addressData;
            _DEVICE_WRITE_VALUE_6 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D21:To ST, block 7
            int ToSTDword = 21;
            address = ToSTDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse(cleanTO_ST).ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_7 = addressData;
            _DEVICE_WRITE_VALUE_7 = writeValueData;

            address = string.Empty;
            addressData = string.Empty;
            writeValue = string.Empty;
            writeValueData = string.Empty;
            //D72:request to agv monitor, block 8
            int RequestDword = 72;
            address = RequestDword.ToString("X").PadLeft(6, '0');
            addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            writeValue = int.Parse("0").ToString("X").PadLeft(4, '0');//DwordValue.ToString("X").PadLeft(4, '0');
            writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DEVICE_ADDRESS_8 = addressData;
            _DEVICE_WRITE_VALUE_8 = writeValueData;
        }
        #endregion

        #region AGV LifeCycle function event
        public void setLifeCycle(DeviceInfoModel _entity, Thread _threadObject)
        {
            entity = _entity;
            threadObject = _threadObject;

            string address = Dword.ToString("X").PadLeft(6, '0');
            string addressData = address.Substring(4, 2) + address.Substring(2, 2) + address.Substring(0, 2); //反轉從低到高的順序, ex:123456 => 563412

            //string writeValue = DwordValue.ToString("X").PadLeft(4, '0');
            //string writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);

            _DATA_LENGTH = "1000";
            _MAIN_COMMAND = "0614";
            _WORD_BLOCK_COUNT = "01";
            _DEVICE_ADDRESS = addressData;
            _DEVICE_POINT = "0100";
            //_DEVICE_WRITE_VALUE = writeValueData;
        }

        public void executeLifeCycle()
        {
            bool lifeCycle = true;
            int TimeCycle = 1000;
            byte[] requestWriteData = new byte[singleWriteDataSize];
            byte[] responseWriteData = new byte[1024];
            int responseLength = 0;
            string writeValue = string.Empty;
            string writeValueData = string.Empty;

            while (entity.clientSocket.Connected)
            {
                try
                {
                    Thread.Sleep(TimeCycle);

                    if (lifeCycle)
                    {
                        DwordValue = 0;
                        writeValue = DwordValue.ToString("X").PadLeft(4, '0');
                        writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);
                        lifeCycle = false;
                    }
                    else
                    {
                        DwordValue = 1;
                        writeValue = DwordValue.ToString("X").PadLeft(4, '0');
                        writeValueData = writeValue.Substring(2, 2) + writeValue.Substring(0, 2);
                        lifeCycle = true;
                    }

                    _DEVICE_WRITE_VALUE = writeValueData;

                    requestWriteData = setFromHexString(getSingleWriteCommand(), singleWriteDataSize);
                    //send cmd data to agv
                    Console.WriteLine("life cycle:send D{0} data: {1}", Dword, BitConverter.ToString(requestWriteData).Replace('-', ' '));
                    entity.clientSocket.Send(requestWriteData);

                    //receive data from agv
                    responseLength = entity.clientSocket.Receive(responseWriteData);
                    Console.WriteLine("life cycle:receive D{0} all data: {1}", Dword, BitConverter.ToString(responseWriteData, 0, responseLength).Replace('-', ' '));
                }
                catch (Exception ex)
                {
                    string issueMessage = ex.Message.ToString();
                    //throw ex;
                    //exceptionProcess();
                }
            }
        }
        #endregion

    }
}
