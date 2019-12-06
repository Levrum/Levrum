﻿namespace Levrum.UI.WinForms
{
    partial class TreeEditorControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_scMain = new System.Windows.Forms.SplitContainer();
            this.m_flpOrganizedData = new System.Windows.Forms.FlowLayoutPanel();
            this.m_flpUnorganizedData = new System.Windows.Forms.FlowLayoutPanel();
            this.m_btnLoadTree = new System.Windows.Forms.Button();
            this.m_btnSaveTree = new System.Windows.Forms.Button();
            this.m_btnLoadIncidents = new System.Windows.Forms.Button();
            this.m_btnAddOrganizedCategory = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.m_scMain)).BeginInit();
            this.m_scMain.Panel1.SuspendLayout();
            this.m_scMain.Panel2.SuspendLayout();
            this.m_scMain.SuspendLayout();
            this.m_flpOrganizedData.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_scMain
            // 
            this.m_scMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_scMain.Location = new System.Drawing.Point(0, 0);
            this.m_scMain.Name = "m_scMain";
            // 
            // m_scMain.Panel1
            // 
            this.m_scMain.Panel1.Controls.Add(this.m_flpOrganizedData);
            // 
            // m_scMain.Panel2
            // 
            this.m_scMain.Panel2.Controls.Add(this.m_flpUnorganizedData);
            this.m_scMain.Size = new System.Drawing.Size(847, 473);
            this.m_scMain.SplitterDistance = 419;
            this.m_scMain.TabIndex = 0;
            // 
            // m_flpOrganizedData
            // 
            this.m_flpOrganizedData.AutoScroll = true;
            this.m_flpOrganizedData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_flpOrganizedData.Controls.Add(this.m_btnAddOrganizedCategory);
            this.m_flpOrganizedData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_flpOrganizedData.Location = new System.Drawing.Point(0, 0);
            this.m_flpOrganizedData.Name = "m_flpOrganizedData";
            this.m_flpOrganizedData.Size = new System.Drawing.Size(419, 473);
            this.m_flpOrganizedData.TabIndex = 0;
            // 
            // m_flpUnorganizedData
            // 
            this.m_flpUnorganizedData.AutoScroll = true;
            this.m_flpUnorganizedData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_flpUnorganizedData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_flpUnorganizedData.Location = new System.Drawing.Point(0, 0);
            this.m_flpUnorganizedData.Name = "m_flpUnorganizedData";
            this.m_flpUnorganizedData.Size = new System.Drawing.Size(424, 473);
            this.m_flpUnorganizedData.TabIndex = 1;
            // 
            // m_btnLoadTree
            // 
            this.m_btnLoadTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnLoadTree.AutoSize = true;
            this.m_btnLoadTree.Location = new System.Drawing.Point(730, 487);
            this.m_btnLoadTree.Name = "m_btnLoadTree";
            this.m_btnLoadTree.Size = new System.Drawing.Size(99, 23);
            this.m_btnLoadTree.TabIndex = 1;
            this.m_btnLoadTree.Text = "Load Tree";
            this.m_btnLoadTree.UseVisualStyleBackColor = true;
            this.m_btnLoadTree.Click += new System.EventHandler(this.m_btnLoadTree_Click);
            // 
            // m_btnSaveTree
            // 
            this.m_btnSaveTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_btnSaveTree.AutoSize = true;
            this.m_btnSaveTree.Location = new System.Drawing.Point(19, 487);
            this.m_btnSaveTree.Name = "m_btnSaveTree";
            this.m_btnSaveTree.Size = new System.Drawing.Size(99, 23);
            this.m_btnSaveTree.TabIndex = 2;
            this.m_btnSaveTree.Text = "Save Tree";
            this.m_btnSaveTree.UseVisualStyleBackColor = true;
            // 
            // m_btnLoadIncidents
            // 
            this.m_btnLoadIncidents.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnLoadIncidents.AutoSize = true;
            this.m_btnLoadIncidents.Location = new System.Drawing.Point(616, 487);
            this.m_btnLoadIncidents.Name = "m_btnLoadIncidents";
            this.m_btnLoadIncidents.Size = new System.Drawing.Size(108, 23);
            this.m_btnLoadIncidents.TabIndex = 3;
            this.m_btnLoadIncidents.Text = "Load Incident Data";
            this.m_btnLoadIncidents.UseVisualStyleBackColor = true;
            this.m_btnLoadIncidents.Click += new System.EventHandler(this.m_btnLoadIncidents_Click);
            // 
            // m_btnAddOrganizedCategory
            // 
            this.m_btnAddOrganizedCategory.AutoSize = true;
            this.m_btnAddOrganizedCategory.Location = new System.Drawing.Point(3, 3);
            this.m_btnAddOrganizedCategory.Name = "m_btnAddOrganizedCategory";
            this.m_btnAddOrganizedCategory.Size = new System.Drawing.Size(81, 23);
            this.m_btnAddOrganizedCategory.TabIndex = 0;
            this.m_btnAddOrganizedCategory.Text = "Add Category";
            this.m_btnAddOrganizedCategory.UseVisualStyleBackColor = true;
            this.m_btnAddOrganizedCategory.Click += new System.EventHandler(this.AddSubcategory_Click);
            // 
            // TreeEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.m_btnLoadIncidents);
            this.Controls.Add(this.m_btnSaveTree);
            this.Controls.Add(this.m_btnLoadTree);
            this.Controls.Add(this.m_scMain);
            this.Name = "TreeEditorControl";
            this.Size = new System.Drawing.Size(847, 521);
            this.m_scMain.Panel1.ResumeLayout(false);
            this.m_scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.m_scMain)).EndInit();
            this.m_scMain.ResumeLayout(false);
            this.m_flpOrganizedData.ResumeLayout(false);
            this.m_flpOrganizedData.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer m_scMain;
        private System.Windows.Forms.FlowLayoutPanel m_flpOrganizedData;
        private System.Windows.Forms.FlowLayoutPanel m_flpUnorganizedData;
        private System.Windows.Forms.Button m_btnLoadTree;
        private System.Windows.Forms.Button m_btnSaveTree;
        private System.Windows.Forms.Button m_btnLoadIncidents;
        private System.Windows.Forms.Button m_btnAddOrganizedCategory;
    }
}
