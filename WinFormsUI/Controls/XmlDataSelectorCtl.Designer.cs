namespace Levrum.UI.WinForms.Controls
{
    partial class XmlDataSelectorCtl
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
            this.m_tvXmlContent = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // m_tvXmlContent
            // 
            this.m_tvXmlContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_tvXmlContent.Location = new System.Drawing.Point(3, 3);
            this.m_tvXmlContent.Name = "m_tvXmlContent";
            this.m_tvXmlContent.Size = new System.Drawing.Size(317, 500);
            this.m_tvXmlContent.TabIndex = 0;
            // 
            // XmlDataSelectorCtl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.m_tvXmlContent);
            this.Name = "XmlDataSelectorCtl";
            this.Size = new System.Drawing.Size(323, 506);
            this.Load += new System.EventHandler(this.HandleCtlLoad);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TreeView m_tvXmlContent;
    }
}
