using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AGVMSModel;

namespace AGVMS
{
    public partial class RFIDCheckForm : Form
    {
        //private string EXECUTE_SEQ = string.Empty;
        private DeviceInfoModel autostockEntity;

        public RFIDCheckForm()
        {
            InitializeComponent();
        }

        public void setData(DeviceInfoModel _autostockEntity) //, string _EXECUTE_SEQ, string _OringinalItemNo, string _OringinalRFIDNo
        {
            autostockEntity = _autostockEntity;
            tbxExecuteSeq.Text = autostockEntity.dsAutostockServerKeepData.EXECUTE_SEQ;
            tbxInOutFlag.Text = autostockEntity.dsAutostockServerKeepData.INOUT_FLAG.ToString().Equals("1") ? "In" : "Out";
            tbxOringinalItemNo.Text = autostockEntity.dsAutostockServerKeepData.ITEM_NO; //_OringinalItemNo.Trim(); //上位PC的ITEM NO
            tbxOringinalRFIDNo.Text = autostockEntity.dsAutostockServerKeepData.RFID_NO; // _OringinalRFIDNo.Trim(); //RFID的ITEM NO
        }

        private void btnCorrectIsItemNo_Click(object sender, EventArgs e)
        {
            tbxModifyItemNo.Text = "";
            tbxModifyRFIDNo.Text = tbxOringinalItemNo.Text.Trim();
        }

        private void btnCorrectIsRFIDNo_Click(object sender, EventArgs e)
        {
            tbxModifyItemNo.Text = tbxOringinalRFIDNo.Text.Trim();
            tbxModifyRFIDNo.Text = "";
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            //bool blModifyItemNo = false;
            bool blModifyRFIDNo = false;

            //ITEM NO有修改
            //上位PC：錯誤, RFID：正確, 修改上位PC的ITEM NO, 
            //if (!string.IsNullOrWhiteSpace(tbxModifyItemNo.Text.Trim()))
            //{
            //    //檢查比對是否為與RFID NO相同
            //    if (!tbxModifyItemNo.Text.Trim().Equals(tbxOringinalRFIDNo.Text.Trim()))
            //    {
            //        MessageBox.Show("與RFID不符合, The RFID Number not equal modify Item number");
            //        return;
            //    }
            //    else
            //    {
            //        blModifyItemNo = true;
            //    }
            //}
            //上位PC：正確, RFID：錯誤, 修改RFID的ITEM NO, 
            //else 
            if (!string.IsNullOrWhiteSpace(tbxModifyRFIDNo.Text.Trim()))
            {
                //檢查比對是否為與RFID NO相同
                if (!tbxModifyRFIDNo.Text.Trim().Equals(tbxOringinalItemNo.Text.Trim()))
                {
                    MessageBox.Show("與Item No不符合, The Item number not equal modify RFID number");
                    return;
                }
                else
                {
                    blModifyRFIDNo = true;
                }
            }
            else
            {
                MessageBox.Show("與Item No不符合, The Item number not equal modify RFID number");
                return;
            }

            //if (blModifyItemNo && blModifyRFIDNo)
            //{
            //    MessageBox.Show("無法將Item No和RFID No一起調整, The Item number can not modify with RFID number, please check data");
            //    return;
            //}
            //else 
            if (blModifyRFIDNo) //blModifyItemNo || 
            {
                dsAutoStockTransferJsonModel modifyItem = new dsAutoStockTransferJsonModel();
                modifyItem.EXECUTE_SEQ = autostockEntity.dsAutostockServerKeepData.EXECUTE_SEQ;

                ////變更ITEM NO
                //if (blModifyItemNo)
                //{
                //    modifyItem.ITEM_NO = tbxModifyItemNo.Text.Trim();
                //    modifyItem.RFID_NO = tbxOringinalRFIDNo.Text.Trim();
                //    modifyItem.RFID_REWRITE_RESPONSE_STATUS = "1";
                //}
                ////變更RFID NO
                //else 
                //if (blModifyRFIDNo)
                //{
                modifyItem.ITEM_NO = tbxOringinalItemNo.Text.Trim();
                modifyItem.RFID_NO = tbxModifyRFIDNo.Text.Trim();
                modifyItem.RFID_REWRITE_RESPONSE_STATUS = "2";
                //}

                autostockEntity.dsModify = modifyItem;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void btnModifyByAutostockServer_Click(object sender, EventArgs e)
        {
            dsAutoStockTransferJsonModel modifyItem = new dsAutoStockTransferJsonModel();
            modifyItem.EXECUTE_SEQ = autostockEntity.dsAutostockServerKeepData.EXECUTE_SEQ;

            modifyItem.RFID_REWRITE_RESPONSE_STATUS = "3";

            autostockEntity.dsModify = modifyItem;

            this.DialogResult = DialogResult.No;
            this.Close();
        }

        private void RFIDCheckForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void RFIDCheckForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //cannot close
            if (this.DialogResult == DialogResult.OK || this.DialogResult == DialogResult.No)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

    }
}
