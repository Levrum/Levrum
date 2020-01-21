using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using NLog;

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
            // DataSourcesListBox.ItemsSource = _map.DataSources;
            DataSourcesListBox.DisplayMemberPath = "Info";
        }

        private void logMessage(LogLevel level, string message = "", Exception ex = null)
        {
            App app = Application.Current as App;
            if (ex == null || level == LogLevel.Debug)
            {
                app.LogMessage(level, ex, message);
            } else
            {
                app.LogException(ex, message, true);
            }
                   
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
                        if (Window != null && Window.ActiveEditor != null)
                        {
                            Window.ActiveEditor.UpdateStaticMappingButtons();
                        }
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
    }
}
