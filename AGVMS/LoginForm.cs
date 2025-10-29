using AGVMSDataAccess;
using AGVMSModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace AGVMS
{
    public partial class LoginForm : Form
    {
        private DaoLoginUser daoLogin_User = new DaoLoginUser();

        public static string AGVMShortName = ConfigurationManager.AppSettings["SystemShortName"];
        public static string AGVMVer = ConfigurationManager.AppSettings["SystemVersion"];

        public LoginForm()
        {
            InitializeComponent();
            this.Text = AGVMShortName + " " + AGVMVer;
            initial();
        }

        #region initial event
        private void initial()
        {
            DBconn.setDBconnection("");

            //test
            tbxLoginID.Text = "user1";
            tbxLoginPassword.Text = "user_123";
        }
        #endregion

        #region custom function event
        private void LoginCheck()
        {
            dsLOGIN_USER entity = new dsLOGIN_USER();
            entity.LOGIN_ID = tbxLoginID.Text.ToUpper().Trim();
            entity.LOGIN_PASSWORD = tbxLoginPassword.Text.Trim();

            if (entity != null && !string.IsNullOrWhiteSpace(entity.LOGIN_ID) && !string.IsNullOrWhiteSpace(entity.LOGIN_PASSWORD))
            {
                DataTable dtUser = new DataTable();
                dtUser = daoLogin_User.QueryUSER_DATA(entity);

                if (dtUser.Rows.Count > 0)
                {
                    this.Hide();
                    MainForm main = new MainForm();
                    main.Show();
                }
                else
                {
                    MessageBox.Show("Please fill correct Login ID or Password.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Please fill Login ID or Password.");
                return;
            }

        }
        #endregion

        #region tools controller event
        private void LoginForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void LoginForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(13))
            {
                e.Handled = true;
                if (this.ActiveControl != btnLoginConfirm)
                {
                    SendKeys.Send("{TAB}");
                    //this.SelectNextControl(this.ActiveControl, true, true, true, false); 
                }
            }
        }

        private void btnLoginConfirm_Click(object sender, EventArgs e)
        {
            LoginCheck();
        }

        private void tbxLoginID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                e.Handled = true;
                LoginCheck();
            }
        }
        #endregion




    }
}
