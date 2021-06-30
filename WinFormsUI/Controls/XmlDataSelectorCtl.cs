using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using Levrum.Utils.Infra;
using System.Xml.Linq;
using System.Xml;

namespace Levrum.UI.WinForms.Controls
{

    public enum XmlItemType
    {
        Element,
        Attribute
    }


    public class XmlDataSelectionInfo
    {
        public XmlDataSelectionInfo(string sPath, XmlItemType qItemType = XmlItemType.Element)
        {
            ItemPath = sPath;
            ItemType = qItemType;

        }

        public XmlItemType ItemType = XmlItemType.Element;
        public string ItemPath = "";

        public override string ToString()
        {
            string sret = ItemType.ToString() + " -- " + ItemPath;
            return (sret);
        }
    }


    public partial class XmlDataSelectorCtl : UserControl
    {


        /// <summary>
        /// Does this control support multiple selection?
        /// </summary>
        public bool EnableMultiSelection
        {
            get { return ((null != m_tvXmlContent) ? m_tvXmlContent.CheckBoxes : false); }
            set
            {
                if (null!=m_tvXmlContent) { m_tvXmlContent.CheckBoxes = value; }
            }
        }


        /// <summary>
        /// Single item (element or attribute) selected.
        /// </summary>
        public XmlDataSelectionInfo SelectedItem = null;

        /// <summary>
        /// Multiple selected items.
        /// </summary>
        public List<XmlDataSelectionInfo> SelectedItems = new List<XmlDataSelectionInfo>();

        /// <summary>
        /// Event fired when user double-clicks an item.
        /// </summary>
        public event EventHandler DoubleClickItemEvent;


        public XmlDataSelectorCtl()
        {
            InitializeComponent();
        }

        private void HandleCtlLoad(object sender, EventArgs e)
        {
            m_tvXmlContent.NodeMouseDoubleClick += HandleNodeDoubleClick;
            m_tvXmlContent.CheckBoxes = this.EnableMultiSelection;
        }

        private void HandleNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (!EnableMultiSelection)
                {
                    TreeNode node = e.Node;
                    XmlDataSelectionInfo xdsi = node.Tag as XmlDataSelectionInfo;
                    this.SelectedItems = new List<XmlDataSelectionInfo>();
                    this.SelectedItem = xdsi;
                }
                else
                {
                    this.SelectedItem = null;
                    this.SelectedItems = new List<XmlDataSelectionInfo>();
                    getSelectedXmlNodeInfo(this.SelectedItems, null);
                }
                if (null != DoubleClickItemEvent) { DoubleClickItemEvent(this, new EventArgs()); }
            }
            catch(Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return;
            }
        }

        private bool getSelectedXmlNodeInfo(List<XmlDataSelectionInfo> oList, TreeNode oRootNode = null)
        {
            TreeNodeCollection tnc = null;
            if (null == oRootNode) { tnc = m_tvXmlContent.Nodes; }
            else { tnc = oRootNode.Nodes;  }

            foreach (TreeNode tnode in tnc)
            {
                if (tnode.Checked) 
                {
                    XmlDataSelectionInfo xdsi = tnode.Tag as XmlDataSelectionInfo;
                    if (null!=xdsi) { oList.Add(xdsi); }
                }
                if (!getSelectedXmlNodeInfo(oList, tnode)) { return (false); }
            }
            return (true);
        }

        public virtual bool FillXmlFromString(string sXml)
        { 
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {

                
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sXml);
                XmlElement root = doc.DocumentElement;
                return (FillSubtree(null,root));
            }

            catch(Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        } // end method()

        private bool FillSubtree(TreeNode oParentNode, XmlElement oXmlElement, string sPath = "")
        {
            string fn = "XmlDataSelectorCtl.FillSubtree()";
            try
            {
                string scurtitle = "<" + oXmlElement.Name + ">";
                if (!hasChildElements(oXmlElement)) { scurtitle += " = '" + oXmlElement.InnerText + "'"; }
                TreeNode tncur = new TreeNode(scurtitle);
                tncur.ForeColor = Color.Blue;
                if (null!=oParentNode) { oParentNode.Nodes.Add(tncur); }
                else { m_tvXmlContent.Nodes.Add(tncur); }
                string spath = (null == sPath) ? "" : sPath;
                if (!string.IsNullOrEmpty(spath)) { spath += "."; }
                spath += oXmlElement.Name;
                XmlDataSelectionInfo xdsi = new XmlDataSelectionInfo(spath, XmlItemType.Element);
                tncur.Tag = xdsi;


                foreach(XmlAttribute attribute in oXmlElement.Attributes)
                {
                    TreeNode tnatt = new TreeNode(attribute.Name + " = '" + attribute.Value + "'");
                    tnatt.ForeColor = Color.DarkRed;
                    string sattpath = spath + ":" + attribute.Name;
                    XmlDataSelectionInfo xdsia = new XmlDataSelectionInfo(sattpath, XmlItemType.Attribute);
                    tnatt.Tag = xdsia;
                    tncur.Nodes.Add(tnatt); 
                    
                }

                foreach(XmlNode ochildnode in oXmlElement.ChildNodes)
                {
                    XmlElement ochildelt = ochildnode as XmlElement;
                    if (null==ochildelt) { continue;  }

                    if (!FillSubtree(tncur,ochildelt,spath)) { return (false); }
                }
                return (true);
            }
            catch(Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }

        private bool hasChildElements(XmlElement oXmlElement)
        {
            if (null==oXmlElement) { return (false); }
            foreach (XmlNode node in oXmlElement.ChildNodes)
            {
                XmlElement child_element = node as XmlElement;
                if (null!=child_element) { return (true);  }
            }
            return (false);

        }
    }
}
