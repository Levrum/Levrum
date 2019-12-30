﻿using Levrum.Data.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Levrum.UI.WinForms
{
    public partial class TreeEditorControl : UserControl
    {
        [System.ComponentModel.Category("Load Incidents Button"), System.ComponentModel.Description("Whether or not to display the 'Load Incidents' button.")]
        public bool LoadIncidentsButton
        {
            get
            {
                return m_btnLoadIncidents.Visible;
            }
            set
            {
                m_btnLoadIncidents.Visible = value;
                Invalidate();
            }
        }

        public List<ICategoryData> Tree
        {
            get
            {
                return ConvertToTree();
            }
        }

        List<FlowLayoutPanel> m_selectedPanels = new List<FlowLayoutPanel>();
        List<Button> m_selectedButtons = new List<Button>();
        List<DraggedData> m_recyclingBin = new List<DraggedData>();

        public TreeEditorControl()
        {
            InitializeComponent();
            m_btnLoadIncidents.Visible = LoadIncidentsButton;
            MoveCursor = new Cursor(Properties.Resources.move_button.Handle);
        }

        private void SetUndoDeleteTimer()
        {
            m_btnUndoDelete.Visible = true;
            m_btnUndoDelete.BringToFront();
            m_undoDeleteTimer.Enabled = true;
        }

        private void OrganizedPanel_DragEnter(object sender, DragEventArgs e)
        {
            // Allow dropping of category panels
            if (e.Data.GetDataPresent(typeof(List<DraggedData>)) && (e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>)[0].Data is ICategoryData)
            {
                e.Effect = DragDropEffects.Move | DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OrganizedPanel_DragDrop(object sender, DragEventArgs e)
        {
            FlowLayoutPanel receivingPanel = sender as FlowLayoutPanel;
            if (receivingPanel == null)
            {
                return;
            }

            List<DraggedData> allDraggedData = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;
            m_flpOrganizedData.SuspendLayout();
            foreach (DraggedData draggedData in allDraggedData)
            {
                if (e.AllowedEffect == DragDropEffects.Copy)
                {
                    AddSubPanel(receivingPanel, draggedData.Data as ICategoryData);
                }
                else if (e.AllowedEffect == DragDropEffects.Move)
                {
                    ICategoryData oldParentData = draggedData.Control.Parent.Tag as ICategoryData;
                    oldParentData?.Children?.Remove(draggedData.Data as ICategoryData);
                    receivingPanel.Controls.Add(draggedData.Control);
                }

                Button receivingPanelAddButton = null;
                foreach (Control control in receivingPanel.Controls)
                {
                    if (control is Button)
                    {
                        receivingPanelAddButton = control as Button;
                        break;
                    }
                }

                if (receivingPanelAddButton != null)
                {
                    receivingPanel.Controls.Remove(receivingPanelAddButton);
                    receivingPanel.Controls.Add(receivingPanelAddButton);
                }
            }
            m_flpOrganizedData.ResumeLayout();

            if (m_selectedButtons.Count > 1)
            {
                ClearValueBlockSelection();
            }
            if (m_selectedPanels.Count > 1)
            {
                ClearPanelSelection();
            }
        }

        private void UnorganizedPanel_DragEnter(object sender, DragEventArgs e)
        {
            List<DraggedData> data = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;
            if (data != null && data[0].Control.Parent != m_flpUnorganizedData)
            {
                e.Effect = DragDropEffects.Move;
                m_pDelete.Visible = true;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void DeletePanel_DragEnter(object sender, DragEventArgs e)
        {
            List<DraggedData> data = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;
            if (data != null && data[0].Control.Parent != m_flpUnorganizedData)
            {
                e.Effect = DragDropEffects.Move;
                m_pDelete.Visible = true;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }



        private void DeletePanel_DragDrop(object sender, DragEventArgs e)
        {
            List<DraggedData> allDraggedData = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;

            if (e.AllowedEffect == DragDropEffects.Move)
            {
                DeleteRecycledItems();
                // Suspent layout for all parent controls to smooth UI
                m_flpOrganizedData.SuspendLayout();
                HashSet<Control> parentControls = new HashSet<Control>();
                foreach (DraggedData draggedData in m_recyclingBin)
                {
                    if (!parentControls.Contains(draggedData.Control.Parent))
                    {
                        parentControls.Add(draggedData.Control.Parent);
                    }
                }
                foreach (Control control in parentControls)
                {
                    if (control != null)
                    {
                        control.SuspendLayout();
                    }
                }

                m_recyclingBin = allDraggedData;
                foreach (DraggedData draggedData in allDraggedData)
                {
                    draggedData.Control.Visible = false;
                }

                m_flpOrganizedData.ResumeLayout();
                foreach (Control control in parentControls)
                {
                    control.ResumeLayout();
                }
            }

            if (m_selectedButtons.Count > 1)
            {
                ClearValueBlockSelection();
            }
            if (m_selectedPanels.Count > 1)
            {
                ClearPanelSelection();
            }
            m_pDelete.Visible = false;
            SetUndoDeleteTimer();

        }

        private void DeleteRecycledItems()
        {
            // Delete all dropped items
            foreach (DraggedData draggedData in m_recyclingBin)
            {
                ICategoryData oldParentData = draggedData.Control.Parent.Tag as ICategoryData;
                if (draggedData.Data is ICategoryData)
                {
                    oldParentData?.Children?.Remove(draggedData.Data as ICategoryData);
                }
                else if (draggedData.Data is ICategorizedValue)
                {
                    oldParentData?.Values?.Remove(draggedData.Data as ICategorizedValue);

                    // Mark block as not added
                    if (!ValueBlockIsAdded(draggedData.Data as ICategorizedValue, m_flpOrganizedData))
                    {
                        foreach (Control control in m_flpUnorganizedData.Controls)
                        {
                            if (control is Button && (control as Button).Tag == draggedData.Data)
                            {
                                MarkValueBlockAsNotAdded(control as Button);
                                break;
                            }
                        }
                    }
                }
                draggedData.Control.Parent.Controls.Remove(draggedData.Control);
            }

            m_recyclingBin.Clear();
        }

        private void m_btnLoadTree_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string fileName = ofd.FileName;

            List<ICategoryData> tree = JsonConvert.DeserializeObject<List<ICategoryData>>(File.ReadAllText(fileName), new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
            LoadTree(tree, m_flpUnorganizedData);
        }

        private void LoadTree(IEnumerable<ICategoryData> categoryData, FlowLayoutPanel parentPanel)
        {
            parentPanel.Controls.Clear();
            parentPanel.FlowDirection = FlowDirection.TopDown;
            parentPanel.SuspendLayout();
            foreach (ICategoryData data in categoryData)
            {
                FlowLayoutPanel newPanel = AddSubPanel(parentPanel, data);
            }
            parentPanel.Controls.Add(GenerateSubcategoryButton());
            parentPanel.ResumeLayout();
        }

        public void LoadTree(IEnumerable<ICategoryData> categoryData)
        {
            LoadTree(categoryData, m_flpOrganizedData);
        }

        private FlowLayoutPanel AddSubPanel(FlowLayoutPanel parentPanel, ICategoryData panelCatData)
        {
            string panelName = panelCatData.Name;

            FlowLayoutPanel newPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Height = 200,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(10),
                Padding = new Padding(8),
                Name = panelName,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoScroll = false,
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                AllowDrop = true,
                Tag = panelCatData,
            };
            newPanel.MouseDown += SubPanel_MouseDown;
            newPanel.DragEnter += SubPanel_DragEnter;
            newPanel.DragDrop += SubPanel_DragDrop;
            newPanel.QueryContinueDrag += SubPanel_QueryContinueDrag;

            Label panelLabel = new Label
            {
                Text = panelName,
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4),
                AutoSize = true,
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Cursor = MoveCursor
            };
            panelLabel.MouseDown += PanelLabel_MouseDown;

            newPanel.Controls.Add(panelLabel);

            // Add values
            foreach (ICategorizedValue value in panelCatData.Values)
            {
                newPanel.Controls.Add(GenerateValueBlock(value));
            }

            // Add children
            foreach (ICategoryData categoryData in panelCatData.Children)
            {
                AddSubPanel(newPanel, categoryData);
            }

            newPanel.Controls.Add(GenerateSubcategoryButton());

            parentPanel.Controls.Add(newPanel);

            return newPanel;
        }

        private void PanelLabel_MouseDown(object sender, MouseEventArgs e)
        {
            Label clickedLabel = sender as Label;
            if (clickedLabel == null)
            {
                return;
            }

            SubPanel_MouseDown(clickedLabel.Parent, e);
        }

        private void SubPanel_MouseDown(object sender, MouseEventArgs e)
        {
            FlowLayoutPanel clickedPanel = sender as FlowLayoutPanel;
            if (clickedPanel == null)
            {
                return;
            }

            if (Form.ModifierKeys == Keys.Shift)
            {
                SelectRangeOfPanels(clickedPanel.Parent, clickedPanel);
                return;
            }
            else if (Form.ModifierKeys == Keys.Control && ListContainsPanel(m_selectedPanels, clickedPanel))
            {
                RemovePanelFromSelection(clickedPanel);
            }
            else if (Form.ModifierKeys == Keys.Control)
            {
                AddPanelToSelection(clickedPanel);
            }
            else if (!ListContainsPanel(m_selectedPanels, clickedPanel))
            {
                ClearPanelSelection();
                AddPanelToSelection(clickedPanel);
            }

            if (Form.ModifierKeys != Keys.Control)
            {
                List<DraggedData> selectedPanels = new List<DraggedData>();
                foreach (FlowLayoutPanel flp in m_selectedPanels)
                {
                    selectedPanels.Add(new DraggedData { Control = flp, Data = flp.Tag, Type = flp.Tag.GetType() });
                }

                DragDropEffects dragDropEffects = clickedPanel.Parent == m_flpUnorganizedData ? DragDropEffects.Copy : DragDropEffects.Move;
                if (m_flpOrganizedData.Contains(clickedPanel))
                {
                    m_pDelete.Visible = true;
                }
                clickedPanel.DoDragDrop(selectedPanels, DragDropEffects.Move);

                if (m_selectedPanels.Count > 1)
                {
                    ClearPanelSelection();
                }
            }
        }

        private void SubPanel_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            FlowLayoutPanel draggedPanel = sender as FlowLayoutPanel;
            if (draggedPanel == null)
            {
                return;
            }

            if (m_flpOrganizedData.Contains(draggedPanel) && e.Action != DragAction.Continue)
            {
                m_pDelete.Visible = false;
            }
        }

        private void ClearPanelSelection()
        {
            foreach (FlowLayoutPanel flp in m_selectedPanels)
            {
                flp.BackColor = Color.White;
            }
            m_selectedPanels.Clear();
        }

        private void SelectRangeOfPanels(Control container, FlowLayoutPanel clickedPanel)
        {
            AddPanelToSelection(clickedPanel);

            FlowLayoutPanel panelOtherEnd = null;
            foreach (Control control in container.Controls)
            {
                if (!(control is FlowLayoutPanel))
                {
                    continue;
                }
                if (control == clickedPanel)
                {
                    continue;
                }

                if (m_selectedPanels.Contains(control as FlowLayoutPanel))
                {
                    panelOtherEnd = control as FlowLayoutPanel;
                    break;
                }
            }

            bool seenOne = false;
            foreach (Control control in container.Controls)
            {
                if (!(control is FlowLayoutPanel))
                {
                    continue;
                }

                if (seenOne)
                {
                    AddPanelToSelection(control as FlowLayoutPanel);
                }

                if (control == clickedPanel || control == panelOtherEnd)
                {
                    if (!seenOne)
                    {
                        seenOne = true;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void AddPanelToSelection(FlowLayoutPanel clickedPanel)
        {
            clickedPanel.BackColor = Color.LightGray;
            if (!ListContainsPanel(m_selectedPanels, clickedPanel))
            {
                m_selectedPanels.Add(clickedPanel);
            }
        }

        private void RemovePanelFromSelection(FlowLayoutPanel flp)
        {
            flp.BackColor = Color.White;
            if (ListContainsPanel(m_selectedPanels, flp))
            {
                m_selectedPanels.Remove(flp);
            }
        }

        private bool ListContainsPanel(IEnumerable<FlowLayoutPanel> panels, FlowLayoutPanel panel)
        {
            ICategoryData panelData = panel.Tag as ICategoryData;
            if (panelData == null)
            {
                throw new Exception("Panel contains no data");
            }
            bool containsPanel = false;
            foreach (FlowLayoutPanel flp in m_selectedPanels)
            {
                ICategoryData flpData = flp.Tag as ICategoryData;
                if (flpData == null)
                {
                    continue;
                }

                if (panelData.Name == flpData.Name && panelData.Description == flpData.Description && panel.Parent == flp.Parent)
                {
                    containsPanel = true;
                    break;
                }
            }

            return containsPanel;
        }

        private void SubPanel_DragEnter(object sender, DragEventArgs e)
        {
            List<DraggedData> data = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;
            if (data[0].Control == sender || data[0].Control.Parent == sender)
            {
                e.Effect = DragDropEffects.None;
            }
            else if (e.Data.GetDataPresent(typeof(List<DraggedData>)))
            {
                e.Effect = DragDropEffects.Move | DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void SubPanel_DragDrop(object sender, DragEventArgs e)
        {
            FlowLayoutPanel receivingPanel = sender as FlowLayoutPanel;
            if (receivingPanel == null)
            {
                return;
            }
            ICategoryData receivingPanelData = receivingPanel.Tag as ICategoryData;
            if (receivingPanelData == null)
            {
                throw new Exception("Dropped object does not contain data");
            }

            List<DraggedData> allDraggedData = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;
            m_flpUnorganizedData.SuspendLayout();
            receivingPanel.SuspendLayout();
            foreach (DraggedData draggedData in allDraggedData)
            {
                ICategoryData oldParentData = draggedData.Control.Parent.Tag as ICategoryData;

                if (draggedData.Control == receivingPanel)
                {
                    continue;
                }

                // Handle drop of category data panel
                if (draggedData.Data is ICategoryData)
                {
                    ICategoryData droppedData = draggedData.Data as ICategoryData;
                    if (droppedData == null)
                    {
                        throw new Exception("Null data received");
                    }

                    if (receivingPanelData.Children.Contains(droppedData))
                    {
                        continue;
                    }

                    if (oldParentData != null)
                    {
                        oldParentData.Children.Remove(droppedData);
                    }

                    if (e.AllowedEffect == DragDropEffects.Move && draggedData.Control.Parent != null)
                    {
                        draggedData.Control.Parent.Controls.Remove(draggedData.Control);
                    }

                    receivingPanelData.Children.Add(droppedData);
                    AddSubPanel(receivingPanel, droppedData);
                }
                // Handle drop of value block
                else if (draggedData.Data is ICategorizedValue)
                {
                    ICategorizedValue droppedData = draggedData.Data as ICategorizedValue;
                    if (droppedData == null)
                    {
                        throw new Exception("Null data received");
                    }

                    if (receivingPanelData.Values.Contains(droppedData))
                    {
                        continue;
                    }

                    if (oldParentData != null)
                    {
                        oldParentData.Values.Remove(droppedData);
                    }

                    if (e.AllowedEffect == DragDropEffects.Move && draggedData.Control.Parent != null)
                    {
                        draggedData.Control.Parent.Controls.Remove(draggedData.Control);
                    }
                    else if (e.AllowedEffect == DragDropEffects.Copy && draggedData.Control.Parent == m_flpUnorganizedData)
                    {
                        MarkValueBlockAsAdded(draggedData.Control as Button);
                    }

                    receivingPanelData.Values.Add(droppedData);

                    // Keep values on top and containers on bottome
                    List<Control> panels = new List<Control>();
                    foreach (Control control in receivingPanel.Controls)
                    {
                        if (control is FlowLayoutPanel)
                        {
                            panels.Add(control);
                        }
                    }

                    panels.ForEach(x => receivingPanel.Controls.Remove(x));
                    receivingPanel.Controls.Add(GenerateValueBlock(droppedData));
                    panels.ForEach(x => receivingPanel.Controls.Add(x));
                }
                else
                {
                    throw new Exception("Invalid data received");
                }

                // Keep the add subcategory button at the bottom
                Control addSubBtn = null;
                foreach (Control control in receivingPanel.Controls)
                {
                    if (control.Name == "Add Subcategory")
                    {
                        addSubBtn = control;
                        break;
                    }
                }
                if (addSubBtn == null)
                {
                    continue;
                }

                receivingPanel.Controls.Remove(addSubBtn);
                receivingPanel.Controls.Add(addSubBtn);
            }
            m_flpUnorganizedData.ResumeLayout();
            receivingPanel.ResumeLayout();

            if (m_selectedButtons.Count > 0)
            {
                ClearValueBlockSelection();
            }
            if (m_selectedPanels.Count > 0)
            {
                ClearPanelSelection();
            }
        }

        private Button GenerateSubcategoryButton()
        {
            Button btnSubPanelAddNewSub = new Button
            {
                Name = "Add Subcategory",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Image = Properties.Resources.add_subcategory,
                ImageAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2)
            };
            btnSubPanelAddNewSub.FlatAppearance.BorderColor = Color.White;
            btnSubPanelAddNewSub.Click += AddSubcategory_Click;

            return btnSubPanelAddNewSub;
        }
        private Control GenerateValueBlock(ICategorizedValue value)
        {
            Button newValueBlock = new Button
            {
                Text = value.Value,
                Font = new Font(Font.FontFamily, 9),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Tag = value
            };
            newValueBlock.FlatAppearance.BorderColor = Color.LightGray;
            newValueBlock.MouseDown += ValueBlock_MouseDown;
            newValueBlock.QueryContinueDrag += ValueBlock_QueryContinueDrag;

            return newValueBlock;
        }

        private void ValueBlock_MouseDown(object sender, MouseEventArgs e)
        {
            Button clickedValueBlock = sender as Button;

            if (clickedValueBlock == null)
            {
                return;
            }

            if (Form.ModifierKeys == Keys.Shift)
            {
                SelectRangeOfValueBlocks(clickedValueBlock.Parent, clickedValueBlock);
                return;
            }
            else if (Form.ModifierKeys == Keys.Control && ListContainsValueBlock(m_selectedButtons, clickedValueBlock))
            {
                RemoveValueBlockFromSelection(clickedValueBlock);
            }
            else if (Form.ModifierKeys == Keys.Control)
            {
                AddValueBlockToSelection(clickedValueBlock);
            }
            else if (!ListContainsValueBlock(m_selectedButtons, clickedValueBlock))
            {
                ClearValueBlockSelection();
                AddValueBlockToSelection(clickedValueBlock);
            }

            if (Form.ModifierKeys != Keys.Control)
            {
                List<DraggedData> selectedValueBlocks = new List<DraggedData>();
                foreach (Button valueBlock in m_selectedButtons)
                {
                    selectedValueBlocks.Add(new DraggedData { Type = valueBlock.Tag.GetType(), Control = valueBlock, Data = valueBlock.Tag });
                }
                DragDropEffects dragDropEffects = clickedValueBlock.Parent == m_flpUnorganizedData ? DragDropEffects.Copy : DragDropEffects.Move;
                if (m_flpOrganizedData.Contains(clickedValueBlock))
                {
                    m_pDelete.Visible = true;
                }
                clickedValueBlock.DoDragDrop(selectedValueBlocks, dragDropEffects);

                if (m_selectedButtons.Count > 1)
                {
                    ClearValueBlockSelection();
                }
            }

        }

        private void ValueBlock_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            Button draggedBlock = sender as Button;
            if (draggedBlock == null)
            {
                return;
            }

            if (m_flpOrganizedData.Contains(draggedBlock) && e.Action != DragAction.Continue)
            {
                m_pDelete.Visible = false;
            }
        }

        private void m_btnLoadIncidents_Click(object sender, EventArgs e)
        {
            // Load incident json
            OpenFileDialog ofd = new OpenFileDialog { Filter = "JOSN Files (*.JSON, *.json)|*.JSON;*.json" };
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            LoadIncidents(ofd.FileName);
        }

        public void LoadIncidents(string fileName)
        {
            m_btnLoadIncidents.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            m_bgwLoadIncidentData.RunWorkerAsync(fileName);
        }

        private void LoadValueBlocks(FlowLayoutPanel flp, IEnumerable<string> data)
        {
            flp.Controls.Clear();
            flp.FlowDirection = FlowDirection.LeftToRight;
            Control[] newBlocks = new Control[data.Count()];
            int nbIdx = 0;
            foreach (string dataString in data.OrderBy(x => x))
            {
                CategorizedValue newCatValue = new CategorizedValue { Value = dataString };
                Button newBlock = GenerateValueBlock(newCatValue) as Button;
                newBlocks[nbIdx] = newBlock;
                nbIdx++;
            }
            m_flpUnorganizedData.Controls.Clear();
            m_flpUnorganizedData.Controls.AddRange(newBlocks);
        }

        private void AddSubcategory_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null)
            {
                return;
            }
            FlowLayoutPanel parentPanel = clickedButton.Parent as FlowLayoutPanel;
            if (parentPanel == null)
            {
                return;
            }

            parentPanel.Controls.Remove(clickedButton);

            TextBox tbNewCatNameEntry = new TextBox
            {
                Text = "Enter name of new category",
                Font = new Font(Font.FontFamily, 9),
                Margin = new Padding(10),
                Width = 200
            };
            string newCatName = null;
            tbNewCatNameEntry.Click += NewCatNameEntry_Click;
            tbNewCatNameEntry.KeyDown += ((sndr, args) =>
            {
                if (args.KeyCode == Keys.Enter)
                {
                    newCatName = tbNewCatNameEntry.Text;
                    parentPanel.Controls.Remove(tbNewCatNameEntry);
                    CategoryData newCategory = new CategoryData { Name = newCatName };
                    AddSubPanel(parentPanel, newCategory);
                    parentPanel.Controls.Add(clickedButton);
                }
                else if (args.KeyCode == Keys.Escape)
                {
                    parentPanel.Controls.Remove(tbNewCatNameEntry);
                    parentPanel.Controls.Add(clickedButton);
                    return;
                }
            });
            parentPanel.Controls.Add(tbNewCatNameEntry);
            tbNewCatNameEntry.Focus();
        }

        private void NewCatNameEntry_Click(object sender, EventArgs e)
        {
            TextBox newCatNameEntry = sender as TextBox;
            if (newCatNameEntry == null)
            {
                return;
            }
            newCatNameEntry.Text = string.Empty;
        }

        private List<ICategoryData> ConvertToTree()
        {
            List<ICategoryData> tree = new List<ICategoryData>();
            foreach (Control node in m_flpOrganizedData.Controls)
            {
                if (node is FlowLayoutPanel)
                {
                    tree.Add(node.Tag as ICategoryData);
                }
            }

            return tree;
        }

        private void m_btnSaveTree_Click(object sender, EventArgs e)
        {
            SaveTree();
        }

        private void SaveTree()
        {
            List<ICategoryData> tree = ConvertToTree();
            SaveFileDialog ofd = new SaveFileDialog();
            ofd.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string fileName = ofd.FileName;
                try
                {
                    string jsonTree = JsonConvert.SerializeObject(tree, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
                    File.WriteAllText(fileName, jsonTree);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not save tree.");
                }

                MessageBox.Show("Successfully Saved Tree!");
            }
        }

        private void OrganizedData_Click(object sender, EventArgs e)
        {
            ClearValueBlockSelection();
            ClearPanelSelection();
        }

        private void UnorganizedData_Click(object sender, EventArgs e)
        {
            ClearValueBlockSelection();
            ClearPanelSelection();
        }

        private void TreeEditorControl_Click(object sender, EventArgs e)
        {
            ClearValueBlockSelection();
            ClearPanelSelection();
        }

        private void ClearValueBlockSelection()
        {
            foreach (Button button in m_selectedButtons)
            {
                button.BackColor = Color.White;
            }
            m_selectedButtons.Clear();
        }

        private void AddValueBlockToSelection(Button valueBlock)
        {
            valueBlock.BackColor = Color.LightGray;

            if (!ListContainsValueBlock(m_selectedButtons, valueBlock))
            {
                m_selectedButtons.Add(valueBlock);
            }
        }

        private void MarkValueBlockAsAdded(Button valueBlock)
        {
            valueBlock.Image = Properties.Resources.ok_icon;
            valueBlock.ImageAlign = ContentAlignment.MiddleLeft;
            valueBlock.TextImageRelation = TextImageRelation.ImageBeforeText;
        }

        private void MarkValueBlockAsNotAdded(Button valueBlock)
        {
            valueBlock.Image = null;
        }

        private bool ValueBlockIsAdded(ICategorizedValue vbData, FlowLayoutPanel parentPanel)
        {
            // Check parent panel's data
            ICategoryData parentPanelData = parentPanel.Tag as ICategoryData;
            if (parentPanelData != null)
            {
                foreach (ICategorizedValue value in parentPanelData.Values)
                {
                    if (value == vbData)
                    {
                        return true;
                    }
                }
            }

            // Check children's data
            foreach (Control control in parentPanel.Controls)
            {
                if (!(control is FlowLayoutPanel))
                {
                    continue;
                }

                if (ValueBlockIsAdded(vbData, control as FlowLayoutPanel))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ListContainsValueBlock(IEnumerable<Button> blocks, Button valueBlock)
        {
            ICategorizedValue vbData = valueBlock.Tag as ICategorizedValue;
            if (vbData == null)
            {
                return false;
            }
            bool containsBlock = false;
            foreach (Button vb in m_selectedButtons)
            {
                ICategorizedValue curVBData = vb.Tag as ICategorizedValue;
                if (curVBData == null)
                {
                    continue;
                }

                if (vbData.Value == curVBData.Value && vbData.Description == curVBData.Description && valueBlock.Parent == vb.Parent)
                {
                    containsBlock = true;
                    break;
                }
            }

            return containsBlock;
        }

        private void RemoveValueBlockFromSelection(Button valueBlock)
        {
            valueBlock.BackColor = Color.White;
            if (m_selectedButtons.Contains(valueBlock))
            {
                m_selectedButtons.Remove(valueBlock);
            }
        }

        private void SelectRangeOfValueBlocks(Control container, Button valueBlock)
        {
            AddValueBlockToSelection(valueBlock);

            Button vbOtherEnd = null;
            foreach (Control control in container.Controls)
            {
                if (!(control.Tag is ICategorizedValue))
                {
                    continue;
                }
                if (control == valueBlock)
                {
                    continue;
                }

                if (ListContainsValueBlock(m_selectedButtons, control as Button))
                {
                    vbOtherEnd = control as Button;
                    break;
                }
            }

            if (vbOtherEnd == null)
            {
                return;
            }

            bool seenOne = false;
            foreach (Control control in container.Controls)
            {
                if (!(control is Button))
                {
                    continue;
                }

                if (seenOne)
                {
                    AddValueBlockToSelection(control as Button);
                }

                if (control == valueBlock || control == vbOtherEnd)
                {
                    if (!seenOne)
                    {
                        seenOne = true;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private List<ICategoryData> CreateDefaultTree_Causes()
        {
            List<ICategoryData> tree = new List<ICategoryData>
            {
                new CauseData
                {
                    Name = "Fire",
                },
                new CauseData
                {
                    Name = "EMS",
                },
                new CauseData
                {
                    Name = "MVA"
                },
                new CauseData
                {
                    Name = "Service"
                },
                new CauseData
                {
                    Name = "Rescue"
                },
                new CauseData
                {
                    Name = "Hazmat"
                },
                new CauseData
                {
                    Name = "AFA"
                },
                new CauseData
                {
                    Name = "Other"
                },
                new CauseData
                {
                    Name = "Non-Incident"
                }
            };

            return tree;
        }

        private List<ICategoryData> CreateDefaultTree_NFIRSCauses()
        {
            List<ICategoryData> tree = new List<ICategoryData>();
            Dictionary<string, HashSet<int>> NFIRSCodes = new Dictionary<string, HashSet<int>>
                {
                    {"Fire", new HashSet<int> { 111, 112, 113, 114, 115, 116, 117, 118, 121, 122, 123, 120, 131, 132, 133, 134, 135, 136, 137, 138, 141, 142, 143, 140, 151, 152, 153, 154, 155, 150, 161, 162, 163, 164, 160, 171, 172, 173, 170, 100 } },
                    {"Rupture/Explosion",  new HashSet<int> { 211, 212, 213, 210, 221, 222, 223, 220, 231, 241, 242, 243, 244, 240, 251, 200 }},
                    {"Rescue/EMS", new HashSet<int> { 311, 321, 322, 323, 324, 320, 331, 341, 342, 343, 340, 351, 352, 353, 354, 355, 356, 357, 350, 361, 362, 363, 364, 365, 360, 371, 372, 370, 381, 300 } },
                    {"Hazardous Conditions", new HashSet<int> { 411, 412, 413, 410, 421, 422, 423, 424, 420, 431, 430, 441, 442, 443, 444, 445, 440, 451, 461, 462, 463, 460, 471, 481, 482, 480, 400 } },
                    {"Service", new HashSet<int> { 511, 512, 510, 521, 522, 520, 531, 541, 542, 540, 551, 552, 553, 554, 555, 550, 561, 571, 500 } },
                    {"Good Intent", new HashSet<int> { 611, 621, 622, 631, 632, 641, 651, 652, 653, 650, 661, 671, 672, 600 } },
                    {"False Alarm", new HashSet<int> { 711, 712, 713, 714, 715, 710, 721, 731, 732, 733, 734, 735, 736, 730, 741, 742, 743, 744, 745, 746, 740, 751, 700 } },
                    {"Severe Weather", new HashSet<int> { 811, 812, 813, 814, 815, 800 } },
                    {"Special", new HashSet<int> { 911, 900 } }
                };

            foreach (var entry in NFIRSCodes)
            {
                CauseData causeData = new CauseData();
                causeData.Name = entry.Key;
                foreach (int code in entry.Value)
                {
                    causeData.Values.Add(new NatureCode { Value = code.ToString() });
                }
                tree.Add(causeData);
            }

            return tree;
        }

        private List<ICategoryData> CreateDefaultTree_CharlotteResponse()
        {
            List<ICategoryData> tree = new List<ICategoryData>
            {
                new CauseData
                {
                    Name = "Alarms"
                },
                new CauseData
                {
                    Name = "Structure Fire"
                },
                new CauseData
                {
                    Name = "MVA"
                },
                new CauseData
                {
                    Name = "Non-Emergency"
                },
                new CauseData
                {
                    Name = "Rescue"
                },
                new CauseData
                {
                    Name = "Hazmat"
                },
                new CauseData
                {
                    Name = "ARFF"
                },
                new CauseData
                {
                    Name = "Other"
                },
                new CauseData
                {
                    Name = "EMS: Med-Urgent"
                },
                new CauseData
                {
                    Name = "EMS: Trauma"
                },
                new CauseData
                {
                    Name = "Med-Critical"
                },
                new CauseData
                {
                    Name = "Fire (other)"
                }
            };

            return tree;
        }

        private List<ICategoryData> CreateDefaultTree_CharlotteRootCause()
        {
            List<ICategoryData> tree = new List<ICategoryData>
            {
                new CauseData
                {
                    Name = "Structure Fire"
                },
                new CauseData
                {
                    Name = "Fire (other)"
                },
                new CauseData
                {
                    Name = "MVA"
                },
                new CauseData
                {
                    Name = "EMS-Metabolic"
                },
                new CauseData
                {
                    Name = "EMS-Trauma-Gen"
                },
                new CauseData
                {
                    Name = "EMS-Trauma-Criminal"
                },
                new CauseData
                {
                    Name = "EMS-Respitory"
                },
                new CauseData
                {
                    Name = "EMS-Psych"
                },
                new CauseData
                {
                    Name = "EMS-Neuro"
                },
                new CauseData
                {
                    Name = "Hazmat"
                },
                new CauseData
                {
                    Name = "Rescue"
                },
                new CauseData
                {
                    Name = "Other"
                },
                new CauseData
                {
                    Name = "Non-Emergency"
                },
                new CauseData
                {
                    Name = "ARFF"
                }
            };

            return tree;
        }

        class DraggedData
        {
            public Type Type { get; set; }
            public Control Control { get; set; }
            public object Data { get; set; }
        }

        private void m_cbDefaultTree_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ICategoryData> tree = null;
            switch (m_cbDefaultTree.SelectedItem.ToString())
            {
                case "Causes":
                    tree = CreateDefaultTree_Causes();
                    break;
                case "NFIRS Causes":
                    tree = CreateDefaultTree_NFIRSCauses();
                    break;
                case "Charlotte (response)":
                    tree = CreateDefaultTree_CharlotteResponse();
                    break;
                case "Charlotte (root cause)":
                    tree = CreateDefaultTree_CharlotteRootCause();
                    break;
            }
            if (tree != null)
            {
                LoadTree(tree, m_flpOrganizedData);
            }
        }

        private void SubPanel_Paint(object sender, PaintEventArgs e)
        {
            FlowLayoutPanel panel = sender as FlowLayoutPanel;
            Color borderColor = Color.Gray;
            int borderThickness = 2;
            ButtonBorderStyle borderStyle = ButtonBorderStyle.Solid;

            ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, borderColor, borderThickness, ButtonBorderStyle.Solid, borderColor, borderThickness, borderStyle, borderColor, borderThickness, borderStyle, borderColor, borderThickness, borderStyle);
        }

        private void m_bgwLoadIncidentData_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string fileName = e.Argument.ToString();
            List<IncidentData> incidents = null;
            try
            {
                incidents = JsonConvert.DeserializeObject<List<IncidentData>>(File.ReadAllText(fileName), new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load incidents");
                Cursor.Current = Cursors.Default;
                return;
            }
            if (incidents == null)
            {
                MessageBox.Show("Could not load incidents");
                Cursor.Current = Cursors.Default;
                return;
            }

            e.Result = incidents;
        }

        private void m_bgwLoadIncidentData_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            GetDataFieldFromUser(e.Result as List<IncidentData>);
        }

        public void GetDataFieldFromUser(List<IncidentData> incidents)
        {
            this.Cursor = Cursors.Default;

            // Get list of all incident data fields
            HashSet<string> incidentDataFields = new HashSet<string>();
            foreach (IncidentData incident in incidents)
            {
                foreach (string key in incident.Data.Keys.ToList())
                {
                    if (!incidentDataFields.Contains(key))
                    {
                        incidentDataFields.Add(key);
                    }
                }
            }
            if (incidentDataFields.Count < 1)
            {
                MessageBox.Show("Could not find any data fields in incident data.");
                return;
            }
            Cursor.Current = Cursors.Default;

            // Prompt user to select incident data field
            string selectedField = null;
            ListBox listBox = new ListBox();
            Label header = new Label();
            listBox.Items.AddRange(incidentDataFields.ToArray());
            listBox.DoubleClick += (sdr, args) =>
            {
                selectedField = listBox.SelectedItem.ToString();
                listBox.Hide();
                this.Controls.Remove(listBox);
                this.Controls.Remove(header);

                // Populate unorganized data with data field values
                Cursor.Current = Cursors.WaitCursor;
                HashSet<string> dataFieldValues = new HashSet<string>();
                foreach (IncidentData incident in incidents)
                {
                    if (incident.Data.ContainsKey(selectedField) && !dataFieldValues.Contains(incident.Data[selectedField]))
                    {
                        dataFieldValues.Add(incident.Data[selectedField].ToString());
                    }
                }
                LoadValueBlocks(m_flpUnorganizedData, dataFieldValues);
                m_btnLoadIncidents.Enabled = true;
            };
            listBox.Location = new Point(m_btnLoadIncidents.Location.X, m_btnLoadIncidents.Location.Y - (m_btnLoadIncidents.Height + listBox.Height - listBox.Margin.Top - header.Margin.Bottom));
            listBox.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            listBox.Font = new Font(listBox.Font.FontFamily, 9);

            header.Text = "Please choose a data field";
            header.Font = new Font(header.Font.FontFamily, 9, FontStyle.Bold);
            header.Padding = new Padding(2);
            header.BackColor = Color.White;
            header.Location = new Point(listBox.Location.X, listBox.Location.Y - header.Height);
            header.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            header.AutoSize = true;
            header.BorderStyle = BorderStyle.FixedSingle;
            listBox.Width = TextRenderer.MeasureText(header.Text, header.Font).Width + header.Padding.Right * 2 + 1;
            Cursor.Current = Cursors.Default;

            this.Controls.Add(header);
            this.Controls.Add(listBox);
            header.BringToFront();
            listBox.BringToFront();
        }

        private void m_undoDeleteTimer_Tick(object sender, EventArgs e)
        {
            m_btnUndoDelete.Visible = false;
            m_undoDeleteTimer.Enabled = false;

            DeleteRecycledItems();
        }

        private void m_btnUndoDelete_Click(object sender, EventArgs e)
        {
            foreach (DraggedData draggedData in m_recyclingBin)
            {
                draggedData.Control.Visible = true;
            }
            m_recyclingBin.Clear();
            m_btnUndoDelete.Visible = false;
            m_undoDeleteTimer.Enabled = false;
        }
    }
}
