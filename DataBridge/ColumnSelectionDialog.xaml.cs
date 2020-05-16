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
using System.Windows.Shapes;

using Levrum.Data.Map;
using Levrum.Data.Sources;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for ColumnSelectionDialog.xaml
    /// </summary>
    public partial class ColumnSelectionDialog : Window
    {
        DataMapping m_result;
        bool m_saving = false;
        bool m_closing = false;

        public DataMapping Result 
        { 
            get 
            { 
                return m_result; 
            } 
            set 
            {
                m_result = value;
                if (!m_closing)
                {
                    FieldNameComboBox.Text = value.Field;
                    FieldSourceComboBox.SelectedItem = value.Column.DataSource;
                    ColumnComboBox.SelectedItem = value.Column.ColumnName;
                }
            } 
        }

        public List<string> DefaultIncidentDataFields = new List<string>(new string[] { "Time", "Latitude", "Longitude", "Location", "City", "State", "Code", "Category", "Type", "Jurisdiction", "District", "CallProcessed", "Cancelled", "FirstAction" });
        public List<string> DefaultResponseDataFields = new List<string>(new string[] { "Unit", "UnitType", "Urgency", "Shift" });
        public List<string> DefaultResponseTimingFields = new List<string>(new string[] { "Assigned", "Responding", "OnScene", "ClearScene", "Transport", "Hospital", "InService", "InQuarters" });

        public ColumnSelectionDialog(List<IDataSource> _dataSources, DataMapping _mapping, DataMapEditor.DataMappingType mappingType)
        {
            InitializeComponent();
            DataMapping copy = new DataMapping();
            copy.Column = new ColumnMapping();
            copy.Column.ColumnName = _mapping.Column.ColumnName;
            copy.Column.DataSource = _mapping.Column.DataSource;
            copy.Field = _mapping.Field;

            getFieldNameSourceForMappingType(mappingType);
            
            
            FieldSourceComboBox.ItemsSource = _dataSources;
            FieldSourceComboBox.DisplayMemberPath = "Name";
            
            Result = copy;
        }

        public ColumnSelectionDialog(List<IDataSource> _dataSources, string _fieldName = null, bool fieldNameReadOnly = false, DataMapEditor.DataMappingType mappingType = DataMapEditor.DataMappingType.IncidentData)
        {
            InitializeComponent();
            Result = new DataMapping();
            getFieldNameSourceForMappingType(mappingType);
            

            FieldNameComboBox.Text = _fieldName;
            FieldNameComboBox.IsReadOnly = fieldNameReadOnly;

            FieldSourceComboBox.ItemsSource = _dataSources;
            FieldSourceComboBox.DisplayMemberPath = "Name";
        }

        private void getFieldNameSourceForMappingType(DataMapEditor.DataMappingType mappingType)
        {
            switch (mappingType)
            {
                case DataMapEditor.DataMappingType.IncidentData:
                    FieldNameComboBox.ItemsSource = DefaultIncidentDataFields;
                    break;
                case DataMapEditor.DataMappingType.ResponseData:
                    FieldNameComboBox.ItemsSource = DefaultResponseDataFields;
                    break;
                case DataMapEditor.DataMappingType.ResponseTiming:
                    FieldNameComboBox.ItemsSource = DefaultResponseTimingFields;
                    break;
            }
        }

        private void ColumnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Result.Column.ColumnName = ColumnComboBox.SelectedItem as string;
        }

        private void FieldSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IDataSource selectedSource = (IDataSource)FieldSourceComboBox.SelectedItem;
            Result.Column.DataSource = selectedSource;
            ColumnComboBox.ItemsSource = selectedSource.GetColumns();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result.Field = FieldNameComboBox.Text;
            m_saving = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FieldNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FieldNameComboBox.Text = FieldNameComboBox.SelectedItem as string;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_closing = true;

            if (!m_saving)
            {
                Result = null;
            }
        }
    }
}
