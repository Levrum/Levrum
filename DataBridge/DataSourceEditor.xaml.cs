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
using Levrum.Utils.Data;

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
        private bool Loading { get; set; } = true;

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
                    CsvSource csvSource = _dataSource as CsvSource;
                    if (csvSource == null)
                    {
                        return;
                    }

                    List<string> columns = csvSource.GetColumns();
                    IdColumnComboBox.ItemsSource = columns;
                    IdColumnComboBox.SelectedItem = _dataSource.IDColumn;
                    ResponseIdColumnComboBox.ItemsSource = columns;
                    ResponseIdColumnComboBox.SelectedItem = _dataSource.ResponseIDColumn;
                }
                else if (DataSource.Type == DataSourceType.SqlSource)
                {
                    DataSourceTypeComboBox.SelectedItem = DataSourceTypes[1];
                }
                ChangesMade = false;
            } else
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                DataSource = new CsvSource();
                ChangesMade = false;
            }
            Loading = false;
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
            if (Loading)
            {
                return;
            }

            if (DataSourceTypeComboBox.SelectedItem.ToString() == "CSV File")
            {
                CsvOptionsGrid.Visibility = Visibility.Visible;
                CsvOptionsButtons.Visibility = Visibility.Visible;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                DataSource = new CsvSource();
                ChangesMade = true;
            } else if (DataSourceTypeComboBox.SelectedItem.ToString() == "SQL Server")
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Visible;
                DataSource = new SqlSource();
                ChangesMade = true;
            }
        }

        private void SummarizeCsvButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                CsvSource csvSource = DataSource as CsvSource;
                CsvAnalyzer analyzer = new CsvAnalyzer(csvSource.CsvFile);
                string summary = analyzer.GetSummary();
                CsvSummaryTextBox.Text = summary;
                Cursor = Cursors.Arrow;
            }
            catch (Exception ex)
            {

            }
        }

        private void CsvFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileInfo info = null;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "csv";
                ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == false)
                {
                    return;
                }

                CsvSource source = new CsvSource();
                DataSource = source;
                info = new FileInfo(ofd.FileName);
                if (source.CsvFile != null && (info.FullName == source.CsvFile.FullName))
                {
                    return;
                }
                source.CsvFile = new FileInfo(ofd.FileName);
                FileNameTextBox.Text = ofd.FileName;

                List<string> columns = source.GetColumns();
                IdColumnComboBox.ItemsSource = columns;
                ResponseIdColumnComboBox.ItemsSource = columns;
                ChangesMade = true;
            } catch (Exception ex)
            {
                string name = "Unknown";
                if (info != null)
                    name = info.Name;
                MessageBox.Show(string.Format("Exception reading CSV File '{0}': {1}", name, ex.Message));
            }
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

        private void IdColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataSource.IDColumn = IdColumnComboBox.SelectedItem as string;
        }

        private void ResponseIdColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataSource.ResponseIDColumn = ResponseIdColumnComboBox.SelectedItem as string;
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
