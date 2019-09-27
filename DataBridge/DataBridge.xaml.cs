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
        public Dictionary<DataMap, LayoutDocument> openDocuments = new Dictionary<DataMap, LayoutDocument>();

        public MainWindow()
        {
            InitializeComponent();
        }

        public void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LayoutDocument document = new LayoutDocument();
            string title = "New DataMap.dmap";
            int counter = 1;
            foreach (DataMap map in openDocuments.Keys)
            {
                while (map.Name == title)
                {
                    title = string.Format("New DataMap {0}.dmap", counter);
                    counter++;
                }
            }
            
            DataMap newMap = new DataMap(title);
            document.Title = title;
            document.Content = new DataMapEditor(newMap);
            DocumentPane.Children.Add(document);
            openDocuments.Add(newMap, document);
        }

        public void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "dmap";
                ofd.Filter = "Levrum DataMap (*.dmap)|*.dmap|All files (*.*)|*.*";
                if (ofd.ShowDialog() == true) {
                    DataMap map = (from DataMap d in openDocuments.Keys
                                   where d.Path == ofd.FileName
                                   select d).FirstOrDefault();

                    if (map != null)
                    {
                        LayoutDocument openDocument = openDocuments[map];
                        int index = DocumentPane.IndexOfChild(openDocument);
                        DocumentPane.SelectedContentIndex = index;
                        return;
                    }

                    FileInfo file = new FileInfo(ofd.FileName);
                    map = JsonConvert.DeserializeObject<DataMap>(File.ReadAllText(ofd.FileName));
                    map.Name = file.Name;
                    map.Path = ofd.FileName;
                    map.SaveNeeded = false;

                    LayoutDocument document = new LayoutDocument();
                    document.Title = map.Name;
                    document.Content = new DataMapEditor(map);
                    DocumentPane.Children.Add(document);
                    openDocuments.Add(map, document);
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
            if (map.SaveNeeded)
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

            DocumentPane.Children.Remove(document);
            openDocuments.Remove(map);
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
                    if (openDocuments.ContainsKey(map))
                    {
                        openDocuments[map].Title = file.Name;
                    }
                }

                string mapJson = JsonConvert.SerializeObject(map);
                File.WriteAllText(map.Path, mapJson);
                map.SaveNeeded = false;
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
                            SaveMenuItem.Header = string.Format("Save {0}", map.Name);
                            return;
                        }
                    }
                }

                SaveAsMenuItem.Header = "Save _As...";
                SaveAsMenuItem.IsEnabled = false;
                SaveMenuItem.Header = "Save";
                SaveMenuItem.IsEnabled = false;
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }
    }
}
