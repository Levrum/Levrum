using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Map;
using Levrum.UI.WPF;

using CsvHelper;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainDataBridgeWindow : Window
    {
        public List<DataMapDocument> openDocuments = new List<DataMapDocument>();

        public DataMapEditor ActiveEditor { get; set; } = null;

        public JavascriptDebugWindow DebugWindow = new JavascriptDebugWindow();
        // public Dictionary<DataMap, LayoutDocument> openDocuments = new Dictionary<DataMap, LayoutDocument>();

        public MainDataBridgeWindow()
        {
            InitializeComponent();
            DataSources.Window = this;
            App app = Application.Current as App;
            if (app != null && !string.IsNullOrWhiteSpace(app.StartupFileName))
            {
                try
                {
                    OpenDataMap(app.StartupFileName);
                } catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Unable to open DataMap file '{0}': {1}", app.StartupFileName, ex.Message));
                }
            }
        }

        public void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewDataMapWindow window = new NewDataMapWindow();
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
        }

        public void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Levrum\\Data Maps"));
                di.Create();

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = di.FullName;
                ofd.DefaultExt = "dmap";
                ofd.Filter = "Levrum DataMap (*.dmap)|*.dmap|All files (*.*)|*.*";
                if (ofd.ShowDialog() == true) {
                    OpenDataMap(ofd.FileName);
                }
            } catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to open DataMap: {0}", ex.Message));
            }
        }

        public void OpenDataMap(string fileName)
        {
            DataMapDocument mapDocument = (from DataMapDocument d in openDocuments
                                           where d.Map.Path == fileName
                                           select d).FirstOrDefault();

            if (mapDocument != null)
            {
                int index = DocumentPane.IndexOfChild(mapDocument.Document);
                DocumentPane.SelectedContentIndex = index;
                return;
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
            openDocuments.Add(new DataMapDocument(map, document));
        }

        public void CloseMenuItem_Click(object sender, RoutedEventArgs e)
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
                    if(!SaveMap(map))
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

        public void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
            DataMapEditor editor = document.Content as DataMapEditor;
            DataMap map = editor.DataMap;
            SaveMap(map);
        }

        public void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
            DataMapEditor editor = document.Content as DataMapEditor;
            DataMap map = editor.DataMap;
            SaveMap(map, true);
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
                    sfd.Filter = "Levrum DataMap (*.dmap)|*.dmap|All files (*.*)|*.*";
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

                string mapJson = JsonConvert.SerializeObject(map, Formatting.Indented, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All });
                File.WriteAllText(map.Path, mapJson);
                if (document != null) {
                    document.ChangesMade = false;
                }

                SaveAsMenuItem.Header = string.Format("Save {0} _As...", map.Name);
                SaveMenuItem.Header = string.Format("_Save {0}", map.Name);
                return true;
            } catch (Exception ex)
            {

            }
            return false;
        }

        public void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DebugWindow.Close();
            Close();
        }

        private void DocumentPane_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedContentIndex")
            {
                LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
                if (document != null) {
                    DataMapEditor editor = document.Content as DataMapEditor;
                    ActiveEditor = editor;
                    if (editor != null)
                    {
                        DataMap map = editor.DataMap;
                        if (map != null)
                        {
                            SaveAsMenuItem.IsEnabled = true;
                            SaveAsMenuItem.Header = string.Format("Save {0} _As...", map.Name);
                            SaveMenuItem.IsEnabled = true;
                            SaveMenuItem.Header = string.Format("_Save {0}", map.Name);
                            CloseMenuItem.IsEnabled = true;

                            CreateIncidentJsonMenuItem.IsEnabled = true;
                            CreateCallResponseCSVsMenuItem.IsEnabled = true;

                            DataSources.Map = map;
                            DataSources.IsEnabled = true;
                            CoordinateConversionMenuItem.IsEnabled = true;
                            updateCoordinateConversionHeader();

                            ToggleInvertLatitude.IsEnabled = true;
                            ToggleInvertLongitude.IsEnabled = true;
                            updateInvertLatitudeHeader();
                            updateInvertLongitudeHeader();                            

                            SelectCauseTreeMenuItem.IsEnabled = true;
                            DefinePostProcessingScript.IsEnabled = true;
                            return;
                        }
                    }
                }

                SaveAsMenuItem.Header = "Save _As...";
                SaveAsMenuItem.IsEnabled = false;
                SaveMenuItem.Header = "_Save";
                SaveMenuItem.IsEnabled = false;
                CloseMenuItem.IsEnabled = false;
                
                CreateIncidentJsonMenuItem.IsEnabled = false;
                CreateCallResponseCSVsMenuItem.IsEnabled = false;

                DataSources.Map = null;
                DataSources.IsEnabled = false;
                CoordinateConversionMenuItem.IsEnabled = false;
                DefineProjectionMenuItem.IsEnabled = false;
                SelectCauseTreeMenuItem.IsEnabled = false;
                DefinePostProcessingScript.IsEnabled = false;
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void CoordinateConversionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataSources.Map.EnableCoordinateConversion = !DataSources.Map.EnableCoordinateConversion;
            DefineProjectionMenuItem.IsEnabled = DataSources.Map.EnableCoordinateConversion;
            updateCoordinateConversionHeader();

            SetChangesMade(DataSources.Map, true);
        }

        private void updateCoordinateConversionHeader()
        {
            if (DataSources.Map.EnableCoordinateConversion)
            {
                CoordinateConversionMenuItem.Header = "_Coordinate Conversion Enabled";
                CoordinateConversionMenuItem.IsChecked = true;
                DefineProjectionMenuItem.IsEnabled = true;
            }
            else
            {
                CoordinateConversionMenuItem.Header = "_Coordinate Conversion Disabled";
                CoordinateConversionMenuItem.IsChecked = false;
                DefineProjectionMenuItem.IsEnabled = false;
            }
        }

        private void DefineProjectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string projection = null;
            if (!string.IsNullOrWhiteSpace(DataSources.Map.Projection))
            {
                projection = DataSources.Map.Projection;
            }
            TextInputDialog dialog = new TextInputDialog("Define Projection", "Projection:", projection);
            dialog.ShowDialog();

            if (dialog.Result != null)
            {
                DataSources.Map.Projection = dialog.Result;
                SetChangesMade(DataSources.Map, true);
            }
        }

        private void SelectCauseTreeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "json";
            ofd.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    List<CauseData> causeTree = JsonConvert.DeserializeObject<List<CauseData>>(File.ReadAllText(ofd.FileName), new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
                    DataSources.Map.CauseTree = causeTree;
                    SetChangesMade(DataSources.Map, true);
                } catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Unable to load cause JSON from file '{0}':\n{1}", ofd.FileName, ex.Message));
                }
            }
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

        private void CreateIncidentJsonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            sfd.DefaultExt = "json";
            sfd.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (sfd.ShowDialog() == false)
            {
                return;
            }
            Cursor = Cursors.Wait;

            MapLoader loader = new MapLoader();
            loader.DebugHost.OnDebugMessage += DebugWindow.OnMessageReceived;
            try
            {   
                loader.LoadMap(DataSources.Map);
                
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.PreserveReferencesHandling = PreserveReferencesHandling.All;
                settings.Formatting = Formatting.Indented;
                string incidentJson = JsonConvert.SerializeObject(loader.Incidents, settings);
                File.WriteAllText(sfd.FileName, incidentJson);
                
                FileInfo file = new FileInfo(sfd.FileName);
                MessageBox.Show(string.Format("Incidents saved as JSON file '{0}'", file.Name));
            } catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to create JSON: {0}", ex.Message));
            } finally
            {
                loader.DebugHost.OnDebugMessage -= DebugWindow.OnMessageReceived;
                loader.Incidents.Clear();
                loader.IncidentsById.Clear();

                GC.Collect();
                Cursor = Cursors.Arrow;
            }
        }

        private void CreateCallResponseCSVsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            sfd.Title = "Save Incident CSV";
            sfd.DefaultExt = "csv";
            sfd.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";
            if (sfd.ShowDialog() == false)
            {
                return;
            }
            Cursor = Cursors.Wait;
            string incidentCsvFileName = sfd.FileName;

            FileInfo csvFileInfo = new FileInfo(incidentCsvFileName);
            sfd.Title = "Save Response CSV";
            sfd.FileName = string.Format("{0} Responses.csv", csvFileInfo.Name.Substring(0, csvFileInfo.Name.Length - 4));
            if (sfd.ShowDialog() == false)
            {
                return;
            }
            string responseCsvFileName = sfd.FileName;


            MapLoader loader = new MapLoader();
            loader.DebugHost.OnDebugMessage += DebugWindow.OnMessageReceived;
            try
            {
                loader.LoadMap(DataSources.Map);

                HashSet<string> incidentDataFields = new HashSet<string>();
                HashSet<string> responseDataFields = new HashSet<string>();
                HashSet<string> benchmarkNames = new HashSet<string>();
                foreach (IncidentData incident in loader.Incidents)
                {
                    foreach (string key in incident.Data.Keys)
                    {
                        incidentDataFields.Add(key);
                    }
                    foreach (ResponseData response in incident.Responses)
                    {
                        foreach (string key in response.Data.Keys)
                        {
                            responseDataFields.Add(key);
                        }
                        foreach (BenchmarkData benchmark in response.Benchmarks)
                        {
                            benchmarkNames.Add(benchmark.Name);
                        }
                    }
                }

                List<dynamic> incidentRecords = new List<dynamic>();
                List<dynamic> responseRecords = new List<dynamic>();
                foreach (IncidentData incident in loader.Incidents)
                {
                    dynamic incidentRecord = new ExpandoObject();
                    incidentRecord.Id = incident.Id;
                    incidentRecord.Time = incident.Time;
                    incidentRecord.Location = incident.Location;
                    incidentRecord.Latitude = incident.Latitude;
                    incidentRecord.Longitude = incident.Longitude;
                    foreach (string field in incidentDataFields)
                    {
                        if (incident.Data.ContainsKey(field))
                        {
                            ((IDictionary<string, object>)incidentRecord).Add(field, incident.Data[field]);
                        }
                        else
                        {
                            ((IDictionary<string, object>)incidentRecord).Add(field, string.Empty);
                        }
                    }

                    foreach (ResponseData response in incident.Responses)
                    {
                        dynamic responseRecord = new ExpandoObject();
                        responseRecord.Id = incident.Id;
                        foreach (string field in responseDataFields) { 
                            if (response.Data.ContainsKey(field))
                            {
                                ((IDictionary<string, object>)responseRecord).Add(field, response.Data[field]);
                            } else
                            {
                                ((IDictionary<string, object>)responseRecord).Add(field, string.Empty);
                            }
                        }

                        foreach (string benchmarkName in benchmarkNames)
                        {
                            BenchmarkData benchmark = (from bmk in response.Benchmarks
                                                       where bmk.Name == benchmarkName
                                                       select bmk).FirstOrDefault();

                            if (benchmark != null)
                            {
                                object value;
                                if (benchmark.Data.ContainsKey("DateTime")) {
                                    value = benchmark.Data["DateTime"];
                                } else
                                {
                                    value = benchmark.Value;
                                }
                                ((IDictionary<string, object>)responseRecord).Add(benchmarkName, value);
                            } else
                            {
                                ((IDictionary<string, object>)responseRecord).Add(benchmarkName, string.Empty);
                            }
                        }
                        responseRecords.Add(responseRecord);
                    }
                    incidentRecords.Add(incidentRecord);
                }

                using (StringWriter writer = new StringWriter())
                {
                    using (CsvWriter csv = new CsvWriter(writer))
                    {
                        csv.WriteRecords(incidentRecords);
                    }
                    File.WriteAllText(incidentCsvFileName, writer.ToString());
                }

                using (StringWriter writer = new StringWriter())
                {
                    using (CsvWriter csv = new CsvWriter(writer))
                    {
                        csv.WriteRecords(responseRecords);
                    }
                    File.WriteAllText(responseCsvFileName, writer.ToString());
                }
                
                MessageBox.Show(string.Format("Incidents saved as CSV files '{0}' and '{1}'", incidentCsvFileName, responseCsvFileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to create CSV files: {0}", ex.Message));
            }
            finally
            {
                loader.DebugHost.OnDebugMessage -= DebugWindow.OnMessageReceived;
                loader.Incidents.Clear();
                loader.IncidentsById.Clear();

                GC.Collect();
                Cursor = Cursors.Arrow;
            }
        }

        private void DockingManager_DocumentClosing(object sender, DocumentClosingEventArgs e)
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

        private void ToggleInvertLongitude_Click(object sender, RoutedEventArgs e)
        {
            DataSources.Map.InvertLongitude = !DataSources.Map.InvertLongitude;
            updateInvertLongitudeHeader();

            SetChangesMade(DataSources.Map, true);
        }

        private void updateInvertLongitudeHeader()
        {
            if (DataSources.Map.InvertLongitude)
            {
                ToggleInvertLongitude.Header = "Invert Lon_gitude Enabled";
                ToggleInvertLongitude.IsChecked = true;
            }
            else
            {
                ToggleInvertLongitude.Header = "Invert Lon_gitude Disabled";
                ToggleInvertLongitude.IsChecked = false;
            }
        }

        private void ToggleInvertLatitude_Click(object sender, RoutedEventArgs e)
        {
            DataSources.Map.InvertLatitude = !DataSources.Map.InvertLatitude;
            updateInvertLatitudeHeader();

            SetChangesMade(DataSources.Map, true);
        }

        private void updateInvertLatitudeHeader()
        {
            if (DataSources.Map.InvertLatitude)
            {
                ToggleInvertLatitude.Header = "Invert _Latitude Enabled";
                ToggleInvertLatitude.IsChecked = true;
            }
            else
            {
                ToggleInvertLatitude.Header = "Invert _Latitude Disabled";
                ToggleInvertLatitude.IsChecked = false;
            }
        }

        private void DefinePostProcessingScript_Click(object sender, RoutedEventArgs e)
        {
            string script = null;
            if (!string.IsNullOrWhiteSpace(DataSources.Map.PostProcessingScript))
            {
                script = DataSources.Map.PostProcessingScript;
            }
            TextInputDialog dialog = new TextInputDialog("Define PostProcessing Script", "PostProcessing Script:", script);
            dialog.ShowDialog();

            if (dialog.Result != null)
            {
                DataSources.Map.PostProcessingScript = dialog.Result;
                SetChangesMade(DataSources.Map, true);
            }
        }

        private void ShowJSDebugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DebugWindow.Show();
            DebugWindow.BringIntoView();
        }
    }

    public class DataMapDocument
    {
        public DataMap Map { get; set; }
        public LayoutDocument Document { get; set; } = null;
        public bool ChangesMade { get; set; } = false;

        public DataMapDocument (DataMap _map, LayoutDocument _document, bool _changesMade = false)
        {
            Map = _map;
            Document = _document;
            ChangesMade = _changesMade;
        }
    }
}
