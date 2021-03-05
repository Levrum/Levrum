namespace Levrum.UI.WinForms.Forms
{
    partial class XmlDataSelectorForm
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
            this.m_ucXmlSelectorCtl = new Levrum.UI.WinForms.Controls.XmlDataSelectorCtl();
            this.m_btnOK = new System.Windows.Forms.Button();
            this.m_btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_ucXmlSelectorCtl
            // 
            this.m_ucXmlSelectorCtl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_ucXmlSelectorCtl.BackColor = System.Drawing.SystemColors.ControlLight;
            this.m_ucXmlSelectorCtl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_ucXmlSelectorCtl.EnableMultiSelection = false;
            this.m_ucXmlSelectorCtl.Location = new System.Drawing.Point(1, 35);
            this.m_ucXmlSelectorCtl.Name = "m_ucXmlSelectorCtl";
            this.m_ucXmlSelectorCtl.Size = new System.Drawing.Size(370, 552);
            this.m_ucXmlSelectorCtl.TabIndex = 0;
            // 
            // m_btnOK
            // 
            this.m_btnOK.Location = new System.Drawing.Point(4, 5);
            this.m_btnOK.Name = "m_btnOK";
            this.m_btnOK.Size = new System.Drawing.Size(75, 23);
            this.m_btnOK.TabIndex = 1;
            this.m_btnOK.Text = "OK";
            this.m_btnOK.UseVisualStyleBackColor = true;
            this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
            // 
            // m_btnCancel
            // 
            this.m_btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.m_btnCancel.Location = new System.Drawing.Point(294, 5);
            this.m_btnCancel.Name = "m_btnCancel";
            this.m_btnCancel.Size = new System.Drawing.Size(75, 23);
            this.m_btnCancel.TabIndex = 1;
            this.m_btnCancel.Text = "Cancel";
            this.m_btnCancel.UseVisualStyleBackColor = true;
            this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
            // 
            // XmlDataSelectorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(375, 587);
            this.Controls.Add(this.m_btnCancel);
            this.Controls.Add(this.m_btnOK);
            this.Controls.Add(this.m_ucXmlSelectorCtl);
            this.Name = "XmlDataSelectorForm";
            this.Text = "Map XML Content to Items";
            this.Load += new System.EventHandler(this.HandleFormLoad);
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.XmlDataSelectorCtl m_ucXmlSelectorCtl;
        private System.Windows.Forms.Button m_btnOK;
        private System.Windows.Forms.Button m_btnCancel;
    }
}