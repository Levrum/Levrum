using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

using ProjNet;

using NetTopologySuite.IO.ShapeFile;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Point = NetTopologySuite.Geometries.Point;
using NetTopologySuite.IO;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Map;
using Levrum.Data.Sources;

using Levrum.Utils;
using Levrum.Utils.Data;
using Levrum.Utils.Geography;

using Levrum.UI.WPF;

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

        public static List<DataSourceTypeInfo> DataSourceTypes { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { DataSourceTypeInfo.CsvSource, DataSourceTypeInfo.SqlSource, DataSourceTypeInfo.GeoSource, DataSourceTypeInfo.XmlSource, DataSourceTypeInfo.DailyDigestXmlSource });

        public static Dictionary<string, DataSourceTypeInfo> SourceTypesByExtension = new Dictionary<string, DataSourceTypeInfo>()
        {
            { ".csv", DataSourceTypeInfo.CsvSource },
            { ".sql", DataSourceTypeInfo.SqlSource },
            { ".shp", DataSourceTypeInfo.GeoSource },
            { ".zip", DataSourceTypeInfo.GeoSource },
            { ".geojson", DataSourceTypeInfo.GeoSource },
            { ".xml", DataSourceTypeInfo.XmlSource }
        };

        public static List<DataSourceTypeInfo> SqlSourceTypes { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { "Table", "Query" });

        public static ObservableCollection<DataSourceTypeInfo> Projections { get; } = new ObservableCollection<DataSourceTypeInfo>()
        {
            new DataSourceTypeInfo("EPSG:3857 Pseudo-Mercator", CoordinateConverter.WebMercator.WKT),
            new DataSourceTypeInfo("EPSG:4326 Lat-Long", CoordinateConverter.WGS84.WKT)
        };

        bool ChangesMade { get; set; } = false;
        private bool Loading { get; set; } = true;

        public DataSourceEditor(IDataSource _dataSource = null)
        {
            InitializeComponent();
            DataSourceTypeComboBox.ItemsSource = DataSourceTypes;
            DataSourceTypeComboBox.DisplayMemberPath = "Name";
            SqlDataTypeComboBox.ItemsSource = SqlSourceTypes;
            SqlDataTypeComboBox.DisplayMemberPath = "Name";

            ProjectionColumnComboBox.ItemsSource = Projections;
            ProjectionColumnComboBox.DisplayMemberPath = "Name";

            if (_dataSource != null)
            {
                OriginalDataSource = _dataSource.Clone() as IDataSource;
                DataSource = _dataSource;
                updateDisplayForDataSource();
                ChangesMade = false;
            }
            else
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                SqlDataTypeComboBox.SelectedItem = SqlSourceTypes[0];
                DataSource = new CsvSource();
                ChangesMade = false;
            }
            Loading = false;
        }

        private void updateDisplayForDataSource()
        {
            NameTextBox.Text = DataSource.Name;
            if (DataSource.Type == DataSourceType.CsvSource)
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                updateDisplayOptions();
                if (DataSource.Parameters.ContainsKey("File"))
                {
                    CsvFileNameTextBox.Text = DataSource.Parameters["File"];
                }
                CsvSource csvSource = DataSource as CsvSource;
                if (csvSource == null)
                {
                    return;
                }
                if (DataSource.Parameters.ContainsKey("EmbedCSV"))
                {
                    bool embedCsv = false;
                    if (bool.TryParse(DataSource.Parameters["EmbedCSV"], out embedCsv))
                    {
                        EmbedCsvCheckBox.IsChecked = true;
                    }
                }

                List<string> columns = csvSource.GetColumns();
                IdColumnComboBox.ItemsSource = columns;
                IdColumnComboBox.SelectedItem = DataSource.IDColumn;
                ResponseIdColumnComboBox.ItemsSource = columns;
                ResponseIdColumnComboBox.SelectedItem = DataSource.ResponseIDColumn;
                DateColumnComboBox.ItemsSource = columns;
                DateColumnComboBox.SelectedItem = DataSource.DateColumn;
            }
            else if (DataSource.Type == DataSourceType.SqlSource)
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[1];
                updateDisplayOptions();
                SqlServerAddress.Text = DataSource.Parameters["Server"];
                SqlServerPort.Text = DataSource.Parameters["Port"];
                SqlServerUser.Text = DataSource.Parameters["User"];
                SqlServerPassword.Password = DataSource.Parameters["Password"];
                SqlServerDatabase.Text = DataSource.Parameters["Database"];
                connectToSqlSource();
                if (DataSource.Parameters.ContainsKey("Query") && DataSource.Parameters["Query"] != null)
                {
                    SqlDataTypeComboBox.SelectedItem = SqlSourceTypes[1];
                    SqlQueryTextBox.Text = DataSource.Parameters["Query"];
                }
                else
                {
                    SqlTableComboBox.SelectedItem = DataSource.Parameters["Table"];
                }

                SqlIdColumnComboBox.SelectedItem = DataSource.IDColumn;
                SqlResponseIdColumnComboBox.SelectedItem = DataSource.ResponseIDColumn;
                SqlDateColumnComboBox.SelectedItem = DataSource.DateColumn;
            }
            else if (DataSource.Type == DataSourceType.GeoSource)
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[2];
                if (DataSource.Parameters.ContainsKey("File"))
                {
                    try
                    {
                        summarizeGeoFile(DataSource.Parameters["File"]);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogMessage(LogLevel.Error, string.Format("Unable to load GeoSource from file '{0}'", DataSource.Parameters["File"]), ex);
                    }

                    GeoFileNameTextBox.Text = DataSource.Parameters["File"];
                }
                GeoSource geoSource = DataSource as GeoSource;
                if (geoSource == null)
                {
                    updateDisplayOptions();
                    return;
                }

                if (DataSource.Parameters.ContainsKey("ProjectionName"))
                {
                    string projectionName = DataSource.Parameters["ProjectionName"];
                    var proj = (from p in Projections
                                where p.Name == projectionName
                                select p).FirstOrDefault();
                    if (proj != null)
                    {
                        ProjectionColumnComboBox.SelectedItem = proj;
                    }
                    else
                    {
                        DataSourceTypeInfo info = new DataSourceTypeInfo(projectionName);
                        if (DataSource.Parameters.ContainsKey("Projection"))
                        {
                            info.Data = DataSource.Parameters["Projection"];
                        }
                        Projections.Add(info);
                        ProjectionColumnComboBox.SelectedItem = info;
                    }
                }
                updateDisplayOptions();
            } else if (DataSource.Type == DataSourceType.XmlSource)
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[3];
                updateDisplayOptions();
                if (DataSource.Parameters.ContainsKey("File"))
                {
                    try
                    {
                        XmlFileNameTextBox.Text = DataSource.Parameters["File"];
                        XmlSource xmlSource = DataSource as XmlSource;
                        if (xmlSource == null)
                        {
                            return;
                        }
                        if (DataSource.Parameters.ContainsKey("EmbedXML"))
                        {
                            bool embedXml = false;
                            if (bool.TryParse(DataSource.Parameters["EmbedXML"], out embedXml))
                            {
                                EmbedXmlCheckBox.IsChecked = true;
                            }
                        }

                        List<string> columns = xmlSource.GetColumns();
                        XmlIncidentNodeAutoCompleteBox.ItemsSource = columns;
                        XmlIncidentNodeAutoCompleteBox.SelectedItem = xmlSource.IncidentNode;
                        XmlIncidentIdNodeAutoCompleteBox.ItemsSource = columns;
                        XmlIncidentIdNodeAutoCompleteBox.SelectedItem = xmlSource.IDColumn;
                        XmlResponseNodeAutoCompleteBox.ItemsSource = columns;
                        XmlResponseNodeAutoCompleteBox.SelectedItem = xmlSource.ResponseNode;
                        XmlResponseIdNodeAutoCompleteBox.ItemsSource = columns;
                        XmlResponseIdNodeAutoCompleteBox.SelectedItem = xmlSource.ResponseIDColumn;
                        XmlDateNodeAutoCompleteBox.ItemsSource = columns;
                        XmlDateNodeAutoCompleteBox.SelectedItem = xmlSource.DateColumn;
                    } catch (Exception ex)
                    {
                        LogHelper.LogMessage(LogLevel.Error, string.Format("Unable to load XmlSource from file '{0}'", DataSource.Parameters["File"]), ex);
                        return;
                    }
                }
            }
            else if (DataSource.Type == DataSourceType.DailyDigestXmlSource)
            {
                DataSourceTypeComboBox.SelectedItem = DataSourceTypes[4];
                updateDisplayOptions();
                if (DataSource.Parameters.ContainsKey("Directory"))
                {
                    try
                    {
                        DailyDigestFolderNameTextBox.Text = DataSource.Parameters["Directory"];
                        DailyDigestXmlSource dailyDigestSource = DataSource as DailyDigestXmlSource;
                        if (dailyDigestSource == null)
                        {
                            return;
                        }

                        List<string> columns = dailyDigestSource.GetColumns();
                        DailyDigestIncidentNodeAutoCompleteBox.ItemsSource = columns;
                        DailyDigestIncidentNodeAutoCompleteBox.SelectedItem = dailyDigestSource.IncidentNode;
                        DailyDigestIncidentIdNodeAutoCompleteBox.ItemsSource = columns;
                        DailyDigestIncidentIdNodeAutoCompleteBox.SelectedItem = dailyDigestSource.IDColumn;
                        DailyDigestResponseNodeAutoCompleteBox.ItemsSource = columns;
                        DailyDigestResponseNodeAutoCompleteBox.SelectedItem = dailyDigestSource.ResponseNode;
                        DailyDigestResponseIdNodeAutoCompleteBox.ItemsSource = columns;
                        DailyDigestResponseIdNodeAutoCompleteBox.SelectedItem = dailyDigestSource.ResponseIDColumn;
                        DailyDigestDateNodeAutoCompleteBox.ItemsSource = columns;
                        DailyDigestDateNodeAutoCompleteBox.SelectedItem = dailyDigestSource.DateColumn;
                    } catch (Exception ex)
                    {
                        LogHelper.LogMessage(LogLevel.Error, $"Unable to load DailyDigestSource from folder '{DataSource.Parameters["Directory"]}'", ex);
                        return;
                    }
                }
            }
            
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChangesMade && SaveButton.IsEnabled)
            {
                MessageBoxResult result = MessageBox.Show("Save changes before closing?", "Save Changes?", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (result == MessageBoxResult.No)
                {
                    DataSource.Disconnect();
                    DataSource = OriginalDataSource;
                }
            }
            else
            {
                DataSource.Disconnect();
                DataSource = OriginalDataSource;
            }
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                updateParameters();
                DataSource.Disconnect();
                Close();
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Unable to save Data Source", true);
            } finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void updateParameters()
        {
            if (DataSource is CsvSource)
            {
                DataSource.Parameters["File"] = CsvFileNameTextBox.Text;
                bool embedCsv;
                if (DataSource.Parameters.ContainsKey("EmbedCSV") &&
                    bool.TryParse(DataSource.Parameters["EmbedCSV"], out embedCsv)
                    && embedCsv)
                {
                    string fileContents = File.ReadAllText(CsvFileNameTextBox.Text);
                    string compressedContents = LZString.compressToUTF16(fileContents);
                    DataSource.Parameters["CompressedContentsTimeStamp"] = File.GetLastWriteTime(CsvFileNameTextBox.Text).Ticks.ToString();
                    DataSource.Parameters["CompressedContents"] = compressedContents;
                }
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
                    SqlDateColumnComboBox.Visibility = Visibility.Visible;
                }
                else
                {
                    if (DataSource.Parameters.ContainsKey("Table"))
                    {
                        DataSource.Parameters.Remove("Table");
                    }
                    DataSource.Parameters["Query"] = SqlQueryTextBox.Text;
                    SqlDateColumnComboBox.Visibility = Visibility.Hidden;
                }
            }
            else if (DataSource is GeoSource)
            {
                DataSource.Parameters["File"] = GeoFileNameTextBox.Text;
                DataSourceTypeInfo info = ProjectionColumnComboBox.SelectedItem as DataSourceTypeInfo;
                DataSource.Parameters["Projection"] = info.Data;
                DataSource.Parameters["ProjectionName"] = info.Name;
            }
            else if (DataSource is XmlSource)
            {
                DataSource.Parameters["File"] = XmlFileNameTextBox.Text;
                bool embedXml;
                if (DataSource.Parameters.ContainsKey("EmbedXML") &&
                    bool.TryParse(DataSource.Parameters["EmbedXML"], out embedXml)
                    && embedXml)
                {
                    string fileContents = File.ReadAllText(XmlFileNameTextBox.Text);
                    string compressedContents = LZString.compressToUTF16(fileContents);
                    DataSource.Parameters["CompressedContentsTimeStamp"] = File.GetLastWriteTime(XmlFileNameTextBox.Text).Ticks.ToString();
                    DataSource.Parameters["CompressedContents"] = compressedContents;
                }
            }
            else if (DataSource is DailyDigestXmlSource)
            {
                DataSource.Parameters["Directory"] = DailyDigestFolderNameTextBox.Text;
            }
            else
            {
                throw new NotImplementedException();
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

            if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.CsvSource)
            {
                DataSource = new CsvSource();
                ChangesMade = true;
            }
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.SqlSource)
            {
                DataSource = new SqlSource();
                ChangesMade = true;
            }
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.GeoSource)
            {
                DataSource = new GeoSource();
                ChangesMade = true;
            } else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.XmlSource)
            {
                DataSource = new XmlSource();
                ChangesMade = true;
            }
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.DailyDigestXmlSource)
            {
                DataSource = new DailyDigestXmlSource();
                ChangesMade = true;
            }
            updateDisplayOptions();
        }

        private void updateDisplayOptions()
        {
            if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.CsvSource)
            {
                CsvOptionsGrid.Visibility = Visibility.Visible;
                CsvOptionsButtons.Visibility = Visibility.Visible;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsPanel.Visibility = Visibility.Hidden;
                GeoOptionsGrid.Visibility = Visibility.Hidden;
                GeoOptionsPanel.Visibility = Visibility.Hidden;
                XmlOptionsGrid.Visibility = Visibility.Hidden;
                XmlOptionsPanel.Visibility = Visibility.Hidden;
                DailyDigestOptionsGrid.Visibility = Visibility.Hidden;
                DailyDigestOptionsPanel.Visibility = Visibility.Hidden;
            }
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.SqlSource)
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Visible;
                SqlOptionsPanel.Visibility = Visibility.Visible;
                GeoOptionsGrid.Visibility = Visibility.Hidden;
                GeoOptionsPanel.Visibility = Visibility.Hidden;
                XmlOptionsGrid.Visibility = Visibility.Hidden;
                XmlOptionsPanel.Visibility = Visibility.Hidden;
                DailyDigestOptionsGrid.Visibility = Visibility.Hidden;
                DailyDigestOptionsPanel.Visibility = Visibility.Hidden;
            }
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.GeoSource)
            {
                CsvOptionsGrid.Visibility = Visibility.Collapsed;
                CsvOptionsButtons.Visibility = Visibility.Collapsed;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsPanel.Visibility = Visibility.Hidden;
                GeoOptionsGrid.Visibility = Visibility.Visible;
                GeoOptionsPanel.Visibility = Visibility.Visible;
                XmlOptionsGrid.Visibility = Visibility.Hidden;
                XmlOptionsPanel.Visibility = Visibility.Hidden;
                DailyDigestOptionsGrid.Visibility = Visibility.Hidden;
                DailyDigestOptionsPanel.Visibility = Visibility.Hidden;
            } 
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.XmlSource)
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsPanel.Visibility = Visibility.Hidden;
                GeoOptionsGrid.Visibility = Visibility.Hidden;
                GeoOptionsPanel.Visibility = Visibility.Hidden;
                XmlOptionsGrid.Visibility = Visibility.Visible;
                XmlOptionsPanel.Visibility = Visibility.Visible;
                DailyDigestOptionsGrid.Visibility = Visibility.Hidden;
                DailyDigestOptionsPanel.Visibility = Visibility.Hidden;
            }
            else if (DataSourceTypeComboBox.SelectedItem == DataSourceTypeInfo.DailyDigestXmlSource)
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsPanel.Visibility = Visibility.Hidden;
                GeoOptionsGrid.Visibility = Visibility.Hidden;
                GeoOptionsPanel.Visibility = Visibility.Hidden;
                XmlOptionsGrid.Visibility = Visibility.Hidden;
                XmlOptionsPanel.Visibility = Visibility.Hidden;
                DailyDigestOptionsGrid.Visibility = Visibility.Visible;
                DailyDigestOptionsPanel.Visibility = Visibility.Visible;
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
                CsvFileNameTextBox.Text = ofd.FileName;

                List<string> columns = source.GetColumns();
                IdColumnComboBox.ItemsSource = columns;
                ResponseIdColumnComboBox.ItemsSource = columns;
                DateColumnComboBox.ItemsSource = columns;
                ChangesMade = true;
            }
            catch (Exception ex)
            {
                string name = "Unknown";
                if (info != null)
                    name = info.Name;
                LogHelper.LogMessage(LogLevel.Error, string.Format("Exception reading CSV File '{0}'", name), ex);
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
                SaveButton.IsEnabled = true;
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
                MessageBox.Show(dataSource.ErrorMessage);  // sorry, I don't have time to figure out WPF enough to do this more reasonably.  (20200606 CDN).
            }
            List<string> tables = dataSource.GetTables();
            tables.Sort();
            SqlTableComboBox.ItemsSource = tables;
            if (dataSource.Parameters.ContainsKey("Query"))
            {
                List<string> columns = dataSource.GetColumns();
                SqlIdColumnComboBox.ItemsSource = columns;
                SqlResponseIdColumnComboBox.ItemsSource = columns;
                SqlDateColumnComboBox.ItemsSource = columns;
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
            }
            else
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

        private void ProjectionColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataSourceTypeInfo info = (DataSourceTypeInfo)ProjectionColumnComboBox.SelectedItem;
            DataSource.Parameters["ProjectionName"] = info.Name;
            DataSource.Parameters["Projection"] = info.Data;
        }

        private void AddProjectionButton_Click(object sender, RoutedEventArgs e)
        {
            SingleValueForm svf = new SingleValueForm("Enter Projection Name", "Enter Projection Name:", "Custom Projection");
            svf.ShowDialog();
            if (svf.DialogResult == false)
                return;

            TextInputDialog tid = new TextInputDialog("Enter Projection", "Enter Projection", null);
            tid.ShowDialog();
            if (tid.DialogResult == false)
                return;

            string projectionName = svf.Result;
            string projection = tid.Result;
            DataSourceTypeInfo newProjection = new DataSourceTypeInfo(projectionName);
            newProjection.Data = projection;

            Projections.Add(newProjection);

            ProjectionColumnComboBox.SelectedItem = newProjection;
        }

        private void GeoFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "shp";
            ofd.Filter = "SHP Files (*.shp)|*.shp|GeoJSON Files (*.geojson)|*.geojson|Zipped SHP Files (*.zip)|*.zip|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == false)
            {
                return;
            }

            try
            {
                FileInfo file = new FileInfo(ofd.FileName);
                GeoSource source = DataSource as GeoSource;
                if (source == null)
                {
                    IDataSource lastSource = DataSource;
                    DataSource = source = new GeoSource();
                    source.Name = lastSource.Name;
                }
                source.GeoFile = file;

                string projectionName, projection;
                if (GeoSource.GetProjectionFromFile(file.FullName, out projectionName, out projection))
                {
                    source.Parameters["ProjectionName"] = projectionName;
                    source.Parameters["Projection"] = projection;
                }
                var existingProjection = (from DataSourceTypeInfo t in Projections
                                          where t.Name == projectionName
                                          select t).FirstOrDefault();
                if (existingProjection == null)
                {
                    DataSourceTypeInfo newProjection = new DataSourceTypeInfo(projectionName);
                    newProjection.Data = projection;
                    Projections.Add(newProjection);
                    ProjectionColumnComboBox.SelectedItem = newProjection;
                }
                else
                {
                    ProjectionColumnComboBox.SelectedItem = existingProjection;
                }

                summarizeGeoFile(ofd.FileName);
                GeoFileNameTextBox.Text = ofd.FileName;
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, string.Format("Unable to load polygons from file '{0}'", ofd.FileName), ex);
            }
        }

        private void summarizeGeoFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);
            List<AnnotatedObject<Geometry>> geoms = new List<AnnotatedObject<Geometry>>();

            if (file.Extension == ".shp" || file.Extension == ".zip")
            {
                geoms.AddRange(GeoSource.GetGeomsFromShpFile(fileName));
            }
            else if (file.Extension == ".geojson")
            {
                string geoJson = File.ReadAllText(fileName);
                geoms.AddRange(GeoSource.GetGeomsFromGeoJson(geoJson));
            }

            HashSet<string> fields = new HashSet<string>();
            foreach (AnnotatedObject<Geometry> geom in geoms)
            {
                foreach (string field in geom.Data.Keys)
                {
                    fields.Add(field);
                }
            }

            StringBuilder summaryBuilder = new StringBuilder(string.Format("File contains {0} shapes with {1} properties:\n------------------------------------------------------------\n\n", geoms.Count, fields.Count));
            int index = 0;
            foreach (AnnotatedObject<Geometry> geom in geoms)
            {
                summaryBuilder.AppendFormat("[{0}]: Type={1} Area={2}\n", index, geom.Object.GeometryType, geom.Object.Area);
                foreach (KeyValuePair<string, object> kvp in geom.Data)
                {
                    string value = kvp.Value != null ? kvp.Value.ToString() : "null";
                    summaryBuilder.AppendLine(string.Format("{0}: {1}", kvp.Key, value));
                }
                summaryBuilder.AppendLine();
                index++;
            }
            GeoSummaryTextBox.Text = summaryBuilder.ToString();
            CsvOptionsButtons.Visibility = Visibility.Hidden;
            CsvOptionsGrid.Visibility = Visibility.Hidden;
        }

        private void EmbedCsvCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CsvSource source = DataSource as CsvSource;
                source.Parameters["EmbedCSV"] = (bool)EmbedCsvCheckBox.IsChecked ? "true" : "false";
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Unable to toggle Embed CSV", true);
            }
        }

        public static IDataSource CreateDataSourceFromFile(string fileName)
        {
            FileInfo info = new FileInfo(fileName);
            string nameWithoutExtension = info.Name.Substring(0, info.Name.Length - info.Extension.Length);
            DataSourceTypeInfo type;
            IDataSource output = null;
            if (SourceTypesByExtension.TryGetValue(info.Extension, out type))
            {
                if (type == DataSourceTypeInfo.CsvSource)
                {
                    output = new CsvSource(info);
                }
                else if (type == DataSourceTypeInfo.GeoSource)
                {
                    GeoSource source = new GeoSource();
                    source.GeoFile = info;

                    string projectionName, projection;
                    if (GeoSource.GetProjectionFromFile(info.FullName, out projectionName, out projection))
                    {
                        source.Parameters["ProjectionName"] = projectionName;
                        source.Parameters["Projection"] = projection;
                    }
                    output = source;
                }
                else if (type == DataSourceTypeInfo.SqlSource)
                {
                    string query = File.ReadAllText(info.FullName);
                    output = new SqlSource();
                    output.Parameters["Server"] = string.Empty;
                    output.Parameters["Port"] = string.Empty;
                    output.Parameters["User"] = string.Empty;
                    output.Parameters["Password"] = string.Empty;
                    output.Parameters["Database"] = string.Empty;
                    output.Parameters["Query"] = query;
                }
            }

            return output;
        }

        private void DataSourceEditorGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (fileNames.Length == 1)
                {
                    FileInfo info = new FileInfo(fileNames[0]);
                    string nameWithoutExtension = info.Name.Substring(0, info.Name.Length - info.Extension.Length);
                    DataSourceTypeInfo type;
                    if (SourceTypesByExtension.TryGetValue(info.Extension, out type))
                    {
                        IDataSource newSource = CreateDataSourceFromFile(fileNames[0]);
                        DataSource = newSource;
                        updateDisplayForDataSource();
                    }
                }
            }
        }

        private void DataSourceEditorGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
                bool dragAllowed = fileNames.Length == 1;

                if (dragAllowed)
                {    
                    foreach (string fileName in fileNames)
                    {
                        FileInfo info = new FileInfo(fileName);
                        if (!SourceTypesByExtension.ContainsKey(info.Extension))
                        {
                            dragAllowed = false;
                        }
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

        private void XmlFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileInfo info = null;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "xml";
                ofd.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == false)
                {
                    return;
                }

                XmlSource source = DataSource as XmlSource;
                if (source == null)
                {
                    IDataSource lastSource = DataSource;
                    DataSource = source = new XmlSource();
                    if (lastSource != null)
                    {
                        DataSource.Name = lastSource.Name;
                    }
                }
                info = new FileInfo(ofd.FileName);
                if (source.XmlFile != null && (info.FullName == source.XmlFile.FullName))
                {
                    return;
                }
                source.XmlFile = new FileInfo(ofd.FileName);
                XmlFileNameTextBox.Text = ofd.FileName;

                List<string> nodes = source.GetColumns();
                XmlIncidentNodeAutoCompleteBox.ItemsSource = nodes;
                XmlIncidentIdNodeAutoCompleteBox.ItemsSource = nodes;
                XmlResponseNodeAutoCompleteBox.ItemsSource = nodes;
                XmlResponseIdNodeAutoCompleteBox.ItemsSource = nodes;
                XmlDateNodeAutoCompleteBox.ItemsSource = nodes;
                ChangesMade = true;
            }
            catch (Exception ex)
            {
                string name = "Unknown";
                if (info != null)
                    name = info.Name;
                LogHelper.LogMessage(LogLevel.Error, string.Format("Exception reading CSV File '{0}'", name), ex);
            }
        }

        private void XmlIncidentNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            XmlSource source = DataSource as XmlSource;
            source.IncidentNode = XmlIncidentNodeAutoCompleteBox.SelectedItem as string;
        }

        private void XmlIncidentIdNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            XmlSource source = DataSource as XmlSource;
            source.IDColumn = XmlIncidentIdNodeAutoCompleteBox.SelectedItem as string;
        }

        private void XmlResponseNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            XmlSource source = DataSource as XmlSource;
            source.ResponseNode = XmlResponseNodeAutoCompleteBox.SelectedItem as string;
        }

        private void XmlResponseIdNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            XmlSource source = DataSource as XmlSource;
            source.ResponseIDColumn = XmlResponseIdNodeAutoCompleteBox.SelectedItem as string;
        }

        private void XmlDateNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            XmlSource source = DataSource as XmlSource;
            source.DateColumn = XmlDateNodeAutoCompleteBox.SelectedItem as string;
        }

        private void SummarizeXmlButton_Click(object sender, RoutedEventArgs e)
        {
            FileInfo xmlFileInfo = new FileInfo(XmlFileNameTextBox.Text);
            XDocument doc = XDocument.Load(XmlFileNameTextBox.Text);
            var nodes = doc.Descendants().ToList();
            var topLevel = doc.Root.Nodes().ToList();
            long bytes = xmlFileInfo.Length;

            XmlSummaryTextBox.Text = string.Format("XML File: {0} was last modified at {1} and is {2} bytes long.\n\nThe file contains {3} top level nodes and {4} nodes total.",
                xmlFileInfo.Name, xmlFileInfo.LastWriteTime, bytes, topLevel.Count, nodes.Count);

        }

        private void EmbedXmlCheckBox_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DailyDigestFolderSelectButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo directory = null;
            try
            {
                using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }
                    directory = new DirectoryInfo(fbd.SelectedPath);
                }

                DailyDigestXmlSource source = DataSource as DailyDigestXmlSource;
                if (source == null)
                {
                    IDataSource lastSource = DataSource;
                    DataSource = source = new DailyDigestXmlSource();
                    if (lastSource != null)
                    {
                        DataSource.Name = lastSource.Name;
                    }
                }
                if (source.DailyDigestDirectory != null && (directory.FullName == source.DailyDigestDirectory.FullName))
                {
                    return;
                }
                source.DailyDigestDirectory = new DirectoryInfo(directory.FullName);
                DailyDigestFolderNameTextBox.Text = directory.FullName;

                List<string> nodes = source.GetColumns();
                DailyDigestIncidentNodeAutoCompleteBox.ItemsSource = nodes;
                DailyDigestIncidentIdNodeAutoCompleteBox.ItemsSource = nodes;
                DailyDigestResponseNodeAutoCompleteBox.ItemsSource = nodes;
                DailyDigestResponseIdNodeAutoCompleteBox.ItemsSource = nodes;
                DailyDigestDateNodeAutoCompleteBox.ItemsSource = nodes;
                ChangesMade = true;
            }
            catch (Exception ex)
            {
                string name = "Unknown";
                if (directory != null)
                    name = directory.Name;
                LogHelper.LogMessage(LogLevel.Error, string.Format("Exception reading from directory '{0}'", name), ex);
            }
        }

        private void DailyDigestIncidentNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DailyDigestXmlSource source = DataSource as DailyDigestXmlSource;
            source.IncidentNode = DailyDigestIncidentNodeAutoCompleteBox.SelectedItem as string;
        }

        private void DailyDigestIncidentIdNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DailyDigestXmlSource source = DataSource as DailyDigestXmlSource;
            source.IDColumn = DailyDigestIncidentIdNodeAutoCompleteBox.SelectedItem as string;
        }

        private void DailyDigestResponseNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DailyDigestXmlSource source = DataSource as DailyDigestXmlSource;
            source.ResponseNode = DailyDigestResponseNodeAutoCompleteBox.SelectedItem as string;
        }

        private void DailyDigestResponseIdNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DailyDigestXmlSource source = DataSource as DailyDigestXmlSource;
            source.ResponseIDColumn = DailyDigestResponseIdNodeAutoCompleteBox.SelectedItem as string;
        }

        private void DateColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CsvSource source = DataSource as CsvSource;
            source.DateColumn = DateColumnComboBox.SelectedItem as string;
        }

        private void SqlDateColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SqlSource source = DataSource as SqlSource;
            source.DateColumn = SqlDateColumnComboBox.SelectedItem as string;
        }

        private void DailyDigestDateNodeAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DailyDigestXmlSource source = DataSource as DailyDigestXmlSource;
            source.DateColumn = DailyDigestDateNodeAutoCompleteBox.SelectedItem as string;
        }

        private void SummarizeDailyDigestButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class DataSourceTypeInfo
    {
        public static DataSourceTypeInfo CsvSource = new DataSourceTypeInfo("CSV File");
        public static DataSourceTypeInfo SqlSource = new DataSourceTypeInfo("SQL Server");
        public static DataSourceTypeInfo GeoSource = new DataSourceTypeInfo("Geographical Data");
        public static DataSourceTypeInfo XmlSource = new DataSourceTypeInfo("XML File");
        public static DataSourceTypeInfo DailyDigestXmlSource = new DataSourceTypeInfo("Daily Digest XML Source");

        public string Name { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;

        public DataSourceTypeInfo(string _name, string _data = "")
        {
            Name = _name;
            Data = _data;
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
