using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AGVMSUtility
{
    public class UtilityHelper
    {

        public static void checkPathExist(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);

            }
        }

    }
}
