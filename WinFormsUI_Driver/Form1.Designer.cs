namespace WinFormsUI_Driver
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
            this.m_btnXmlSelector = new System.Windows.Forms.Button();
            this.m_chbEnableMultiSelection = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // m_btnXmlSelector
            // 
            this.m_btnXmlSelector.Location = new System.Drawing.Point(49, 73);
            this.m_btnXmlSelector.Name = "m_btnXmlSelector";
            this.m_btnXmlSelector.Size = new System.Drawing.Size(151, 36);
            this.m_btnXmlSelector.TabIndex = 0;
            this.m_btnXmlSelector.Text = "XML Selector";
            this.m_btnXmlSelector.UseVisualStyleBackColor = true;
            this.m_btnXmlSelector.Click += new System.EventHandler(this.m_btnXmlSelector_Click);
            // 
            // m_chbEnableMultiSelection
            // 
            this.m_chbEnableMultiSelection.AutoSize = true;
            this.m_chbEnableMultiSelection.Location = new System.Drawing.Point(228, 84);
            this.m_chbEnableMultiSelection.Name = "m_chbEnableMultiSelection";
            this.m_chbEnableMultiSelection.Size = new System.Drawing.Size(95, 17);
            this.m_chbEnableMultiSelection.TabIndex = 1;
            this.m_chbEnableMultiSelection.Text = "Multi-Selection";
            this.m_chbEnableMultiSelection.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.m_chbEnableMultiSelection);
            this.Controls.Add(this.m_btnXmlSelector);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button m_btnXmlSelector;
        private System.Windows.Forms.CheckBox m_chbEnableMultiSelection;
    }
}

