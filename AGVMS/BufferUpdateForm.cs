using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.Generic;
using AGVMSModel;
using AGVMSDataAccess;
using AGVMSUtility;
using System.Linq;
using System;

namespace AGVMS
{
    public partial class BufferUpdateForm : Form
    {
        private DaoCode daoCode = new DaoCode();
        private BindingList<BufferStatus> listBufferStatus;
        private List<dsCODE_DETAIL> lsInOut = new List<dsCODE_DETAIL>();
        private List<dsCODE_DETAIL> lsPriorityArea = new List<dsCODE_DETAIL>();

        public delegate void TbxLogMessageEventHandler(dsLOG_MESSAGE entity);
        public TbxLogMessageEventHandler tbxLogAddMessageTrigger;

        public delegate void lblBufferSetItemNoEventHandler(BufferStatus transData);
        public lblBufferSetItemNoEventHandler lblBufferItemNoTrigger;

        public BufferUpdateForm()
        {
            InitializeComponent();
            initial();
        }

        public void setData(BindingList<BufferStatus> _listBufferStatus)
        {
            listBufferStatus = _listBufferStatus;

            initialData();
        }

        #region initial event

        private void initial()
        {
            lsInOut = daoCode.getInOutToList();
            lsPriorityArea = daoCode.getPriorityAreaToList();

            if (lsInOut != null)
            {
                DataToolHelper.SetComboboxByDictionary(cbxBuffer1InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer2InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer3InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer4InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer5InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer6InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer7InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer8InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer9InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer10InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer11InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer12InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer13InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer14InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer15InOut, lsInOut.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
            }

            if (lsPriorityArea != null)
            {
                DataToolHelper.SetComboboxByDictionary(cbxBuffer1PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer2PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer3PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer4PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer5PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer6PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer7PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer8PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer9PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer10PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer11PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer12PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer13PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer14PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
                DataToolHelper.SetComboboxByDictionary(cbxBuffer15PriorityArea, lsPriorityArea.ToDictionary(k => k.PARA_1, v => v.PARA_1 + ":" + v.PARA_NAME), "Key", "Value");
            }
        }

        private void initialData()
        {
            for (int i = 0; i < listBufferStatus.Count; i++)
            {
                BufferStatus BItem = new BufferStatus();
                BItem = listBufferStatus[i];

                if ((int)AGVStation.ST20_Buffer1 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer1ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer1ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer1InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer1InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer1PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer1PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST21_Buffer2 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer2ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer2ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer2InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer2InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer2PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer2PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST22_Buffer3 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer3ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer3ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer3InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer3InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer3PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer3PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST23_Buffer4 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer4ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer4ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer4InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer4InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer4PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer4PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST24_Buffer5 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer5ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer5ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer5InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer5InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer5PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer5PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST25_Buffer6 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer6ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer6ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer6InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer6InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer6PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer6PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST26_Buffer7 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer7ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer7ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer7InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer7InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer7PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer7PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST40_Buffer8 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer8ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer8ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer8InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer8InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer8PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer8PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST41_Buffer9 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer9ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer9ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer9InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer9InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer9PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer9PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST42_Buffer10 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer10ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer10ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer10InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer10InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer10PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer10PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST43_Buffer11 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer11ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer11ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer11InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer11InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer11PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer11PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST44_Buffer12 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer12ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer12ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer12InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer12InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer12PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer12PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST45_Buffer13 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer13ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer13ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer13InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer13InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer13PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer13PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST46_Buffer14 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer14ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer14ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer14InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer14InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer14PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer14PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST47_Buffer15 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer15ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer15ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer15InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer15InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer15PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer15PriorityArea.SelectedIndex = -1;
                    }
                }
                else if ((int)AGVStation.ST48_Buffer16 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(BItem.ItemNo))
                    {
                        tbxBuffer16ItemNo.Text = BItem.ItemNo;
                    }
                    else
                    {
                        tbxBuffer16ItemNo.Text = "";
                    }
                    //in/out flag
                    if (!string.IsNullOrWhiteSpace(BItem.InOutFlag))
                    {
                        cbxBuffer16InOut.SelectedValue = BItem.InOutFlag;
                    }
                    else
                    {
                        cbxBuffer16InOut.SelectedIndex = -1;
                    }
                    //PriorityArea
                    if (!string.IsNullOrWhiteSpace(BItem.PriorityArea))
                    {
                        cbxBuffer16PriorityArea.SelectedValue = BItem.PriorityArea;
                    }
                    else
                    {
                        cbxBuffer16PriorityArea.SelectedIndex = -1;
                    }
                }
            }
        }

        #endregion

        #region custom function event

