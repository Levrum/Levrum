using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xaml;



namespace Levrum.UI.WinForms
{
    public partial class XamlHostForm : Form
    {
        public ElementHost Host { get; protected set; } = new ElementHost();

        HostedControl m_childControl = null;
        public HostedControl ChildControl
        {
            get { return m_childControl; }
            set
            {
                if (value != null)
                {
                    m_childControl = value;
                    double height = value.Height;
                    double width = value.Width;

                    Host.Child = value;
                    value.HostForm = this;

                    Height = (int)height;
                    Width = (int)width;

                    FormClosing += value.HostClosing;
                } else if (m_childControl != null)
                {
                    FormClosing -= m_childControl.HostClosing;
                    Host.Child = null;
                    m_childControl.HostForm = null;
                    m_childControl = null;
                }
            }
        }

        public XamlHostForm(HostedControl _childControl = null)
        {
            InitializeComponent();
            Host.Dock = DockStyle.Fill;

            ChildControl = _childControl;

            Controls.Add(Host);
        }
    }
}
