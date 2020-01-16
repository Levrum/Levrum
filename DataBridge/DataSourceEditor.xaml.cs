using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Levrum.Data.Classes;
using Levrum.Data.Map;
using Levrum.Data.Sources;

using Levrum.Utils.Data;
using Levrum.Utils.Geography;

using Levrum.UI.WPF;

using Microsoft.Win32;
using Microsoft.Data.SqlClient;

using ProjNet;

using NetTopologySuite.IO.ShapeFile;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Point = NetTopologySuite.Geometries.Point;
using NetTopologySuite.IO;


using Newtonsoft.Json;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for DataSourceEditor.xaml
    /// </summary>
    public partial class DataSourceEditor : Window
    {
        public IDataSource DataSource { get; set; } = null;
        private IDataSource OriginalDataSource { get; set; } = null;

        List<DataSourceTypeInfo> DataSourceTypes { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { "CSV File", "SQL Server", "GeoData" });

        List<DataSourceTypeInfo> SqlSourceTypes { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { "Table", "Query" });

        List<DataSourceTypeInfo> Projections { get; } = new List<DataSourceTypeInfo>(new DataSourceTypeInfo[] { "EPSG:3857 Pseudo-Mercator", "EPSG:4326 Lat-Long" });

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
                NameTextBox.Text = _dataSource.Name;
                if (DataSource.Type == DataSourceType.CsvSource)
                {
                    DataSourceTypeComboBox.SelectedItem = DataSourceTypes[0];
                    updateDataSourceOptionsDisplay();
                    if (_dataSource.Parameters.ContainsKey("File")) {
                        CsvFileNameTextBox.Text = _dataSource.Parameters["File"];
                    }
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
                    }
                    else
                    {
                        SqlTableComboBox.SelectedItem = DataSource.Parameters["Table"];
                    }

                    SqlIdColumnComboBox.SelectedItem = DataSource.IDColumn;
                    SqlResponseIdColumnComboBox.SelectedItem = DataSource.ResponseIDColumn;
                }
                else if (DataSource.Type == DataSourceType.GeoSource)
                {
                    DataSourceTypeComboBox.SelectedItem = DataSourceTypes[2];
                    updateDataSourceOptionsDisplay();
                    if (_dataSource.Parameters.ContainsKey("File"))
                    {
                        try
                        {
                            summarizeGeoFile(_dataSource.Parameters["File"]);
                        } catch (Exception ex)
                        {
                            MessageBox.Show(string.Format("Unable to load GeoSource from file '{0}': {1}", _dataSource.Parameters["File"], ex.Message));
                        }

                        GeoFileNameTextBox.Text = _dataSource.Parameters["File"];
                    }
                    GeoSource geoSource = _dataSource as GeoSource;
                    if (geoSource == null)
                    {
                        return;
                    }

                    if (_dataSource.Parameters.ContainsKey("ProjectionName"))
                    {
                        string projectionName = _dataSource.Parameters["ProjectionName"];
                        ProjectionColumnComboBox.SelectedItem = projectionName;
                    }
                }
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChangesMade)
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
            updateParameters();
            DataSource.Disconnect();
            Close();
        }

        private void updateParameters()
        {
            if (DataSource is CsvSource)
            {
                DataSource.Parameters["File"] = CsvFileNameTextBox.Text;
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
                }
                else
                {
                    if (DataSource.Parameters.ContainsKey("Table"))
                    {
                        DataSource.Parameters.Remove("Table");
                    }
                    DataSource.Parameters["Query"] = SqlQueryTextBox.Text;
                }
            }
            else if (DataSource is GeoSource)
            {
                DataSource.Parameters["File"] = GeoFileNameTextBox.Text;
                DataSource.Parameters["ProjectionName"] = ProjectionColumnComboBox.SelectedItem as string;
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

            if (DataSourceTypeComboBox.SelectedItem.ToString() == "CSV File")
            {
                DataSource = new CsvSource();
                ChangesMade = true;
            }
            else if (DataSourceTypeComboBox.SelectedItem.ToString() == "SQL Server")
            {
                DataSource = new SqlSource();
                ChangesMade = true;
            }
            else if (DataSourceTypeComboBox.SelectedItem.ToString() == "GeoData")
            {
                DataSource = new GeoSource();
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
                GeoOptionsGrid.Visibility = Visibility.Hidden;
                GeoOptionsPanel.Visibility = Visibility.Hidden;
            }
            else if (DataSourceTypeComboBox.SelectedItem.ToString() == "SQL Server")
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Visible;
                SqlOptionsPanel.Visibility = Visibility.Visible;
                GeoOptionsGrid.Visibility = Visibility.Hidden;
                GeoOptionsPanel.Visibility = Visibility.Hidden;
            }
            else if (DataSourceTypeComboBox.SelectedItem.ToString() == "GeoData")
            {
                CsvOptionsGrid.Visibility = Visibility.Hidden;
                CsvOptionsButtons.Visibility = Visibility.Hidden;
                SqlOptionsGrid.Visibility = Visibility.Hidden;
                SqlOptionsPanel.Visibility = Visibility.Hidden;
                GeoOptionsGrid.Visibility = Visibility.Visible;
                GeoOptionsPanel.Visibility = Visibility.Visible;
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
                ChangesMade = true;
            }
            catch (Exception ex)
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
            DataSource.Parameters["ProjectionName"] = ProjectionColumnComboBox.SelectedItem as string;
            if (ProjectionColumnComboBox.SelectedIndex == 0)
            {   
                DataSource.Parameters["Projection"] = CoordinateConverter.WebMercator.WKT;
            } else if (ProjectionColumnComboBox.SelectedIndex == 1)
            {
                DataSource.Parameters["Projection"] = CoordinateConverter.WGS84.WKT;
            } else
            {
                DataSourceTypeInfo info = (DataSourceTypeInfo)ProjectionColumnComboBox.SelectedItem;
                DataSource.Parameters["Projection"] = info.Data;
            }
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

            ProjectionColumnComboBox.ItemsSource = Projections;
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
                summarizeGeoFile(ofd.FileName);
                GeoFileNameTextBox.Text = ofd.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to load polygons from file '{0}': {1}", ofd.FileName, ex.Message));
            }
        }

        private void summarizeGeoFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);
            GeoSource source = DataSource as GeoSource;
            if (source == null)
            {
                IDataSource lastSource = DataSource;
                DataSource = source = new GeoSource();
                source.Name = lastSource.Name;
            }
            source.GeoFile = file;
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
        }


        /*
        private List<AnnotatedObject<LPolygon>> getPolysFromGeoms(List<AnnotatedObject<Geometry>> geoms)
        {
            List<AnnotatedObject<LPolygon>> output = new List<AnnotatedObject<LPolygon>>();

            foreach (AnnotatedObject<Geometry> geom in geoms)
            {
                List<LPolygon> polys = getPolysFromGeom(geom.Object);
                foreach (LPolygon poly in polys)
                {
                    AnnotatedObject<LPolygon> aPoly = new AnnotatedObject<LPolygon>(poly);
                    foreach (KeyValuePair<string, object> kvp in geom.Data)
                    {
                        aPoly.Data.Add(kvp.Key, kvp.Value);
                    }
                    output.Add(aPoly);
                }
            }

            return output;
        }

        private List<LPolygon> getPolysFromGeom(Geometry geom)
        {
            List<LPolygon> output = new List<LPolygon>();
            switch (geom.GeometryType)
            {
                case "MultiPolygon":
                    SMPolygon mp = geom as SMPolygon;
                    foreach (Geometry subGeom in mp.Geometries)
                    {
                        output.Add(getPolyFromGeom(subGeom));
                    }
                    break;
                case "Polygon":
                    output.Add(getPolyFromGeom(geom));
                    break;
                default:
                    break;
            }

            return output;
        }

        private LPolygon getPolyFromGeom(Geometry geom)
        {
            LPolygon poly = new LPolygon();
            foreach (Coordinate v in geom.Coordinates)
            {
                poly.AddPoint(v.X, v.Y);
            }

            return poly;
        }
        */
            }

    public class DataSourceTypeInfo
    {
        public string Name { get; set; } = "";
        public string Data { get; set; } = "";

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

        public static implicit operator string(DataSourceTypeInfo _info)
        {
            return _info.Name;
        }
    }
}
