using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Win32;

using Levrum.Utils;
using Levrum.UI.WPF;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class EditScriptDialog : Window
    {
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

        public ScriptType Type { get; set; } = ScriptType.PostLoad;

        bool TextSet { get; set; } = false;

        bool WaitingForEditor { get; set; } = false;

        public EditScriptDialog(ScriptType _type = ScriptType.PostLoad, string _title = null, string _caption = null, string _text = null)
        {
            InitializeComponent();
            Type = _type;
            TextSet = false;
            if (!string.IsNullOrEmpty(_title))
            {
                Title = _title;
            }

            if (!string.IsNullOrEmpty(_text))
            {
                TextSet = true;
                InitialText = _text;
                TextBox.Text = _text;
                TextBox.Foreground = Brushes.Black;
            }

            TextBox.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("JavaScript");
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

        private void TextBox_TextChanged(object sender, EventArgs e)
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

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "js";
            ofd.Filter = "JavaScript files (*.js)|*.js|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            ofd.Title = "Select File";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (ofd.ShowDialog() == false)
            {
                return;
            }

            string contents = File.ReadAllText(ofd.FileName);
            TextBox.Text = contents;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo tempDir = new DirectoryInfo(string.Format("{0}\\Levrum\\Temp", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
            if (!tempDir.Exists)
                tempDir.Create();

            string fileName = string.Format("{0}\\{1}.js", tempDir.FullName, DateTime.Now.Ticks);
            File.WriteAllText(fileName, Text);

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Levrum\DataMap");
            var value = key.GetValue("ExternalJSEditor");
            string editorPath;
            if (value == null || !File.Exists(value as string)) 
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Application (*.exe)|*.exe|All files (*.*)|*.*";
                ofd.DefaultExt = "*.exe";
                ofd.Title = "Select Editor Application";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if (ofd.ShowDialog() == false)
                {
                    return;
                }

                editorPath = ofd.FileName;
                key.SetValue("ExternalJSEditor", editorPath);
            } else
            {
                editorPath = value as string;
            }

            BackgroundWorker bw = new BackgroundWorker();
            disableForm();

            bw.DoWork += new DoWorkEventHandler((object obj, DoWorkEventArgs args) =>
            {
                try
                {
                    string arguments = fileName;
                    if (editorPath.ToLower().Contains("code.exe"))
                    {
                        arguments = string.Format("-n {0}", fileName); // If we're using VS Code, ensure we get a new window so WaitForExit() works
                    }
                    ProcessStartInfo startInfo = new ProcessStartInfo(editorPath);
                    startInfo.Arguments = arguments;
                    startInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    Process p = new Process();
                    p.StartInfo = startInfo;
                    p.Start();

                    p.WaitForExit();
                }
                catch (Exception ex)
                {
                    LogHelper.LogMessage(LogLevel.Error, "Unable to launch editor", ex);
                }
            });

            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler((object obj, RunWorkerCompletedEventArgs args) =>
            {
                string contents = File.ReadAllText(fileName);
                setText(contents);
                enableForm();
                File.Delete(fileName);
            });

            bw.RunWorkerAsync();
        }

        private void disableForm()
        {
            WaitingForEditor = true;
            WaitingForEditorText.Visibility = Visibility.Visible;
            TextBox.Visibility = Visibility.Hidden;
            IsEnabled = false;
        }

        private void setText(string contents)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { setText(contents); }));
                return;
            }

            TextBox.Text = contents;
        }

        private void enableForm()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => { enableForm(); }));
                return;
            }

            WaitingForEditor = false;
            IsEnabled = true;
            WaitingForEditorText.Visibility = Visibility.Hidden;
            TextBox.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = WaitingForEditor;
        }

        private void TemplatesButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> availableTemplates;
            switch (Type)
            {
                case ScriptType.PostLoad:
                    availableTemplates = ScriptTemplates.TemplateDictionary[ScriptType.PostLoad];
                    break;
                case ScriptType.PerIncident:
                    availableTemplates = ScriptTemplates.TemplateDictionary[ScriptType.PerIncident];
                    break;
                case ScriptType.FinalProcessing:
                    availableTemplates = ScriptTemplates.TemplateDictionary[ScriptType.FinalProcessing];
                    break;
                default:
                    throw new NotImplementedException();
            }

            ComboBoxSelectorForm selectorForm = new ComboBoxSelectorForm(new List<object>(availableTemplates.Keys), "Select Template", "Select Template:");
            bool? result = selectorForm.ShowDialog();
            if (result == true)
            {
                string selectedKey = selectorForm.Selection.ToString();
                string template;
                if (availableTemplates.TryGetValue(selectedKey, out template)) {
                    if (TextSet == true)
                    {
                        MessageBoxResult okayToOverwrite = MessageBox.Show(string.Format("Overwrite your script with the template '{0}'?", selectedKey), "Overwrite script?", MessageBoxButton.YesNo);
                        if (okayToOverwrite == MessageBoxResult.No)
                        {
                            return;
                        }
                    }

                    Text = template;
                } else
                {
                    LogHelper.LogErrOnce("EditScriptDialog.TemplateButton_OnClick()", string.Format("Unable to load template {0}", selectedKey));
                }
            }
        }
    }
}
