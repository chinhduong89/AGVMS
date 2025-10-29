
namespace AGVMS
{
    partial class TaskAddForm
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
            this.cbxPriorityArea = new System.Windows.Forms.ComboBox();
            this.cbxToST = new System.Windows.Forms.ComboBox();
            this.cbxFromST = new System.Windows.Forms.ComboBox();
            this.tbxItemNo = new System.Windows.Forms.TextBox();
            this.cbxInOut = new System.Windows.Forms.ComboBox();
            this.lblPriorityArea = new System.Windows.Forms.Label();
            this.lblToST = new System.Windows.Forms.Label();
            this.lblFromST = new System.Windows.Forms.Label();
            this.lblItemNo = new System.Windows.Forms.Label();
            this.lblInOut = new System.Windows.Forms.Label();
            this.btnAddTaskConfirm = new System.Windows.Forms.Button();
            this.btnAddTaskCancel = new System.Windows.Forms.Button();
            this.tbxCutInLine = new System.Windows.Forms.TextBox();
            this.lblCutInLine = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbxPriorityArea
            // 
            this.cbxPriorityArea.FormattingEnabled = true;
            this.cbxPriorityArea.Location = new System.Drawing.Point(109, 207);
            this.cbxPriorityArea.Name = "cbxPriorityArea";
            this.cbxPriorityArea.Size = new System.Drawing.Size(150, 24);
            this.cbxPriorityArea.TabIndex = 35;
            // 
            // cbxToST
            // 
            this.cbxToST.FormattingEnabled = true;
            this.cbxToST.Location = new System.Drawing.Point(109, 158);
            this.cbxToST.Name = "cbxToST";
            this.cbxToST.Size = new System.Drawing.Size(241, 24);
            this.cbxToST.TabIndex = 34;
            // 
            // cbxFromST
            // 
            this.cbxFromST.FormattingEnabled = true;
            this.cbxFromST.Location = new System.Drawing.Point(109, 106);
            this.cbxFromST.Name = "cbxFromST";
            this.cbxFromST.Size = new System.Drawing.Size(241, 24);
            this.cbxFromST.TabIndex = 33;
            // 
            // tbxItemNo
            // 
            this.tbxItemNo.Location = new System.Drawing.Point(109, 60);
            this.tbxItemNo.MaxLength = 16;
            this.tbxItemNo.Name = "tbxItemNo";
            this.tbxItemNo.Size = new System.Drawing.Size(241, 22);
            this.tbxItemNo.TabIndex = 32;
            this.tbxItemNo.TextChanged += new System.EventHandler(this.tbxItemNo_TextChanged);
            // 
            // cbxInOut
            // 
            this.cbxInOut.FormattingEnabled = true;
            this.cbxInOut.Location = new System.Drawing.Point(109, 12);
            this.cbxInOut.Name = "cbxInOut";
            this.cbxInOut.Size = new System.Drawing.Size(150, 24);
            this.cbxInOut.TabIndex = 31;
            // 
            // lblPriorityArea
            // 
            this.lblPriorityArea.AutoSize = true;
            this.lblPriorityArea.Location = new System.Drawing.Point(8, 210);
            this.lblPriorityArea.Name = "lblPriorityArea";
            this.lblPriorityArea.Size = new System.Drawing.Size(100, 17);
            this.lblPriorityArea.TabIndex = 30;
            this.lblPriorityArea.Text = "Priority Area：";
            // 
            // lblToST
            // 
            this.lblToST.AutoSize = true;
            this.lblToST.Location = new System.Drawing.Point(8, 161);
            this.lblToST.Name = "lblToST";
            this.lblToST.Size = new System.Drawing.Size(61, 17);
            this.lblToST.TabIndex = 29;
            this.lblToST.Text = "To ST：";
            // 
            // lblFromST
            // 
            this.lblFromST.AutoSize = true;
            this.lblFromST.Location = new System.Drawing.Point(8, 109);
            this.lblFromST.Name = "lblFromST";
            this.lblFromST.Size = new System.Drawing.Size(76, 17);
            this.lblFromST.TabIndex = 28;
            this.lblFromST.Text = "From ST：";
            // 
            // lblItemNo
            // 
            this.lblItemNo.AutoSize = true;
            this.lblItemNo.Location = new System.Drawing.Point(8, 63);
            this.lblItemNo.Name = "lblItemNo";
            this.lblItemNo.Size = new System.Drawing.Size(70, 17);
            this.lblItemNo.TabIndex = 27;
            this.lblItemNo.Text = "Item No：";
            // 
            // lblInOut
            // 
            this.lblInOut.AutoSize = true;
            this.lblInOut.Location = new System.Drawing.Point(8, 19);
            this.lblInOut.Name = "lblInOut";
            this.lblInOut.Size = new System.Drawing.Size(60, 17);
            this.lblInOut.TabIndex = 26;
            this.lblInOut.Text = "In/Out：";
            // 
            // btnAddTaskConfirm
            // 
            this.btnAddTaskConfirm.Location = new System.Drawing.Point(416, 308);
            this.btnAddTaskConfirm.Name = "btnAddTaskConfirm";
            this.btnAddTaskConfirm.Size = new System.Drawing.Size(117, 42);
            this.btnAddTaskConfirm.TabIndex = 25;
            this.btnAddTaskConfirm.Text = "OK";
            this.btnAddTaskConfirm.UseVisualStyleBackColor = true;
            this.btnAddTaskConfirm.Click += new System.EventHandler(this.btnAddTaskConfirm_Click);
            // 
            // btnAddTaskCancel
            // 
            this.btnAddTaskCancel.Location = new System.Drawing.Point(158, 308);
            this.btnAddTaskCancel.Name = "btnAddTaskCancel";
            this.btnAddTaskCancel.Size = new System.Drawing.Size(117, 42);
            this.btnAddTaskCancel.TabIndex = 24;
            this.btnAddTaskCancel.Text = "Cancel";
            this.btnAddTaskCancel.UseVisualStyleBackColor = true;
            this.btnAddTaskCancel.Click += new System.EventHandler(this.btnAddTaskCancel_Click);
            // 
            // tbxCutInLine
            // 
            this.tbxCutInLine.Location = new System.Drawing.Point(109, 255);
            this.tbxCutInLine.MaxLength = 20;
            this.tbxCutInLine.Name = "tbxCutInLine";
            this.tbxCutInLine.Size = new System.Drawing.Size(87, 22);
            this.tbxCutInLine.TabIndex = 37;
            // 
            // lblCutInLine
            // 
            this.lblCutInLine.AutoSize = true;
            this.lblCutInLine.Location = new System.Drawing.Point(8, 258);
            this.lblCutInLine.Name = "lblCutInLine";
            this.lblCutInLine.Size = new System.Drawing.Size(89, 17);
            this.lblCutInLine.TabIndex = 36;
            this.lblCutInLine.Text = "Cut In Line：";
            // 
            // TaskAddForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(545, 366);
            this.Controls.Add(this.tbxCutInLine);
            this.Controls.Add(this.lblCutInLine);
            this.Controls.Add(this.cbxPriorityArea);
            this.Controls.Add(this.cbxToST);
            this.Controls.Add(this.cbxFromST);
            this.Controls.Add(this.tbxItemNo);
            this.Controls.Add(this.cbxInOut);
            this.Controls.Add(this.lblPriorityArea);
            this.Controls.Add(this.lblToST);
            this.Controls.Add(this.lblFromST);
            this.Controls.Add(this.lblItemNo);
            this.Controls.Add(this.lblInOut);
            this.Controls.Add(this.btnAddTaskConfirm);
            this.Controls.Add(this.btnAddTaskCancel);
            this.Name = "TaskAddForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Task Add Form";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TaskAddForm_FormClosed);
            this.Load += new System.EventHandler(this.TaskAddForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbxPriorityArea;
        private System.Windows.Forms.ComboBox cbxToST;
        private System.Windows.Forms.ComboBox cbxFromST;
        private System.Windows.Forms.TextBox tbxItemNo;
        private System.Windows.Forms.ComboBox cbxInOut;
        private System.Windows.Forms.Label lblPriorityArea;
        private System.Windows.Forms.Label lblToST;
        private System.Windows.Forms.Label lblFromST;
        private System.Windows.Forms.Label lblItemNo;
        private System.Windows.Forms.Label lblInOut;
        private System.Windows.Forms.Button btnAddTaskConfirm;
        private System.Windows.Forms.Button btnAddTaskCancel;
        private System.Windows.Forms.TextBox tbxCutInLine;
        private System.Windows.Forms.Label lblCutInLine;
    }
}