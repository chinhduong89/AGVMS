
namespace AGVMS
{
    partial class RFIDCheckForm
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
            this.tbxModifyItemNo = new System.Windows.Forms.TextBox();
            this.tbxModifyRFIDNo = new System.Windows.Forms.TextBox();
            this.lblModifyItemNo = new System.Windows.Forms.Label();
            this.lblModifyRFIDNo = new System.Windows.Forms.Label();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.lblOriginalRFIDNo = new System.Windows.Forms.Label();
            this.lblOringinalItemNo = new System.Windows.Forms.Label();
            this.tbxOringinalRFIDNo = new System.Windows.Forms.TextBox();
            this.tbxOringinalItemNo = new System.Windows.Forms.TextBox();
            this.btnCorrectIsItemNo = new System.Windows.Forms.Button();
            this.btnCorrectIsRFIDNo = new System.Windows.Forms.Button();
            this.btnModifyByAutostockServer = new System.Windows.Forms.Button();
            this.lblExecuteSeq = new System.Windows.Forms.Label();
            this.tbxExecuteSeq = new System.Windows.Forms.TextBox();
            this.lblInOutFlag = new System.Windows.Forms.Label();
            this.tbxInOutFlag = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tbxModifyItemNo
            // 
            this.tbxModifyItemNo.Location = new System.Drawing.Point(125, 236);
            this.tbxModifyItemNo.MaxLength = 16;
            this.tbxModifyItemNo.Name = "tbxModifyItemNo";
            this.tbxModifyItemNo.Size = new System.Drawing.Size(309, 22);
            this.tbxModifyItemNo.TabIndex = 0;
            this.tbxModifyItemNo.Visible = false;
            // 
            // tbxModifyRFIDNo
            // 
            this.tbxModifyRFIDNo.Location = new System.Drawing.Point(125, 198);
            this.tbxModifyRFIDNo.MaxLength = 16;
            this.tbxModifyRFIDNo.Name = "tbxModifyRFIDNo";
            this.tbxModifyRFIDNo.Size = new System.Drawing.Size(309, 22);
            this.tbxModifyRFIDNo.TabIndex = 1;
            // 
            // lblModifyItemNo
            // 
            this.lblModifyItemNo.AutoSize = true;
            this.lblModifyItemNo.Location = new System.Drawing.Point(13, 236);
            this.lblModifyItemNo.Name = "lblModifyItemNo";
            this.lblModifyItemNo.Size = new System.Drawing.Size(101, 17);
            this.lblModifyItemNo.TabIndex = 4;
            this.lblModifyItemNo.Text = "Modify Item No";
            this.lblModifyItemNo.Visible = false;
            // 
            // lblModifyRFIDNo
            // 
            this.lblModifyRFIDNo.AutoSize = true;
            this.lblModifyRFIDNo.Location = new System.Drawing.Point(13, 198);
            this.lblModifyRFIDNo.Name = "lblModifyRFIDNo";
            this.lblModifyRFIDNo.Size = new System.Drawing.Size(106, 17);
            this.lblModifyRFIDNo.TabIndex = 5;
            this.lblModifyRFIDNo.Text = "Modify RFID No";
            // 
            // btnConfirm
            // 
            this.btnConfirm.Location = new System.Drawing.Point(359, 295);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(75, 36);
            this.btnConfirm.TabIndex = 6;
            this.btnConfirm.Text = "OK";
            this.btnConfirm.UseVisualStyleBackColor = true;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // lblOriginalRFIDNo
            // 
            this.lblOriginalRFIDNo.AutoSize = true;
            this.lblOriginalRFIDNo.Location = new System.Drawing.Point(13, 128);
            this.lblOriginalRFIDNo.Name = "lblOriginalRFIDNo";
            this.lblOriginalRFIDNo.Size = new System.Drawing.Size(61, 17);
            this.lblOriginalRFIDNo.TabIndex = 10;
            this.lblOriginalRFIDNo.Text = "RFID No";
            // 
            // lblOringinalItemNo
            // 
            this.lblOringinalItemNo.AutoSize = true;
            this.lblOringinalItemNo.Location = new System.Drawing.Point(13, 89);
            this.lblOringinalItemNo.Name = "lblOringinalItemNo";
            this.lblOringinalItemNo.Size = new System.Drawing.Size(56, 17);
            this.lblOringinalItemNo.TabIndex = 9;
            this.lblOringinalItemNo.Text = "Item No";
            // 
            // tbxOringinalRFIDNo
            // 
            this.tbxOringinalRFIDNo.Location = new System.Drawing.Point(125, 125);
            this.tbxOringinalRFIDNo.MaxLength = 32;
            this.tbxOringinalRFIDNo.Name = "tbxOringinalRFIDNo";
            this.tbxOringinalRFIDNo.ReadOnly = true;
            this.tbxOringinalRFIDNo.Size = new System.Drawing.Size(309, 22);
            this.tbxOringinalRFIDNo.TabIndex = 8;
            // 
            // tbxOringinalItemNo
            // 
            this.tbxOringinalItemNo.Location = new System.Drawing.Point(125, 86);
            this.tbxOringinalItemNo.MaxLength = 32;
            this.tbxOringinalItemNo.Name = "tbxOringinalItemNo";
            this.tbxOringinalItemNo.ReadOnly = true;
            this.tbxOringinalItemNo.Size = new System.Drawing.Size(309, 22);
            this.tbxOringinalItemNo.TabIndex = 7;
            // 
            // btnCorrectIsItemNo
            // 
            this.btnCorrectIsItemNo.Location = new System.Drawing.Point(122, 169);
            this.btnCorrectIsItemNo.Name = "btnCorrectIsItemNo";
            this.btnCorrectIsItemNo.Size = new System.Drawing.Size(143, 23);
            this.btnCorrectIsItemNo.TabIndex = 11;
            this.btnCorrectIsItemNo.Text = "Correct is Item No";
            this.btnCorrectIsItemNo.UseVisualStyleBackColor = true;
            this.btnCorrectIsItemNo.Click += new System.EventHandler(this.btnCorrectIsItemNo_Click);
            // 
            // btnCorrectIsRFIDNo
            // 
            this.btnCorrectIsRFIDNo.Location = new System.Drawing.Point(285, 169);
            this.btnCorrectIsRFIDNo.Name = "btnCorrectIsRFIDNo";
            this.btnCorrectIsRFIDNo.Size = new System.Drawing.Size(143, 23);
            this.btnCorrectIsRFIDNo.TabIndex = 12;
            this.btnCorrectIsRFIDNo.Text = "Correct is RFID No";
            this.btnCorrectIsRFIDNo.UseVisualStyleBackColor = true;
            this.btnCorrectIsRFIDNo.Visible = false;
            this.btnCorrectIsRFIDNo.Click += new System.EventHandler(this.btnCorrectIsRFIDNo_Click);
            // 
            // btnModifyByAutostockServer
            // 
            this.btnModifyByAutostockServer.Location = new System.Drawing.Point(13, 277);
            this.btnModifyByAutostockServer.Name = "btnModifyByAutostockServer";
            this.btnModifyByAutostockServer.Size = new System.Drawing.Size(208, 54);
            this.btnModifyByAutostockServer.TabIndex = 13;
            this.btnModifyByAutostockServer.Text = "Cancel \r\nModify by Autostock System";
            this.btnModifyByAutostockServer.UseVisualStyleBackColor = true;
            this.btnModifyByAutostockServer.Click += new System.EventHandler(this.btnModifyByAutostockServer_Click);
            // 
            // lblExecuteSeq
            // 
            this.lblExecuteSeq.AutoSize = true;
            this.lblExecuteSeq.Location = new System.Drawing.Point(13, 22);
            this.lblExecuteSeq.Name = "lblExecuteSeq";
            this.lblExecuteSeq.Size = new System.Drawing.Size(91, 17);
            this.lblExecuteSeq.TabIndex = 14;
            this.lblExecuteSeq.Text = "Execute SEQ";
            // 
            // tbxExecuteSeq
            // 
            this.tbxExecuteSeq.Location = new System.Drawing.Point(125, 22);
            this.tbxExecuteSeq.MaxLength = 32;
            this.tbxExecuteSeq.Name = "tbxExecuteSeq";
            this.tbxExecuteSeq.ReadOnly = true;
            this.tbxExecuteSeq.Size = new System.Drawing.Size(309, 22);
            this.tbxExecuteSeq.TabIndex = 15;
            // 
            // lblInOutFlag
            // 
            this.lblInOutFlag.AutoSize = true;
            this.lblInOutFlag.Location = new System.Drawing.Point(13, 58);
            this.lblInOutFlag.Name = "lblInOutFlag";
            this.lblInOutFlag.Size = new System.Drawing.Size(77, 17);
            this.lblInOutFlag.TabIndex = 16;
            this.lblInOutFlag.Text = "In/Out Flag";
            // 
            // tbxInOutFlag
            // 
            this.tbxInOutFlag.Location = new System.Drawing.Point(125, 55);
            this.tbxInOutFlag.MaxLength = 32;
            this.tbxInOutFlag.Name = "tbxInOutFlag";
            this.tbxInOutFlag.ReadOnly = true;
            this.tbxInOutFlag.Size = new System.Drawing.Size(309, 22);
            this.tbxInOutFlag.TabIndex = 17;
            // 
            // RFIDCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 348);
            this.Controls.Add(this.tbxInOutFlag);
            this.Controls.Add(this.lblInOutFlag);
            this.Controls.Add(this.tbxExecuteSeq);
            this.Controls.Add(this.lblExecuteSeq);
            this.Controls.Add(this.btnModifyByAutostockServer);
            this.Controls.Add(this.btnCorrectIsRFIDNo);
            this.Controls.Add(this.btnCorrectIsItemNo);
            this.Controls.Add(this.lblOriginalRFIDNo);
            this.Controls.Add(this.lblOringinalItemNo);
            this.Controls.Add(this.tbxOringinalRFIDNo);
            this.Controls.Add(this.tbxOringinalItemNo);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.lblModifyRFIDNo);
            this.Controls.Add(this.lblModifyItemNo);
            this.Controls.Add(this.tbxModifyRFIDNo);
            this.Controls.Add(this.tbxModifyItemNo);
            this.Name = "RFIDCheckForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RFID Check Form";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RFIDCheckForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbxModifyItemNo;
        private System.Windows.Forms.TextBox tbxModifyRFIDNo;
        private System.Windows.Forms.Label lblModifyItemNo;
        private System.Windows.Forms.Label lblModifyRFIDNo;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Label lblOriginalRFIDNo;
        private System.Windows.Forms.Label lblOringinalItemNo;
        private System.Windows.Forms.TextBox tbxOringinalRFIDNo;
        private System.Windows.Forms.TextBox tbxOringinalItemNo;
        private System.Windows.Forms.Button btnCorrectIsItemNo;
        private System.Windows.Forms.Button btnCorrectIsRFIDNo;
        private System.Windows.Forms.Button btnModifyByAutostockServer;
        private System.Windows.Forms.Label lblExecuteSeq;
        private System.Windows.Forms.TextBox tbxExecuteSeq;
        private System.Windows.Forms.Label lblInOutFlag;
        private System.Windows.Forms.TextBox tbxInOutFlag;
    }
}