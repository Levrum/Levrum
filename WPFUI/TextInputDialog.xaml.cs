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

namespace Levrum.WPFUI
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
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

        public TextInputDialog(string _title = null, string _caption = null, string _text = null)
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
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = TextBox.Text;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
