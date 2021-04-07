using Levrum.Utils.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Levrum.UI.WinForms.Controls
{
    public sealed class GradientComboBox : ComboBox
    {
        //Used to display gradients in a list. Can also support text if needed...
        //Add gradients with gradientComboBox.AddGradient(gradient);
        //Get the selected gradient with gradientComboBox.GetSelectedGradient();

        Gradients g = new Gradients();
        public GradientComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();
            if (e.Index >= 0 && e.Index < Items.Count)
            {
                var item = Items[e.Index];
                if (item.GetType().Name == "GradientDropDownItem")
                {
                    GradientDropDownItem gradientItem = (GradientDropDownItem)Items[e.Index];
                    e.Graphics.DrawImage(g.CreateGradientImage(gradientItem.Gradient, (e.Bounds.Right - e.Bounds.Left), (e.Bounds.Bottom - e.Bounds.Top)),
                        e.Bounds.Left, e.Bounds.Top);
                }
                else if (item.GetType().Name == "String")
                {
                    e.Graphics.DrawString(item.ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + 2, e.Bounds.Top + 2);
                }
            }
            base.OnDrawItem(e);
        }

        public void AddGradient(List<Color> gradient)
        {
            try
            {
                Items.Add(new GradientDropDownItem(gradient));
            }
            catch
            {

            }
        }

        public void AddGradient(Image gradient)
        {
            try
            {
                Items.Add(new GradientDropDownItem(gradient));
            }
            catch
            {

            }
        }

        public void AddAllGradients()
        {
            try
            {
                foreach (Image gradient in g.LoadAllGradientImages())
                {
                    AddGradient(gradient);
                }
            }
            catch
            {

            }

        }

        public List<Color> GetSelectedGradient()
        {
            var item = Items[SelectedIndex];
            if (item.GetType().Name == "GradientDropDownItem")
            {
                GradientDropDownItem gradient = (GradientDropDownItem)item;
                return gradient.Gradient;
            }
            return new List<Color>();
        }

        private class GradientDropDownItem
        {
            //The items added to the ComboBox that will display the gradient.
            public List<Color> Gradient { get; set; }

            public GradientDropDownItem(Image gradient)
            {
                Gradient = new Gradients().ExtractGradientFromImage(gradient);
            }

            public GradientDropDownItem(List<Color> gradient)
            {
                Gradient = gradient;
            }
        }
    }
}
