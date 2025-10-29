using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AGVMSUtility
{
    public class DataToolHelper
    {
        public static bool IsJsonFormat(string value)
        {

            if (string.IsNullOrWhiteSpace(value))
                return false;

            if ((value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]")))
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject(value);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
            }

            return false;

        }

        public static DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
              
        public static void SetComboboxByDictionary(ComboBox ctrl, Dictionary<string, string> dict, string strValue, string strDisplay, int index = 0)
        {
            ctrl.DataSource = null;
            ctrl.Items.Clear();

            ctrl.DisplayMember = strDisplay;
            ctrl.ValueMember = strValue;
            ctrl.DataSource = new BindingSource(dict, string.Empty);
            ctrl.SelectedIndex = index;
        }
    }
}
