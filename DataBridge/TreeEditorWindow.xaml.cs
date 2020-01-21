using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Levrum.Data.Classes;
using Levrum.UI.WinForms;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for TreeEditorWindow.xaml
    /// </summary>
    public partial class TreeEditorWindow : Window
    {
        public bool DialogResult { get; set; } = false;
        public List<ICategoryData> Result { get; set; } = null;
        public List<ICategoryData> Tree { get; set; } = null;

        public TreeEditorWindow(List<ICategoryData> tree)
        {
            InitializeComponent();
            Tree = tree;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowsFormsHost host = new WindowsFormsHost();

            TreeEditorControl treeEditorControl = new TreeEditorControl();
            if (Tree != null)
            {
                treeEditorControl.LoadTree(Tree);
            }
            treeEditorControl.SaveTreeToFile = false;
            treeEditorControl.OnSaveTree += TreeEditorControl_OnSaveTree;
            host.Child = treeEditorControl;
            Grid.Children.Add(host);
        }

        private void TreeEditorControl_OnSaveTree(List<ICategoryData> tree)
        {
            DialogResult = true;
            Result = tree;
            Tree = tree;
            Close();
        }
    }
}
