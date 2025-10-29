using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVMSModel
{
    public class AutostockEnum
    {
    }

    public enum AutostockNormalFeedback
    {
        OFF = 0,
        ON = 1,
        OK = 3,
        NG = 5,
    }

    public enum AutostockExecutingStatusFlag
    {
        Line = 0,
        Start = 1,
        TransCmd = 2,
        AutostockOperating = 3,
        AutostockKeepingOperating = 4,
        AutostockIssueKeepingOperating = 5,
        AutostockOperationFinish = 10,
        AutostockOperationAbnormal = 11,
        WaitingAGV = 99,
        
    }

}
