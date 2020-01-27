using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Map;
using Levrum.UI.WPF;
using Levrum.Utils;
using Levrum.Utils.Data;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainDataBridgeWindow : Window
    {
        public List<DataMapDocument> openDocuments = new List<DataMapDocument>();

        public DataMapEditor ActiveEditor { get; set; } = null;

        public JavascriptDebugWindow JSDebugWindow { get; set; } = new JavascriptDebugWindow();
        public JsonViewerWindow JsonViewWindow { get; set; } = new JsonViewerWindow();

        public BackgroundWorker Worker { get; set; } = null;

        public MainDataBridgeWindow()
        {
            InitializeComponent();
            DataSources.Window = this;
            App app = Application.Current as App;
            if (app != null)
            {
                if (app.StartupFileNames.Length > 0)
                {
                    DataMapDocument firstDocument = null;
                    foreach (string fileName in app.StartupFileNames)
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

                if (app.DebugMode)
                {
                    ViewLogsMenuItem.Visibility = Visibility.Visible;
                }
            }
            updateToolbarsAndMenus();
        }

        private void onLoaderProgress(object sender, string message, double progress)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { onLoaderProgress(sender, message, progress); }));
                return;
            }

            StatusBarProgress.Value = progress;
            StatusBarText.Text = message;
        }

        private void logException(object sender, string message, Exception ex)
        {
            LogHelper.LogMessage(LogLevel.Error, message, ex);
        }

        public DataMapDocument OpenDataMap(string fileName)
        {
            try
            {
                DataMapDocument mapDocument = (from DataMapDocument d in openDocuments
                                               where d.Map.Path == fileName
                                               select d).FirstOrDefault();

                if (mapDocument != null)
                {
                    int index = DocumentPane.IndexOfChild(mapDocument.Document);
                    DocumentPane.SelectedContentIndex = index;
                    return mapDocument;
                }

                FileInfo file = new FileInfo(fileName);
                DataMap map = JsonConvert.DeserializeObject<DataMap>(File.ReadAllText(fileName), new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented });
                map.Name = file.Name;
                map.Path = fileName;

                LayoutDocument document = new LayoutDocument();
                document.Title = map.Name;
                DataMapEditor editor = new DataMapEditor(map);
                ActiveEditor = editor;
                editor.Window = this;
                document.Content = editor;
                DocumentPane.Children.Add(document);
                mapDocument = new DataMapDocument(map, document);
                openDocuments.Add(mapDocument);
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
                DataMapDocument openDocument = (from DataMapDocument d in openDocuments
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
                    openDocuments.Remove(openDocument);
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
                DataMapDocument document = (from DataMapDocument d in openDocuments
                                            where d.Map == map
                                            select d).FirstOrDefault();

                if (forceSaveAs || string.IsNullOrEmpty(map.Path))
                {
                    DirectoryInfo di = new DirectoryInfo(string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Levrum\\Data Maps"));
                    di.Create();

                    SaveFileDialog sfd = new SaveFileDialog();
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
                    document.ChangesMade = false;
                }

                SaveAsMenuItem.Header = string.Format("Save {0} _As...", map.Name);
                SaveMenuItem.Header = string.Format("_Save {0}", map.Name);
                return true;
            }
            catch (Exception ex)
            {
                logException(this, "Error saving DataMap", ex);
                return false;
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
            } else

            Cursor = Cursors.Arrow;
            toggleControls(false);
        }

        public void DisableControls()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { DisableControls(); }));
                return;
            } else

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

            NewButton.IsEnabled = controlsEnabled;
            OpenButton.IsEnabled = controlsEnabled;
            SaveButton.IsEnabled = controlsEnabled;
            CreateJsonButton.IsEnabled = controlsEnabled;
            CreateCsvButton.IsEnabled = controlsEnabled;
            InvertLatitudeButton.IsEnabled = controlsEnabled;
            InvertLongitudeButton.IsEnabled = controlsEnabled;
            EditProjectionButton.IsEnabled = controlsEnabled;
            ConvertCoordinateButton.IsEnabled = controlsEnabled;
            EditPostProcessingButton.IsEnabled = controlsEnabled;
            EditCauseTreeButton.IsEnabled = controlsEnabled;

            StopButton.IsEnabled = runningOperation;
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

                EditCauseTreeMenuItem.IsEnabled = documentOpen;
                EditPostProcessingScript.IsEnabled = documentOpen;

                SaveButton.IsEnabled = documentOpen;
                CreateJsonButton.IsEnabled = documentOpen;
                CreateCsvButton.IsEnabled = documentOpen;
                ConvertCoordinateButton.IsEnabled = documentOpen;
                InvertLatitudeButton.IsEnabled = documentOpen;
                InvertLongitudeButton.IsEnabled = documentOpen;
                EditProjectionButton.IsEnabled = documentOpen;
                EditCauseTreeButton.IsEnabled = documentOpen;
                EditPostProcessingButton.IsEnabled = documentOpen;

                if (documentOpen)
                {
                    SaveAsMenuItem.Header = string.Format("Save {0} _As...", map.Name);
                    SaveMenuItem.Header = string.Format("_Save {0}", map.Name);
                    DataSources.Map = map;

                    updateCoordinateConversionControls();
                    updateInvertLatitude();
                    updateInvertLongitude();
                }
                else
                {
                    SaveAsMenuItem.Header = "Save _As...";
                    SaveMenuItem.Header = "_Save";
                    DataSources.Map = null;

                    InvertLatitudeButton.IsChecked = false;
                    InvertLongitudeButton.IsChecked = false;
                    ConvertCoordinateButton.IsChecked = false;
                }

                DataSources.IsEnabled = documentOpen;
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
            DataMapDocument document = (from DataMapDocument d in openDocuments
                                        where d.Map == DataSources.Map
                                        select d).FirstOrDefault();

            if (document != null)
            {
                document.ChangesMade = status;
            }
        }

        public DataMapDocument GetDocumentForMap(DataMap map)
        {
            return (from DataMapDocument d in openDocuments
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
                DataMapDocument openDocument = (from DataMapDocument d in openDocuments
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

                openDocuments.Remove(openDocument);
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
                sfd.FileName = string.Format("{0} Responses.csv", csvFileInfo.Name.Substring(0, csvFileInfo.Name.Length - 4));
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
                        string incidentJson = File.ReadAllText(ofd.FileName);
                        DataSet<IncidentData> incidents = JsonConvert.DeserializeObject<DataSet<IncidentData>>(incidentJson);

                        IncidentDataTools.CreateCsvs(incidents, incidentCsvFileName, responseCsvFileName);

                        MessageBox.Show(string.Format("Incidents saved as CSV files '{0}' and '{1}'", incidentCsvFileName, responseCsvFileName));
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
                        StatusBarProgress.Value = 0;
                        StatusBarText.Text = "Ready";
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
                foreach (DataMapDocument doc in openDocuments)
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
                Application.Current.Shutdown();
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
                foreach (DataMapDocument mapDocument in openDocuments)
                {
                    while (mapDocument.Map.Name == title)
                    {
                        title = string.Format("{0} {1}.dmap", baseTitle, counter);
                        counter++;
                    }
                }

                newMap.Name = title;
                document.Title = title;
                DataMapEditor editor = new DataMapEditor(newMap);
                ActiveEditor = editor;
                editor.Window = this;
                document.Content = editor;
                DocumentPane.Children.Add(document);
                openDocuments.Add(new DataMapDocument(newMap, document));
                DocumentPane.SelectedContentIndex = DocumentPane.Children.Count - 1;
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

        private void CreateJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                FileInfo file;
                if (DataSources.Map.Data.ContainsKey("LastJsonExport"))
                {
                    file = new FileInfo(DataSources.Map.Data["LastJsonExport"] as string);
                    if (file.Exists)
                    {
                        sfd.InitialDirectory = file.DirectoryName;
                        sfd.FileName = DataSources.Map.Data["LastJsonExport"] as string;
                    }
                }
                else
                {
                    sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                }

                sfd.DefaultExt = "json";
                sfd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (sfd.ShowDialog() == false)
                {
                    return;
                }
                file = new FileInfo(sfd.FileName);
                Cursor = Cursors.Wait;
                if (!DataSources.Map.Data.ContainsKey("LastJsonExport") || DataSources.Map.Data["LastJsonExport"] as string != sfd.FileName)
                {
                    DataSources.Map.Data["LastJsonExport"] = sfd.FileName;

                    SetChangesMade(DataSources.Map, true);
                }
                LogHelper.LogMessage(LogLevel.Info, string.Format("Creating JSON {0} from DataMap {1}", sfd.FileName, DataSources.Map.Name));

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

                        loader.LoadMap(DataSources.Map);
                        foreach (IncidentData incident in loader.Incidents)
                        {
                            incident.Intern();
                            foreach (ResponseData response in incident.Responses)
                            {
                                response.Intern();
                                foreach (BenchmarkData benchmark in response.Benchmarks)
                                {
                                    response.Intern();
                                }
                            }
                        }

                        GC.Collect();
                        if (Worker.CancellationPending)
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("Cancelled JSON creation at user request"));
                            return;
                        }
                        onLoaderProgress(this, "Generating and saving JSON", 0);
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.TypeNameHandling = TypeNameHandling.All;
                        settings.Formatting = Formatting.Indented;
                        using (TextWriter writer = File.CreateText(sfd.FileName)) {
                            var serializer = new JsonSerializer();
                            serializer.Formatting = Formatting.Indented;
                            serializer.TypeNameHandling = TypeNameHandling.All;
                            serializer.Serialize(writer, loader.Incidents);
                        }

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
                        LogHelper.LogMessage(LogLevel.Info, "Finished creating JSON");
                        GC.Collect();
                        EnableControls();
                        StatusBarProgress.Value = 0;
                        StatusBarText.Text = "Ready";
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
                if (DataSources.Map.Data.ContainsKey("LastCsvIncidentExport"))
                {
                    FileInfo file = new FileInfo(DataSources.Map.Data["LastCsvIncidentExport"] as string);
                    if (file.Exists)
                    {
                        sfd.InitialDirectory = file.DirectoryName;
                        sfd.FileName = DataSources.Map.Data["LastCsvIncidentExport"] as string;
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
                Cursor = Cursors.Wait;
                string incidentCsvFileName = sfd.FileName;

                sfd.Title = "Save Response CSV";
                if (DataSources.Map.Data.ContainsKey("LastCsvReponseExport"))
                {
                    FileInfo file = new FileInfo(DataSources.Map.Data["LastCsvResponseExport"] as string);
                    if (file.Exists)
                    {
                        sfd.InitialDirectory = file.DirectoryName;
                        sfd.FileName = DataSources.Map.Data["LastCsvResponseExport"] as string;
                    }
                }
                else
                {
                    FileInfo csvFileInfo = new FileInfo(incidentCsvFileName);
                    sfd.FileName = string.Format("{0} Responses.csv", csvFileInfo.Name.Substring(0, csvFileInfo.Name.Length - 4));
                }
                if (sfd.ShowDialog() == false)
                {
                    return;
                }
                string responseCsvFileName = sfd.FileName;
                if (!DataSources.Map.Data.ContainsKey("LastCsvIncidentExport") || DataSources.Map.Data["LastCsvIncidentExport"] as string != incidentCsvFileName ||
                    !DataSources.Map.Data.ContainsKey("LastCsvResponseExport") || DataSources.Map.Data["LastCsvResponseExport"] as string != responseCsvFileName)
                {
                    DataSources.Map.Data["LastCsvIncidentExport"] = incidentCsvFileName;
                    DataSources.Map.Data["LastCsvResponseExport"] = responseCsvFileName;

                    SetChangesMade(DataSources.Map, true);
                }

                LogHelper.LogMessage(LogLevel.Info, string.Format("Creating CSVs {0} and {1} from DataMap {2}", incidentCsvFileName, responseCsvFileName, DataSources.Map.Name));

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
                        loader.LoadMap(DataSources.Map);
                        foreach (IncidentData incident in loader.Incidents)
                        {
                            incident.Intern();
                            foreach (ResponseData response in incident.Responses)
                            {
                                response.Intern();
                                foreach (BenchmarkData benchmark in response.Benchmarks)
                                {
                                    response.Intern();
                                }
                            }
                        }

                        if (Worker.CancellationPending)
                        {
                            LogHelper.LogMessage(LogLevel.Info, string.Format("Cancelled CSV creation at user request"));
                            return;
                        }

                        GC.Collect();
                        onLoaderProgress(this, "Generating and saving CSVs", 0);
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
                        LogHelper.LogMessage(LogLevel.Info, string.Format("Finished creating CSVs"));
                        GC.Collect();
                        EnableControls();
                        StatusBarProgress.Value = 0;
                        StatusBarText.Text = "Ready";
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
                DataSources.Map.EnableCoordinateConversion = !DataSources.Map.EnableCoordinateConversion;
                updateCoordinateConversionControls();

                SetChangesMade(DataSources.Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle coordinate conversion", ex);
            }
        }

        private void updateCoordinateConversionControls()
        {
            if (DataSources.Map.EnableCoordinateConversion)
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
                if (!string.IsNullOrWhiteSpace(DataSources.Map.Projection))
                {
                    projection = DataSources.Map.Projection;
                }
                TextInputDialog dialog = new TextInputDialog("Define Projection", "Projection:", projection);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    DataSources.Map.Projection = dialog.Result;
                    SetChangesMade(DataSources.Map, true);
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
                DataSources.Map.InvertLatitude = !DataSources.Map.InvertLatitude;
                updateInvertLatitude();

                SetChangesMade(DataSources.Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle invert latitude", ex);
            }
        }

        private void updateInvertLatitude()
        {
            if (DataSources.Map.InvertLatitude)
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
                DataSources.Map.InvertLongitude = !DataSources.Map.InvertLongitude;
                updateInvertLongitude();

                SetChangesMade(DataSources.Map, true);
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to toggle invert longitude", ex);
            }
        }

        private void updateInvertLongitude()
        {
            if (DataSources.Map.InvertLongitude)
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

        private void EditCauseTree_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<ICategoryData> tree = new List<ICategoryData>();
                foreach (CauseData cause in DataSources.Map.CauseTree)
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

                    DataSources.Map.CauseTree = causeTree;
                    SetChangesMade(DataSources.Map, true);
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
                if (!string.IsNullOrWhiteSpace(DataSources.Map.PostProcessingScript))
                {
                    script = DataSources.Map.PostProcessingScript;
                }
                EditScriptDialog dialog = new EditScriptDialog(null, null, script);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    DataSources.Map.PostProcessingScript = dialog.Result;
                    SetChangesMade(DataSources.Map, true);
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
                if (!string.IsNullOrWhiteSpace(DataSources.Map.PerIncidentScript))
                {
                    script = DataSources.Map.PerIncidentScript;
                }
                EditScriptDialog dialog = new EditScriptDialog(null, null, script);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    DataSources.Map.PerIncidentScript = dialog.Result;
                    SetChangesMade(DataSources.Map, true);
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
                if (!string.IsNullOrWhiteSpace(DataSources.Map.FinalProcessingScript))
                {
                    script = DataSources.Map.FinalProcessingScript;
                }
                EditScriptDialog dialog = new EditScriptDialog(null, null, script);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    DataSources.Map.FinalProcessingScript = dialog.Result;
                    SetChangesMade(DataSources.Map, true);
                }
            }
            catch (Exception ex)
            {
                logException(sender, "Unable to edit final processing script", ex);
            }
        }
    }

    public class DataMapDocument
    {
        public DataMap Map { get; set; }
        public LayoutDocument Document { get; set; } = null;
        public bool ChangesMade { get; set; } = false;

        public DataMapDocument(DataMap _map, LayoutDocument _document, bool _changesMade = false)
        {
            Map = _map;
            Document = _document;
            ChangesMade = _changesMade;
        }
    }
}
