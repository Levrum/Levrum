using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32;

using AvalonDock;
using AvalonDock.Layout;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Classes.Tools;
using Levrum.Data.Map;

using Levrum.Licensing.Client.WPF;

using Levrum.UI.WPF;

using Levrum.Utils;
using Levrum.Utils.Data;
using Levrum.Utils.Messaging;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainDataBridgeWindow : Window, IDisposable
    {
        public List<DataMapDocument> m_openDocs = new List<DataMapDocument>();

        public DataMapEditor ActiveEditor { get; set; } = null;
        public DataMap Map { get; set; } = null;

        public JavascriptDebugWindow JSDebugWindow { get; set; } = new JavascriptDebugWindow();
        public JsonViewerWindow JsonViewWindow { get; set; } = new JsonViewerWindow();

        public BackgroundWorker Worker { get; set; } = null;

        public MainDataBridgeWindow()
        {
            InitializeComponent();

            Assembly assembly = Assembly.GetExecutingAssembly();
            LicenseClient client = new LicenseClient(assembly, "databridge");
            client.OnLogMessage += LicenseClient_OnLogMessage;
            client.OnException += LicenseClient_OnException;
            client.VerifyOrRequestLicense();

            App app = Application.Current as App;
            if (app != null)
            {
                if (app.StartupFileNames.Count > 0)
                {
                    openDocuments(app.StartupFileNames.ToArray());
                }

                if (app.DebugMode)
                {
                    ViewLogsMenuItem.Visibility = Visibility.Visible;
                }

                app.OnMessageReceived += OnIpcMessage;
            }
            checkForUpdates();
            updateRecentFilesMenu();
            updateToolbarsAndMenus();
        }

        public void Dispose()
        {
            App app = Application.Current as App;
            if (app == null)
            {
                return;
            }
            app.OnMessageReceived -= OnIpcMessage;
        }

        private void OnIpcMessage(IPCMessage message)
        {
            if (message.Type == IPCMessageType.OpenDocument)
            {
                App app = Application.Current as App;
                if (app.HasMutex)
                {
                    if (message.Data is string)
                    {
                        string[] fileName = new string[1];
                        fileName[0] = message.Data as string;
                        Dispatcher.Invoke(() => { openDocuments(fileName); Activate(); });
                    }
                    else if (message.Data is List<string>)
                    {
                        List<string> fileNames = (List<string>)message.Data;
                        Dispatcher.Invoke(() => { openDocuments(fileNames.ToArray()); Activate(); });
                    }
                }
            }
        }

        private void openDocuments(string[] fileNames)
        {
            DataMapDocument firstDocument = null;
            foreach (string fileName in fileNames)
            {
                try
                {
                    var document = OpenDataMap(fileName);
                    if (firstDocument == null)
                    {
                        firstDocument = document;
                    }
                }
                catch (Exception ex)
                {
                    logException(this, string.Format("Unable to open DataMap file '{0}'", fileName), ex);
                }
            }

            if (firstDocument != null)
            {
                int index = DocumentPane.IndexOfChild(firstDocument.Document);
                DocumentPane.SelectedContentIndex = index;
            }
        }

        private void LicenseClient_OnException(Exception ex)
        {
            LogHelper.LogException(ex, "Exception getting license", true);
        }

        private void LicenseClient_OnLogMessage(string message)
        {
            LogHelper.LogMessage(LogLevel.Warn, message, null);
        }

        private void onLoaderProgress(object sender, string message, double progress)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { onLoaderProgress(sender, message, progress); }));
                return;
            }

            if (double.IsNaN(progress))
            {
                StatusBarProgress.IsIndeterminate = true;
                StatusBarText.Text = message;
            }
            else
            {
                StatusBarProgress.IsIndeterminate = false;
                StatusBarProgress.Value = progress;
                StatusBarText.Text = message;
            }
        }

        private void resetLoaderProgress()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { resetLoaderProgress(); }));
                return;
            }

            StatusBarProgress.IsIndeterminate = false;
            StatusBarProgress.Value = 0;
            StatusBarText.Text = "Ready";
        }

        private void logException(object sender, string message, Exception ex)
        {
            LogHelper.LogMessage(LogLevel.Error, message, ex);
        }

        private void checkForUpdates()
        {
            try
            {
                Version version = Assembly.GetEntryAssembly().GetName().Version;
#if DEBUG
                version = new Version(version.Major, version.Minor, version.Build, 9999);
#endif
                UpdateWindow updateWindow = new UpdateWindow("databridge", version);
                updateWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception while checking for updates", false);
            }
        }

        public DataMapDocument OpenDataMap(string fileName)
        {
            try
            {
                DataMapDocument mapDocument = (from DataMapDocument d in m_openDocs
                                               where d.Map.Path == fileName
                                               select d).FirstOrDefault();

                if (mapDocument != null)
                {
                    mapDocument.Document.IsActive = true;
                    addRecentFile(fileName);
                    return mapDocument;
                }

                FileInfo file = new FileInfo(fileName);
                DataMap map = JsonConvert.DeserializeObject<DataMap>(File.ReadAllText(fileName), new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented });
                map.Name = file.Name;
                map.Path = fileName;

                LayoutDocument document = new LayoutDocument();
                document.Title = map.Name;
                DataMapEditor editor = new DataMapEditor(map, this);
                ActiveEditor = editor;
                document.Content = editor;
                var firstPane = DocumentPaneGroup.Children[0];
                if (firstPane is LayoutDocumentPane)
                {
                    LayoutDocumentPane docPane = firstPane as LayoutDocumentPane;
                    docPane.Children.Add(document);
                }

                mapDocument = new DataMapDocument(map, document);
                m_openDocs.Add(mapDocument);
                document.IsActive = true;
                addRecentFile(fileName);
                return mapDocument;
            }
            catch (Exception ex)
            {
                logException(this, "Unable to open DataMap", ex);
                return null;
            }
        }

        public void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
                DataMapEditor editor = document.Content as DataMapEditor;
                ActiveEditor = null;
                DataMap map = editor.DataMap;
                DataMapDocument openDocument = (from DataMapDocument d in m_openDocs
                                                where d.Map == map
                                                select d).FirstOrDefault();

                if (openDocument.ChangesMade)
                {
                    MessageBoxResult result = MessageBox.Show(string.Format("Save {0} before closing?", map.Name), "Save File?", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        if (!SaveMap(map))
                        {
                            return;
                        }
                    }
                }


                if (openDocument != null)
                {
                    DocumentPane.Children.Remove(document);
                    m_openDocs.Remove(openDocument);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Error closing DataMap", ex);
            }
        }

        public void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
                DataMapEditor editor = document.Content as DataMapEditor;
                DataMap map = editor.DataMap;
                SaveMap(map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Error saving DataMap", ex);
            }
        }

        public bool SaveMap(DataMap map, bool forceSaveAs = false)
        {
            try
            {
                DataMapDocument document = (from DataMapDocument d in m_openDocs
                                            where d.Map == map
                                            select d).FirstOrDefault();

                if (forceSaveAs || string.IsNullOrEmpty(map.Path))
                {
                    DirectoryInfo di;
                    SaveFileDialog sfd = new SaveFileDialog();

                    if (string.IsNullOrEmpty(map.Path))
                    {
                        di = new DirectoryInfo(string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Levrum\\Data Maps"));
                        di.Create();
                    }
                    else
                    {
                        FileInfo lastFile = new FileInfo(map.Path);
                        di = lastFile.Directory;
                        sfd.FileName = lastFile.Name;
                    }

                    sfd.InitialDirectory = di.FullName;
                    sfd.DefaultExt = "dmap";
                    sfd.Filter = "Levrum DataMap files (*.dmap)|*.dmap|All files (*.*)|*.*";
                    if (sfd.ShowDialog() == false)
                    {
                        return false;
                    }
                    FileInfo file = new FileInfo(sfd.FileName);
                    map.Path = sfd.FileName;
                    map.Name = file.Name;

                    if (document != null)
                    {
                        document.Document.Title = file.Name;
                    }
                }

                string mapJson = JsonConvert.SerializeObject(map, Formatting.Indented, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
                File.WriteAllText(map.Path, mapJson);
                if (document != null)
                {
                    SetChangesMade(map, false);
                }

                SaveAsMenuItem.Header = string.Format("Save {0} _As...", map.Name);
                SaveMenuItem.Header = string.Format("_Save {0}", map.Name);
                addRecentFile(map.Path);
                return true;
            }
            catch (Exception ex)
            {
                logException(this, "Error saving DataMap", ex);
                return false;
            }
        }

        private void addRecentFile(string fileName)
        {
            List<string> recentFiles = getRecentFiles();
            int lastIndex = recentFiles.IndexOf(fileName);
            if (lastIndex == -1)
            {
                recentFiles.Insert(0, fileName);
                if (recentFiles.Count > 10)
                {
                    recentFiles.RemoveAt(10);
                }
            }
            else
            {
                recentFiles.RemoveAt(lastIndex);
                recentFiles.Insert(0, fileName);
            }

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Levrum\DataMap\RecentFiles");
            for (int i = 0; i < recentFiles.Count; i++)
            {
                key.SetValue(i.ToString(), recentFiles[i]);
            }

            updateRecentFilesMenu();
        }

        private List<string> getRecentFiles()
        {
            List<string> output = new List<string>();

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Levrum\DataMap\RecentFiles");
            for (int i = 0; i < 10; i++)
            {
                string path = key.GetValue(i.ToString()) as string;
                if (!string.IsNullOrEmpty(path))
                {
                    output.Add(path);
                }
            }

            return output;
        }

        private void updateRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();

            List<string> recentFiles = getRecentFiles();
            int index = 1;
            for (int i = 0; i < recentFiles.Count; i++)
            {
                string path = recentFiles[i];
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                {
                    MenuItem menuItem = new MenuItem();
                    string usablePath = file.FullName;
                    if (file.FullName.Length > 50)
                    {
                        var pathSegment = file.FullName.Substring(file.FullName.Length - 50, 50);
                        usablePath = string.Format("...{0}", pathSegment.Substring(pathSegment.IndexOf('\\')));
                    }
                    if (usablePath.StartsWith(file.FullName.Substring(0, 3)))
                    {
                        usablePath = usablePath.Substring(3);
                    }
                    string shortcut = index != 10 ? string.Format("_{0}", index) : "1_0";
                    menuItem.Header = string.Format("{0}: {1}{2}", shortcut, file.FullName.Substring(0, 3).ToUpper(), usablePath);
                    menuItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        OpenDataMap(file.FullName);
                    };
                    RecentFilesMenu.Items.Add(menuItem);
                    index++;
                }
            }
        }

        public void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DocumentPane_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedContentIndex")
            {
                updateToolbarsAndMenus();
            }
        }

        public void EnableControls()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { EnableControls(); }));
                return;
            }
            else

            Cursor = Cursors.Arrow;
            toggleControls(false);
        }

        public void DisableControls()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { DisableControls(); }));
                return;
            }
            else

            Cursor = Cursors.Wait;
            toggleControls(true);
        }

        private void toggleControls(bool runningOperation)
        {
            bool controlsEnabled = !runningOperation;

            NewMenuItem.IsEnabled = controlsEnabled;
            OpenMenuItem.IsEnabled = controlsEnabled;
            SaveMenuItem.IsEnabled = controlsEnabled;
            SaveAsMenuItem.IsEnabled = controlsEnabled;
            CreateCallResponseCSVsMenuItem.IsEnabled = controlsEnabled;
            CreateIncidentJsonMenuItem.IsEnabled = controlsEnabled;
            ToolsMenu.IsEnabled = controlsEnabled;
            PropertiesMenu.IsEnabled = controlsEnabled;
            DockingManager.IsEnabled = controlsEnabled;

            EditPhaseOneScript.IsEnabled = controlsEnabled;
            EditPhaseTwoScript.IsEnabled = controlsEnabled;
            EditPhaseThreeScript.IsEnabled = controlsEnabled;

            NewButton.IsEnabled = controlsEnabled;
            OpenButton.IsEnabled = controlsEnabled;
            SaveButton.IsEnabled = controlsEnabled;
            CreateJsonButton.IsEnabled = controlsEnabled;
            CreateCsvButton.IsEnabled = controlsEnabled;
            InvertLatitudeButton.IsEnabled = controlsEnabled;
            InvertLongitudeButton.IsEnabled = controlsEnabled;
            ToggleRestorePrecisionButton.IsEnabled = controlsEnabled;
            EditProjectionButton.IsEnabled = controlsEnabled;
            ConvertCoordinateButton.IsEnabled = controlsEnabled;
            EditPostProcessingButton.IsEnabled = controlsEnabled;
            EditCauseTreeButton.IsEnabled = controlsEnabled;
            ToggleTransportAsClearSceneButton.IsEnabled = controlsEnabled;

            StopButton.IsEnabled = runningOperation;
            Uri uri;
            if (runningOperation)
            {
                uri = new Uri(@"/DataBridge;component/Resources/StopIconRed.png", UriKind.Relative);                
            } else
            {
                uri = new Uri(@"/DataBridge;component/Resources/StopIcon.png", UriKind.Relative);
            }
            StopButtonImage.Source = new System.Windows.Media.Imaging.BitmapImage(uri);
        }

        public void updateToolbarsAndMenus()
        {
            try
            {
                LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
                DataMapEditor editor = null;
                DataMap map = null;
                bool documentOpen = false;
                if (document != null)
                {
                    editor = document.Content as DataMapEditor;
                    ActiveEditor = editor;
                    if (editor != null)
                    {
                        map = editor.DataMap;
                        if (map != null)
                        {
                            documentOpen = true;
                        }
                    }
                }

                SaveAsMenuItem.IsEnabled = documentOpen;
                SaveMenuItem.IsEnabled = documentOpen;
                CloseMenuItem.IsEnabled = documentOpen;
                CreateIncidentJsonMenuItem.IsEnabled = documentOpen;
                CreateCallResponseCSVsMenuItem.IsEnabled = documentOpen;
                CoordinateConversionMenuItem.IsEnabled = documentOpen;
                EditProjectionMenuItem.IsEnabled = documentOpen;
                ToggleInvertLatitudeMenuItem.IsEnabled = documentOpen;
                ToggleInvertLongitudeMenuItem.IsEnabled = documentOpen;
                ToggleRestorePrecisionMenuItem.IsEnabled = documentOpen;
                ToggleTransportAsClearSceneMenuItem.IsEnabled = documentOpen;

                EditCauseTreeMenuItem.IsEnabled = documentOpen;
                ScriptsMenu.IsEnabled = documentOpen;

                SaveButton.IsEnabled = documentOpen;
                CreateJsonButton.IsEnabled = documentOpen;
                CreateCsvButton.IsEnabled = documentOpen;
                ConvertCoordinateButton.IsEnabled = documentOpen;
                InvertLatitudeButton.IsEnabled = documentOpen;
                InvertLongitudeButton.IsEnabled = documentOpen;
                ToggleRestorePrecisionButton.IsEnabled = documentOpen;
                EditProjectionButton.IsEnabled = documentOpen;
                EditCauseTreeButton.IsEnabled = documentOpen;
                EditPostProcessingButton.IsEnabled = documentOpen;
                ToggleTransportAsClearSceneButton.IsEnabled = documentOpen;

                if (documentOpen)
                {
                    SaveAsMenuItem.Header = string.Format("Save {0} _As...", map.Name);
                    SaveMenuItem.Header = string.Format("_Save {0}", map.Name);
                    Map = map;

                    updateCoordinateConversionControls();
                    updateInvertLatitude();
                    updateInvertLongitude();
                    updateRestorePrecision();
                    updateTransportAsClearScene();
                }
                else
                {
                    SaveAsMenuItem.Header = "Save _As...";
                    SaveMenuItem.Header = "_Save";
                    Map = null;

                    InvertLatitudeButton.IsChecked = false;
                    InvertLongitudeButton.IsChecked = false;
                    ToggleRestorePrecisionButton.IsChecked = false;
                    ToggleTransportAsClearSceneButton.IsChecked = false;
                    ConvertCoordinateButton.IsChecked = false;
                }

                // DataSources.IsEnabled = documentOpen;
            }
            catch (Exception ex)
            {
                logException(this, "Unable to update menus", ex);
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        public void SetChangesMade(DataMap map, bool status)
        {
            DataMapDocument document = (from DataMapDocument d in m_openDocs
                                        where d.Map == map
                                        select d).FirstOrDefault();

            if (document != null)
            {
                document.ChangesMade = status;
            }
        }

        public DataMapDocument GetDocumentForMap(DataMap map)
        {
            return (from DataMapDocument d in m_openDocs
                    where d.Map == map
                    select d).FirstOrDefault();
        }

        private void DockingManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            try
            {
                LayoutDocument document = e.Document;
                DataMapEditor editor = document.Content as DataMapEditor;
                DataMap map = editor.DataMap;
                DataMapDocument openDocument = (from DataMapDocument d in m_openDocs
                                                where d.Map == map
                                                select d).FirstOrDefault();

                if (openDocument.ChangesMade)
                {
                    MessageBoxResult result = MessageBox.Show(string.Format("Save {0} before closing?", map.Name), "Save File?", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        if (!SaveMap(map))
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                m_openDocs.Remove(openDocument);
            }
            catch (Exception ex)
            {
                logException(this, "Error on document close", ex);
            }
        }

        private void ShowJSDebugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            JSDebugWindow.Owner = this;
            JSDebugWindow.Show();
            JSDebugWindow.BringIntoView();
        }

        private void ConvertJsonToCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                ofd.Title = "Select Incident JSON File";
                ofd.DefaultExt = "json";
                ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (ofd.ShowDialog() == false)
                {
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                sfd.Title = "Save Incident CSV";
                sfd.DefaultExt = "csv";
                sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (sfd.ShowDialog() == false)
                {
                    return;
                }
                string incidentCsvFileName = sfd.FileName;

                FileInfo csvFileInfo = new FileInfo(incidentCsvFileName);
                sfd.Title = "Save Response CSV";

                string fn = csvFileInfo.Name.Substring(0, csvFileInfo.Name.Length - 4);
                if (fn.ToLowerInvariant().EndsWith("incidents"))
                {
                    fn = fn.Substring(0, fn.Length - 9);
                }

                sfd.FileName = string.Format("{0} Responses.csv", fn);
                if (sfd.ShowDialog() == false)
                {
                    return;
                }
                string responseCsvFileName = sfd.FileName;

                Worker = new BackgroundWorker();
                Worker.WorkerSupportsCancellation = true;
                Worker.DoWork += new DoWorkEventHandler((object otherSender, DoWorkEventArgs args) =>
                {
                    try
                    {
                        LogHelper.LogMessage(LogLevel.Info, string.Format("Converting JSON {0} to CSVs {1} and {2}", ofd.FileName, incidentCsvFileName, responseCsvFileName));
                        DisableControls();
                        onLoaderProgress(otherSender, "Loading JSON", double.NaN);
                        string incidentJson = File.ReadAllText(ofd.FileName);
                        DataSet<IncidentData> incidents = DataSet<IncidentData>.Deserialize(ofd.FileName);
                        onLoaderProgress(otherSender, "Creating CSVs", double.NaN);
                        IncidentDataTools.CreateCsvs(incidents, incidentCsvFileName, responseCsvFileName);
                        resetLoaderProgress();
                        MessageBox.Show(string.Format("Incidents saved as CSV files '{0}' and '{1}'", incidentCsvFileName, responseCsvFileName), "Incidents Saved");
                    }
                    catch (Exception ex)
                    {
                        logException(this, "Unable to convert Incident JSON to CSV", ex);
                    }
                });
                Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((object obj, RunWorkerCompletedEventArgs args) =>
                {
                    try
                    {
                        EnableControls();
                        LogHelper.LogMessage(LogLevel.Info, "Finished converting JSON to CSVs");
                    }
                    catch (Exception ex)
                    {
                        logException(obj, "Error on work complete", ex);
                    }
                });
                Worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                logException(sender, "Error converting JSON to CSV", ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (Worker != null && Worker.IsBusy)
                {
                    MessageBoxResult result = MessageBox.Show("A data processing operation is in progress. Cancel and exit?", "Cancel operation?", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                    Worker.CancelAsync();
                }
                foreach (DataMapDocument doc in m_openDocs)
                {
                    if (doc.ChangesMade)
                    {
                        MessageBoxResult result = MessageBox.Show(string.Format("Save changes to '{0}' before closing?", doc.Map.Name), "Save Changes?", MessageBoxButton.YesNoCancel);
                        if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                        else if (result == MessageBoxResult.Yes)
                        {
                            SaveMap(doc.Map, false);
                        }
                    }
                }
                JSDebugWindow.Close();
            }
            catch (Exception ex)
            {
                logException(sender, "Error on application shutdown", ex);
            }
            finally
            {
                if (e.Cancel == false)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NewDataMapWindow window = new NewDataMapWindow();
                window.Owner = this;
                window.ShowDialog();

                if (window.Result == null)
                {
                    return;
                }

                LayoutDocument document = new LayoutDocument();
                DataMap newMap = window.Result;
                string baseTitle = newMap.Name.Substring(0, newMap.Name.Length - 5);
                string title = newMap.Name;
                int counter = 1;
                bool titleInUse = true;
                while (titleInUse)
                {
                    titleInUse = false;
                    foreach (DataMapDocument mapDocument in m_openDocs)
                    {
                        if (mapDocument.Map.Name == title)
                        {
                            titleInUse = true;
                        }
                    }
                    if (titleInUse)
                    {
                        title = string.Format("{0} {1}.dmap", baseTitle, counter);
                        counter++;
                    }
                }

                newMap.Name = title;
                document.Title = title;
                DataMapEditor editor = new DataMapEditor(newMap, this);
                ActiveEditor = editor;
                document.Content = editor;

                var firstPane = DocumentPaneGroup.Children[0];
                if (firstPane is LayoutDocumentPane)
                {
                    LayoutDocumentPane docPane = firstPane as LayoutDocumentPane;
                    docPane.Children.Add(document);
                }
                
                DataMapDocument newDocument = new DataMapDocument(newMap, document);
                m_openDocs.Add(newDocument);
                document.IsActive = true;
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to create new DataMap", ex);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Levrum\\Data Maps"));
                di.Create();

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = di.FullName;
                ofd.DefaultExt = "dmap";
                ofd.Filter = "Levrum DataMap files (*.dmap)|*.dmap|All files (*.*)|*.*";
                if (ofd.ShowDialog() == true)
                {
                    OpenDataMap(ofd.FileName);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to open DataMap", ex);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
                DataMapEditor editor = document.Content as DataMapEditor;
                DataMap map = editor.DataMap;
                SaveMap(map);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to save DataMap", ex);
            }
        }



        private void HandleShowLog(object oSrc, RoutedEventArgs oArgs)
        {
            string slog = LogHelper.PrettyprintLogEntries(30);
            MessageBox.Show(slog);
        }

        private void HandleClearLog(object oSrc, RoutedEventArgs oArgs)
        {
            LogHelper.ClearLogEntries();
        }

        private void CreateJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                FileInfo file;
                if (Map.Data.ContainsKey("LastJsonExport"))
                {
                    file = new FileInfo(Map.Data["LastJsonExport"] as string);
                    if (file.Exists)
                    {
                        sfd.InitialDirectory = file.DirectoryName;
                        sfd.FileName = file.Name;
                    }
                }
                else
                {
                    sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                }

                sfd.DefaultExt = "json";
                sfd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                sfd.OverwritePrompt = false;
                if (sfd.ShowDialog() == false)
                {
                    return;
                }
                file = new FileInfo(sfd.FileName);

                bool appendData = false;
                if (file.Exists)
                {
                    MessageBoxResult result = MessageBox.Show("Do you want to append to this existing DataSet if possible?", "Append to DataSet?", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Cancel)
                        return;

                    appendData = result == MessageBoxResult.Yes;
                }

                Cursor = Cursors.Wait;
                if (!Map.Data.ContainsKey("LastJsonExport") || Map.Data["LastJsonExport"] as string != sfd.FileName)
                {
                    Map.Data["LastJsonExport"] = sfd.FileName;

                    SetChangesMade(Map, true);
                }
                LogHelper.LogMessage(LogLevel.Info, string.Format("Creating JSON {0} from DataMap {1}", sfd.FileName, Map.Name));

                MapLoader loader = new MapLoader();
                loader.OnProgressUpdate += onLoaderProgress;
                loader.DebugHost.OnDebugMessage += JSDebugWindow.OnMessageReceived;
                Worker = new BackgroundWorker();
                Worker.WorkerSupportsCancellation = true;
                loader.Worker = Worker;
                Worker.DoWork += new DoWorkEventHandler((object newSender, DoWorkEventArgs args) =>
                {
                    try
                    {
                        DisableControls();

                        DataSet<IncidentData> lastData;
                        if (appendData)
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("Attempting to append incidents to existing DataSet"));
                            try
                            {
                                onLoaderProgress(this, "Loading previous DataSet", double.NaN);
                                lastData = DataSet<IncidentData>.Deserialize(sfd.FileName);
                                loader.LoadMapAndAppend(Map, lastData);
                            } 
                            catch (Exception ex)
                            {
                                appendData = false;
                                logException(newSender, "Unable to load previous DataSet", ex);
                                loader.LoadMap(Map);
                            }
                        } else
                        {
                            loader.LoadMap(Map);
                        }

                        onLoaderProgress(this, "Generating and saving JSON", double.NaN);

                        GC.Collect();
                        if (Worker.CancellationPending)
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("Cancelled JSON creation at user request"));
                            return;
                        }

                        loader.Incidents.Serialize(sfd.FileName);

                        MessageBox.Show(string.Format("Incidents saved as JSON file '{0}'", file.Name));
                    }
                    catch (Exception ex)
                    {
                        logException(newSender, "Unable to create JSON", ex);
                    }
                });
                Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((object obj, RunWorkerCompletedEventArgs args) =>
                {
                    try
                    {
                        loader.DebugHost.OnDebugMessage -= JSDebugWindow.OnMessageReceived;
                        loader.Incidents.Clear();
                        loader.IncidentsById.Clear();
                        if (!loader.Cancelling())
                        {
                            LogHelper.LogMessage(LogLevel.Info, "Finished creating JSON");
                        } else
                        {
                            LogHelper.LogMessage(LogLevel.Info, "JSON creation cancelled");
                        }
                        GC.Collect();
                        EnableControls();
                        resetLoaderProgress();
                    }
                    catch (Exception ex)
                    {
                        logException(obj, "Error on work complete", ex);
                    }
                });

                Worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to create JSON", ex);
            }
        }

        private void CreateCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                if (Map.Data.ContainsKey("LastCsvIncidentExport"))
                {
                    FileInfo file = new FileInfo(Map.Data["LastCsvIncidentExport"] as string);
                    if (file.Exists)
                    {
                        sfd.InitialDirectory = file.DirectoryName;
                        sfd.FileName = file.Name;
                    }
                }
                else
                {
                    sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                }

                sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                sfd.Title = "Save Incident CSV";
                sfd.DefaultExt = "csv";
                sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (sfd.ShowDialog() == false)
                {
                    return;
                }

                string incidentCsvFileName = sfd.FileName;

                sfd.Title = "Save Response CSV";
                if (Map.Data.ContainsKey("LastCsvReponseExport"))
                {
                    FileInfo file = new FileInfo(Map.Data["LastCsvResponseExport"] as string);
                    if (file.Exists)
                    {
                        sfd.InitialDirectory = file.DirectoryName;
                        sfd.FileName = Map.Data["LastCsvResponseExport"] as string;
                    }
                }
                else
                {
                    FileInfo csvFileInfo = new FileInfo(incidentCsvFileName);
                    string fn = csvFileInfo.Name.Substring(0, csvFileInfo.Name.Length - 4);
                    if (fn.ToLowerInvariant().EndsWith("incidents"))
                    {
                        fn = fn.Substring(0, fn.Length - 9).Trim();
                    }

                    sfd.FileName = string.Format("{0} Responses.csv", fn);
                }
                if (sfd.ShowDialog() == false)
                {
                    return;
                }
                Cursor = Cursors.Wait;
                string responseCsvFileName = sfd.FileName;
                if (!Map.Data.ContainsKey("LastCsvIncidentExport") || Map.Data["LastCsvIncidentExport"] as string != incidentCsvFileName ||
                    !Map.Data.ContainsKey("LastCsvResponseExport") || Map.Data["LastCsvResponseExport"] as string != responseCsvFileName)
                {
                    Map.Data["LastCsvIncidentExport"] = incidentCsvFileName;
                    Map.Data["LastCsvResponseExport"] = responseCsvFileName;

                    SetChangesMade(Map, true);
                }

                LogHelper.LogMessage(LogLevel.Info, string.Format("Creating CSVs {0} and {1} from DataMap {2}", incidentCsvFileName, responseCsvFileName, Map.Name));

                MapLoader loader = new MapLoader();
                loader.OnProgressUpdate += onLoaderProgress;
                loader.DebugHost.OnDebugMessage += JSDebugWindow.OnMessageReceived;
                Worker = new BackgroundWorker();
                Worker.WorkerSupportsCancellation = true;
                loader.Worker = Worker;
                Worker.DoWork += new DoWorkEventHandler((object obj, DoWorkEventArgs args) =>
                {
                    try
                    {
                        DisableControls();
                        loader.LoadMap(Map);

                        if (Worker.CancellationPending)
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("Cancelled CSV creation at user request"));
                            return;
                        }

                        GC.Collect();
                        onLoaderProgress(this, "Generating and saving CSVs", double.NaN);
                        IncidentDataTools.CreateCsvs(loader.Incidents, incidentCsvFileName, responseCsvFileName);
                        MessageBox.Show(string.Format("Incidents saved as CSV files '{0}' and '{1}'", incidentCsvFileName, responseCsvFileName));
                    }
                    catch (Exception ex)
                    {
                        logException(obj, "Unable to create CSV files", ex);
                    }
                });
                Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((object obj, RunWorkerCompletedEventArgs args) =>
                {
                    try
                    {
                        loader.DebugHost.OnDebugMessage -= JSDebugWindow.OnMessageReceived;
                        loader.Incidents.Clear();
                        loader.IncidentsById.Clear();
                        if (!loader.Cancelling())
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("Finished creating CSVs"));
                        } else
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("CSV creation cancelled"));
                        }
                        GC.Collect();
                        EnableControls();
                        resetLoaderProgress();
                    }
                    catch (Exception ex)
                    {
                        logException(obj, "Error on work complete", ex);
                    }
                });
                Worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to create CSV files", ex);
            }
        }

        private void ToggleConvertCoordinate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Map.EnableCoordinateConversion = !Map.EnableCoordinateConversion;
                updateCoordinateConversionControls();

                SetChangesMade(Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle coordinate conversion", ex);
            }
        }

        private void updateCoordinateConversionControls()
        {
            if (Map.EnableCoordinateConversion)
            {
                CoordinateConversionMenuItem.Header = "_Coordinate Conversion Enabled";
                CoordinateConversionMenuItem.IsChecked = true;
                ConvertCoordinateButton.IsChecked = true;
            }
            else
            {
                CoordinateConversionMenuItem.Header = "_Coordinate Conversion Disabled";
                CoordinateConversionMenuItem.IsChecked = false;
                ConvertCoordinateButton.IsChecked = false;
            }
        }

        private void EditProjection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string projection = null;
                if (!string.IsNullOrWhiteSpace(Map.Projection))
                {
                    projection = Map.Projection;
                }
                TextInputDialog dialog = new TextInputDialog("Define Projection", "Projection:", projection);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    Map.Projection = dialog.Result;
                    SetChangesMade(Map, true);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to edit projection", ex);
            }
        }

        private void ToggleInvertLatitude_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Map.InvertLatitude = !Map.InvertLatitude;
                updateInvertLatitude();

                SetChangesMade(Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle invert latitude", ex);
            }
        }

        private void updateInvertLatitude()
        {
            if (Map.InvertLatitude)
            {
                ToggleInvertLatitudeMenuItem.Header = "Invert _Latitude Enabled";
                ToggleInvertLatitudeMenuItem.IsChecked = true;
                InvertLatitudeButton.IsChecked = true;
            }
            else
            {
                ToggleInvertLatitudeMenuItem.Header = "Invert _Latitude Disabled";
                ToggleInvertLatitudeMenuItem.IsChecked = false;
                InvertLatitudeButton.IsChecked = false;
            }
        }

        private void ToggleInvertLongitude_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Map.InvertLongitude = !Map.InvertLongitude;
                updateInvertLongitude();

                SetChangesMade(Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle invert longitude", ex);
            }
        }

        private void updateInvertLongitude()
        {
            if (Map.InvertLongitude)
            {
                ToggleInvertLongitudeMenuItem.Header = "Invert Lon_gitude Enabled";
                ToggleInvertLongitudeMenuItem.IsChecked = true;
                InvertLongitudeButton.IsChecked = true;
            }
            else
            {
                ToggleInvertLongitudeMenuItem.Header = "Invert Lon_gitude Disabled";
                ToggleInvertLongitudeMenuItem.IsChecked = false;
                InvertLongitudeButton.IsChecked = false;
            }
        }

        private void ToggleTransportAsClearScene_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Map.TransportAsClearScene = !Map.TransportAsClearScene;
                updateTransportAsClearScene();

                SetChangesMade(Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle transport as clearscene", ex);
            }
        }

        private void updateTransportAsClearScene()
        {
            if (Map.TransportAsClearScene)
            {
                ToggleTransportAsClearSceneMenuItem.Header = "_Transport As ClearScene Enabled";
                ToggleTransportAsClearSceneMenuItem.IsChecked = true;
                ToggleTransportAsClearSceneButton.IsChecked = true;
            }
            else
            {
                ToggleTransportAsClearSceneMenuItem.Header = "_Transport As ClearScene Disabled";
                ToggleTransportAsClearSceneMenuItem.IsChecked = false;
                ToggleTransportAsClearSceneButton.IsChecked = false;
            }
        }

        private void ToggleRestorePrecision_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Map.RestorePrecision == -1)
                {
                    SingleValueForm svf = new SingleValueForm("Configure Restore Precision", "Number of digits of precision to restore");
                    svf.Numeric = true;
                    svf.ShowDialog();
                    if (svf.DialogResult == false)
                    {
                        return;
                    }
                    Map.RestorePrecision = int.Parse(svf.Text);
                } else
                {
                    Map.RestorePrecision = -1;
                }
                updateRestorePrecision();

                SetChangesMade(Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle restore precision", ex);
            }
        }

        private void updateRestorePrecision()
        {
            if (Map.RestorePrecision == -1)
            {
                ToggleRestorePrecisionMenuItem.Header = "_Restore Precision Disabled";
                ToggleRestorePrecisionMenuItem.IsChecked = false;
                ToggleRestorePrecisionButton.IsChecked = false;
                ToggleRestorePrecisionButton.ToolTip = "Toggle Restore Precision";
            } else
            {
                ToggleRestorePrecisionMenuItem.Header = "_Restore Precision Enabled";
                ToggleRestorePrecisionMenuItem.IsChecked = true;
                ToggleRestorePrecisionButton.IsChecked = true;
                ToggleRestorePrecisionButton.ToolTip = string.Format("Restoring {0} digits of precision", Map.RestorePrecision);
            }
        }

        private void EditCauseTree_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<ICategoryData> tree = new List<ICategoryData>();
                foreach (CauseData cause in Map.CauseTree)
                {
                    tree.Add(cause);
                }
                TreeEditorWindow window = new TreeEditorWindow(tree);
                window.ShowDialog();
                if (window.DialogResult == true)
                {
                    List<ICategoryData> result = window.Result;
                    List<CauseData> causeTree = new List<CauseData>();
                    foreach (ICategoryData item in result)
                    {
                        causeTree.Add(CauseData.ConvertICategoryData(item));
                    }

                    Map.CauseTree = causeTree;
                    SetChangesMade(Map, true);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to edit Cause Tree", ex);
            }
        }

        private void ShowJSDebugWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JSDebugWindow.Owner = this;
                JSDebugWindow.Show();
                JSDebugWindow.BringIntoView();
                JSDebugWindow.Activate();
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to show script debug window", ex);
            }
        }

        private void ViewLogsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", string.Format("{0}\\Levrum\\Logs", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to view logs folder", ex);
            }
        }

        private void JsonViewerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JsonViewWindow.Owner = this;
                JsonViewWindow.Show();
                JsonViewWindow.BringIntoView();
                JsonViewWindow.Activate();
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to show JSON viewer window", ex);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (Worker == null || !Worker.IsBusy)
            {
                EnableControls();
                return;
            }

            MessageBoxResult result = MessageBox.Show("Are you sure you want to cancel the current operation?", "Cancel Operation?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            Worker.CancelAsync();
        }

        private void EditPhaseOneScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string script = null;
                if (!string.IsNullOrWhiteSpace(Map.PostProcessingScript))
                {
                    script = Map.PostProcessingScript;
                }
                EditScriptDialog dialog = new EditScriptDialog(ScriptType.PostLoad, "Edit Post-Loading Script", null, script);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    Map.PostProcessingScript = dialog.Result;
                    SetChangesMade(Map, true);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to edit post-loading script", ex);
            }
        }

        private void EditPhaseTwoScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string script = null;
                if (!string.IsNullOrWhiteSpace(Map.PerIncidentScript))
                {
                    script = Map.PerIncidentScript;
                }
                EditScriptDialog dialog = new EditScriptDialog(ScriptType.PerIncident, "Edit Per Incident Script", null, script);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    Map.PerIncidentScript = dialog.Result;
                    SetChangesMade(Map, true);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to edit per incident script", ex);
            }
        }

        private void EditPhaseThreeScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string script = null;
                if (!string.IsNullOrWhiteSpace(Map.FinalProcessingScript))
                {
                    script = Map.FinalProcessingScript;
                }
                EditScriptDialog dialog = new EditScriptDialog(ScriptType.FinalProcessing, "Edit Final Processing Script", null, script);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    Map.FinalProcessingScript = dialog.Result;
                    SetChangesMade(Map, true);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to edit final processing script", ex);
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            AboutWindow aboutWindow = new AboutWindow();
            System.Windows.Media.Imaging.BitmapImage img = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Levrum.UI.WPF;component/databridge.png", UriKind.Relative));
            aboutWindow.ImageSource = img;
            aboutWindow.ShowDialog();
            this.Cursor = Cursors.Arrow;
        }

        private void UserManualPDFMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileInfo assemblyInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string fileName = string.Format("{0}\\Resources\\DataBridge Manual.pdf", assemblyInfo.DirectoryName);
            FileInfo info = new FileInfo(fileName);
            if (!info.Exists)
            {
                return;
            }
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(fileName);
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }

        private void ScriptingPDFMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileInfo assemblyInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string fileName = string.Format("{0}\\Resources\\PostProcessing Script Manual.pdf", assemblyInfo.DirectoryName);
            FileInfo info = new FileInfo(fileName);
            if (!info.Exists)
            {
                return;
            }
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(fileName);
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool isDataMap = true;
                foreach (string fileName in files)
                {
                    FileInfo file = new FileInfo(fileName);
                    if (file.Extension.ToLowerInvariant() == ".dmap")
                    {
                        try
                        {
                            OpenDataMap(fileName);
                            Activate();                            
                        } catch (Exception ex)
                        {
                            continue;
                        }
                    }
                }
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool isDataMap = true;
                foreach (string fileName in files)
                {
                    FileInfo file = new FileInfo(fileName);
                    isDataMap = isDataMap && file.Extension.ToLowerInvariant() == ".dmap";
                }
                if (isDataMap)
                {
                    e.Effects = DragDropEffects.Copy;
                    return;
                }
            } 

            e.Effects = DragDropEffects.None;
        }
    }

    public class DataMapDocument
    {
        public DataMap Map { get; set; }
        public LayoutDocument Document { get; set; } = null;

        private bool m_changesMade = false;
        public bool ChangesMade 
        { 
            get 
            { 
                return m_changesMade; 
            } 
            set 
            { 
                if (m_changesMade == false && value == true)
                {
                    OnChangesMade?.Invoke(Map, value);
                }
                m_changesMade = value;
                Document.Title = value == true ? string.Format("{0}*", Map.Name) : Map.Name;
            } 
        }

        public delegate void ChangesMadeDelegate(DataMap map, bool status);

        public event ChangesMadeDelegate OnChangesMade;

        public DataMapDocument(DataMap _map, LayoutDocument _document, bool _changesMade = false)
        {
            Map = _map;
            Document = _document;
            ChangesMade = _changesMade;

            Map.IncidentDataMappings.CollectionChanged += dataMappings_CollectionChanged;
            Map.ResponseDataMappings.CollectionChanged += dataMappings_CollectionChanged;
            Map.BenchmarkMappings.CollectionChanged += dataMappings_CollectionChanged;
            Map.DataSources.CollectionChanged += dataMappings_CollectionChanged;
        }

        private void dataMappings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ChangesMade = true;
        }
    }
}
