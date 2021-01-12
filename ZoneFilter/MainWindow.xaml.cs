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

using Newtonsoft.Json;

using Levrum.Data.Classes;

namespace ZoneFilter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            ofd.Title = "Select Incident JSON File";
            ofd.DefaultExt = "json";
            ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (ofd.ShowDialog() == false)
            {
                return;
            }

            FileInfo file = new FileInfo(ofd.FileName);
            string filename = file.Name.Substring(0, file.Name.Length - file.Extension.Length);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = string.Format("{0}\\Levrum", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            sfd.Title = "Select Output JSON File";
            sfd.FileName = string.Format("{0} (Filtered).json", filename);
            sfd.DefaultExt = "json";
            sfd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (sfd.ShowDialog() == false)
            {
                return;
            }

            string incidentJson = File.ReadAllText(ofd.FileName);
            DataSet<IncidentData> dataset = JsonConvert.DeserializeObject<DataSet<IncidentData>>(incidentJson);

            DataSet<IncidentData> output = new DataSet<IncidentData>();

            foreach (IncidentData incident in dataset)
            {
                if (incident.Data.ContainsKey("Zone"))
                {
                    output.Add(incident);
                }
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;
            settings.Formatting = Formatting.Indented;
            using (TextWriter writer = File.CreateText(sfd.FileName))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.TypeNameHandling = TypeNameHandling.Auto;
                serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                serializer.Serialize(writer, output);
            }

            MessageBox.Show(string.Format("Incidents saved as JSON file '{0}'", sfd.FileName));
        }
    }
}
