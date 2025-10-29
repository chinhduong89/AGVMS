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

namespace AGVMSObject
{
    public class PLCObject
    {
        private DeviceInfoModel entity;
        private Thread threadObject;
        private ushort DWord = 0;
        private ushort DWordValue = 0;
        private int TimeCycle;
        private Dictionary<int, ushort> dictMoveIdDWord_1;
        private AGVTaskModel agvData;
        private int MoveIDAreaType;
        private TextBox tbxLog;
        private byte slaveAddress;
        private Socket clientSocket;

        public PLCObject()
        {

        }
        public PLCObject(DeviceInfoModel _entity)
        {
            entity = _entity;
        }

        public PLCObject(DeviceInfoModel _entity, ushort _DWord, ushort _DWordValue)
        {
            entity = _entity;
            DWord = _DWord;
            DWordValue = _DWordValue;
        }

        #region Melsec Communication protocol

        public virtual void sendCommand()
        { 
        
        }

        #endregion



        #region Read PLC DWord Value for Delta AS218TX series

        public virtual void WriteSingleRegisterSetData(ushort _DWord, ushort _DWordValue)
        {
            DWord = _DWord;
            DWordValue = _DWordValue;
        }
        public virtual void WriteSingleRegister()
        {
            try
            {
                //entity.MBusM.WriteSingleRegister(DWord, DWordValue);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public virtual void ReadPLCSetData(DeviceInfoModel _entity, ushort _DWord, Thread _threadObject, int _TimeCycle, TextBox _tbxLog)
        {
            entity = _entity;
            DWord = _DWord;
            threadObject = _threadObject;
            TimeCycle = _TimeCycle;
            tbxLog = _tbxLog;
        }

        public virtual void ReadPLCRegister()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(TimeCycle);
                    ushort[] value = new ushort[2];

                    if (entity.clientSocket.Connected)
                    {
                        //value = entity.MBusM.ReadHoldingRegisters(DWord, 1);

                        //if (entity.dictDWordValue[DWord] != value[0])
                        //{
                        //    entity.dictDWordValue[DWord] = value[0];

                        //    string loginfo = string.Format("{0}：thread ID:{1}, PLC:{2}, IP:{3}, DWord:{4} Value:{5}\r\n",
                        //                         DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        //                         Thread.CurrentThread.ManagedThreadId,
                        //                         entity.ConnectMachineID,
                        //                         IPAddress.Parse(((IPEndPoint)entity.TcpClient.Client.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.TcpClient.Client.RemoteEndPoint).Port.ToString(),
                        //                         DWord,
                        //                         value[0]);

                        //    Console.WriteLine(loginfo);

                        //    tbxLog.InvokeIfRequired(() =>
                        //    {
                        //        tbxLog.AppendText(loginfo);

                        //    });
                        //}
                    }
                    else
                    {
                        threadObject.Abort();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    string issueMessage = ex.Message.ToString();
                    exceptionProcess();
                }
            }
        }

        public virtual void ReadPLCCoils()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(TimeCycle);
                    //bool[] value;
                    ushort[] value = new ushort[2];
                    int i = 0;

