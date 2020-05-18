using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Levrum.UI.WinForms
{
    public abstract class HostedControl : UserControl
    {
        public XamlHostForm HostForm { get; set; }

        public HostedControl(XamlHostForm _hostForm = null)
        {
            HostForm = _hostForm;
        }

        public Type ResultType { get { return Result.GetType(); } }

        public virtual object Result { get; set; } = null;
        public virtual void HostClosing(object sender, EventArgs e)
        {

        }
    }
}
