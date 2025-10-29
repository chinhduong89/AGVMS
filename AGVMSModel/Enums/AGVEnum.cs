
namespace AGVMSModel
{
    public class AGVEnum
    {
    }
    public enum AGVMoveAreaDetail
    {
        MoveID = 1,
        FromtST = 2,
        ToST = 3,
        MoveIDWriteToAGVRequest = 4,
        MoveIDWriteToAGVResponse = 5,
        MovingRequest = 6, //AGV監視盤發啟：配車、拾取完了、搬送完了, 異常：切回手動搬運並搬送完了; 搬送中止(AGV斷開); 搬送中止(指令清除);
        MovingResponse = 7, //上位PC回應：配車、拾取完了、搬送完了, 異常：切回手動搬運並搬送完了; 搬送中止(AGV斷開); 搬送中止(指令清除);
        MovingCurrentStatus = 8,
        MovingCurrentPlace = 9,
        MovingCurrentMoveID = 10,
    }

    public enum AGVNormalFeedback
    {
        OFF = 0, //D0, D72, D110
        ON = 1, //D0, D72, D110
        OK = 3, //D72
        NG = 5, //D72
    }
    public enum AGVExecutingStatusFlag
    {
        Line = 0,
        Start = 1,
        TransCmd = 2,
        WaitingAGVFeedback = 3,
        ReceiveAndFeedback = 4,
        MoveCmdTransOK = 5,
        WaitAGVMove = 6,
        AGVMovingFromST = 7,
        AGVMovingGetItem = 8,
        AGVMovingToST = 9,
        AGVOperationFinish = 10,
        AGVOperationAbnormal = 11,        
        WaitingAutostock = 99,     
    }

    public enum StatusFlag //執行序號的狀態
    {
        Line = 0,
        Executing = 1,
        Autostocking = 2,
        AGVing = 3,
        Finish = 4
    }

    public enum InOutStockFlag //人出庫標示符
    {
        In = 1, //in stock
        Out = 2, //ont stock 
        Dispatch = 3 //not in/out stock, maybe buffer to buffer
    }

    public enum AGVStation //D511：現在位置
    {
        ST1_Default = 1, //Default Point :ST1
        ST10_ChainOut = 10, //鏈條區, OUT :ST10
        ST11_ChainIn = 11, //鏈條區, IN :ST11
        ST20_Buffer1 = 20, //buffer 1   :ST20
        ST21_Buffer2 = 21, //buffer 2   :ST21
        ST22_Buffer3 = 22, //buffer 3   :ST22
        ST23_Buffer4 = 23, //buffer 4   :ST23
        ST24_Buffer5 = 24, //buffer 5   :ST24
        ST25_Buffer6 = 25, //buffer 6   :ST25
        ST26_Buffer7 = 26, //Buffer 7   :ST26
        ST40_Buffer8 = 40, //Buffer 8   :ST40
        ST41_Buffer9 = 41, //Buffer 9   :ST41
        ST42_Buffer10 = 42, //Buffer 10 :ST42
        ST43_Buffer11 = 43, //Buffer 11 :ST43
        ST44_Buffer12 = 44, //Buffer 12 :ST44
        ST45_Buffer13 = 45, //Buffer 13 :ST45
        ST46_Buffer14 = 46, //Buffer 14 :ST46
        ST47_Buffer15 = 47, //Buffer 15 :ST47
        ST48_Buffer16 = 48, //Buffer 16 :ST48

        ST30_Rotate1 = 30, //Rotate 1   :ST30
        ST31_Rotate2 = 31, //Rotate 2   :ST31
        ST32_Rotate3 = 32, //Rotate 3   :ST32
        ST33_Rotate4 = 33, //Rotate 4   :ST33
        ST34_Rotate5 = 34, //Rotate 5   :ST34
        ST35_Rotate6 = 35, //Rotate 6   :ST35
        ST36_Rotate7 = 36, //Rotate 7   :ST36
        ST37_Rotate8 = 37, //Rotate 8   :ST37
        ST50_Rotate9 = 50, //Rotate 9   :ST50
        ST51_Rotate10 = 51, //Rotate 10   :ST51
        ST52_Rotate11 = 52, //Rotate 11   :ST52
        ST53_Rotate12 = 53, //Rotate 12   :ST53
        ST54_Rotate13 = 54, //Rotate 13   :ST54
        ST55_Rotate14 = 55, //Rotate 14   :ST55
        ST56_Rotate15 = 56, //Rotate 15   :ST56
        ST57_Rotate16 = 57, //Rotate 16   :ST57
    }
}
