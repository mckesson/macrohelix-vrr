using System;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Linq;
using System.Configuration;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using System.Windows.Forms.VisualStyles;
using System.Text.RegularExpressions;

namespace VRR_Inbound_File_Generator
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblChainStore = new System.Windows.Forms.Label();
            this.txtChainStore = new System.Windows.Forms.TextBox();
            this.lblPID = new System.Windows.Forms.Label();
            this.txtPID = new System.Windows.Forms.TextBox();
            this.lblNDC = new System.Windows.Forms.Label();
            this.txtNDC = new System.Windows.Forms.TextBox();
            this.lblReasonCode = new System.Windows.Forms.Label();
            this.cboReasonCode = new System.Windows.Forms.ComboBox();
            this.lblRecordCount = new System.Windows.Forms.Label();
            this.txtRecordCount = new System.Windows.Forms.TextBox();
            this.lblOutputPath = new System.Windows.Forms.Label();
            this.txtOutputPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.lblRequestExecutionID = new System.Windows.Forms.Label();
            this.txtRequestExecutionID = new System.Windows.Forms.TextBox();
            this.chkUseDatabaseValidation = new System.Windows.Forms.CheckBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.btnFetchOutboundData = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblChainStore
            // 
            this.lblChainStore.AutoSize = true;
            this.lblChainStore.Location = new System.Drawing.Point(20, 20);
            this.lblChainStore.Name = "lblChainStore";
            this.lblChainStore.Size = new System.Drawing.Size(65, 13);
            this.lblChainStore.TabIndex = 0;
            this.lblChainStore.Text = "Chain Store:";
            // 
            // txtChainStore
            // 
            this.txtChainStore.Location = new System.Drawing.Point(140, 20);
            this.txtChainStore.Name = "txtChainStore";
            this.txtChainStore.Size = new System.Drawing.Size(200, 20);
            this.txtChainStore.TabIndex = 1;
            this.txtChainStore.Text = "WMT";
            // 
            // lblPID
            // 
            this.lblPID.AutoSize = true;
            this.lblPID.Location = new System.Drawing.Point(20, 80);
            this.lblPID.Name = "lblPID";
            this.lblPID.Size = new System.Drawing.Size(28, 13);
            this.lblPID.TabIndex = 4;
            this.lblPID.Text = "PID:";
            // 
            // txtPID
            // 
            this.txtPID.Location = new System.Drawing.Point(140, 80);
            this.txtPID.Name = "txtPID";
            this.txtPID.Size = new System.Drawing.Size(200, 20);
            this.txtPID.TabIndex = 5;
            // 
            // lblNDC
            // 
            this.lblNDC.AutoSize = true;
            this.lblNDC.Location = new System.Drawing.Point(20, 110);
            this.lblNDC.Name = "lblNDC";
            this.lblNDC.Size = new System.Drawing.Size(33, 13);
            this.lblNDC.TabIndex = 6;
            this.lblNDC.Text = "NDC:";
            // 
            // txtNDC
            // 
            this.txtNDC.Location = new System.Drawing.Point(140, 103);
            this.txtNDC.Name = "txtNDC";
            this.txtNDC.Size = new System.Drawing.Size(200, 20);
            this.txtNDC.TabIndex = 7;
            // 
            // lblReasonCode
            // 
            this.lblReasonCode.AutoSize = true;
            this.lblReasonCode.Location = new System.Drawing.Point(20, 140);
            this.lblReasonCode.Name = "lblReasonCode";
            this.lblReasonCode.Size = new System.Drawing.Size(75, 13);
            this.lblReasonCode.TabIndex = 8;
            this.lblReasonCode.Text = "Reason Code:";
            // 
            // cboReasonCode
            // 
            this.cboReasonCode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboReasonCode.FormattingEnabled = true;
            this.cboReasonCode.Location = new System.Drawing.Point(140, 137);
            this.cboReasonCode.Name = "cboReasonCode";
            this.cboReasonCode.Size = new System.Drawing.Size(200, 21);
            this.cboReasonCode.TabIndex = 9;
            // 
            // lblRecordCount
            // 
            this.lblRecordCount.AutoSize = true;
            this.lblRecordCount.Location = new System.Drawing.Point(20, 170);
            this.lblRecordCount.Name = "lblRecordCount";
            this.lblRecordCount.Size = new System.Drawing.Size(76, 13);
            this.lblRecordCount.TabIndex = 10;
            this.lblRecordCount.Text = "Record Count:";
            // 
            // txtRecordCount
            // 
            this.txtRecordCount.Location = new System.Drawing.Point(140, 170);
            this.txtRecordCount.Name = "txtRecordCount";
            this.txtRecordCount.Size = new System.Drawing.Size(200, 20);
            this.txtRecordCount.TabIndex = 11;
            this.txtRecordCount.Text = "1000";
            // 
            // lblOutputPath
            // 
            this.lblOutputPath.AutoSize = true;
            this.lblOutputPath.Location = new System.Drawing.Point(20, 200);
            this.lblOutputPath.Name = "lblOutputPath";
            this.lblOutputPath.Size = new System.Drawing.Size(64, 13);
            this.lblOutputPath.TabIndex = 12;
            this.lblOutputPath.Text = "Output Path";
            // 
            // txtOutputPath
            // 
            this.txtOutputPath.Location = new System.Drawing.Point(120, 200);
            this.txtOutputPath.Name = "txtOutputPath";
            this.txtOutputPath.Size = new System.Drawing.Size(350, 20);
            this.txtOutputPath.TabIndex = 13;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(480, 200);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(60, 23);
            this.btnBrowse.TabIndex = 14;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 260);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(520, 20);
            this.progressBar.TabIndex = 15;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(120, 230);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(38, 13);
            this.lblStatus.TabIndex = 16;
            this.lblStatus.Text = "Ready";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(20, 230);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(100, 23);
            this.btnGenerate.TabIndex = 17;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.BtnGenerate_Click);
            // 
            // lblRequestExecutionID
            // 
            this.lblRequestExecutionID.AutoSize = true;
            this.lblRequestExecutionID.Location = new System.Drawing.Point(20, 50);
            this.lblRequestExecutionID.Name = "lblRequestExecutionID";
            this.lblRequestExecutionID.Size = new System.Drawing.Size(114, 13);
            this.lblRequestExecutionID.TabIndex = 2;
            this.lblRequestExecutionID.Text = "Request Execution ID:";
            // 
            // txtRequestExecutionID
            // 
            this.txtRequestExecutionID.Location = new System.Drawing.Point(140, 47);
            this.txtRequestExecutionID.Name = "txtRequestExecutionID";
            this.txtRequestExecutionID.Size = new System.Drawing.Size(200, 20);
            this.txtRequestExecutionID.TabIndex = 3;
            // 
            // chkUseDatabaseValidation
            // 
            this.chkUseDatabaseValidation.AutoSize = true;
            this.chkUseDatabaseValidation.Checked = true;
            this.chkUseDatabaseValidation.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUseDatabaseValidation.Location = new System.Drawing.Point(390, 35);
            this.chkUseDatabaseValidation.Name = "chkUseDatabaseValidation";
            this.chkUseDatabaseValidation.Size = new System.Drawing.Size(143, 17);
            this.chkUseDatabaseValidation.TabIndex = 18;
            this.chkUseDatabaseValidation.Text = "Use Database Validation";
            this.chkUseDatabaseValidation.UseVisualStyleBackColor = true;
            this.chkUseDatabaseValidation.CheckedChanged += new System.EventHandler(this.ChkUseDatabaseValidation_CheckedChanged);
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.AutoSize = true;
            this.btnTestConnection.Location = new System.Drawing.Point(390, 121);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(115, 23);
            this.btnTestConnection.TabIndex = 19;
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.BtnTestConnection_Click);
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Location = new System.Drawing.Point(387, 87);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(147, 13);
            this.lblConnectionStatus.TabIndex = 20;
            this.lblConnectionStatus.Text = "Connection status: Not tested";
            // 
            // btnFetchOutboundData
            // 
            this.btnFetchOutboundData.AutoSize = true;
            this.btnFetchOutboundData.Location = new System.Drawing.Point(390, 58);
            this.btnFetchOutboundData.Name = "btnFetchOutboundData";
            this.btnFetchOutboundData.Size = new System.Drawing.Size(120, 23);
            this.btnFetchOutboundData.TabIndex = 21;
            this.btnFetchOutboundData.Text = "Fetch Outbound Data";
            this.btnFetchOutboundData.UseVisualStyleBackColor = true;
            this.btnFetchOutboundData.Click += new System.EventHandler(this.BtnFetchOutboundData_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 321);
            this.Controls.Add(this.btnFetchOutboundData);
            this.Controls.Add(this.lblConnectionStatus);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.chkUseDatabaseValidation);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtOutputPath);
            this.Controls.Add(this.lblOutputPath);
            this.Controls.Add(this.txtRecordCount);
            this.Controls.Add(this.lblRecordCount);
            this.Controls.Add(this.cboReasonCode);
            this.Controls.Add(this.lblReasonCode);
            this.Controls.Add(this.txtNDC);
            this.Controls.Add(this.lblNDC);
            this.Controls.Add(this.txtPID);
            this.Controls.Add(this.lblPID);
            this.Controls.Add(this.txtRequestExecutionID);
            this.Controls.Add(this.lblRequestExecutionID);
            this.Controls.Add(this.txtChainStore);
            this.Controls.Add(this.lblChainStore);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VRR Inbound File Generator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
        
        private System.Windows.Forms.Label lblChainStore;
        private System.Windows.Forms.TextBox txtChainStore;
        private System.Windows.Forms.Label lblPID;
        private System.Windows.Forms.TextBox txtPID;
        private System.Windows.Forms.Label lblNDC;
        private System.Windows.Forms.TextBox txtNDC;
        private System.Windows.Forms.Label lblReasonCode;
        private System.Windows.Forms.ComboBox cboReasonCode;
        private System.Windows.Forms.Label lblRecordCount;
        private System.Windows.Forms.TextBox txtRecordCount;
        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Label lblRequestExecutionID;
        private System.Windows.Forms.TextBox txtRequestExecutionID;
        private CheckBox chkUseDatabaseValidation;
        private Button btnTestConnection;
        private Label lblConnectionStatus;
        private Button btnFetchOutboundData;
    }
}

