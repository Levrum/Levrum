namespace FrameworkUI.Test
{
    partial class TreeControlTest
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
            this.treeEditorControl1 = new Levrum.UI.WinForms.TreeEditorControl();
            this.SuspendLayout();
            // 
            // treeEditorControl1
            // 
            this.treeEditorControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeEditorControl1.Location = new System.Drawing.Point(0, 0);
            this.treeEditorControl1.Name = "treeEditorControl1";
            this.treeEditorControl1.Size = new System.Drawing.Size(800, 450);
            this.treeEditorControl1.TabIndex = 0;
            // 
            // TreeControlTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.treeEditorControl1);
            this.Name = "TreeControlTest";
            this.Text = "TreeControlTest";
            this.ResumeLayout(false);

        }

        #endregion

        private Levrum.UI.WinForms.TreeEditorControl treeEditorControl1;
    }
}