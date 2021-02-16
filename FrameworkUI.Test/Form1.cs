using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FrameworkUI.Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void exToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void treeEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (TreeControlTest treeControlTest = new TreeControlTest())
            {
                treeControlTest.ShowDialog();
                Console.WriteLine($"Tree size: {treeControlTest.treeEditorControl1.Tree.Count}");
            }               

        }
    }
}
