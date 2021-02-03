using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Levrum.Utils;
using Levrum.Utils.Messaging;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            AppId = "Levrum_DataBridge";
            FileTypes = new string[]{ ".dmap" };
        }
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitializeApp();
        }
    }
}
