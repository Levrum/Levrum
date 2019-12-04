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

        public List<string> DefaultFieldNames = new List<string>(new string[] { "Code", "Category", "Type", "Jurisdiction", "District", "Priority", "CallProcessed", "Unit", "UnitType", "Shift", "Assigned", "Responding", "OnScene", "ClearScene", "InService", "InQuarters" });

        public ColumnSelectionDialog(List<IDataSource> _dataSources, DataMapping _mapping)
        {
            InitializeComponent();
            FieldNameComboBox.ItemsSource = DefaultFieldNames;

            FieldSourceComboBox.ItemsSource = _dataSources;
            FieldSourceComboBox.DisplayMemberPath = "Name";

            DataMapping copy = new DataMapping();
            copy.Column = new ColumnMapping();
            copy.Column.ColumnName = _mapping.Column.ColumnName;
            copy.Column.DataSource = _mapping.Column.DataSource;
            copy.Field = _mapping.Field;

            Result = copy;
        }

        public ColumnSelectionDialog(List<IDataSource> _dataSources, string _fieldName = null, bool fieldNameReadOnly = false)
        {
            InitializeComponent();
            Result = new DataMapping();
            FieldNameComboBox.ItemsSource = DefaultFieldNames;

            FieldNameComboBox.Text = _fieldName;
            FieldNameComboBox.IsReadOnly = fieldNameReadOnly;

            FieldSourceComboBox.ItemsSource = _dataSources;
            FieldSourceComboBox.DisplayMemberPath = "Name";
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
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            m_closing = true;
            Result = null;
            Close();
        }

        private void FieldNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FieldNameComboBox.Text = FieldNameComboBox.SelectedItem as string;
        }
    }
}
