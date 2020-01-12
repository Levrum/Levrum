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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DataSourceEditor editor = new DataSourceEditor(null);
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
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            IDataSource currentSource = DataSourcesListBox.SelectedItem as IDataSource;
            DataSourceEditor editor = new DataSourceEditor(currentSource);
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
        }

        private void updateDataSource(IDataSource oldSource, IDataSource newSource)
        {
            foreach (DataMapping mapping in Map.IncidentMappings)
            {
                if (mapping.Column.DataSource == oldSource)
                {
                    mapping.Column.DataSource = newSource;
                }
            }

            foreach (DataMapping mapping in Map.IncidentDataMappings)
            {
                if (mapping.Column.DataSource == oldSource)
                {
                    mapping.Column.DataSource = newSource;
                }
            }

            foreach (DataMapping mapping in Map.ResponseDataMappings)
            {
                if (mapping.Column.DataSource == oldSource)
                {
                    mapping.Column.DataSource = newSource;
                }
            }

            foreach (DataMapping mapping in Map.BenchmarkMappings)
            {
                if (mapping.Column.DataSource == oldSource)
                {
                    mapping.Column.DataSource = newSource;
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            IDataSource currentSource = DataSourcesListBox.SelectedItem as IDataSource;
            if (dataSourceInUse(currentSource))
            {
                MessageBoxResult result = MessageBox.Show(string.Format("DataSource {0} is in use. Would you like to remove mappings that reference it?", currentSource.Name), "DataSource in use!", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                } else if (result == MessageBoxResult.Yes)
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
        }

        private bool dataSourceInUse(IDataSource dataSource)
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
        }

        private void deleteDataSourceReferences(IDataSource dataSource)
        {
            removeMappingFromList(Map.IncidentMappings, dataSource);
            removeMappingFromList(Map.IncidentDataMappings, dataSource);
            removeMappingFromList(Map.ResponseDataMappings, dataSource);
            removeMappingFromList(Map.BenchmarkMappings, dataSource);
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