        private void updateBufferData()
        {
            for (int i = 0; i < listBufferStatus.Count; i++)
            {
                BufferStatus BItem = new BufferStatus();
                BItem = listBufferStatus[i];

                if ((int)AGVStation.ST20_Buffer1 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer1ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer1ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer1InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer1InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer1PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer1PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST21_Buffer2 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer2ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer2ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer2InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer2InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer2PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer2PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST22_Buffer3 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer3ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer3ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer3InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer3InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer3PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer3PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST23_Buffer4 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer4ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer4ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer4InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer4InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer4PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer4PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST24_Buffer5 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer5ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer5ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer5InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer5InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer5PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer5PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST25_Buffer6 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer6ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer6ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer6InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer6InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer6PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer6PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST26_Buffer7 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer7ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer7ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer7InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer7InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer7PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer7PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST40_Buffer8 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer8ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer8ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer8InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer8InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer8PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer8PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST41_Buffer9 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer9ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer9ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer9InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer9InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer9PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer9PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST42_Buffer10 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer10ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer10ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer10InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer10InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer10PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer10PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST43_Buffer11 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer11ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer11ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer11InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer11InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer11PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer11PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST44_Buffer12 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer12ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer12ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer12InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer12InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer12PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer12PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST45_Buffer13 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer13ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer13ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer13InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer13InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer13PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer13PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST46_Buffer14 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer14ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer14ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer14InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer14InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer14PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer14PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST47_Buffer15 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer15ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer15ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer15InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer15InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer15PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer15PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
                else if ((int)AGVStation.ST48_Buffer16 == int.Parse(BItem.StationNo))
                {
                    //item no
                    if (!string.IsNullOrWhiteSpace(tbxBuffer16ItemNo.Text.Trim()))
                    {
                        BItem.ItemNo = tbxBuffer16ItemNo.Text.Trim();
                    }
                    else
                    {
                        BItem.ItemNo = "";
                    }
                    //in/out flag
                    if (cbxBuffer16InOut.SelectedIndex != -1)
                    {
                        BItem.InOutFlag = cbxBuffer16InOut.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.InOutFlag = "";
                    }
                    //PriorityArea
                    if (cbxBuffer16PriorityArea.SelectedIndex != -1)
                    {
                        BItem.PriorityArea = cbxBuffer16PriorityArea.SelectedValue.ToString();
                    }
                    else
                    {
                        BItem.PriorityArea = "";
                    }
                }
            }
        }

        private void updateMainFormUIObject()
        {
            for (int i = 0; i < listBufferStatus.Count; i++)
            {
                BufferStatus BItem = new BufferStatus();
                BItem = listBufferStatus[i];

                lblBufferItemNoTrigger(BItem);
            }
        }

        private void textboxKeyPressToUpper(KeyPressEventArgs e)
        {
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
        }
        #endregion

        #region tools controller event
        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            //DialogResult dlResult = MessageBox.Show("Are you sure you want to close this Form? Warning: Modified data will not be saved!", "Cancel Modify", MessageBoxButtons.YesNo);

            ////close
            //if (dlResult == DialogResult.Yes)
            //{
            //    e.Cancel = false;
            //this.Close();
            //}
            ////cannot close
            //else
            //{
            //    e.Cancel = true;
            // return;
            //}

            this.DialogResult = DialogResult.No;
            this.Close();
        }

        private void btnRestoreData_Click(object sender, System.EventArgs e)
        {
            DialogResult dlResult = MessageBox.Show("Are you sure you want to restore the data? Warning: Modified data will not be saved!", "Restore Data", MessageBoxButtons.YesNo);

            //restore
            if (dlResult == DialogResult.Yes)
            {
                initialData();
            }
            //return 
            else
            {
                return;
            }
        }

        private void btnConfirm_Click(object sender, System.EventArgs e)
        {

            //Item no - buffer 1
            if (!string.IsNullOrWhiteSpace(tbxBuffer1ItemNo.Text))
            {

            }


            //DialogResult dlResult = MessageBox.Show("Are you sure you want to save the data?", "Modify Data", MessageBoxButtons.YesNo);

            ////restore
            //if (dlResult == DialogResult.Yes)
            //{
            updateBufferData();
            updateMainFormUIObject();
            this.DialogResult = DialogResult.OK;
            this.Close();
            //}
            ////return 
            //else
            //{
            //    return;
            //}
        }

        private void BufferUpdateForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            //close
            if (this.DialogResult == DialogResult.OK || this.DialogResult == DialogResult.No)
            {
                e.Cancel = false;
            }
            //cannot close
            else
            {
                e.Cancel = true;
            }
        }

        private void tbxBuffer1ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer2ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer3ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer4ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer5ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer6ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer7ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }
        #endregion

        private void tbxBuffer8ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer9ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer10ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer11ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer12ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer13ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer14ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }

        private void tbxBuffer15ItemNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            textboxKeyPressToUpper(e);
        }
    }
}
