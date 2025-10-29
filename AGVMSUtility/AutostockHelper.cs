using AGVMSModel;
using System.Collections.Generic;
using System.Text;

namespace AGVMSUtility
{
   public  class AutostockHelper
    {

        public static byte[] GetTransMsgByte(string msg, MessageTypeEnum _enumType)
        {
            byte[] byMsg = Encoding.UTF8.GetBytes(msg);
            List<byte> byMsgAndType = new List<byte>();
            byMsgAndType.Add((byte)_enumType);
            byMsgAndType.AddRange(byMsg);
            return byMsgAndType.ToArray();
        }
    }
}
