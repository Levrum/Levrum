using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
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

using Newtonsoft.Json;

using Levrum.Utils;

namespace Levrum.UI.WPF
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        public string AppName { get; set; } = string.Empty;
        public Version Version { get; set; } = null;
        public UpdateInfo UpdateInfo { get; protected set; } = null;
        public Thread UpdateThread { get; set; } = null;
        public bool WindowClosing { get; set; } = false;

        public UpdateWindow(string appName, Version version)
        {
            InitializeComponent();
            AppName = appName;
            Version = version;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                checkForUpdate(AppName, Version);
            }
        }

        public void checkForUpdate(string appName, Version version)
        {
            try
            {
                WebClient client = new WebClient();
                string updateServer = "https://updates.levrum.com/";

                string url = string.Format("{0}api/update?app={1}&version={2}", updateServer, appName, version);

                string updates = client.DownloadString(url);
                UpdateInfo = JsonConvert.DeserializeObject<UpdateInfo>(updates);

                if (UpdateInfo != null && !string.IsNullOrWhiteSpace(UpdateInfo.URL))
                {
                    StatusTextBlock.Text = "Update Available!";
                    Title = "Update Available!";

                    MessageBoxResult result = MessageBox.Show(string.Format("An update to version {0} is available. Update now? The application will restart once downloading is complete.", UpdateInfo.Version), "Update Available", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        WindowClosing = true;
                        Close();
                        return;
                    }
                }
                else
                {
                    WindowClosing = true;
                    Close();
                    return;
                }

                UpdateThread = new Thread(downloadUpdate);
                UpdateThread.Start();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception while checking for updates", false);
            }
        }

        public void downloadUpdate()
        {
            DirectoryInfo tempDir = new DirectoryInfo(string.Format("{0}\\Levrum\\Temp\\", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
            if (!tempDir.Exists)
            {
                tempDir.Create();
            }
            Title = "Downloading Update...";
            StatusTextBlock.Text = "Beginning Download...";

            WebClient client = new WebClient();
            FileInfo file = new FileInfo(tempDir.FullName + "\\" + UpdateInfo.FileName);
            client.DownloadFile(new Uri(UpdateInfo.URL), file.FullName);
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            StatusTextBlock.Text = "Download Complete!";
            StatusTextBlock.FontSize = 32;
            Process.Start(file.FullName);
            Application.Current.Shutdown();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            StatusTextBlock.FontSize = 24;
            double remainingMB = (e.TotalBytesToReceive - e.BytesReceived) / (1024 * 1024);
            StatusTextBlock.Text = string.Format("Downloaded {0}% ({1:F2}MB remaining)...", e.ProgressPercentage, remainingMB);
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateThread != null)
            {
                UpdateThread.Abort();
            }
            WindowClosing = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (UpdateThread != null && UpdateThread.IsAlive && !WindowClosing)
            {
                MessageBoxResult result = MessageBox.Show("Closing the window will cancel your update. Would you like to cancel it?", "Cancel update?", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                UpdateThread.Abort();
            }
        }
    }
}
