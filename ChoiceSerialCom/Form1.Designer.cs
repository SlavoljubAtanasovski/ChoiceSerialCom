namespace ChoiceSerialCom
{
    partial class Form1
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
            this.buttonSendMcuState = new System.Windows.Forms.Button();
            this.buttonSendMcuControl = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelMcuControlBytes = new System.Windows.Forms.Label();
            this.labelMcuStateBytes = new System.Windows.Forms.Label();
            this.labelError = new System.Windows.Forms.Label();
            this.labelButtonUp = new System.Windows.Forms.Label();
            this.labelButtonDown = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonSendMcuState
            // 
            this.buttonSendMcuState.Location = new System.Drawing.Point(356, 28);
            this.buttonSendMcuState.Name = "buttonSendMcuState";
            this.buttonSendMcuState.Size = new System.Drawing.Size(148, 23);
            this.buttonSendMcuState.TabIndex = 0;
            this.buttonSendMcuState.Text = "Send MCU State";
            this.buttonSendMcuState.UseVisualStyleBackColor = true;
            this.buttonSendMcuState.Click += new System.EventHandler(this.buttonSendMcuState_Click);
            // 
            // buttonSendMcuControl
            // 
            this.buttonSendMcuControl.Location = new System.Drawing.Point(15, 28);
            this.buttonSendMcuControl.Name = "buttonSendMcuControl";
            this.buttonSendMcuControl.Size = new System.Drawing.Size(157, 23);
            this.buttonSendMcuControl.TabIndex = 1;
            this.buttonSendMcuControl.Text = "Send MCU Control";
            this.buttonSendMcuControl.UseVisualStyleBackColor = true;
            this.buttonSendMcuControl.Click += new System.EventHandler(this.buttonSendMcuControl_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 83);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "MCU Control: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 121);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "MCU State: ";
            // 
            // labelMcuControlBytes
            // 
            this.labelMcuControlBytes.AutoSize = true;
            this.labelMcuControlBytes.Location = new System.Drawing.Point(91, 83);
            this.labelMcuControlBytes.Name = "labelMcuControlBytes";
            this.labelMcuControlBytes.Size = new System.Drawing.Size(94, 13);
            this.labelMcuControlBytes.TabIndex = 4;
            this.labelMcuControlBytes.Text = "00 00 00 00 00 00";
            // 
            // labelMcuStateBytes
            // 
            this.labelMcuStateBytes.AutoSize = true;
            this.labelMcuStateBytes.Location = new System.Drawing.Point(91, 121);
            this.labelMcuStateBytes.Name = "labelMcuStateBytes";
            this.labelMcuStateBytes.Size = new System.Drawing.Size(304, 13);
            this.labelMcuStateBytes.TabIndex = 5;
            this.labelMcuStateBytes.Text = "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00";
            // 
            // labelError
            // 
            this.labelError.AutoSize = true;
            this.labelError.ForeColor = System.Drawing.Color.Red;
            this.labelError.Location = new System.Drawing.Point(15, 158);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(73, 13);
            this.labelError.TabIndex = 6;
            this.labelError.Text = "error message";
            // 
            // labelButtonUp
            // 
            this.labelButtonUp.AutoSize = true;
            this.labelButtonUp.Location = new System.Drawing.Point(356, 82);
            this.labelButtonUp.Name = "labelButtonUp";
            this.labelButtonUp.Size = new System.Drawing.Size(56, 13);
            this.labelButtonUp.TabIndex = 7;
            this.labelButtonUp.Text = "Button UP";
            // 
            // labelButtonDown
            // 
            this.labelButtonDown.AutoSize = true;
            this.labelButtonDown.Location = new System.Drawing.Point(428, 82);
            this.labelButtonDown.Name = "labelButtonDown";
            this.labelButtonDown.Size = new System.Drawing.Size(76, 13);
            this.labelButtonDown.TabIndex = 8;
            this.labelButtonDown.Text = "Button DOWN";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(516, 362);
            this.Controls.Add(this.labelButtonDown);
            this.Controls.Add(this.labelButtonUp);
            this.Controls.Add(this.labelError);
            this.Controls.Add(this.labelMcuStateBytes);
            this.Controls.Add(this.labelMcuControlBytes);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonSendMcuControl);
            this.Controls.Add(this.buttonSendMcuState);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSendMcuState;
        private System.Windows.Forms.Button buttonSendMcuControl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelMcuControlBytes;
        private System.Windows.Forms.Label labelMcuStateBytes;
        private System.Windows.Forms.Label labelError;
        private System.Windows.Forms.Label labelButtonUp;
        private System.Windows.Forms.Label labelButtonDown;
    }
}

