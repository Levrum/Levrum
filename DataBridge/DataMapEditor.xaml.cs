using System;
using System.Collections.Generic;
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

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for DataMapEditor.xaml
    /// </summary>
    public partial class DataMapEditor : UserControl
    {
        public MainDataBridgeWindow Window { get; set; } = null;
        public DataMap DataMap { get; protected set; }

        public DataMapEditor(DataMap _map = null)
        {
            InitializeComponent();
            DataMap = _map;
            IncidentDataListBox.ItemsSource = _map.IncidentDataMappings;
            ResponseDataListBox.ItemsSource = _map.ResponseDataMappings;
            BenchmarkListBox.ItemsSource = _map.BenchmarkMappings;
            UpdateStaticMappingButtons();
        }

        private void AddIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), null, false);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                DataMap.IncidentDataMappings.Add(dialog.Result);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void RemoveIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            DataMapping incidentMapping = IncidentDataListBox.SelectedItem as DataMapping;
            if (incidentMapping != null)
            {
                DataMap.IncidentDataMappings.Remove(incidentMapping);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void AddResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {
            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), null, false);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                DataMap.ResponseDataMappings.Add(dialog.Result);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void RemoveResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {
            DataMapping responseMapping = ResponseDataListBox.SelectedItem as DataMapping;
            if (responseMapping != null)
            {
                DataMap.ResponseDataMappings.Remove(responseMapping);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void IncidentTimeButton_Click(object sender, RoutedEventArgs e)
        {
            editStaticMapping("Time");
        }

        private void IncidentLatitudeButton_Click(object sender, RoutedEventArgs e)
        {
            editStaticMapping("Latitude");
        }

        private void IncidentLongitudeButton_Click(object sender, RoutedEventArgs e)
        {
            editStaticMapping("Longitude");
        }

        private void IncidentLocationButton_Click(object sender, RoutedEventArgs e)
        {
            editStaticMapping("Location");
        }

        private void editStaticMapping(string fieldName)
        {
            DataMapping oldMapping = (from m in DataMap.IncidentMappings
                                      where m.Field == fieldName
                                      select m).FirstOrDefault();

            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), fieldName, true);
            if (oldMapping != null)
            {
                dialog.Result = oldMapping;
            }

            dialog.ShowDialog();
            DataMapping newMapping = dialog.Result;
            if (oldMapping != null)
            {
                DataMap.IncidentMappings.Remove(oldMapping);
            }
            DataMap.IncidentMappings.Add(newMapping);
            if (Window != null)
            {
                Window.SetChangesMade(DataMap, true);
            }
            UpdateStaticMappingButtons();
        }

        public void UpdateStaticMappingButtons()
        {
            DataMapping timeMapping = (from m in DataMap.IncidentMappings
                                       where m != null && m.Field == "Time"
                                       select m).FirstOrDefault();

            DataMapping latMapping = (from m in DataMap.IncidentMappings
                                       where m != null && m.Field == "Latitude"
                                       select m).FirstOrDefault();

            DataMapping longMapping = (from m in DataMap.IncidentMappings
                                       where m != null && m.Field == "Longitude"
                                       select m).FirstOrDefault();

            DataMapping locMapping = (from m in DataMap.IncidentMappings
                                       where m != null && m.Field == "Location"
                                       select m).FirstOrDefault();

            IncidentTimeButtonText.Text = getStaticMappingButtonText(timeMapping);
            IncidentLatitudeButtonText.Text = getStaticMappingButtonText(latMapping);
            IncidentLongitudeButtonText.Text = getStaticMappingButtonText(longMapping);
            IncidentLocationButtonText.Text = getStaticMappingButtonText(locMapping);
        }

        private string getStaticMappingButtonText(DataMapping mapping)
        {
            try
            {
                return string.Format("{0}: {1}", mapping.Column.DataSource.Name, mapping.Column.ColumnName);
            } catch (Exception ex)
            {
                return "Click here to select...";
            }
        }

        private void AddBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), null, false);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                DataMap.BenchmarkMappings.Add(dialog.Result);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void RemoveBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            DataMapping benchmarkMapping = BenchmarkListBox.SelectedItem as DataMapping;
            if (benchmarkMapping != null)
            {
                DataMap.BenchmarkMappings.Remove(benchmarkMapping);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void IncidentDataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveIncidentFieldButton.IsEnabled = IncidentDataListBox.SelectedIndex != -1;
            EditIncidentFieldButton.IsEnabled = IncidentDataListBox.SelectedIndex != -1;
        }

        private void ResponseDataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveResponseFieldButton.IsEnabled = ResponseDataListBox.SelectedIndex != -1;
            EditResponseFieldButton.IsEnabled = ResponseDataListBox.SelectedIndex != -1;
        }

        private void BenchmarkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveBenchmarkButton.IsEnabled = BenchmarkListBox.SelectedIndex != -1;
            EditBenchmarkButton.IsEnabled = BenchmarkListBox.SelectedIndex != -1;
        }

        private void EditIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            DataMapping selectedField = IncidentDataListBox.SelectedItem as DataMapping;
            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), selectedField);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                int index = DataMap.IncidentMappings.IndexOf(selectedField);
                DataMap.IncidentMappings.Remove(selectedField);
                DataMap.IncidentMappings.Insert(index, dialog.Result);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void EditResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {
            DataMapping selectedField = ResponseDataListBox.SelectedItem as DataMapping;
            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), selectedField);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                int index = DataMap.ResponseDataMappings.IndexOf(selectedField);
                DataMap.ResponseDataMappings.Remove(selectedField);
                DataMap.ResponseDataMappings.Insert(index, dialog.Result);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }

        private void EditBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            DataMapping selectedField = BenchmarkListBox.SelectedItem as DataMapping;
            ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), selectedField);
            dialog.ShowDialog();
            if (dialog.Result != null)
            {
                int index = DataMap.BenchmarkMappings.IndexOf(selectedField);
                DataMap.BenchmarkMappings.Remove(selectedField);
                DataMap.BenchmarkMappings.Insert(index, dialog.Result);
                if (Window != null)
                {
                    Window.SetChangesMade(DataMap, true);
                }
            }
        }
    }
}
