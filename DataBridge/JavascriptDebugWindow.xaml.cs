using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
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
        public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;
        public BackgroundWorker UpdateWorker { get; set; } = new BackgroundWorker();

        public JavascriptDebugWindow()
        {
            InitializeComponent();
            UpdateWorker.WorkerSupportsCancellation = true;
            UpdateWorker.DoWork += UpdateWorker_DoWork;
        }

        private void UpdateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while ((DateTime.Now - LastUpdateTime).TotalMilliseconds < 100) {
                Thread.Sleep(100);
            }

            try
            {
                SetOutputText(this, DebugOutput.ToString());
                LastUpdateTime = DateTime.Now;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public void SetOutputText(object sender, string outputText)
        {
            const string fn = "JavascriptDebugWindow.SetOutputText()";
            try
            {


                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new Action(() => { SetOutputText(sender, outputText); }));
                    return;
                }

                int caretPosition = DebugOutputTextBox.CaretOffset;
                bool resetCaret = false;
                if (caretPosition >= (DebugOutputTextBox.Text.Length - 1))
                {
                    resetCaret = true;
                }

                DebugOutputTextBox.Text = outputText;
                if (resetCaret)
                {
                    DebugOutputTextBox.CaretOffset = outputText.Length - 1;
                    DebugOutputTextBox.ScrollToEnd();
                }
            }
            catch(Exception exc)
            {
                LogHelper.LogException(exc);
            }
        }

        public void OnMessageReceived(object sender, string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { OnMessageReceived(sender, message); }));
                return;
            }

            Console.WriteLine("JSDBG: " + message);
            DebugOutput.Append(message);
            if (!UpdateWorker.IsBusy)
            {
                UpdateWorker.RunWorkerAsync();
            }
        }

        private void HandleTextboxRightClick(object sender, MouseButtonEventArgs e)
        {
            const string fn = "JavascriptDebugWindow.HandleTextboxRightClick()";
            try
            {
                // should do this with a right click menu item, but need time to figure out how to do that in WPF
                DebugOutput.Clear();
                DebugOutputTextBox.Text = DebugOutput.ToString();
            }
            catch(Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Exception clearing log output", ex);
            }
        }

        private void HandleFormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateWorker.CancelAsync();
            Hide();
            e.Cancel = true;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                DebugOutputTextBox.Text = DebugOutput.ToString();
            }
        }
    }
}
