using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Levrum.Data.Sources;
using Levrum.Utils;

using Microsoft.Win32;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for DataSourceEditor.xaml
    /// </summary>
    public partial class DataSourceEditor : Window
    {
        public IDataSource DataSource { get; set; } = null;
        private IDataSource OriginalDataSource { get; set; } = null;

        List<DataSourceTypeInfo> DataSourceTypes { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { "CSV File", "SQL Server" });

        bool ChangesMade { get; set; } = false;

        public DataSourceEditor(IDataSource _dataSource = null)
        {
            InitializeComponent();
            DataSourceTypeComboBox.ItemsSource = DataSourceTypes;
            DataSourceTypeComboBox.DisplayMemberPath = "Name";
            if (_dataSource != null)
            {
                OriginalDataSource = _dataSource.Clone() as IDataSource;
                DataSource = _dataSource;
                if (DataSource.Type == DataSourceType.CsvSource)
                {
                    DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                    CsvNameTextBox.Text = _dataSource.Name;
                    FileNameTextBox.Text = _dataSource.Parameters["File"];
                }
                else if (DataSource.Type == DataSourceType.SqlSource)
                {
                    DataSourceTypeComboBox.SelectedItem = DataSourceTypes[1];
                }
                ChangesMade = false;
            } else
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                ChangesMade = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChangesMade)
            {
                MessageBoxResult result = MessageBox.Show("Save changes before closing?", "Save Changes?", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) 
                {
                    return;
                } else if (result == MessageBoxResult.No)
                {
                    DataSource = OriginalDataSource;
                }
            } else
            {
                DataSource = OriginalDataSource;
            }
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataSourceTypeComboBox.SelectedItem.ToString() == "CSV File")
            {
                CsvOptionsGrid.Visibility = Visibility.Visible;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                DataSource = new CsvSource();
                ChangesMade = true;
            } else if (DataSourceTypeComboBox.SelectedItem.ToString() == "SQL Server")
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Visible;
                DataSource = new SqlSource();
                ChangesMade = true;
            }
        }

        private void CsvFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "csv";
            ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            if(ofd.ShowDialog() == false)
            {
                return;
            }

            CsvSource source = DataSource as CsvSource;
            FileInfo info = new FileInfo(ofd.FileName);
            if (source.File != null && (info.FullName == source.File.FullName))
            {
                return;
            }
            source.File = new FileInfo(ofd.FileName);
            FileNameTextBox.Text = ofd.FileName;
            CsvAnalyzer analyzer = new CsvAnalyzer(source.File);
            string stuff = analyzer.GetSummary();
            CsvSummaryTextBox.Text = stuff;
            ChangesMade = true;
        }

        private void CsvNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CsvSource source = DataSource as CsvSource;
            source.Name = CsvNameTextBox.Text;
            if (OriginalDataSource == null || (OriginalDataSource != null && OriginalDataSource.Name != source.Name))
            {
                ChangesMade = true;
            }
        }
    }

    public class DataSourceTypeInfo
    {
        public string Name { get; set; } = "";

        public DataSourceTypeInfo(string _name)
        {
            Name = _name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static implicit operator DataSourceTypeInfo(string _name)
        {
            return new DataSourceTypeInfo(_name);
        }
    }
}
