
namespace AGVMS
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnLoginConfirm = new System.Windows.Forms.Button();
            this.tbxLoginPassword = new System.Windows.Forms.TextBox();
            this.tbxLoginID = new System.Windows.Forms.TextBox();
            this.lblLoginPassword = new System.Windows.Forms.Label();
            this.lblLoginID = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnLoginConfirm
            // 
            this.btnLoginConfirm.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnLoginConfirm.Location = new System.Drawing.Point(323, 52);
            this.btnLoginConfirm.Name = "btnLoginConfirm";
            this.btnLoginConfirm.Size = new System.Drawing.Size(89, 33);
            this.btnLoginConfirm.TabIndex = 9;
            this.btnLoginConfirm.Text = "Login";
            this.btnLoginConfirm.UseVisualStyleBackColor = true;
            this.btnLoginConfirm.Click += new System.EventHandler(this.btnLoginConfirm_Click);
            // 
            // tbxLoginPassword
            // 
            this.tbxLoginPassword.Location = new System.Drawing.Point(149, 95);
            this.tbxLoginPassword.Name = "tbxLoginPassword";
            this.tbxLoginPassword.Size = new System.Drawing.Size(156, 22);
            this.tbxLoginPassword.TabIndex = 8;
            this.tbxLoginPassword.UseSystemPasswordChar = true;
            // 
            // tbxLoginID
            // 
            this.tbxLoginID.Location = new System.Drawing.Point(149, 57);
            this.tbxLoginID.Name = "tbxLoginID";
            this.tbxLoginID.Size = new System.Drawing.Size(156, 22);
            this.tbxLoginID.TabIndex = 7;
            this.tbxLoginID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbxLoginID_KeyDown);
            // 
            // lblLoginPassword
            // 
            this.lblLoginPassword.AutoSize = true;
            this.lblLoginPassword.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblLoginPassword.Location = new System.Drawing.Point(57, 95);
            this.lblLoginPassword.Name = "lblLoginPassword";
            this.lblLoginPassword.Size = new System.Drawing.Size(73, 17);
            this.lblLoginPassword.TabIndex = 6;
            this.lblLoginPassword.Text = "Password:";
            // 
            // lblLoginID
            // 
            this.lblLoginID.AutoSize = true;
            this.lblLoginID.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblLoginID.Location = new System.Drawing.Point(57, 60);
            this.lblLoginID.Name = "lblLoginID";
            this.lblLoginID.Size = new System.Drawing.Size(64, 17);
            this.lblLoginID.TabIndex = 5;
            this.lblLoginID.Text = "Login ID:";
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 180);
            this.Controls.Add(this.btnLoginConfirm);
            this.Controls.Add(this.tbxLoginPassword);
            this.Controls.Add(this.tbxLoginID);
            this.Controls.Add(this.lblLoginPassword);
            this.Controls.Add(this.lblLoginID);
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login Form";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.LoginForm_FormClosed);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LoginForm_KeyPress);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoginConfirm;
        private System.Windows.Forms.TextBox tbxLoginPassword;
        private System.Windows.Forms.TextBox tbxLoginID;
        private System.Windows.Forms.Label lblLoginPassword;
        private System.Windows.Forms.Label lblLoginID;
    }
}