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
using Microsoft.Data.SqlClient;

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

        List<DataSourceTypeInfo> SqlSourceTypes { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { "Table", "Query" });

        bool ChangesMade { get; set; } = false;
        private bool Loading { get; set; } = true;

        public DataSourceEditor(IDataSource _dataSource = null)
        {
            InitializeComponent();
            DataSourceTypeComboBox.ItemsSource = DataSourceTypes;
            DataSourceTypeComboBox.DisplayMemberPath = "Name";
            SqlDataTypeComboBox.ItemsSource = SqlSourceTypes;
            SqlDataTypeComboBox.DisplayMemberPath = "Name";

            if (_dataSource != null)
            {
                OriginalDataSource = _dataSource.Clone() as IDataSource;
                DataSource = _dataSource;
                NameTextBox.Text = _dataSource.Name;
                if (DataSource.Type == DataSourceType.CsvSource)
                {
                    DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                    updateDataSourceOptionsDisplay();
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
                    updateDataSourceOptionsDisplay();
                    SqlServerAddress.Text = DataSource.Parameters["Server"];
                    SqlServerPort.Text = DataSource.Parameters["Port"];
                    SqlServerUser.Text = DataSource.Parameters["User"];
                    SqlServerPassword.Password = DataSource.Parameters["Password"];
                    SqlServerDatabase.Text = DataSource.Parameters["Database"];
                    connectToSqlSource();
                    if (DataSource.Parameters["Query"] != null)
                    {
                        SqlDataTypeComboBox.SelectedItem = SqlSourceTypes[1];
                        SqlQueryTextBox.Text = DataSource.Parameters["Query"];
                    } else
                    {
                        SqlTableComboBox.SelectedItem = DataSource.Parameters["Table"];
                    }

                    SqlIdColumnComboBox.SelectedItem = DataSource.IDColumn;
                    SqlResponseIdColumnComboBox.SelectedItem = DataSource.ResponseIDColumn;
                }
                ChangesMade = false;
            } else
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                SqlDataTypeComboBox.SelectedItem = SqlSourceTypes[0];
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
                    DataSource.Disconnect();
                    DataSource = OriginalDataSource;
                }
            } else
            {
                DataSource.Disconnect();
                DataSource = OriginalDataSource;
            }
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            updateParameters();
            DataSource.Disconnect();
            Close();
        }

        private void updateParameters()
        {
            if (DataSource is CsvSource)
            {
                DataSource.Parameters["File"] = FileNameTextBox.Text;
            }
            else if (DataSource is SqlSource)
            {
                DataSource.Parameters["Server"] = SqlServerAddress.Text;
                DataSource.Parameters["Port"] = SqlServerPort.Text;
                DataSource.Parameters["User"] = SqlServerUser.Text;
                DataSource.Parameters["Password"] = SqlServerPassword.Password;
                DataSource.Parameters["Database"] = SqlServerDatabase.Text;
                if (SqlDataTypeComboBox.SelectedItem == SqlSourceTypes[0])
                {
                    if (DataSource.Parameters.ContainsKey("Query"))
                    {
                        DataSource.Parameters.Remove("Query");
                    }
                    DataSource.Parameters["Table"] = SqlTableComboBox.SelectedItem as string;
                } else
                {
                    if (DataSource.Parameters.ContainsKey("Table"))
                    {
                        DataSource.Parameters.Remove("Table");
                    }
                    DataSource.Parameters["Query"] = SqlQueryTextBox.Text;
                }
            }
        }

        private void DataSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Loading)
            {
                return;
            }

            if (DataSource != null)
            {
                DataSource.Disconnect();
            }

            if (DataSourceTypeComboBox.SelectedItem.ToString() == "CSV File")
            {
                DataSource = new CsvSource();
                ChangesMade = true;
            } else if (DataSourceTypeComboBox.SelectedItem.ToString() == "SQL Server")
            {
                DataSource = new SqlSource();
                ChangesMade = true;
            }
            updateDataSourceOptionsDisplay();
        }

        private void updateDataSourceOptionsDisplay()
        {
            if (DataSourceTypeComboBox.SelectedItem.ToString() == "CSV File")
            {
                CsvOptionsGrid.Visibility = Visibility.Visible;
                CsvOptionsButtons.Visibility = Visibility.Visible;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsPanel.Visibility = Visibility.Hidden;
            }
            else if (DataSourceTypeComboBox.SelectedItem.ToString() == "SQL Server")
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Visible;
                SqlOptionsPanel.Visibility = Visibility.Visible;
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

                CsvSource source = DataSource as CsvSource;
                if (source == null)
                {
                    IDataSource lastSource = DataSource;
                    DataSource = source = new CsvSource();
                    if (lastSource != null)
                    {
                        DataSource.Name = lastSource.Name;
                    }
                }
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

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataSource.Name = NameTextBox.Text;
            if (OriginalDataSource == null || (OriginalDataSource != null && OriginalDataSource.Name != DataSource.Name))
            {
                ChangesMade = true;
            }
        }

        private void IdColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                DataSource.IDColumn = comboBox.SelectedItem as string;
            }
        }

        private void ResponseIdColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                DataSource.ResponseIDColumn = comboBox.SelectedItem as string;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            SqlStatusText.Text = "Connecting...";
            connectToSqlSource();
        }

        private void connectToSqlSource()
        {
            SqlSource dataSource = DataSource as SqlSource;
            dataSource.Parameters["Server"] = SqlServerAddress.Text;
            dataSource.Parameters["Port"] = SqlServerPort.Text;
            dataSource.Parameters["User"] = SqlServerUser.Text;
            dataSource.Parameters["Password"] = SqlServerPassword.Password;
            dataSource.Parameters["Database"] = SqlServerDatabase.Text;

            if (!string.IsNullOrWhiteSpace(SqlQueryTextBox.Text))
            {
                dataSource.Parameters["Query"] = SqlQueryTextBox.Text;
            }

            bool connected = dataSource.Connect();
            if (connected)
            {
                SqlStatusText.Text = "Connected OK!";
            }
            else
            {
                SqlStatusText.Text = "Unable to connect!";
            }
            List<string> tables = dataSource.GetTables();
            tables.Sort();
            SqlTableComboBox.ItemsSource = tables;
            if (dataSource.Parameters.ContainsKey("Query"))
            {
                SqlIdColumnComboBox.ItemsSource = dataSource.GetColumns();
                SqlResponseIdColumnComboBox.ItemsSource = dataSource.GetColumns();
            }
            dataSource.Disconnect();
        }

        private void SqlDataTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SqlDataTypeComboBox.SelectedItem == SqlSourceTypes[0])
            {
                // Table
                SqlTableComboBox.Visibility = Visibility.Visible;
                SqlTableTextBlock.Visibility = Visibility.Visible;
                SqlTableDetailsTextBox.Visibility = Visibility.Visible;
                SqlQueryTextBox.Visibility = Visibility.Hidden;
                SqlQueryTextBox.Text = string.Empty;
                if (DataSource != null)
                {
                    DataSource.Parameters.Remove("Query");
                }
            } else
            {
                // Query
                SqlTableComboBox.Visibility = Visibility.Hidden;
                SqlTableTextBlock.Visibility = Visibility.Hidden;
                SqlTableDetailsTextBox.Visibility = Visibility.Hidden;
                SqlQueryTextBox.Visibility = Visibility.Visible;
                if (DataSource != null)
                {
                    DataSource.Parameters.Remove("Table");
                }
            }
        }

        private void SqlTableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataSource != null)
            {
                DataSource.Parameters["Table"] = SqlTableComboBox.SelectedItem as string;
                SqlIdColumnComboBox.ItemsSource = DataSource.GetColumns();
                SqlResponseIdColumnComboBox.ItemsSource = DataSource.GetColumns();
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
