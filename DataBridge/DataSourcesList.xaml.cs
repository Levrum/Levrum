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
        public MainWindow Window { get; set; } = null;

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
            currentSource = editor.DataSource;
            DataSources.Insert(index, currentSource);
            if (Window != null)
            {
                Window.SetChangesMade(Map, true);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            IDataSource currentSource = DataSourcesListBox.SelectedItem as IDataSource;
            DataSources.Remove(currentSource);
            if (Window != null)
            {
                Window.SetChangesMade(Map, true);
            }
        }

        private void DataSourcesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditButton.IsEnabled = DataSourcesListBox.SelectedIndex != -1;
            DeleteButton.IsEnabled = DataSourcesListBox.SelectedIndex != -1;
        }
    }
}
