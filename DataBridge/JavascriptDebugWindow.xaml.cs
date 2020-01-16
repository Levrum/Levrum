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
    }
}
