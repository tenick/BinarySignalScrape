namespace BinarySignalScrape
{
    partial class MainForm
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
            this.start_btn = new System.Windows.Forms.Button();
            this.signals_data_tabCtrl = new System.Windows.Forms.TabControl();
            this.stop_btn = new System.Windows.Forms.Button();
            this.filePath_btn = new System.Windows.Forms.Button();
            this.filePath_txtBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // start_btn
            // 
            this.start_btn.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.start_btn.Location = new System.Drawing.Point(16, 10);
            this.start_btn.Name = "start_btn";
            this.start_btn.Size = new System.Drawing.Size(82, 28);
            this.start_btn.TabIndex = 7;
            this.start_btn.Text = "Start";
            this.start_btn.UseVisualStyleBackColor = true;
            this.start_btn.Click += new System.EventHandler(this.start_btn_Click);
            // 
            // signals_data_tabCtrl
            // 
            this.signals_data_tabCtrl.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.signals_data_tabCtrl.Location = new System.Drawing.Point(12, 85);
            this.signals_data_tabCtrl.Name = "signals_data_tabCtrl";
            this.signals_data_tabCtrl.SelectedIndex = 0;
            this.signals_data_tabCtrl.Size = new System.Drawing.Size(776, 371);
            this.signals_data_tabCtrl.TabIndex = 8;
            // 
            // stop_btn
            // 
            this.stop_btn.Enabled = false;
            this.stop_btn.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stop_btn.Location = new System.Drawing.Point(104, 10);
            this.stop_btn.Name = "stop_btn";
            this.stop_btn.Size = new System.Drawing.Size(82, 28);
            this.stop_btn.TabIndex = 9;
            this.stop_btn.Text = "Stop";
            this.stop_btn.UseVisualStyleBackColor = true;
            this.stop_btn.Click += new System.EventHandler(this.stop_btn_Click);
            // 
            // filePath_btn
            // 
            this.filePath_btn.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.filePath_btn.Location = new System.Drawing.Point(439, 12);
            this.filePath_btn.Name = "filePath_btn";
            this.filePath_btn.Size = new System.Drawing.Size(142, 28);
            this.filePath_btn.TabIndex = 10;
            this.filePath_btn.Text = "Select File Path";
            this.filePath_btn.UseVisualStyleBackColor = true;
            this.filePath_btn.Click += new System.EventHandler(this.filePath_btn_Click);
            // 
            // filePath_txtBox
            // 
            this.filePath_txtBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.filePath_txtBox.Location = new System.Drawing.Point(587, 12);
            this.filePath_txtBox.Name = "filePath_txtBox";
            this.filePath_txtBox.Size = new System.Drawing.Size(201, 26);
            this.filePath_txtBox.TabIndex = 13;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 468);
            this.Controls.Add(this.filePath_txtBox);
            this.Controls.Add(this.filePath_btn);
            this.Controls.Add(this.stop_btn);
            this.Controls.Add(this.signals_data_tabCtrl);
            this.Controls.Add(this.start_btn);
            this.Name = "MainForm";
            this.Text = "Binary Signal Scrape 1.36";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button start_btn;
        private System.Windows.Forms.TabControl signals_data_tabCtrl;
        private System.Windows.Forms.Button stop_btn;
        private System.Windows.Forms.Button filePath_btn;
        private System.Windows.Forms.TextBox filePath_txtBox;
    }
}

