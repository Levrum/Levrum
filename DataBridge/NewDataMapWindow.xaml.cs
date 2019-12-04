using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Newtonsoft.Json;

using Levrum.Data.Map;
using Levrum.Data.Sources;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for NewDataMapWindow.xaml
    /// </summary>
    public partial class NewDataMapWindow : Window
    {
        private enum NewDataMapStep
        {
            ChooseTemplate,
            SqlServerSettings
        }

        public List<DataMapTemplateInfo> Templates { get; protected set; } = new List<DataMapTemplateInfo>(
            new DataMapTemplateInfo[] {
                new DataMapTemplateInfo(){ Image = "/DataBridge;component/BlankTemplate.png", Description = "Empty DataMap", Template = null, Type = DataMapType.CsvFile },
                new DataMapTemplateInfo(){ Image = "/DataBridge;component/SuperionTemplate.png", Description = "Superion", Template = "Templates\\Superion.dmap", Type = DataMapType.SqlServer },
                new DataMapTemplateInfo(){ Image = "/DataBridge;component/TritechTemplate.png", Description = "TriTech Inform", Template = "Templates\\TriTech.dmap", Type = DataMapType.SqlServer },
                }
            );

        public DataMapTemplateInfo Info { get; protected set; } = null;
        public DataMap Result { get; protected set; } = null;

        private NewDataMapStep Step { get; set; } = NewDataMapStep.ChooseTemplate;
        private NewDataMapStep LastStep { get; set; } = NewDataMapStep.ChooseTemplate;
        private NewDataMapStep NextStep { get; set; } = NewDataMapStep.SqlServerSettings;

        public NewDataMapWindow()
        {
            InitializeComponent();
            DataMapTemplateListBox.ItemsSource = Templates;
            DataMapTemplateListBox.SelectedItem = Templates[0];
        }

        private void DataMapTemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataMapTemplateInfo info = DataMapTemplateListBox.SelectedItem as DataMapTemplateInfo;
            Info = info;
            Result = loadTemplate(info);
            if (info.Type == DataMapType.CsvFile)
            {
                NextButton.IsEnabled = false;
            } else if (info.Type == DataMapType.SqlServer)
            {
                NextStep = NewDataMapStep.SqlServerSettings;
                NextButton.IsEnabled = true;
            }
        }

        private DataMap loadTemplate(DataMapTemplateInfo info)
        {
            if (info.Type == DataMapType.CsvFile)
            {
                return new DataMap("New DataMap.dmap");
            } else
            {
                FileInfo template = new FileInfo(info.Template);
                if (!template.Exists)
                {
                    throw new FileNotFoundException(info.Template);
                }

                string templateJson = File.ReadAllText(info.Template);
                return JsonConvert.DeserializeObject<DataMap>(templateJson);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NextStep = Step;
            Step = LastStep;
            updateUiForStep();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            LastStep = Step;
            Step = NextStep;
            updateUiForStep();
        }

        private void updateUiForStep()
        {
            switch (Step)
            {
                case NewDataMapStep.ChooseTemplate:
                    DataMapTemplateSelectorGrid.Visibility = Visibility.Visible;
                    DataMapSqlServerSettingsGrid.Visibility = Visibility.Hidden;
                    BackButton.IsEnabled = false;
                    NextButton.IsEnabled = true;
                    break;
                case NewDataMapStep.SqlServerSettings:
                    DataMapTemplateSelectorGrid.Visibility = Visibility.Hidden;
                    DataMapSqlServerSettingsGrid.Visibility = Visibility.Visible;
                    BackButton.IsEnabled = true;
                    NextButton.IsEnabled = false;
                    if (string.IsNullOrWhiteSpace(SqlServerDatabase.Text))
                    {
                        if (Result.DataSources.Count > 0 && Result.DataSources[0].Parameters.ContainsKey("Database"))
                        {
                            SqlServerDatabase.Text = Result.DataSources[0].Parameters["Database"];
                        }
                    }
                    break;
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (Info.Type == DataMapType.SqlServer)
            {
                updateSqlSettings(Result);
            }
            Close();
        }

        private void updateSqlSettings(DataMap map)
        {
            foreach (IDataSource dataSource in map.DataSources)
            {
                if (dataSource is SqlSource)
                {
                    dataSource.Parameters["Server"] = SqlServerAddress.Text;
                    dataSource.Parameters["Port"] = SqlServerPort.Text;
                    dataSource.Parameters["Database"] = SqlServerDatabase.Text;
                    dataSource.Parameters["User"] = SqlServerUser.Text;
                    dataSource.Parameters["Password"] = SqlServerPassword.Password;
                }
            }
        }
    }

    public class DataMapTemplateInfo
    {
        public string Image { get; set; }
        public string Description { get; set; }
        public string Template { get; set; }
        public DataMapType Type { get; set; }
    }

    public enum DataMapType
    {
        CsvFile,
        SqlServer,
        EmergencyReporting
    }
}
