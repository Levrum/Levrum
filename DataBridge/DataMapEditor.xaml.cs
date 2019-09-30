using System;
using System.Collections.Generic;
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
        public DataMap DataMap { get; protected set; }

        public DataMapEditor(DataMap _map = null)
        {
            InitializeComponent();
            DataMap = _map;
        }

        private void AddIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void RemoveIncidentFieldButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveResponseFieldButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void IncidentIDButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void IncidentTimeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void IncidentLatitudeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void IncidentLongitudeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void IncidentLocationButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
