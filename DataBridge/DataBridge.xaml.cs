using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Map;
using Levrum.WPFUI;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<DataMapDocument> openDocuments = new List<DataMapDocument>();

        // public Dictionary<DataMap, LayoutDocument> openDocuments = new Dictionary<DataMap, LayoutDocument>();

        public MainWindow()
        {
            InitializeComponent();
            DataSources.Window = this;
        }

        public void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LayoutDocument document = new LayoutDocument();
            string title = "New DataMap.dmap";
            int counter = 1;
            foreach (DataMapDocument mapDocument in openDocuments)
            {
                while (mapDocument.Map.Name == title)
                {
                    title = string.Format("New DataMap {0}.dmap", counter);
                    counter++;
                }
            }
            
            DataMap newMap = new DataMap(title);
            document.Title = title;
            DataMapEditor editor = new DataMapEditor(newMap);
            editor.Window = this;
            document.Content = editor;
            DocumentPane.Children.Add(document);
            openDocuments.Add(new DataMapDocument(newMap, document));
        }

        public void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "dmap";
                ofd.Filter = "Levrum DataMap (*.dmap)|*.dmap|All files (*.*)|*.*";
                if (ofd.ShowDialog() == true) {
                    DataMapDocument mapDocument = (from DataMapDocument d in openDocuments
                                                  where d.Map.Path == ofd.FileName
                                                  select d).FirstOrDefault();

                    if (mapDocument != null)
                    {
                        int index = DocumentPane.IndexOfChild(mapDocument.Document);
                        DocumentPane.SelectedContentIndex = index;
                        return;
                    }

                    FileInfo file = new FileInfo(ofd.FileName);
                    DataMap map = JsonConvert.DeserializeObject<DataMap>(File.ReadAllText(ofd.FileName), new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All });
                    map.Name = file.Name;
                    map.Path = ofd.FileName;

                    LayoutDocument document = new LayoutDocument();
                    document.Title = map.Name;
                    DataMapEditor editor = new DataMapEditor(map);
                    editor.Window = this;
                    document.Content = editor;
                    DocumentPane.Children.Add(document);
                    openDocuments.Add(new DataMapDocument(map, document));
                }
            } catch (Exception ex)
            {

            }
        }

        public void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
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
                    SaveFileDialog sfd = new SaveFileDialog();
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
                return true;
            } catch (Exception ex)
            {

            }
            return false;
        }

        public void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DocumentPane_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedContentIndex")
            {
                LayoutDocument document = DocumentPane.SelectedContent as LayoutDocument;
                if (document != null) {
                    DataMapEditor editor = document.Content as DataMapEditor;
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

                            DataSources.Map = map;
                            DataSources.IsEnabled = true;
                            CoordinateConversionMenuItem.IsEnabled = true;
                            if (map.EnableCoordinateConversion)
                            {
                                CoordinateConversionMenuItem.Header = "Disable _Coordinate Conversion";
                                DefineProjectionMenuItem.IsEnabled = true;
                            }
                            else
                            {
                                CoordinateConversionMenuItem.Header = "Enable _Coordinate Conversion";
                                DefineProjectionMenuItem.IsEnabled = false;
                            }
                            SelectCauseTreeMenuItem.IsEnabled = true;
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

                DataSources.Map = null;
                DataSources.IsEnabled = false;
                CoordinateConversionMenuItem.IsEnabled = false;
                DefineProjectionMenuItem.IsEnabled = false;
                SelectCauseTreeMenuItem.IsEnabled = false;
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void CoordinateConversionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataSources.Map.EnableCoordinateConversion = !DataSources.Map.EnableCoordinateConversion;
            DefineProjectionMenuItem.IsEnabled = DataSources.Map.EnableCoordinateConversion;
            if (DataSources.Map.EnableCoordinateConversion)
            {
                CoordinateConversionMenuItem.Header = "Disable _Coordinate Conversion";
            } else
            {
                CoordinateConversionMenuItem.Header = "Enable _Coordinate Conversion";
            }

            SetChangesMade(DataSources.Map, true);
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
                    List<CauseData> causeTree = JsonConvert.DeserializeObject<List<CauseData>>(File.ReadAllText(ofd.FileName), new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All });
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
            sfd.DefaultExt = "json";
            sfd.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (sfd.ShowDialog() == false)
            {
                return;
            }

            try
            {
                Cursor = Cursors.Wait;
                MapLoader loader = new MapLoader();
                loader.LoadMap(DataSources.Map);

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.PreserveReferencesHandling = PreserveReferencesHandling.All;
                settings.Formatting = Formatting.Indented;
                string incidentJson = JsonConvert.SerializeObject(loader.Incidents, settings);
                File.WriteAllText(sfd.FileName, incidentJson);
                Cursor = Cursors.Arrow;
                FileInfo file = new FileInfo(sfd.FileName);
                MessageBox.Show(string.Format("Incidents saved as JSON file '{0}'", file.Name));
            } catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to create JSON: {0}", ex.Message));
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
