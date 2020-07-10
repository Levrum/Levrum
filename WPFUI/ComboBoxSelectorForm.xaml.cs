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

using Levrum.Utils;

namespace Levrum.UI.WPF
{
    /// <summary>
    /// Interaction logic for ComboBoxSelectorForm.xaml
    /// </summary>
    public partial class ComboBoxSelectorForm : Window
    {
        public object Selection { get; set; } = null;

        public ComboBoxSelectorForm(List<object> _comboBoxOptions, string _title = null, string _label = null, string _displayMemberPath = null)
        {
            InitializeComponent();
            try
            {

                if (!string.IsNullOrWhiteSpace(_title))
                {
                    Title = _title;
                }

                if (!string.IsNullOrWhiteSpace(_label))
                {
                    Label.Content = _label;
                }

                if (!string.IsNullOrEmpty(_displayMemberPath))
                {
                    ComboBox.DisplayMemberPath = _displayMemberPath;
                }

                ComboBox.ItemsSource = _comboBoxOptions;
                if (_comboBoxOptions.Count < 0)
                {
                    LogHelper.LogErrOnce("ComboBoxSelectorForm()", "Empty list passed to ComboBoxSelectorForm");
                    throw new Exception();
                }
                ComboBox.SelectedIndex = 0;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception loading ComboBoxSelectorForm");
                Close();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Selection = ComboBox.SelectedItem;
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Selection = null;
            DialogResult = false;
            Close();
        }
    }
}
