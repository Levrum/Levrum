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
            this.components = new System.ComponentModel.Container();
            this.m_scMain = new System.Windows.Forms.SplitContainer();
            this.m_flpOrganizedData = new System.Windows.Forms.FlowLayoutPanel();
            this.m_btnAddOrganizedCategory = new System.Windows.Forms.Button();
            this.m_btnUndoDelete = new System.Windows.Forms.Button();
            this.m_cbOnlyUnadded = new System.Windows.Forms.CheckBox();
            this.m_pDelete = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.m_flpUnorganizedData = new System.Windows.Forms.FlowLayoutPanel();
            this.m_btnSaveTree = new System.Windows.Forms.Button();
            this.m_btnLoadIncidents = new System.Windows.Forms.Button();
            this.m_cbDefaultTree = new System.Windows.Forms.ComboBox();
            this.m_bgwLoadIncidentData = new System.ComponentModel.BackgroundWorker();
            this.m_undoDeleteTimer = new System.Windows.Forms.Timer(this.components);
            this.m_lbDataFields = new System.Windows.Forms.ListBox();
            this.m_labelDataFieldsHeader = new System.Windows.Forms.Label();
            this.m_btnSaveAs = new System.Windows.Forms.Button();
            this.m_labelTreeSaved = new System.Windows.Forms.Label();
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
            this.m_scMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.m_scMain.Location = new System.Drawing.Point(0, 0);
            this.m_scMain.Name = "m_scMain";
            // 
            // m_scMain.Panel1
            // 
            this.m_scMain.Panel1.Controls.Add(this.m_flpOrganizedData);
            // 
            // m_scMain.Panel2
            // 
            this.m_scMain.Panel2.Controls.Add(this.m_btnUndoDelete);
            this.m_scMain.Panel2.Controls.Add(this.m_cbOnlyUnadded);
            this.m_scMain.Panel2.Controls.Add(this.m_pDelete);
            this.m_scMain.Panel2.Controls.Add(this.m_flpUnorganizedData);
            this.m_scMain.Size = new System.Drawing.Size(847, 461);
            this.m_scMain.SplitterDistance = 496;
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
            this.m_flpOrganizedData.Size = new System.Drawing.Size(496, 461);
            this.m_flpOrganizedData.TabIndex = 0;
            this.m_flpOrganizedData.Click += new System.EventHandler(this.OrganizedData_Click);
            this.m_flpOrganizedData.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.m_flpOrganizedData_ControlAdded);
            this.m_flpOrganizedData.DragDrop += new System.Windows.Forms.DragEventHandler(this.OrganizedPanel_DragDrop);
            this.m_flpOrganizedData.DragEnter += new System.Windows.Forms.DragEventHandler(this.OrganizedPanel_DragEnter);
            // 
            // m_btnAddOrganizedCategory
            // 
            this.m_btnAddOrganizedCategory.AutoSize = true;
            this.m_btnAddOrganizedCategory.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.m_btnAddOrganizedCategory.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.m_btnAddOrganizedCategory.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.m_btnAddOrganizedCategory.FlatAppearance.BorderSize = 0;
            this.m_btnAddOrganizedCategory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.m_btnAddOrganizedCategory.Image = global::Levrum.UI.WinForms.Properties.Resources.add_subcategory;
            this.m_btnAddOrganizedCategory.Location = new System.Drawing.Point(10, 10);
            this.m_btnAddOrganizedCategory.Margin = new System.Windows.Forms.Padding(10);
            this.m_btnAddOrganizedCategory.Name = "m_btnAddOrganizedCategory";
            this.m_btnAddOrganizedCategory.Size = new System.Drawing.Size(34, 34);
            this.m_btnAddOrganizedCategory.TabIndex = 0;
            this.m_btnAddOrganizedCategory.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.m_btnAddOrganizedCategory.UseVisualStyleBackColor = false;
            this.m_btnAddOrganizedCategory.Click += new System.EventHandler(this.AddSubcategory_Click);
            // 
            // m_btnUndoDelete
            // 
            this.m_btnUndoDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnUndoDelete.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.m_btnUndoDelete.BackColor = System.Drawing.Color.Black;
            this.m_btnUndoDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.m_btnUndoDelete.ForeColor = System.Drawing.Color.LightGray;
            this.m_btnUndoDelete.Image = global::Levrum.UI.WinForms.Properties.Resources.undo;
            this.m_btnUndoDelete.Location = new System.Drawing.Point(-3, 429);
            this.m_btnUndoDelete.Name = "m_btnUndoDelete";
            this.m_btnUndoDelete.Size = new System.Drawing.Size(347, 32);
            this.m_btnUndoDelete.TabIndex = 4;
            this.m_btnUndoDelete.Text = "Undo Delete";
            this.m_btnUndoDelete.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_btnUndoDelete.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.m_btnUndoDelete.UseVisualStyleBackColor = false;
            this.m_btnUndoDelete.Visible = false;
            this.m_btnUndoDelete.Click += new System.EventHandler(this.m_btnUndoDelete_Click);
            // 
            // m_cbOnlyUnadded
            // 
            this.m_cbOnlyUnadded.AutoSize = true;
            this.m_cbOnlyUnadded.Location = new System.Drawing.Point(5, 4);
            this.m_cbOnlyUnadded.Name = "m_cbOnlyUnadded";
            this.m_cbOnlyUnadded.Size = new System.Drawing.Size(77, 17);
            this.m_cbOnlyUnadded.TabIndex = 1;
            this.m_cbOnlyUnadded.Text = "Not Added";
            this.m_cbOnlyUnadded.UseVisualStyleBackColor = true;
            this.m_cbOnlyUnadded.Click += new System.EventHandler(this.m_cbOnlyUnadded_Click);
            // 
            // m_pDelete
            // 
            this.m_pDelete.AllowDrop = true;
            this.m_pDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_pDelete.Controls.Add(this.pictureBox1);
            this.m_pDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_pDelete.Location = new System.Drawing.Point(0, 0);
            this.m_pDelete.Name = "m_pDelete";
            this.m_pDelete.Size = new System.Drawing.Size(347, 461);
            this.m_pDelete.TabIndex = 1;
            this.m_pDelete.Visible = false;
            this.m_pDelete.DragDrop += new System.Windows.Forms.DragEventHandler(this.DeletePanel_DragDrop);
            this.m_pDelete.DragEnter += new System.Windows.Forms.DragEventHandler(this.DeletePanel_DragEnter);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureBox1.Image = global::Levrum.UI.WinForms.Properties.Resources.delete;
            this.pictureBox1.Location = new System.Drawing.Point(123, 181);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 99);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // m_flpUnorganizedData
            // 
            this.m_flpUnorganizedData.AllowDrop = true;
            this.m_flpUnorganizedData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_flpUnorganizedData.AutoScroll = true;
            this.m_flpUnorganizedData.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.m_flpUnorganizedData.BackColor = System.Drawing.Color.White;
            this.m_flpUnorganizedData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_flpUnorganizedData.Location = new System.Drawing.Point(0, 27);
            this.m_flpUnorganizedData.Name = "m_flpUnorganizedData";
            this.m_flpUnorganizedData.Size = new System.Drawing.Size(347, 434);
            this.m_flpUnorganizedData.TabIndex = 1;
            this.m_flpUnorganizedData.Click += new System.EventHandler(this.UnorganizedData_Click);
            this.m_flpUnorganizedData.DragDrop += new System.Windows.Forms.DragEventHandler(this.DeletePanel_DragDrop);
            this.m_flpUnorganizedData.DragEnter += new System.Windows.Forms.DragEventHandler(this.DeletePanel_DragEnter);
            // 
            // m_btnSaveTree
            // 
            this.m_btnSaveTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_btnSaveTree.AutoSize = true;
            this.m_btnSaveTree.Enabled = false;
            this.m_btnSaveTree.Location = new System.Drawing.Point(19, 476);
            this.m_btnSaveTree.Name = "m_btnSaveTree";
            this.m_btnSaveTree.Size = new System.Drawing.Size(59, 23);
            this.m_btnSaveTree.TabIndex = 2;
            this.m_btnSaveTree.Text = "Save";
            this.m_btnSaveTree.UseVisualStyleBackColor = true;
            this.m_btnSaveTree.Click += new System.EventHandler(this.m_btnSaveTree_Click);
            // 
            // m_btnLoadIncidents
            // 
            this.m_btnLoadIncidents.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnLoadIncidents.AutoSize = true;
            this.m_btnLoadIncidents.Location = new System.Drawing.Point(713, 477);
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
            this.m_cbDefaultTree.Location = new System.Drawing.Point(149, 478);
            this.m_cbDefaultTree.Name = "m_cbDefaultTree";
            this.m_cbDefaultTree.Size = new System.Drawing.Size(121, 21);
            this.m_cbDefaultTree.TabIndex = 4;
            this.m_cbDefaultTree.Text = "Load Existing Tree";
            this.m_cbDefaultTree.SelectedIndexChanged += new System.EventHandler(this.m_cbDefaultTree_SelectedIndexChanged);
            // 
            // m_bgwLoadIncidentData
            // 
            this.m_bgwLoadIncidentData.DoWork += new System.ComponentModel.DoWorkEventHandler(this.m_bgwLoadIncidentData_DoWork);
            this.m_bgwLoadIncidentData.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.m_bgwLoadIncidentData_RunWorkerCompleted);
            // 
            // m_undoDeleteTimer
            // 
            this.m_undoDeleteTimer.Interval = 5000;
            this.m_undoDeleteTimer.Tick += new System.EventHandler(this.m_undoDeleteTimer_Tick);
            // 
            // m_lbDataFields
            // 
            this.m_lbDataFields.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_lbDataFields.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.m_lbDataFields.FormattingEnabled = true;
            this.m_lbDataFields.ItemHeight = 15;
            this.m_lbDataFields.Location = new System.Drawing.Point(612, 307);
            this.m_lbDataFields.Name = "m_lbDataFields";
            this.m_lbDataFields.Size = new System.Drawing.Size(210, 154);
            this.m_lbDataFields.TabIndex = 5;
            this.m_lbDataFields.Visible = false;
            // 
            // m_labelDataFieldsHeader
            // 
            this.m_labelDataFieldsHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_labelDataFieldsHeader.BackColor = System.Drawing.Color.White;
            this.m_labelDataFieldsHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_labelDataFieldsHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_labelDataFieldsHeader.Location = new System.Drawing.Point(612, 286);
            this.m_labelDataFieldsHeader.Name = "m_labelDataFieldsHeader";
            this.m_labelDataFieldsHeader.Size = new System.Drawing.Size(210, 18);
            this.m_labelDataFieldsHeader.TabIndex = 6;
            this.m_labelDataFieldsHeader.Text = "Please choose a data field";
            this.m_labelDataFieldsHeader.Visible = false;
            // 
            // m_btnSaveAs
            // 
            this.m_btnSaveAs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_btnSaveAs.AutoSize = true;
            this.m_btnSaveAs.Enabled = false;
            this.m_btnSaveAs.Location = new System.Drawing.Point(84, 476);
            this.m_btnSaveAs.Name = "m_btnSaveAs";
            this.m_btnSaveAs.Size = new System.Drawing.Size(59, 23);
            this.m_btnSaveAs.TabIndex = 7;
            this.m_btnSaveAs.Text = "Save As";
            this.m_btnSaveAs.UseVisualStyleBackColor = true;
            this.m_btnSaveAs.Click += new System.EventHandler(this.button1_Click);
            // 
            // m_labelTreeSaved
            // 
            this.m_labelTreeSaved.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_labelTreeSaved.AutoSize = true;
            this.m_labelTreeSaved.ForeColor = System.Drawing.Color.Green;
            this.m_labelTreeSaved.Location = new System.Drawing.Point(18, 503);
            this.m_labelTreeSaved.Name = "m_labelTreeSaved";
            this.m_labelTreeSaved.Size = new System.Drawing.Size(125, 13);
            this.m_labelTreeSaved.TabIndex = 8;
            this.m_labelTreeSaved.Text = "Tree Saved Successfully";
            this.m_labelTreeSaved.Visible = false;
            // 
            // TreeEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.m_labelTreeSaved);
            this.Controls.Add(this.m_btnSaveAs);
            this.Controls.Add(this.m_cbDefaultTree);
            this.Controls.Add(this.m_labelDataFieldsHeader);
            this.Controls.Add(this.m_lbDataFields);
            this.Controls.Add(this.m_btnLoadIncidents);
            this.Controls.Add(this.m_btnSaveTree);
            this.Controls.Add(this.m_scMain);
            this.MinimumSize = new System.Drawing.Size(493, 361);
            this.Name = "TreeEditorControl";
            this.Size = new System.Drawing.Size(847, 521);
            this.Click += new System.EventHandler(this.TreeEditorControl_Click);
            this.m_scMain.Panel1.ResumeLayout(false);
            this.m_scMain.Panel2.ResumeLayout(false);
            this.m_scMain.Panel2.PerformLayout();
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
        private System.Windows.Forms.Button m_btnSaveTree;
        private System.Windows.Forms.Button m_btnLoadIncidents;
        private System.Windows.Forms.Button m_btnAddOrganizedCategory;

        // Cursors
        private System.Windows.Forms.Cursor MoveCursor;
        private System.Windows.Forms.ComboBox m_cbDefaultTree;
        private System.ComponentModel.BackgroundWorker m_bgwLoadIncidentData;
        private System.Windows.Forms.Panel m_pDelete;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer m_undoDeleteTimer;
        private System.Windows.Forms.ListBox m_lbDataFields;
        private System.Windows.Forms.Label m_labelDataFieldsHeader;
        private System.Windows.Forms.CheckBox m_cbOnlyUnadded;
        private System.Windows.Forms.Button m_btnUndoDelete;
        private System.Windows.Forms.Button m_btnSaveAs;
        private System.Windows.Forms.Label m_labelTreeSaved;
    }
}
