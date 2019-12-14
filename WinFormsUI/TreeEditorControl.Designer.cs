namespace Levrum.UI.WinForms
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
            this.m_cbDefaultTree = new System.Windows.Forms.ComboBox();
            this.m_bgwLoadIncidentData = new System.ComponentModel.BackgroundWorker();
            this.m_pDelete = new System.Windows.Forms.Panel();
            this.m_btnAddOrganizedCategory = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.m_scMain)).BeginInit();
            this.m_scMain.Panel1.SuspendLayout();
            this.m_scMain.Panel2.SuspendLayout();
            this.m_scMain.SuspendLayout();
            this.m_flpOrganizedData.SuspendLayout();
            this.m_pDelete.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // m_scMain
            // 
            this.m_scMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_scMain.BackColor = System.Drawing.Color.LightGray;
            this.m_scMain.Location = new System.Drawing.Point(0, 0);
            this.m_scMain.Name = "m_scMain";
            // 
            // m_scMain.Panel1
            // 
            this.m_scMain.Panel1.Controls.Add(this.m_flpOrganizedData);
            // 
            // m_scMain.Panel2
            // 
            this.m_scMain.Panel2.Controls.Add(this.m_pDelete);
            this.m_scMain.Panel2.Controls.Add(this.m_flpUnorganizedData);
            this.m_scMain.Size = new System.Drawing.Size(847, 473);
            this.m_scMain.SplitterDistance = 664;
            this.m_scMain.TabIndex = 0;
            // 
            // m_flpOrganizedData
            // 
            this.m_flpOrganizedData.AllowDrop = true;
            this.m_flpOrganizedData.AutoScroll = true;
            this.m_flpOrganizedData.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.m_flpOrganizedData.BackColor = System.Drawing.Color.White;
            this.m_flpOrganizedData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_flpOrganizedData.Controls.Add(this.m_btnAddOrganizedCategory);
            this.m_flpOrganizedData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_flpOrganizedData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.m_flpOrganizedData.ForeColor = System.Drawing.SystemColors.ControlText;
            this.m_flpOrganizedData.Location = new System.Drawing.Point(0, 0);
            this.m_flpOrganizedData.Name = "m_flpOrganizedData";
            this.m_flpOrganizedData.Size = new System.Drawing.Size(664, 473);
            this.m_flpOrganizedData.TabIndex = 0;
            this.m_flpOrganizedData.Click += new System.EventHandler(this.m_flpOrganizedData_Click);
            this.m_flpOrganizedData.DragDrop += new System.Windows.Forms.DragEventHandler(this.OrganizedPanel_DragDrop);
            this.m_flpOrganizedData.DragEnter += new System.Windows.Forms.DragEventHandler(this.OrganizedPanel_DragEnter);
            // 
            // m_flpUnorganizedData
            // 
            this.m_flpUnorganizedData.AllowDrop = true;
            this.m_flpUnorganizedData.AutoScroll = true;
            this.m_flpUnorganizedData.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.m_flpUnorganizedData.BackColor = System.Drawing.Color.White;
            this.m_flpUnorganizedData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_flpUnorganizedData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_flpUnorganizedData.Location = new System.Drawing.Point(0, 0);
            this.m_flpUnorganizedData.Name = "m_flpUnorganizedData";
            this.m_flpUnorganizedData.Size = new System.Drawing.Size(179, 473);
            this.m_flpUnorganizedData.TabIndex = 1;
            this.m_flpUnorganizedData.Click += new System.EventHandler(this.m_flpUnorganizedData_Click);
            this.m_flpUnorganizedData.DragDrop += new System.Windows.Forms.DragEventHandler(this.UnorganizedPanel_DragDrop);
            this.m_flpUnorganizedData.DragEnter += new System.Windows.Forms.DragEventHandler(this.UnorganizedPanel_DragEnter);
            // 
            // m_btnLoadTree
            // 
            this.m_btnLoadTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnLoadTree.AutoSize = true;
            this.m_btnLoadTree.Location = new System.Drawing.Point(730, 487);
            this.m_btnLoadTree.Name = "m_btnLoadTree";
            this.m_btnLoadTree.Size = new System.Drawing.Size(99, 23);
            this.m_btnLoadTree.TabIndex = 1;
            this.m_btnLoadTree.Text = "&Load Tree";
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
            this.m_btnSaveTree.Text = "&Save Tree";
            this.m_btnSaveTree.UseVisualStyleBackColor = true;
            this.m_btnSaveTree.Click += new System.EventHandler(this.m_btnSaveTree_Click);
            // 
            // m_btnLoadIncidents
            // 
            this.m_btnLoadIncidents.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnLoadIncidents.AutoSize = true;
            this.m_btnLoadIncidents.Location = new System.Drawing.Point(616, 487);
            this.m_btnLoadIncidents.Name = "m_btnLoadIncidents";
            this.m_btnLoadIncidents.Size = new System.Drawing.Size(108, 23);
            this.m_btnLoadIncidents.TabIndex = 3;
            this.m_btnLoadIncidents.Text = "Load &Incident Data";
            this.m_btnLoadIncidents.UseVisualStyleBackColor = true;
            this.m_btnLoadIncidents.Click += new System.EventHandler(this.m_btnLoadIncidents_Click);
            // 
            // m_cbDefaultTree
            // 
            this.m_cbDefaultTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_cbDefaultTree.FormattingEnabled = true;
            this.m_cbDefaultTree.Items.AddRange(new object[] {
            "Causes",
            "NFIRS Causes"});
            this.m_cbDefaultTree.Location = new System.Drawing.Point(150, 487);
            this.m_cbDefaultTree.Name = "m_cbDefaultTree";
            this.m_cbDefaultTree.Size = new System.Drawing.Size(121, 21);
            this.m_cbDefaultTree.TabIndex = 4;
            this.m_cbDefaultTree.Text = "Use Default Tree";
            this.m_cbDefaultTree.SelectedIndexChanged += new System.EventHandler(this.m_cbDefaultTree_SelectedIndexChanged);
            // 
            // m_bgwLoadIncidentData
            // 
            this.m_bgwLoadIncidentData.DoWork += new System.ComponentModel.DoWorkEventHandler(this.m_bgwLoadIncidentData_DoWork);
            this.m_bgwLoadIncidentData.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.m_bgwLoadIncidentData_RunWorkerCompleted);
            // 
            // m_pDelete
            // 
            this.m_pDelete.AllowDrop = true;
            this.m_pDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_pDelete.Controls.Add(this.pictureBox1);
            this.m_pDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_pDelete.Location = new System.Drawing.Point(0, 0);
            this.m_pDelete.Name = "m_pDelete";
            this.m_pDelete.Size = new System.Drawing.Size(179, 473);
            this.m_pDelete.TabIndex = 1;
            this.m_pDelete.Visible = false;
            this.m_pDelete.DragDrop += new System.Windows.Forms.DragEventHandler(this.UnorganizedPanel_DragDrop);
            this.m_pDelete.DragEnter += new System.Windows.Forms.DragEventHandler(this.UnorganizedPanel_DragEnter);
            this.m_pDelete.DragLeave += new System.EventHandler(this.m_pDelete_DragLeave);
            // 
            // m_btnAddOrganizedCategory
            // 
            this.m_btnAddOrganizedCategory.AutoSize = true;
            this.m_btnAddOrganizedCategory.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.m_btnAddOrganizedCategory.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.m_btnAddOrganizedCategory.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.m_btnAddOrganizedCategory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.m_btnAddOrganizedCategory.Image = global::Levrum.UI.WinForms.Properties.Resources.add_subcategory;
            this.m_btnAddOrganizedCategory.Location = new System.Drawing.Point(10, 10);
            this.m_btnAddOrganizedCategory.Margin = new System.Windows.Forms.Padding(10);
            this.m_btnAddOrganizedCategory.Name = "m_btnAddOrganizedCategory";
            this.m_btnAddOrganizedCategory.Size = new System.Drawing.Size(36, 36);
            this.m_btnAddOrganizedCategory.TabIndex = 0;
            this.m_btnAddOrganizedCategory.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.m_btnAddOrganizedCategory.UseVisualStyleBackColor = false;
            this.m_btnAddOrganizedCategory.Click += new System.EventHandler(this.AddSubcategory_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureBox1.Image = global::Levrum.UI.WinForms.Properties.Resources.delete;
            this.pictureBox1.Location = new System.Drawing.Point(41, 182);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 99);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // TreeEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.m_cbDefaultTree);
            this.Controls.Add(this.m_btnLoadIncidents);
            this.Controls.Add(this.m_btnSaveTree);
            this.Controls.Add(this.m_btnLoadTree);
            this.Controls.Add(this.m_scMain);
            this.Name = "TreeEditorControl";
            this.Size = new System.Drawing.Size(847, 521);
            this.Click += new System.EventHandler(this.TreeEditorControl_Click);
            this.m_scMain.Panel1.ResumeLayout(false);
            this.m_scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.m_scMain)).EndInit();
            this.m_scMain.ResumeLayout(false);
            this.m_flpOrganizedData.ResumeLayout(false);
            this.m_flpOrganizedData.PerformLayout();
            this.m_pDelete.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
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

        // Cursors
        private System.Windows.Forms.Cursor MoveCursor;
        private System.Windows.Forms.ComboBox m_cbDefaultTree;
        private System.ComponentModel.BackgroundWorker m_bgwLoadIncidentData;
        private System.Windows.Forms.Panel m_pDelete;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
