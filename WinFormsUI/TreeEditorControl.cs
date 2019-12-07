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
        public TreeEditorControl()
        {
            InitializeComponent();
            
        }

        private void MainPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FlowLayoutPanel)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainPanel_DragDrop(object sender, DragEventArgs e)
        {
            FlowLayoutPanel receivingPanel = sender as FlowLayoutPanel;
            if (receivingPanel == null)
            {
                return;
            }

            Control droppedControl = e.Data.GetData(typeof(FlowLayoutPanel)) as FlowLayoutPanel;
            if (droppedControl == null)
            {
                return;
            }

            receivingPanel.Controls.Add(droppedControl);

            List<Control> receivingPanelControls = new List<Control>();
            foreach (Control control in receivingPanel.Controls)
            {
                receivingPanelControls.Add(control);
            }
            Button receivingPanelAddButton = receivingPanelControls.Where(x => x is Button).FirstOrDefault() as Button;
            if (receivingPanelAddButton != null)
            {
                receivingPanel.Controls.Remove(receivingPanelAddButton);
                receivingPanel.Controls.Add(receivingPanelAddButton);
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
                Font = new Font("Microsoft Sans Serif", 9),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2),
                Dock = DockStyle.Top,
                Anchor = (AnchorStyles.Left | AnchorStyles.Right)
            });

            Button btnSubPanelAddNewSub = new Button { Text = "Add Subcategory", AutoSize = true, Margin = new Padding(2) };
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

            clickedPanel.DoDragDrop(clickedPanel, DragDropEffects.Move);
        }

        private void SubPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)) || e.Data.GetDataPresent(typeof(FlowLayoutPanel)))
            {
                e.Effect = DragDropEffects.Move;
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
                throw new Exception("Panel does not contain data");
            }

            Control droppedControl = e.Data.GetData(typeof(Button)) as Button;
            if (droppedControl == null)
            {
                droppedControl = e.Data.GetData(typeof(FlowLayoutPanel)) as FlowLayoutPanel;
                if (droppedControl == null)
                {
                    return;
                }
            }            

            if (droppedControl is FlowLayoutPanel)
            {
                ICategoryData droppedData = droppedControl.Tag as ICategoryData;
                if (droppedData != null)
                {
                    if (droppedControl.Parent.Tag != null)
                    {
                        ICategoryData oldParentData = droppedControl.Parent.Tag as ICategoryData;
                        oldParentData.Children.Remove(droppedData);
                    }

                    receivingPanelData.Children.Add(droppedData);
                }

                receivingPanel.Controls.Add(droppedControl);
            }
            else if (droppedControl is Button)
            {
                ICategorizedValue droppedData = droppedControl.Tag as ICategorizedValue;
                if (droppedData != null)
                {
                    if (droppedControl.Parent.Tag != null)
                    {
                        ICategoryData oldParentData = droppedControl.Parent.Tag as ICategoryData;
                        oldParentData.Values.Remove(droppedData);
                    }

                    receivingPanelData.Values.Add(droppedData);
                }

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
                receivingPanel.Controls.Add(droppedControl);
                panels.ForEach(x => receivingPanel.Controls.Add(x));
            }

            Control addSubBtn = null;
            foreach (Control control in receivingPanel.Controls)
            {
                if (control.Text == "Add Subcategory")
                {
                    addSubBtn = control;
                    break;
                }
            }
            if (addSubBtn == null)
            {
                return;
            }
            
            receivingPanel.Controls.Remove(addSubBtn);
            receivingPanel.Controls.Add(addSubBtn);
        }

        private Control GenerateValueBlock(ICategorizedValue value)
        {
            Button newValueBlock = new Button
            {
                Text = value.Value,
                Tag = value
            };
            newValueBlock.MouseDown += ValueBlock_MouseDown;

            return newValueBlock;
        }

        private void ValueBlock_MouseDown(object sender, MouseEventArgs e)
        {
            Control valueBlock = sender as Control;
            if (valueBlock == null)
            {
                return;
            }

            valueBlock.DoDragDrop(valueBlock, DragDropEffects.Move);
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
    }
}
