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
        List<Button> m_selectedButtons = new List<Button>();
        public TreeEditorControl()
        {
            InitializeComponent();
            m_btnLoadIncidents.Visible = LoadIncidentsButton;
        }

        private void OrganizedPanel_DragEnter(object sender, DragEventArgs e)
        {
            // Allow dropping of category panels
            if (e.Data.GetDataPresent(typeof(DraggedData)) && (e.Data.GetData(typeof(DraggedData)) as DraggedData).Data is ICategoryData)
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

            DraggedData draggedData = e.Data.GetData(typeof(DraggedData)) as DraggedData;
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
                foreach(DraggedData draggedData in allDraggedData)
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

            List<CategoryData> tree = JsonConvert.DeserializeObject<List<CategoryData>>(File.ReadAllText(fileName), new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
            m_flpUnorganizedData.Controls.Clear();
            LoadTreeIntoFLP(tree, m_flpUnorganizedData);
        }

        public void LoadTreeIntoFLP(IEnumerable<ICategoryData> categoryData, FlowLayoutPanel parentPanel)
        {
            foreach (ICategoryData data in categoryData)
            {
                FlowLayoutPanel newPanel = AddSubPanel(parentPanel, data);
                foreach (ICategorizedValue value in data.Values)
                {
                    newPanel.Controls.Add(GenerateValueBlock(value));
                }

                LoadTreeIntoFLP(data.Children, newPanel);
            }
        }

        private FlowLayoutPanel AddSubPanel(FlowLayoutPanel parentPanel, ICategoryData panelCatData)
        {
            string panelName = panelCatData.Name;

            FlowLayoutPanel newPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Height = 200,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Margin = new Padding(10),
                Padding = new Padding(8),
                Name = panelName,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoScroll = false,
                Anchor = (AnchorStyles.Left | AnchorStyles.Right),
                BackColor = Color.WhiteSmoke,
                AllowDrop = true,
                Tag = panelCatData,
            };
            newPanel.MouseDown += SubPanel_MouseDown;
            newPanel.DragEnter += SubPanel_DragEnter;
            newPanel.DragDrop += SubPanel_DragDrop;

            newPanel.Controls.Add(new Label
            {
                Text = panelName,
                Font = new Font("Microsoft Sans Serif", 10),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                Dock = DockStyle.Top,
                Anchor = (AnchorStyles.Left | AnchorStyles.Right)
            });

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
            newPanel.Controls.Add(btnSubPanelAddNewSub);

            parentPanel.Controls.Add(newPanel);

            return newPanel;
        }

        private void SubPanel_MouseDown(object sender, MouseEventArgs e)
        {
            FlowLayoutPanel clickedPanel = sender as FlowLayoutPanel;
            if (clickedPanel == null)
            {
                return;
            }
            DraggedData draggedData = new DraggedData { Control = clickedPanel, Data = clickedPanel.Tag, Type = clickedPanel.Tag.GetType() };
            DragDropEffects dragDropEffects = clickedPanel.Parent == m_flpUnorganizedData ? DragDropEffects.Copy : DragDropEffects.Move;
            clickedPanel.DoDragDrop(draggedData, DragDropEffects.Move);
        }

        private void SubPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(List<DraggedData>)))
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
        }

        private Control GenerateValueBlock(ICategorizedValue value)
        {
            Button newValueBlock = new Button
            {
                Text = value.Value,
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
            
            if (!m_selectedButtons.Contains(clickedValueBlock))
            {
                clickedValueBlock.BackColor = Color.LightGray;
                m_selectedButtons.Add(clickedValueBlock);
            }
            if (Form.ModifierKeys == Keys.Shift)
            {
                SelectRangeOfValueBlocks(clickedValueBlock.Parent, clickedValueBlock);
                return;
            }
            else if (Form.ModifierKeys == Keys.Control)
            {
                clickedValueBlock.BackColor = Color.White;
                m_selectedButtons.Remove(clickedValueBlock);
            }

            if (clickedValueBlock == null)
            {
                return;
            }

            List<DraggedData> selectedValueBlocks = new List<DraggedData>();
            foreach (Button valueBlock in m_selectedButtons)
            {
                selectedValueBlocks.Add(new DraggedData { Type = valueBlock.Tag.GetType(), Control = valueBlock, Data = valueBlock.Tag });
            }
            DragDropEffects dragDropEffects = clickedValueBlock.Parent == m_flpUnorganizedData ? DragDropEffects.Copy : DragDropEffects.Move;
            clickedValueBlock.DoDragDrop(selectedValueBlocks, dragDropEffects);

            if (Form.ModifierKeys != Keys.Control && m_selectedButtons.Count > 1)
            {
                ClearValueBlockSelection();
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

            // Prompt user to select incident data field
            string selectedField = null;
            ListBox listBox = new ListBox();
            listBox.Items.AddRange(incidentDataFields.ToArray());
            listBox.DoubleClick += (sdr, args) =>
            {
                selectedField = listBox.SelectedItem.ToString();
                listBox.Hide();
                this.Controls.Remove(listBox);

                // Populate unorganized data with data field values
                HashSet<string> dataFieldValues = new HashSet<string>();
                foreach (IncidentData incident in incidents)
                {
                    if (incident.Data.ContainsKey(selectedField) && !dataFieldValues.Contains(incident.Data[selectedField]))
                    {
                        dataFieldValues.Add(incident.Data[selectedField].ToString());
                    }
                }
                PopulateUnorganizedDataWithValueBlocks(dataFieldValues);
            };
            listBox.Location = new Point(m_btnLoadIncidents.Location.X, m_btnLoadIncidents.Location.Y - (m_btnLoadIncidents.Height + listBox.Height + 4));
            listBox.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            this.Controls.Add(listBox);
            listBox.BringToFront();
        }

        private void PopulateUnorganizedDataWithValueBlocks(IEnumerable<string> data)
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

            m_flpUnorganizedData.Controls.AddRange(newBlocks);
        }

        private void AddSingleBlockToUnorganizedData(string code)
        {
            Button newButton = new Button { Text = code, Width = 100 };
            m_flpUnorganizedData.Controls.Add(newButton);
            Control.ControlCollection importedButtons = m_flpUnorganizedData.Controls;
            List<string> importedButtonsText = new List<string>();
            foreach (Control button in importedButtons)
            {
                importedButtonsText.Add(button.Text);
            }
            importedButtonsText = importedButtonsText.OrderBy(x => x).ToList();
            for (int i = 0; i < m_flpUnorganizedData.Controls.Count; i++)
            {
                m_flpUnorganizedData.Controls[i].Text = importedButtonsText[i];
            }
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
        }

        private void m_flpUnorganizedData_Click(object sender, EventArgs e)
        {
            ClearValueBlockSelection();
        }

        private void TreeEditorControl_Click(object sender, EventArgs e)
        {
            ClearValueBlockSelection();
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
            if (!m_selectedButtons.Contains(valueBlock))
            {
                m_selectedButtons.Add(valueBlock);
            }
        }

        private void SelectRangeOfValueBlocks(Control container, Button valueBlock)
        {
            Button vbOtherEnd = null;
            foreach (Control control in container.Controls)
            {
                if (!(control is Button))
                {
                    continue;
                }
                if (control == valueBlock)
                {
                    continue;
                }

                if (m_selectedButtons.Contains(control as Button))
                {
                    vbOtherEnd = control as Button;
                    break;
                }
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

        class DraggedData
        {
            public Type Type { get; set; }
            public Control Control { get; set; }
            public object Data { get; set; }
        }        
    }
}
