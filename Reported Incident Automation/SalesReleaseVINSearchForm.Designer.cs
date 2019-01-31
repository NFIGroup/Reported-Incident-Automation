namespace Reported_Incident_Automation
{
    partial class SalesReleaseVINSearchForm
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
            this.SR_Cmbbx = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Vin_txtbx = new System.Windows.Forms.TextBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Seacrh_Btn = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SelectAll_chkbx = new System.Windows.Forms.CheckBox();
            this.ClearAll_Chkbx = new System.Windows.Forms.CheckBox();
            this.Bulid_btn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.CustomerFleetNo_txtbx = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SR_Cmbbx
            // 
            this.SR_Cmbbx.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SR_Cmbbx.FormattingEnabled = true;
            this.SR_Cmbbx.Location = new System.Drawing.Point(67, 22);
            this.SR_Cmbbx.Name = "SR_Cmbbx";
            this.SR_Cmbbx.Size = new System.Drawing.Size(93, 21);
            this.SR_Cmbbx.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Select SR";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(179, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(25, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "VIN";
            // 
            // Vin_txtbx
            // 
            this.Vin_txtbx.Location = new System.Drawing.Point(210, 23);
            this.Vin_txtbx.Name = "Vin_txtbx";
            this.Vin_txtbx.Size = new System.Drawing.Size(100, 20);
            this.Vin_txtbx.TabIndex = 3;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.GridColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dataGridView1.Location = new System.Drawing.Point(15, 117);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 5;
            this.dataGridView1.Size = new System.Drawing.Size(424, 419);
            this.dataGridView1.TabIndex = 4;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView1CellContentClick);
            // 
            // Seacrh_Btn
            // 
            this.Seacrh_Btn.Location = new System.Drawing.Point(335, 23);
            this.Seacrh_Btn.Name = "Seacrh_Btn";
            this.Seacrh_Btn.Size = new System.Drawing.Size(75, 23);
            this.Seacrh_Btn.TabIndex = 5;
            this.Seacrh_Btn.Text = "Search";
            this.Seacrh_Btn.UseVisualStyleBackColor = true;
            this.Seacrh_Btn.Click += new System.EventHandler(this.SeacrhBtnClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.CustomerFleetNo_txtbx);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.Seacrh_Btn);
            this.groupBox1.Controls.Add(this.SR_Cmbbx);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.Vin_txtbx);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(427, 80);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Filters";
            // 
            // SelectAll_chkbx
            // 
            this.SelectAll_chkbx.AutoSize = true;
            this.SelectAll_chkbx.Location = new System.Drawing.Point(241, 98);
            this.SelectAll_chkbx.Name = "SelectAll_chkbx";
            this.SelectAll_chkbx.Size = new System.Drawing.Size(70, 17);
            this.SelectAll_chkbx.TabIndex = 7;
            this.SelectAll_chkbx.Text = "Select All";
            this.SelectAll_chkbx.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.SelectAll_chkbx.UseVisualStyleBackColor = true;
            this.SelectAll_chkbx.CheckedChanged += new System.EventHandler(this.SelectAllCheckBoxChecked);
            // 
            // ClearAll_Chkbx
            // 
            this.ClearAll_Chkbx.AutoSize = true;
            this.ClearAll_Chkbx.Location = new System.Drawing.Point(317, 98);
            this.ClearAll_Chkbx.Name = "ClearAll_Chkbx";
            this.ClearAll_Chkbx.Size = new System.Drawing.Size(64, 17);
            this.ClearAll_Chkbx.TabIndex = 8;
            this.ClearAll_Chkbx.Text = "Clear All";
            this.ClearAll_Chkbx.UseVisualStyleBackColor = true;
            this.ClearAll_Chkbx.CheckedChanged += new System.EventHandler(this.ClearAllCheckBoxChecked);
            // 
            // Bulid_btn
            // 
            this.Bulid_btn.Location = new System.Drawing.Point(347, 542);
            this.Bulid_btn.Name = "Bulid_btn";
            this.Bulid_btn.Size = new System.Drawing.Size(75, 23);
            this.Bulid_btn.TabIndex = 9;
            this.Bulid_btn.Text = "Build";
            this.Bulid_btn.UseVisualStyleBackColor = true;
            this.Bulid_btn.Click += new System.EventHandler(this.BuildBtnClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 54);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Customer Fleet #";
            // 
            // CustomerFleetNo_txtbx
            // 
            this.CustomerFleetNo_txtbx.Location = new System.Drawing.Point(101, 53);
            this.CustomerFleetNo_txtbx.Name = "CustomerFleetNo_txtbx";
            this.CustomerFleetNo_txtbx.Size = new System.Drawing.Size(100, 20);
            this.CustomerFleetNo_txtbx.TabIndex = 7;
            // 
            // SalesReleaseVINSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(458, 570);
            this.Controls.Add(this.Bulid_btn);
            this.Controls.Add(this.ClearAll_Chkbx);
            this.Controls.Add(this.SelectAll_chkbx);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.dataGridView1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SalesReleaseVINSearchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select SR and VIN";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.IncidentVinFormClosed);
            this.Load += new System.EventHandler(this.IncidentVinFormLoad);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button Seacrh_Btn;
        private System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.ComboBox SR_Cmbbx;
        public System.Windows.Forms.TextBox Vin_txtbx;
        public System.Windows.Forms.CheckBox SelectAll_chkbx;
        public System.Windows.Forms.CheckBox ClearAll_Chkbx;
        public System.Windows.Forms.Button Bulid_btn;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox CustomerFleetNo_txtbx;
    }
}