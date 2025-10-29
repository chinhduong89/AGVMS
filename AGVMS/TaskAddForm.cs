using AGVMSDataAccess;
using AGVMSModel;
using AGVMSUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AGVMS
{
    public partial class TaskAddForm : Form
    {
        private DaoCode daoCode = new DaoCode();
        private List<dsCODE_DETAIL> lsAGVStation = new List<dsCODE_DETAIL>();
        private List<dsCODE_DETAIL> lsInOut = new List<dsCODE_DETAIL>();
        private List<dsCODE_DETAIL> lsPriorityArea = new List<dsCODE_DETAIL>();

        public delegate void DgvItemDataSourceEventHandler(AGVTaskModel addData, decimal CutInLine);
        public DgvItemDataSourceEventHandler dgvItemDataSourceTrigger;

        public delegate void TbxLogMessageEventHandler(dsLOG_MESSAGE entity);
        public TbxLogMessageEventHandler tbxLogAddMessageTrigger;

        private DaoSP daoSP = new DaoSP();

        public TaskAddForm()
        {
            InitializeComponent();
        }

        private void TaskAddForm_Load(object sender, EventArgs e)
        {
            initial();
        }

        private void TaskAddFormLoadsetDataToForm(dsTaskAddTrans entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.ITEM_NO))
                tbxItemNo.Text = entity.ITEM_NO;

            if (!string.IsNullOrWhiteSpace(entity.FROM_ST))
                cbxFromST.SelectedValue = entity.FROM_ST;

            if (!string.IsNullOrWhiteSpace(entity.TO_ST))
                cbxToST.SelectedValue = entity.TO_ST;

            if (!string.IsNullOrWhiteSpace(entity.INOUT_FLAG))
                cbxInOut.SelectedValue = entity.INOUT_FLAG;

            if (!string.IsNullOrWhiteSpace(entity.PRIORITY_AREA))
            {
                cbxPriorityArea.SelectedValue = entity.PRIORITY_AREA;
            }
        }

        #region initial event
        private void initial()
        {
            lsInOut = daoCode.getInOutToList();
            lsAGVStation = daoCode.getAGVStationToList();
            lsPriorityArea = daoCode.getPriorityAreaToList();

            if (lsInOut != null)
            {
                DataToolHelper.SetComboboxByDictionary(cbxInOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
            }

            if (lsAGVStation != null)
            {
                DataToolHelper.SetComboboxByDictionary(cbxFromST, lsAGVStation.ToDictionary(k => k.PARA_1, v => v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxToST, lsAGVStation.ToDictionary(k => k.PARA_1, v => v.PARA_NAME), "Key", "Value", 1);
            }

            if (lsPriorityArea != null)
            {
                DataToolHelper.SetComboboxByDictionary(cbxPriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
            }
        }

        #endregion

        #region custom function event

        private void addExecuteQueue()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tbxCutInLine.Text.Trim()) && !Regex.IsMatch(tbxCutInLine.Text.Trim(), "^(\\-/\\+)?\\d+(\\.\\d+)?$"))
                {
                    MessageBox.Show("cut in line incorrect value");
                    return;
                }

                AGVTaskModel AGVCmd_data = new AGVTaskModel();
                DateTime dateNow = DateTime.Now;

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

                decimal CutInLine = 0;

                //AGV MOVE ID 處理程序
                if (dtAGV_Sequence.Rows.Count > 0)
                {
                    //AGV 執行序號, 共10碼
                    string strMoveID_yymmdd = dtAGV_Sequence.Rows[0]["SEQUENCE_CODE"].ToString().Substring(2, 6); //取yyMMdd
                    string strMoveID_seq = dtAGV_Sequence.Rows[0]["SEQUENCE_NO"].ToString().PadLeft(4, '0'); //流水號4碼, 不足前面補0

                    AGVCmd_data.EXECUTE_SEQ = strMoveID_yymmdd + strMoveID_seq;  //上位電腦執行序號, 共10碼
                    AGVCmd_data.AGV_MOVE_ID = AGVCmd_data.EXECUTE_SEQ; //MOVE ID總流水號16碼, 不足後面補0
                    AGVCmd_data.ITEM_NO = tbxItemNo.Text.Trim();
                    //From ST
                    AGVCmd_data.AGV_FROM_ST = int.Parse(cbxFromST.SelectedValue.ToString()).ToString();

                    //To ST
                    AGVCmd_data.AGV_TO_ST = int.Parse(cbxToST.SelectedValue.ToString()).ToString();

                    if (cbxInOut.SelectedValue.ToString().Equals("1"))
                    {
                        AGVCmd_data.INOUT_FLAG = InOutStockFlag.In.ToString();
                    }
                    else if (cbxInOut.SelectedValue.ToString().Equals("2"))
                    {
                        AGVCmd_data.INOUT_FLAG = InOutStockFlag.Out.ToString();
                    }
                    else if (cbxInOut.SelectedValue.ToString().Equals("3"))
                    {
                        AGVCmd_data.INOUT_FLAG = InOutStockFlag.Dispatch.ToString();
                    }

                    AGVCmd_data.PRIORITY_AREA = cbxPriorityArea.SelectedValue.ToString();

                    AGVCmd_data.EXECUTE_STATUS = StatusFlag.Line.ToString();

                    AGVCmd_data.CMD_TRANS_TO_AGV_FLAG = AGVNormalFeedback.OFF.ToString();
                    AGVCmd_data.CMD_AGV_EXECUTING_STATUS = AGVExecutingStatusFlag.Line.ToString();

                    if (!string.IsNullOrWhiteSpace(tbxCutInLine.Text.Trim()))
                    {
                        CutInLine = Convert.ToDecimal(tbxCutInLine.Text.Trim());
                    }

                    dgvItemDataSourceTrigger(AGVCmd_data, CutInLine); //mainform委派1 
                }
            }
            catch (Exception ex)
            {
                string issueMessage = ex.Message.ToString();

                DateTime dateTimeLog = DateTime.Now;
                dsLOG_MESSAGE entityMsg = new dsLOG_MESSAGE();
                string msg = string.Empty;

                msg = string.Format("{0}：{1} \r\n", dateTimeLog.ToString("yyyy-MM-dd HH:mm:ss"), issueMessage);

                entityMsg.CREATE_DATETIME = dateTimeLog;
                entityMsg.LOG_MESSAGE = msg;
                Console.WriteLine(msg);
                tbxLogAddMessageTrigger(entityMsg);
                //dgvItemRefresh();

                throw ex;
            }
        }

        #endregion

        #region tools controller event

        private void TaskAddForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.Close();
        }
        private void btnAddTaskCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddTaskConfirm_Click(object sender, EventArgs e)
        {
            //check
            if (cbxFromST.SelectedValue == cbxToST.SelectedValue)
            {
                MessageBox.Show("出發站點不能與目的站點相同！, From ST can not be the same as To ST");
                return;
            }

            if (string.IsNullOrWhiteSpace(tbxItemNo.Text.Trim()))
            {
                MessageBox.Show("請輸入治具編號！");
                return;
            }

            if (cbxFromST.SelectedValue.ToString() == "0011")
            {
                MessageBox.Show("出發站點錯誤, [" + cbxFromST.Text.ToString() + "]站點無法取得治具！ From ST wrong, [" + cbxFromST.Text.ToString() +"] can not pick the Jig");
                return;
            }

            if (cbxToST.SelectedValue.ToString() == "0010")
            {
                MessageBox.Show("目的地站點錯誤, [" + cbxToST.Text.ToString() + "]站點無法送入治具！ To ST wrong, [" + cbxToST.Text.ToString() + "] can not send the Jig");
                return;
            }

            if (cbxInOut.SelectedValue.ToString() == "1" && cbxFromST.SelectedValue.ToString() == "0001")
            {
                MessageBox.Show("入庫出發站點錯誤, [" + cbxFromST.Text.ToString() + "]！ In Stock From ST wrong");
                return;
            }

            if (cbxInOut.SelectedValue.ToString() == "2" && cbxToST.SelectedValue.ToString() == "0001")
            {
                MessageBox.Show("出庫目的地站點錯誤, [" + cbxToST.Text.ToString() + "]！ Out Stock To ST wrong");
                return;
            }


            addExecuteQueue();
        }

        private void tbxItemNo_TextChanged(object sender, EventArgs e)
        {
            this.tbxItemNo.Text = this.tbxItemNo.Text.ToUpper();
            this.tbxItemNo.SelectionStart = this.tbxItemNo.Text.Length;
        }

        #endregion


    }
}
