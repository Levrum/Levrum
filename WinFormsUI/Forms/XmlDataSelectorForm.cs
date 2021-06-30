using Levrum.Utils.Infra;
using Levrum.UI.WinForms.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Levrum.UI.WinForms.Forms
{
    public partial class XmlDataSelectorForm : Form
    {

        public bool EnableMultiSelection { set; get; } = false;

        public XmlDataSelectorForm()
        {
            InitializeComponent();
        }                                                                            

        public XmlDataSelectionInfo SelectedItem
        {
            get
            {
                return (m_ucXmlSelectorCtl?.SelectedItem);
            }
        }

        public List<XmlDataSelectionInfo> SelectedItems
        {
            get { return (m_ucXmlSelectorCtl?.SelectedItems); }
        }



        private void HandleFormLoad(object sender, EventArgs e)
        {
            m_ucXmlSelectorCtl.EnableMultiSelection = this.EnableMultiSelection;
            m_ucXmlSelectorCtl.DoubleClickItemEvent += HandleTreeDoubleClick;
        }

        private void HandleTreeDoubleClick(object sender, EventArgs e)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                this.Close();
            }
            catch(Exception exc)
            {
                Util.HandleExc(this, fn, exc);
            }
        }

        public virtual bool FillXmlFromString(string sXml)
        {
            return(m_ucXmlSelectorCtl.FillXmlFromString(sXml));
        }

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void m_btnCancel_Click(object sender, EventArgs e)
        {
            m_ucXmlSelectorCtl.SelectedItem = null;
            m_ucXmlSelectorCtl.SelectedItems = new List<XmlDataSelectionInfo>();
            this.Close();

        }
    }
}
