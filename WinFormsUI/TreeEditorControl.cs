using Levrum.Data.Classes;
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

        List<CauseData> causeDatas = new List<CauseData>
        {
            new CauseData
            {
                Name = "Fire",
                NatureCodes = new List<ICategorizedValue>
                {
                    new NatureCode { Value = "700" },
                    new NatureCode { Value = "701"}
                },
                Children = new List<ICategoryData>
                {
                    new CauseData
                    {
                        Name = "Structure Fire",
                        NatureCodes = new List<ICategorizedValue>
                        {
                            new NatureCode { Value = "731"}
                        }
                    }
                }
            },
            new CauseData
            {
                Name = "EMS",
                NatureCodes = new List<ICategorizedValue>
                {
                    new NatureCode { Value = "100" },
                    new NatureCode { Value = "101"}
                },
                Children = new List<ICategoryData>
                {
                    new CauseData
                    {
                        Name = "EMS Urgent",
                        NatureCodes = new List<ICategorizedValue>
                        {
                            new NatureCode { Value = "131"}
                        }
                    }
                }
            },
        };

        List<FlowLayoutPanel> m_selectedPanels = new List<FlowLayoutPanel>();
        List<Button> m_selectedButtons = new List<Button>();

        public TreeEditorControl()
        {
            InitializeComponent();
            m_btnLoadIncidents.Visible = LoadIncidentsButton;
            MoveCursor = new Cursor(Properties.Resources.move_button.Handle);
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
            // Allow dropping of category panels
            if (e.Data.GetDataPresent(typeof(List<DraggedData>)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void UnorganizedPanel_DragDrop(object sender, DragEventArgs e)
        {
            List<DraggedData> allDraggedData = e.Data.GetData(typeof(List<DraggedData>)) as List<DraggedData>;

            if (e.AllowedEffect == DragDropEffects.Move)
            {
                foreach (DraggedData draggedData in allDraggedData)
                {
                    ICategoryData oldParentData = draggedData.Control.Parent.Tag as ICategoryData;
                    if (draggedData.Data is ICategoryData)
                    {
                        oldParentData?.Children?.Remove(draggedData.Data as ICategoryData);
                    }
                    else if (draggedData.Data is ICategorizedValue)
                    {
                        oldParentData?.Values?.Remove(draggedData.Data as ICategorizedValue);
                    }
                    draggedData.Control.Parent.Controls.Remove(draggedData.Control);
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

        public void LoadTree(IEnumerable<ICategoryData> categoryData, FlowLayoutPanel parentPanel)
        {
            parentPanel.Controls.Clear();
            parentPanel.FlowDirection = FlowDirection.TopDown;
            parentPanel.SuspendLayout();
            foreach (ICategoryData data in categoryData)
            {
                FlowLayoutPanel newPanel = AddSubPanel(parentPanel, data);
            }
            parentPanel.Controls.Add(GenerateAddSubcategoryButton());
            parentPanel.ResumeLayout();
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
                BackColor = Color.WhiteSmoke,
                AllowDrop = true,
                Tag = panelCatData,
            };
            newPanel.MouseDown += SubPanel_MouseDown;
            newPanel.DragEnter += SubPanel_DragEnter;
            newPanel.DragDrop += SubPanel_DragDrop;

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
            
            newPanel.Controls.Add(GenerateAddSubcategoryButton());

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
                clickedPanel.DoDragDrop(selectedPanels, DragDropEffects.Move);

                if (m_selectedPanels.Count > 1)
                {
                    ClearPanelSelection();
                }
            }
        }

        private void ClearPanelSelection()
        {
            foreach (FlowLayoutPanel flp in m_selectedPanels)
            {
                flp.BackColor = Color.WhiteSmoke;
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

        private Button GenerateAddSubcategoryButton()
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
            btnSubPanelAddNewSub.FlatAppearance.BorderColor = Color.WhiteSmoke;
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
            newValueBlock.MouseDown += ValueBlock_MouseDown;

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
                clickedValueBlock.DoDragDrop(selectedValueBlocks, dragDropEffects);

                if (m_selectedButtons.Count > 1)
                {
                    ClearValueBlockSelection();
                }
            }

        }

        private void m_btnLoadIncidents_Click(object sender, EventArgs e)
        {
            // Load incident json
            List<IncidentData> incidents = null;
            OpenFileDialog ofd = new OpenFileDialog { Filter = "JOSN Files (*.JSON, *.json)|*.JSON;*.json" };
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            incidents = JsonConvert.DeserializeObject<List<IncidentData>>(File.ReadAllText(ofd.FileName), new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
            if (incidents == null)
            {
                MessageBox.Show("Could not load incidents");
                return;
            }

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
                HashSet<string> dataFieldValues = new HashSet<string>();
                foreach (IncidentData incident in incidents)
                {
                    if (incident.Data.ContainsKey(selectedField) && !dataFieldValues.Contains(incident.Data[selectedField]))
                    {
                        dataFieldValues.Add(incident.Data[selectedField].ToString());
                    }
                }
                LoadValueBlocks(m_flpUnorganizedData, dataFieldValues);
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

            this.Controls.Add(header);
            this.Controls.Add(listBox);
            header.BringToFront();
            listBox.BringToFront();
        }

        private void LoadValueBlocks(FlowLayoutPanel flp, IEnumerable<string> data)
        {
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

        private void m_flpOrganizedData_Click(object sender, EventArgs e)
        {
            ClearValueBlockSelection();
            ClearPanelSelection();
        }

        private void m_flpUnorganizedData_Click(object sender, EventArgs e)
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

        private void CreateDefaultTree_Causes()
        {
            List<CauseData> causeTree = new List<CauseData>
            {
                new CauseData
                {
                    Name = "Fire",
                    Children = new List<ICategoryData>
                    {
                        new CauseData
                        {
                            Name = "Structure Fire",
                        },
                        new CauseData
                        {
                            Name = "Outdoor Fire"
                        },
                        new CauseData
                        {
                            Name = "Vehicle Fire"
                        },
                        new CauseData
                        {
                            Name = "Other Fire"
                        }
                    }
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

            LoadTree(causeTree, m_flpOrganizedData);
        }

        class DraggedData
        {
            public Type Type { get; set; }
            public Control Control { get; set; }
            public object Data { get; set; }
        }

        private void m_cbDefaultTree_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_cbDefaultTree.SelectedItem.ToString() == "Causes")
            {
                CreateDefaultTree_Causes();
            }
        }
    }
}
