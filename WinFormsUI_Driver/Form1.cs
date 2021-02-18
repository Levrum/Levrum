using C3ModelStandard.Infra;
using Levrum.UI.WinForms.Controls;
using Levrum.UI.WinForms.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsUI_Driver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void m_btnXmlSelector_Click(object sender, EventArgs e)
        {

            string fn = MethodBase.GetCurrentMethod().Name;
            StreamReader sr = null;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "XML Files|*.xml";
                ofd.ShowDialog();
                string sfile = ofd.FileName;
                if (string.IsNullOrEmpty(sfile)) { return; }

                sr = new StreamReader(sfile);
                string sxml = sr.ReadToEnd();
                XmlDataSelectorForm form = new XmlDataSelectorForm();
                form.FillXmlFromString(sxml);
                form.EnableMultiSelection = m_chbEnableMultiSelection.Checked;
                
                form.Show();
                form.BringToFront();


                String smsg = "";
                if (form.EnableMultiSelection)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Selected items:");
                    foreach (XmlDataSelectionInfo xdsi in form.SelectedItems )
                    {
                        sb.Append("   ");
                        sb.AppendLine(xdsi.ToString());
                    }
                    smsg = sb.ToString();
                }
                else
                {
                    smsg = "Selected Item:\r\n";
                    if (null!=form.SelectedItem)
                    {
                        smsg += "   " + form.SelectedItem.ToString();
                    }
                }

                MessageBox.Show(smsg);
                
            }
            catch(Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                MessageBox.Show("An error has occurred; please see the event log");
            }
            finally
            {
                if (null!=sr) { sr.Close(); }
            }


        }
    }
}
