using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Levrum.Data.Map;
using Levrum.Data.Sources;

using Levrum.Utils;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for DataSourcesList.xaml
    /// </summary>
    public partial class DataSourcesList : UserControl
    {
        public MainDataBridgeWindow Window { get; set; } = null;

        private DataMap m_dataMap = null;
        public DataMap Map 
        { 
            get 
            { 
                return m_dataMap; 
            } 
            set 
            {
                m_dataMap = value;
                if (value != null) {
                    DataSourcesListBox.ItemsSource = value.DataSources;
                } else
                {
                    DataSourcesListBox.ItemsSource = null;
                }
            } 
        }

        public ObservableCollection<IDataSource> DataSources { get { if (Map == null) return new ObservableCollection<IDataSource>(); return Map.DataSources; } }

        public DataSourcesList()
        {
            InitializeComponent();
            DataSourcesListBox.DisplayMemberPath = "Info";
        }

        private void logMessage(LogLevel level, string message = "", Exception ex = null)
        {
            LogHelper.LogMessage(level, message, ex);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataSourceEditor editor = new DataSourceEditor(null);
                editor.Owner = Window;
                editor.ShowDialog();
                IDataSource newSource = editor.DataSource;
                if (newSource != null)
                {
                    if (string.IsNullOrWhiteSpace(newSource.Name))
                    {
                        string baseName = "Other Source";
                        if (newSource is CsvSource)
                        {
                            baseName = "CSV Source";
                        } else if (newSource is SqlSource)
                        {
                            baseName = "SQL Source";
                        } else if (newSource is GeoSource)
                        {
                            baseName = "Geo Source";
                        } else if (newSource is XmlSource)
                        {
                            baseName = "XML Source";
                        }

                        int i = 1;
                        bool nameFound = false;
                        while (!nameFound)
                        {
                            string newName = string.Format("{0} #{1}", baseName, i);
                            IDataSource existingSource = (from IDataSource s in DataSources
                                                          where s.Name == newName
                                                          select s).FirstOrDefault();

                            if (existingSource == null)
                            {
                                newSource.Name = newName;
                                nameFound = true;
                            }
                        }
                    }
                    DataSources.Add(newSource);
                    if (Window != null)
                    {
                        Window.SetChangesMade(Map, true);
                    }
                }
            } catch (Exception ex)
            {
                logMessage(LogLevel.Error, "Exception adding DataSource", ex);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IDataSource currentSource = DataSourcesListBox.SelectedItem as IDataSource;
                DataSourceEditor editor = new DataSourceEditor(currentSource);
                editor.Owner = Window;
                editor.ShowDialog();
                int index = DataSources.IndexOf(currentSource);
                DataSources.RemoveAt(index);
                IDataSource newSource = editor.DataSource;
                if (string.IsNullOrWhiteSpace(newSource.Name))
                {
                    getUnusedDataSourceName(newSource);
                }
                updateDataSource(currentSource, newSource);

                currentSource = editor.DataSource;
                DataSources.Insert(index, currentSource);
                if (Window != null)
                {
                    Window.SetChangesMade(Map, true);
                }
            } catch (Exception ex)
            {
                logMessage(LogLevel.Error, "Exception editing DataSource", ex);
            }
        }

        private void updateDataSource(IDataSource oldSource, IDataSource newSource)
        {
            try
            {
                foreach (DataMapping mapping in Map.IncidentMappings)
                {
                    if (mapping.Column?.DataSource == oldSource)
                    {
                        mapping.Column.DataSource = newSource;
                    }
                }

                foreach (DataMapping mapping in Map.IncidentDataMappings)
                {
                    if (mapping.Column?.DataSource == oldSource)
                    {
                        mapping.Column.DataSource = newSource;
                    }
                }

                foreach (DataMapping mapping in Map.ResponseDataMappings)
                {
                    if (mapping.Column?.DataSource == oldSource)
                    {
                        mapping.Column.DataSource = newSource;
                    }
                }

                foreach (DataMapping mapping in Map.BenchmarkMappings)
                {
                    if (mapping.Column?.DataSource == oldSource)
                    {
                        mapping.Column.DataSource = newSource;
                    }
                }
            } catch (Exception ex)
            {
                logMessage(LogLevel.Error, "Exception updating DataSource", ex);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IDataSource currentSource = DataSourcesListBox.SelectedItem as IDataSource;
                if (dataSourceInUse(currentSource))
                {
                    MessageBoxResult result = MessageBox.Show(string.Format("DataSource {0} is in use. Would you like to remove mappings that reference it?", currentSource.Name), "DataSource in use!", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        deleteDataSourceReferences(currentSource);
                    }
                }

                DataSources.Remove(currentSource);
                if (Window != null)
                {
                    Window.SetChangesMade(Map, true);
                }
            } catch (Exception ex)
            {
                logMessage(LogLevel.Error, "Exception deleting DataSource", ex);
            }
        }

        private bool dataSourceInUse(IDataSource dataSource)
        {
            try
            {
                bool inUse = false;
                foreach (DataMapping mapping in Map.IncidentMappings)
                    inUse = inUse || mapping.Column.DataSource == dataSource;

                foreach (DataMapping mapping in Map.IncidentDataMappings)
                    inUse = inUse || mapping.Column.DataSource == dataSource;

                foreach (DataMapping mapping in Map.ResponseDataMappings)
                    inUse = inUse || mapping.Column.DataSource == dataSource;

                foreach (DataMapping mapping in Map.BenchmarkMappings)
                    inUse = inUse || mapping.Column.DataSource == dataSource;

                return inUse;
            } catch (Exception ex)
            {
                logMessage(LogLevel.Error, "Exception checking if DataSource is in use", ex);
                return false;
            }
        }

        private void deleteDataSourceReferences(IDataSource dataSource)
        {
            try
            {
                removeMappingFromList(Map.IncidentMappings, dataSource);
                removeMappingFromList(Map.IncidentDataMappings, dataSource);
                removeMappingFromList(Map.ResponseDataMappings, dataSource);
                removeMappingFromList(Map.BenchmarkMappings, dataSource);
            } catch (Exception ex)
            {
                logMessage(LogLevel.Error, "Exception removing DataSource references", ex);
            }
        }

        private void removeMappingFromList(IList<DataMapping> list, IDataSource dataSource)
        {
            int count = list.Count - 1;
            for (int i = count; i >= 0; --i)
            {
                if (list[i].Column.DataSource == dataSource)
                {
                    list.RemoveAt(i);
                }
            }
        }

        private void DataSourcesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditButton.IsEnabled = DataSourcesListBox.SelectedIndex != -1;
            DeleteButton.IsEnabled = DataSourcesListBox.SelectedIndex != -1;
        }

        private void getUnusedDataSourceName(IDataSource newSource, string baseName = "")
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                if (newSource is CsvSource)
                {
                    baseName = "CSV Source";
                }
                else if (newSource is SqlSource)
                {
                    baseName = "SQL Source";
                }
                else if (newSource is GeoSource)
                {
                    baseName = "Geo Source";
                }
            } else
            {
                IDataSource existingSource = (from IDataSource s in DataSources
                                              where s.Name == baseName
                                              select s).FirstOrDefault();

                if (existingSource == null)
                {
                    newSource.Name = baseName;
                    return;
                }
            }

            int i = 1;
            bool nameFound = false;
            while (!nameFound)
            {
                string newName = string.Format("{0} #{1}", baseName, i);
                IDataSource existingSource = (from IDataSource s in DataSources
                                              where s.Name == newName
                                              select s).FirstOrDefault();

                if (existingSource == null)
                {
                    newSource.Name = newName;
                    nameFound = true;
                }
                i++;
            }
        }

        private void DataSourcesListGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
                foreach (string fileName in fileNames)
                {
                    FileInfo info = new FileInfo(fileName);
                    string nameWithoutExtension = info.Name.Substring(0, info.Name.Length - info.Extension.Length);
                    DataSourceTypeInfo type;
                    if (DataSourceEditor.SourceTypesByExtension.TryGetValue(info.Extension, out type))
                    {
                        if (type == DataSourceTypeInfo.CsvSource || type == DataSourceTypeInfo.GeoSource)
                        {
                            IDataSource existingSource = (from s in Map.DataSources
                                                          where s.Parameters.ContainsKey("File") && s.Parameters["File"] == info.FullName
                                                          select s).FirstOrDefault();

                            if (existingSource != null)
                            {
                                continue;
                            }
                        }

                        IDataSource newSource = DataSourceEditor.CreateDataSourceFromFile(fileName);
                        getUnusedDataSourceName(newSource, nameWithoutExtension);
                        Map.DataSources.Add(newSource);
                    }
                }
            }
        }

        private void DataSourcesListGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
                bool dragAllowed = true;
                foreach (string fileName in fileNames)
                {
                    FileInfo info = new FileInfo(fileName);
                    if (!DataSourceEditor.SourceTypesByExtension.ContainsKey(info.Extension))
                    {
                        dragAllowed = false;
                    }
                }
                if (dragAllowed)
                {
                    e.Effects = DragDropEffects.Copy;
                    return;
                }
            }
        
            e.Effects = DragDropEffects.None;
        }

        private void DataSourcesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            IDataSource source = DataSourcesListBox.SelectedItem as IDataSource;
            if (source != null)
            {
                EditButton_Click(sender, e);
            }
        }

        private void DataSourcesListBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && DataSourcesListBox.SelectedIndex != -1)
            {
                IDataSource source = DataSourcesListBox.SelectedItem as IDataSource;
                MessageBoxResult result = MessageBox.Show(string.Format("Are you sure you want to delete the Data Source '{0}'?", source.Name), "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;

                DeleteButton_Click(sender, e);
            }
        }

        private void DataSourcesListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataSourcesListBox.SelectedIndex == -1)
            {
                EditMenuItem.IsEnabled = false;
                DeleteMenuItem.IsEnabled = false;
            }
        }

        private void ListBoxItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            EditMenuItem.IsEnabled = true;
            DeleteMenuItem.IsEnabled = true;
        }
    }
}
