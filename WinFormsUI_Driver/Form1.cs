using Levrum.Utils.Infra;
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
using CoeloUtils.UiForms;
using RandD.PumpAndPipeSketch;
using AnalysisFramework.Model.Computation;
using C3m.Model;

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

                FileInfo fi = new FileInfo(sfile);
                DateTime fdtm = fi.CreationTime;
                string sfname = fi.Name;

                string scaption = string.Format("{0:D4}-{1:D2}-{2:D2}", fdtm.Year, fdtm.Month, fdtm.Day) +
                    "  " + sfname;


                form.Text = scaption;
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

        private void m_btnPumpAndPipeDemo_Click(object sender, EventArgs e)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                //CoeloUtils.RepositoryCache cache = new CoeloUtils.RepositoryCache();


                //GenericObjectTreeForm form = new CoeloUtils.UiForms.GenericObjectTreeForm();
                //form.Text = form.Text = "Pump and Pipe Demo";
                //form.SubjectType = typeof(Dashboard);
                //form.Cache = cache;
                //form.ShowDialog();

                //OpenFileDialog ofd = new OpenFileDialog();
                //ofd.Title = "Please select the .Net assembly for which to get dynamic calc info";
                //ofd.Filter = ".Net assemblies|*.dll";
                //ofd.ShowDialog();
                //string sfile = ofd.FileName;
                //if (string.IsNullOrEmpty(sfile)) { return; }
                //Assembly assembly = Assembly.LoadFile(sfile);

                Assembly assembly1 = typeof(DynamicCalcComputation).Assembly;    // gets examples built into the Levrum project
                List<DynamicCalcComputation> dccs1 = DynamicCalcComputation.WrapDynamicCalcsFromAssembly(assembly1);
                Assembly assembly2 = typeof(C3mStation).Assembly;

                List<DynamicCalcComputation> dccs = DynamicCalcComputation.WrapDynamicCalcsFromAssembly(assembly2);
                dccs1.AddRange(dccs);

                StringBuilder sb = new StringBuilder();
                foreach (DynamicCalcComputation dcc in dccs1)
                {
                    sb.AppendLine(dcc.Prettyprint());
                    sb.AppendLine();
                }

                TextDisplayForm tdf = new TextDisplayForm(sb.ToString());
                tdf.Text = "Dynamic calculations present in assembly " + assembly2.FullName;
                tdf.ShowInFront();



            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                MessageBox.Show("Internal error; please see event log");
            }
        }
    }
}
