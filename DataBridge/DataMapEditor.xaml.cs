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
using Levrum.Utils;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for DataMapEditor.xaml
    /// </summary>
    public partial class DataMapEditor : UserControl
    {
        private MainDataBridgeWindow m_window = null;
        public MainDataBridgeWindow Window { get { return m_window; } set { m_window = value; DataSources.Window = value; } }

        public DataMap DataMap { get; protected set; }

        public DataMapEditor(DataMap _map = null, MainDataBridgeWindow _window = null)
        {
            InitializeComponent();
            DataMap = _map;
            DataSources.Map = _map;
            IncidentDataListBox.ItemsSource = _map.IncidentDataMappings;
            ResponseDataListBox.ItemsSource = _map.ResponseDataMappings;
            BenchmarkListBox.ItemsSource = _map.BenchmarkMappings;
            Window = _window;
        }

        public enum DataMappingType { IncidentData, ResponseData, ResponseTiming };

        private void AddIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), null, false);
                dialog.Owner = Window;
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
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to add Incident Field", ex);
            }
        }

        private void DeleteIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (object item in IncidentDataListBox.SelectedItems)
                {
                    DataMapping incidentMapping = item as DataMapping;
                    if (incidentMapping != null)
                    {
                        DataMap.IncidentDataMappings.Remove(incidentMapping);
                        if (Window != null)
                        {
                            Window.SetChangesMade(DataMap, true);
                        }
                    }
                }
            } catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to Delete Incident Field", ex);
            }
        }

        private void AddResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), null, false, DataMappingType.ResponseData);
                dialog.Owner = Window;
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
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to add Response Field", ex);
            }
        }

        private void DeleteResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (object item in ResponseDataListBox.SelectedItems)
                {
                    DataMapping responseMapping = item as DataMapping;
                    if (responseMapping != null)
                    {
                        DataMap.ResponseDataMappings.Remove(responseMapping);
                        if (Window != null)
                        {
                            Window.SetChangesMade(DataMap, true);
                        }
                    }
                }
            } catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to Delete Response Field", ex);
            }
        }

        private void AddBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), null, false, DataMappingType.ResponseTiming);
                dialog.Owner = Window;
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
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to add Response Timing Field", ex);
            }
        }

        private void DeleteBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (object item in BenchmarkListBox.SelectedItems)
                {
                    DataMapping benchmarkMapping = item as DataMapping;
                    if (benchmarkMapping != null)
                    {
                        DataMap.BenchmarkMappings.Remove(benchmarkMapping);
                        if (Window != null)
                        {
                            Window.SetChangesMade(DataMap, true);
                        }
                    }
                }
            } catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to Delete Response Timing Field", ex);
            }
        }

        private void IncidentDataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteIncidentFieldButton.IsEnabled = IncidentDataListBox.SelectedIndex != -1;
            EditIncidentFieldButton.IsEnabled = IncidentDataListBox.SelectedIndex != -1;
        }

        private void ResponseDataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteResponseFieldButton.IsEnabled = ResponseDataListBox.SelectedIndex != -1;
            EditResponseFieldButton.IsEnabled = ResponseDataListBox.SelectedIndex != -1;
        }

        private void BenchmarkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteBenchmarkButton.IsEnabled = BenchmarkListBox.SelectedIndex != -1;
            EditBenchmarkButton.IsEnabled = BenchmarkListBox.SelectedIndex != -1;
        }

        private void EditIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataMapping selectedField = IncidentDataListBox.SelectedItem as DataMapping;
                ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), selectedField, DataMappingType.IncidentData);
                dialog.Owner = Window;
                dialog.ShowDialog();
                if (dialog.Result != null)
                {
                    int index = DataMap.IncidentDataMappings.IndexOf(selectedField);
                    DataMap.IncidentDataMappings.Remove(selectedField);
                    DataMap.IncidentDataMappings.Insert(index, dialog.Result);
                    if (Window != null)
                    {
                        Window.SetChangesMade(DataMap, true);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to edit Incident Field", ex);
            }
        }

        private void EditResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataMapping selectedField = ResponseDataListBox.SelectedItem as DataMapping;
                ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), selectedField, DataMappingType.ResponseData);
                dialog.Owner = Window;
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
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to edit Response Field", ex);
            }
        }

        private void EditBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataMapping selectedField = BenchmarkListBox.SelectedItem as DataMapping;
                ColumnSelectionDialog dialog = new ColumnSelectionDialog(DataMap.DataSources.ToList(), selectedField, DataMappingType.ResponseTiming);
                dialog.Owner = Window;
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
            catch (Exception ex)
            {
                LogHelper.LogMessage(LogLevel.Error, "Unable to edit Response Timing Field", ex);
            }
        }

        private void IncidentDataListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataMapping selectedField = IncidentDataListBox.SelectedItem as DataMapping;
            if (selectedField != null)
            {
                EditIncidentFieldButton_Click(sender, e);
            }
        }

        private void ResponseDataListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataMapping selectedField = ResponseDataListBox.SelectedItem as DataMapping;
            if (selectedField != null)
            {
                EditResponseFieldButton_Click(sender, e);
            }
        }

        private void BenchmarkListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataMapping selectedField = BenchmarkListBox.SelectedItem as DataMapping;
            if (selectedField != null)
            {
                EditBenchmarkButton_Click(sender, e);
            }
        }

        private void IncidentDataListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IncidentDataListBox.SelectedItems.Count == 0)
            {
                EditIncidentDataMenuItem.IsEnabled = false;
                DeleteIncidentDataMenuItem.IsEnabled = false;
            }
        }

        private void IncidentDataListBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && IncidentDataListBox.SelectedIndex != -1)
            {
                string prompt;
                if (IncidentDataListBox.SelectedItems.Count > 1)
                {
                    prompt = "Delete selected Incident Data Mappings?";
                } else
                {
                    DataMapping selectedField = IncidentDataListBox.SelectedItem as DataMapping;
                    prompt = string.Format("Delete Incident Data Mapping for field '{0}'?", selectedField.Field);
                }
                
                MessageBoxResult result = MessageBox.Show(prompt, "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;

                DeleteIncidentFieldButton_Click(sender, e);
            }
        }

        private void ResponseDataListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ResponseDataListBox.SelectedItems.Count == 0)
            {
                EditResponseDataMenuItem.IsEnabled = false;
                DeleteResponseDataMenuItem.IsEnabled = false;
            }
        }

        private void ResponseDataListBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && ResponseDataListBox.SelectedIndex != -1)
            {
                string prompt;
                if (IncidentDataListBox.SelectedItems.Count > 1)
                {
                    prompt = "Delete selected Response Data Mappings?";
                }
                else
                {
                    DataMapping selectedField = ResponseDataListBox.SelectedItem as DataMapping;
                    prompt = string.Format("Delete Response Data Mapping for field '{0}'?", selectedField.Field);
                }

                MessageBoxResult result = MessageBox.Show(prompt, "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;

                DeleteResponseFieldButton_Click(sender, e);
            }
        }

        private void BenchmarkListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (BenchmarkListBox.SelectedItems.Count == 0)
            {
                EditTimingDataMenuItem.IsEnabled = false;
                DeleteTimingDataMenuItem.IsEnabled = false;
            }
        }

        private void BenchmarkListBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && BenchmarkListBox.SelectedIndex != -1)
            {
                string prompt;
                if (BenchmarkListBox.SelectedItems.Count > 1)
                {
                    prompt = "Delete selected Timing Data Mappings?";
                }
                else
                {
                    DataMapping selectedField = BenchmarkListBox.SelectedItem as DataMapping;
                    prompt = string.Format("Delete Timing Data Mapping for field '{0}'?", selectedField.Field);
                }

                MessageBoxResult result = MessageBox.Show(prompt, "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;

                DeleteBenchmarkButton_Click(sender, e);
            }
        }

        private void IncidentDataItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            EditIncidentDataMenuItem.IsEnabled = true;
            DeleteIncidentDataMenuItem.IsEnabled = true;
        }

        private void ResponseDataItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            EditResponseDataMenuItem.IsEnabled = true;
            DeleteResponseDataMenuItem.IsEnabled = true;
        }

        private void TimingDataItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            EditTimingDataMenuItem.IsEnabled = true;
            DeleteTimingDataMenuItem.IsEnabled = true;
        }
    }
}
