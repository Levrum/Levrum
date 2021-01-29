using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Levrum.UI.WPF
{
    /// <summary>
    /// Interaction logic for SingleValueForm.xaml
    /// </summary>
    public partial class SingleValueForm : Window
    {
        public string Caption
        {
            get
            {
                return Label.Content as string;
            }
            set
            {
                Label.Content = value;
            }
        }

        public string InitialText { get; set; } = null;

        public string Text
        {
            get
            {
                if (TextSet == true)
                    return TextBox.Text;

                return null;
            }
            set
            {
                TextSet = true;
                TextBox.Text = value;
                TextBox.Foreground = Brushes.Black;
            }
        }

        public string Result { get; set; } = null;

        bool TextSet { get; set; } = false;
        public bool Numeric { get; set; } = false;

        public SingleValueForm(string _title = null, string _caption = null, string _text = null, bool _numeric = false)
        {
            InitializeComponent();
            TextSet = false;
            if (!string.IsNullOrEmpty(_title))
            {
                Title = _title;
            }

            if (!string.IsNullOrEmpty(_caption))
            {
                Caption = _caption;
            }

            if (!string.IsNullOrEmpty(_text))
            {
                TextSet = true;
                InitialText = _text;
                TextBox.Text = _text;
                TextBox.Foreground = Brushes.Black;
            }

            Numeric = _numeric;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Result = TextBox.Text;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Result = InitialText;
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextSet = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextSet == false)
            {
                TextBox.Text = string.Empty;
                TextBox.Foreground = Brushes.Black;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (Numeric) {
                double value;
                bool isNumeric = double.TryParse(e.Text, out value);
                e.Handled = !isNumeric;
            }
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                double value;
                if (!double.TryParse(text, out value))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
