using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Levrum.Utils;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for JavascriptDebugWindow.xaml
    /// </summary>
    public partial class JavascriptDebugWindow : Window
    {
        public StringBuilder DebugOutput { get; set; } = new StringBuilder();

        public JavascriptDebugWindow()
        {
            InitializeComponent();
        }

        public void OnMessageReceived(object sender, string message)
        {
            DebugOutput.Append(message);
            DebugOutputTextBox.Text = DebugOutput.ToString();
        }

        private void HandleTextboxRightClick(object sender, MouseButtonEventArgs e)
        {
            const string fn = "JavascriptDebugWindow.HandleTextboxRightClick()";
            try
            {
                DebugOutputTextBox.Text = "";  // should do this with a right click menu item, but need time to figure out how to do that in WPF
            }
            catch(Exception exc)
            {
                MessageBox.Show("Exception: " + exc.Message + "r\n" + exc.StackTrace);
            }
        }
    }
}
