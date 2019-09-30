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

using Levrum.Data.Map;

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
            document.Content = new DataMapEditor(newMap);
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
                    DataMap map = JsonConvert.DeserializeObject<DataMap>(File.ReadAllText(ofd.FileName));
                    map.Name = file.Name;
                    map.Path = ofd.FileName;

                    LayoutDocument document = new LayoutDocument();
                    document.Title = map.Name;
                    document.Content = new DataMapEditor(map);
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

                string mapJson = JsonConvert.SerializeObject(map);
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

                            DataSources.Map = map;
                            DataSources.IsEnabled = true;
                            return;
                        }
                    }
                }

                SaveAsMenuItem.Header = "Save _As...";
                SaveAsMenuItem.IsEnabled = false;
                SaveMenuItem.Header = "_Save";
                SaveMenuItem.IsEnabled = false;

                DataSources.Map = null;
                DataSources.IsEnabled = false;
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

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