                    if (entity.clientSocket.Connected)
                    {
                        //value = entity.MBusM.ReadHoldingRegisters(DWord, 16);

                        //foreach (var item in value)
                        //{
                        //    Console.WriteLine("test Dword:{0}, coils:{1}, value:{2}", DWord, i, item);
                        //    i++;
                        //}

                        //if (entity.dictDWordValue[DWord] != value[0])
                        //{
                        //    entity.dictDWordValue[DWord] = value[0];

                        //    string loginfo = string.Format("{0}：thread ID:{1}, PLC:{2}, IP:{3}, DWord:{4} Value:{5}\r\n",
                        //                         DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        //                         Thread.CurrentThread.ManagedThreadId,
                        //                         entity.ConnectMachineID,
                        //                         IPAddress.Parse(((IPEndPoint)entity.TcpClient.Client.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)entity.TcpClient.Client.RemoteEndPoint).Port.ToString(),
                        //                         DWord,
                        //                         value[0]);

                        //    Console.WriteLine(loginfo);

                        //    tbxLog.InvokeIfRequired(() =>
                        //    {
                        //        tbxLog.AppendText(loginfo);

                        //    });
                        //}
                    }
                    else
                    {
                        threadObject.Abort();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    string issueMessage = ex.Message.ToString();
                    exceptionProcess();
                }
            }
        }

        //private string LogFile = Path.Combine(System.Environment.CurrentDirectory + "\\Log", @"AGVM -" + DateTime.Now.ToString("yyyyMMdd") + "_LOG.txt");

        //private void WriteLog(string LogString)
        //{
        //    File.AppendAllText(LogFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "：" + LogString + "\r\n\r\n");
        //}
        #endregion

        #region set AGV Move ID & From/To ST for Delta AS218TX series
        public virtual void WriteAGVMoveSetData(DeviceInfoModel _entity, AGVTaskModel _agvData, int _MoveIDAreaType)
        {
            entity = _entity;
            agvData = _agvData;
            MoveIDAreaType = _MoveIDAreaType;
            getMoveIDAreaDWord();
        }
        public virtual void executeAGVWriteMoveIDTask()
        {
            WriteAGVMoveID();
            WriteAGVFromST();
            WriteAGVToST();
            WriteMoveIDToAGVRequest(AGVNormalFeedback.ON);
        }

        private void WriteAGVMoveID()
        {
            //AGV MOVE ID 處理程序
            if (agvData != null && entity != null && entity.clientSocket.Connected)
            {
                List<MOVE_ID_DIGIT_HEX> liMoveID = convertAGVMoveID(agvData);
                agvData.liMoveID = liMoveID;

                int iDWordStart = getMoveAreaStartDWord(MoveIDAreaType, AGVMoveAreaDetail.MoveID);
                int iMoveIDArrayCount = 0;

                for (int i = iDWordStart; i < iDWordStart + (dictMoveIdDWord_1.Count - 2); i++)
                {
                    DWord = 0; //set default
                    DWordValue = 0;  //set default

                    DWord = Convert.ToUInt16(i);
                    DWordValue = Convert.ToUInt16(liMoveID[iMoveIDArrayCount + 1].MOVE_ID_HEX + liMoveID[iMoveIDArrayCount].MOVE_ID_HEX);

                    dictMoveIdDWord_1[i] = DWordValue;
                    WriteSingleRegister();
                    iMoveIDArrayCount = iMoveIDArrayCount + 2;
                }
            }
        }

        private static List<MOVE_ID_DIGIT_HEX> convertAGVMoveID(AGVTaskModel item)
        {
            List<MOVE_ID_DIGIT_HEX> liMoveID_separate = new List<MOVE_ID_DIGIT_HEX>();

            try
            {
                //每位數一個一個轉換HEX
                for (int i = 0; i < item.AGV_MOVE_ID.Length; i++)
                {
                    MOVE_ID_DIGIT_HEX MoveID = new MOVE_ID_DIGIT_HEX();
                    string MOVE_ID_substring = item.AGV_MOVE_ID.ToString().Substring(i, 1);
                    MoveID.MOVE_ID = MOVE_ID_substring;

                    //轉ASCII
                    byte[] SEQ_toASCII = ASCIIEncoding.Default.GetBytes(MOVE_ID_substring); //   AGVCmd_data.AGV_MOVE_ID 

                    //轉HEX > 轉binary, 依每1個的數字轉換
                    foreach (byte itemASCII in SEQ_toASCII)
                    {
                        string MoveID_toHEX = itemASCII.ToString("X");
                        MoveID.MOVE_ID_HEX = MoveID_toHEX;

                        #region 用不到, 留著當範本
                        //用不到, 留著當範本
                        //for (int i = 0; i < itemASCII.Length; i++)
                        //{
                        //    string To_ST_substring = strToST.Substring(iToST, 1);
                        //    //char轉字串>轉int>轉2進制>補到4碼, 不足前面補0
                        //    string To_ST_toBinary = Convert.ToString(int.Parse(To_ST_substring), 2).PadLeft(4, '0');

                        //    lsToST.Add(To_ST_toBinary);
                        //} 
                        #endregion
                    }

                    liMoveID_separate.Add(MoveID);
                }

            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();
                //MessageBox.Show(issueMessage);
            }

            return liMoveID_separate;
        }

        private void getMoveIDAreaDWord()
        {
            dictMoveIdDWord_1 = new Dictionary<int, ushort>();

            if (MoveIDAreaType == 1)
            {
                dictMoveIdDWord_1.Add(12, 0); //D12:搬送ID1, 1~2碼
                dictMoveIdDWord_1.Add(13, 0); //D13:搬送ID1, 3~4碼
                dictMoveIdDWord_1.Add(14, 0); //D14:搬送ID1, 5~6碼
                dictMoveIdDWord_1.Add(15, 0); //D15:搬送ID1, 7~8碼
                dictMoveIdDWord_1.Add(16, 0); //D16:搬送ID1, 9~10碼
                dictMoveIdDWord_1.Add(17, 0); //D17:搬送ID1, 11~12碼
                dictMoveIdDWord_1.Add(18, 0); //D18:搬送ID1, 13~14碼
                dictMoveIdDWord_1.Add(19, 0); //D19:搬送ID1, 15~16碼
                dictMoveIdDWord_1.Add(20, 0); //D20:搬送ID1, FROM ST
                dictMoveIdDWord_1.Add(21, 0); //D21:搬送ID1, TO ST
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

        private void WriteAGVFromST()
        {
            if (agvData != null && entity != null && entity.clientSocket.Connected)
            {
                int iDWordStart = getMoveAreaStartDWord(MoveIDAreaType, AGVMoveAreaDetail.FromtST);

                DWord = 0;  //set default
                DWordValue = 0;  //set default

                DWord = Convert.ToUInt16(iDWordStart);
                DWordValue = Convert.ToUInt16(agvData.AGV_FROM_ST);

                dictMoveIdDWord_1[iDWordStart] = DWordValue;
                WriteSingleRegister();
            }
        }

        private void WriteAGVToST()
        {
            if (agvData != null && entity != null && entity.clientSocket.Connected)
            {
                int iDWordStart = getMoveAreaStartDWord(MoveIDAreaType, AGVMoveAreaDetail.ToST);

                DWord = 0; //set default
                DWordValue = 0;  //set default

                DWord = Convert.ToUInt16(iDWordStart);
                DWordValue = Convert.ToUInt16(agvData.AGV_TO_ST);

                dictMoveIdDWord_1[iDWordStart] = DWordValue;
                WriteSingleRegister();
            }
        }

        private void WriteMoveIDToAGVRequest(AGVNormalFeedback _AGVFeedback)
        {
            if (agvData != null && entity != null && entity.clientSocket.Connected)
            {
                int iDWordStart = getMoveAreaStartDWord(MoveIDAreaType, AGVMoveAreaDetail.MoveIDWriteToAGVRequest);

                DWord = 0; //set default
                DWordValue = 0;  //set default

                DWord = Convert.ToUInt16(iDWordStart);
                DWordValue = Convert.ToUInt16(_AGVFeedback);

                dictMoveIdDWord_1[iDWordStart] = DWordValue;
                WriteSingleRegister();
            }
        }

        #endregion

        private void exceptionProcess()
        {
            entity.lblClientIP_Port.Text = "";
            entity.tbxConnIP.Enabled = true;
            entity.tbxConnPort.Enabled = true;
            entity.lblStatus.Text = "Off line";
            entity.btnConnService.Enabled = true;
            entity.btnDisconn.Enabled = false;
            entity.lblServerConnectLight.ForeColor = Color.Gray;
            entity.ConnStatus = "0";

            //entity.MBusM.Dispose();
            //entity.TcpClient.Client.Dispose();
            //entity.TcpClient.Client.Close();
            entity.clientSocket.Dispose();
            entity.clientSocket.Close();

            threadObject.Abort();
        }


    }
}
