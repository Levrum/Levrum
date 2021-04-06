using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Levrum.Utils.Colors
{
    public class Gradients
    {
        public List<Color> CreateTwoColorGradient(Color low, Color high, int numberOfIntervals = 100)
        {
            //This was adapted from C3mGMapControl.cs
            //It will take in 2 colors and return a gradient with the chosen number of interval steps
            try
            {
                List<Color> gradientColors = new List<Color>();

                double first_interval_A = Convert.ToDouble(high.A - low.A) / (numberOfIntervals - 1);
                double first_interval_R = Convert.ToDouble(high.R - low.R) / (numberOfIntervals - 1);
                double first_interval_G = Convert.ToDouble(high.G - low.G) / (numberOfIntervals - 1);
                double first_interval_B = Convert.ToDouble(high.B - low.B) / (numberOfIntervals - 1);

                double current_A = low.A;
                double current_R = low.R;
                double current_G = low.G;
                double current_B = low.B;
                Color col;
                for (int i = 0; i < numberOfIntervals - 1; i++)
                {
                    col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                    gradientColors.Add(col);

                    //increment.
                    current_A += first_interval_A;
                    current_R += first_interval_R;
                    current_G += first_interval_G;
                    current_B += first_interval_B;
                }
                col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                gradientColors.Add(col);
                return gradientColors;
            }
            catch
            {
                return new List<Color>();
            }
        }

        public List<Color> CreateThreeColorGradient(Color low, Color mid, Color high, int numberOfIntervals = 100)
        {
            //This was taken from C3mGMapControl.cs and made more generic and with a couple of bug fixes
            //It will take in 3 colors and return a gradient with the chosen number of interval steps

            try
            {
                List<Color> gradientColors = new List<Color>();

                double first_interval_A = Convert.ToDouble(mid.A - low.A) / ((numberOfIntervals / 2) - 1);
                double first_interval_R = Convert.ToDouble(mid.R - low.R) / ((numberOfIntervals / 2) - 1);
                double first_interval_G = Convert.ToDouble(mid.G - low.G) / ((numberOfIntervals / 2) - 1);
                double first_interval_B = Convert.ToDouble(mid.B - low.B) / ((numberOfIntervals / 2) - 1);

                double current_A = low.A;
                double current_R = low.R;
                double current_G = low.G;
                double current_B = low.B;
                Color col;
                for (int i = 0; i < (numberOfIntervals / 2) - 1; i++)
                {
                    col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                    gradientColors.Add(col);

                    //increment.
                    current_A += first_interval_A;
                    current_R += first_interval_R;
                    current_G += first_interval_G;
                    current_B += first_interval_B;
                }

                col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                gradientColors.Add(col);

                double second_interval_A = Convert.ToDouble(high.A - mid.A) / ((numberOfIntervals / 2) - 1);
                double second_interval_R = Convert.ToDouble(high.R - mid.R) / ((numberOfIntervals / 2) - 1);
                double second_interval_G = Convert.ToDouble(high.G - mid.G) / ((numberOfIntervals / 2) - 1);
                double second_interval_B = Convert.ToDouble(high.B - mid.B) / ((numberOfIntervals / 2) - 1);

                current_A = mid.A;
                current_R = mid.R;
                current_G = mid.G;
                current_B = mid.B;

                for (int i = numberOfIntervals / 2; i < numberOfIntervals - 1; i++)
                {
                    col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                    gradientColors.Add(col);

                    //increment.
                    current_A += second_interval_A;
                    current_R += second_interval_R;
                    current_G += second_interval_G;
                    current_B += second_interval_B;
                }
                col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                gradientColors.Add(col);
                return gradientColors;
            }
            catch
            {
                return new List<Color>();
            }
        }

        public List<Color> CreateNColorGradient(List<Color> colorsLowToHigh, int numberOfIntervals = 100)
        {
            try
            {
                List<Color> gradient = new List<Color>();

                int intervalsPerColor = (numberOfIntervals / (colorsLowToHigh.Count - 1));
                int leftovers = numberOfIntervals - (intervalsPerColor * (colorsLowToHigh.Count - 1));

                for (int n = 0; n < colorsLowToHigh.Count - 1; n++)
                {
                    if (leftovers >= (colorsLowToHigh.Count - n) - 1)
                    {
                        if (leftovers == 1)
                        {
                            //I actually reintroduced a bug with the original gradients where the last color wasn't getting added to prevent
                            //the middle gradient colors from getting added twice. This "fixes" it for the last color
                            AddGradientSegment(gradient, colorsLowToHigh[n], colorsLowToHigh[n + 1], intervalsPerColor);
                            gradient.Add(colorsLowToHigh[n + 1]);
                        }
                        else
                        {
                            AddGradientSegment(gradient, colorsLowToHigh[n], colorsLowToHigh[n + 1], intervalsPerColor + 1);
                        }
                        leftovers--;
                    }
                    else
                    {
                        AddGradientSegment(gradient, colorsLowToHigh[n], colorsLowToHigh[n + 1], intervalsPerColor);
                    }
                }

                return gradient;
            }
            catch
            {
                return new List<Color>();
            }
        }

        private void AddGradientSegment(List<Color> gradientSoFar, Color low, Color high, int numberOfIntervals)
        {
            double first_interval_A = Convert.ToDouble(high.A - low.A) / numberOfIntervals;
            double first_interval_R = Convert.ToDouble(high.R - low.R) / numberOfIntervals;
            double first_interval_G = Convert.ToDouble(high.G - low.G) / numberOfIntervals;
            double first_interval_B = Convert.ToDouble(high.B - low.B) / numberOfIntervals;

            double current_A = low.A;
            double current_R = low.R;
            double current_G = low.G;
            double current_B = low.B;
            Color col;
            for (int i = 0; i < numberOfIntervals - 1; i++)
            {
                col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
                gradientSoFar.Add(col);
                current_A += first_interval_A;
                current_R += first_interval_R;
                current_G += first_interval_G;
                current_B += first_interval_B;
            }
            col = Color.FromArgb(Convert.ToInt32(current_A), Convert.ToInt32(current_R), Convert.ToInt32(current_G), Convert.ToInt32(current_B));
            gradientSoFar.Add(col);
        }

        public Image CreateGradientImage(List<Color> gradient)
        {
            //Will create a horizontal image with dimensions of color count x 200 pixels
            if (gradient.Count <= 0)
            {
                return null;
            }

            int width = gradient.Count;
            int height = 200;

            Image gradientImage = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(gradientImage))
            {
                for (int i = 0; i < width; i++)
                {
                    Color c = gradient[i];
                    Rectangle line = new Rectangle(i, 0, 1, height);
                    g.DrawRectangle(new Pen(c), line);
                    g.FillRectangle(new SolidBrush(c), line);
                }
            }
            return gradientImage;
        }

        public Image CreateGradientImage(List<Color> gradient, int width, int height, bool drawHorizontally = true)
        {
            //Allows custom dimensions for the gradient.
            //If horizontal, having a width too small will lead to data loss. If vertical, the same is true for the height.
            if (width <= 0 || height <= 0 || gradient.Count <= 0)
            {
                return null;
            }

            Image gradientImage = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(gradientImage))
            {
                if (drawHorizontally)
                {
                    for (int i = 0; i < width; i++)
                    {
                        int currentIndex = Convert.ToInt32((double)gradient.Count * ((double)i / (double)width));
                        if (currentIndex > gradient.Count - 1)
                        {
                            currentIndex = gradient.Count - 1;
                        }
                        Color c = gradient[currentIndex];
                        Rectangle line = new Rectangle(i, 0, 1, height);
                        g.DrawRectangle(new Pen(c), line);
                        g.FillRectangle(new SolidBrush(c), line);
                    }
                }
                else
                {
                    for (int i = 0; i < height; i++)
                    {
                        Color c = gradient[i / height];
                        Rectangle line = new Rectangle(0, i, width, 1);
                        g.DrawRectangle(new Pen(c), line);
                        g.FillRectangle(new SolidBrush(c), line);
                    }
                }
            }
            return gradientImage;
        }

        public List<Color> ExtractGradientFromImage(Image gradientImage)
        {
            List<Color> gradient = new List<Color>();
            int width = gradientImage.Width;
            int height = gradientImage.Height;

            Bitmap gradientBitmap = new Bitmap(gradientImage);
            Color previousColor = Color.Empty;
            for (int x = 0; x < width; x++)
            {
                //Checks each pixel horizonally to get colors
                Color pixel = gradientBitmap.GetPixel(x, 0);
                if (previousColor != Color.Empty && pixel.Name != previousColor.Name)
                {
                    gradient.Add(pixel);
                }
                else
                {
                    gradient.Add(pixel);
                }
                previousColor = pixel;
            }
            if (gradient.Count > 1)
            {
                //If there was only one color, the gradient is going in another direction...
                return gradient;
            }

            previousColor = Color.Empty;
            for (int y = 0; y < height; y++)
            {
                Color pixel = gradientBitmap.GetPixel(0, y);
                if (previousColor != Color.Empty && pixel.Name != previousColor.Name)
                {
                    gradient.Add(pixel);
                }
                else
                {
                    gradient.Add(pixel);
                }
                previousColor = pixel;
            }

            return gradient;
        }

        public bool SaveGradient(Image gradientImage, string fileName = "")
        {
            //If a file name is not provided, it will be named Gradient#.png where # is the first available int...
            //The file name should only be the name. Don't include the folder...
            try
            {
                string folder = AppSettings.ColorDir + "\\Gradients\\";
                Directory.CreateDirectory(folder);

                if (fileName == "")
                {
                    int fileNumber = 1;
                    while (File.Exists(string.Concat(folder, "Gradient", fileNumber, ".png")))
                    {
                        fileNumber++;
                    }
                    gradientImage.Save(string.Concat(folder, "Gradient", fileNumber, ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    if (fileName.EndsWith(".png"))
                    {
                        if(File.Exists(string.Concat(folder, fileName)))
                        {
                            File.Delete(string.Concat(folder, fileName));
                        }
                        gradientImage.Save(string.Concat(folder, fileName), System.Drawing.Imaging.ImageFormat.Png);
                    }
                    else
                    {
                        if (File.Exists(string.Concat(folder, fileName, ".png")))
                        {
                            File.Delete(string.Concat(folder, fileName, ".png"));
                        }
                        gradientImage.Save(string.Concat(folder, fileName, ".png"), System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                return true;
            }
            catch(Exception exc)
            {
                return false;
            }
        }

        public bool SaveGradient(List<Color> gradient, string fileName = "", int customHeight = 100)
        {
            //If a file name is not provided, it will be named Gradient#.png where # is the first available int...
            try
            {
                string folder = AppSettings.ColorDir + "\\Gradients\\";
                Directory.CreateDirectory(folder);

                if (fileName == "")
                {
                    int fileNumber = 1;
                    while (File.Exists(string.Concat(folder, "Gradient", fileNumber, ".png")))
                    {
                        fileNumber++;
                    }
                    CreateGradientImage(gradient, gradient.Count - 1, customHeight).Save(string.Concat(folder, "Gradient", fileNumber, ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    if (fileName.EndsWith(".png"))
                    {
                        CreateGradientImage(gradient, gradient.Count - 1, customHeight).Save(string.Concat(folder, fileName), System.Drawing.Imaging.ImageFormat.Png);
                    }
                    else
                    {
                        CreateGradientImage(gradient, gradient.Count - 1, customHeight).Save(string.Concat(folder, fileName, ".png"), System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SaveGradientList(List<Image> gradientList)
        {
            try
            {
                foreach (Image gradient in gradientList)
                {
                    SaveGradient(gradient);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<Color> LoadGradient(string fileName)
        {
            try
            {
                Image gradient = LoadGradientImage(fileName);

                if (gradient != null)
                {
                    return ExtractGradientFromImage(LoadGradientImage(fileName));
                }
                else
                {
                    return new List<Color>();
                }
            }
            catch
            {
                return new List<Color>();
            }
        }

        public Image LoadGradientImage(string fileName)
        {
            try
            {
                string file = string.Concat(AppSettings.ColorDir, "\\Gradients\\", fileName);
                if (File.Exists(file))
                {
                    return Image.FromFile(file);
                }
                else if (!file.EndsWith(".png"))
                {
                    file += ".png";
                    if (File.Exists(file))
                    {
                        return Image.FromFile(file);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public List<Image> LoadAllGradientImages()
        {

            try
            {
                List<Image> gradientImages = new List<Image>();
                foreach (string file in Directory.EnumerateFiles(string.Concat(AppSettings.ColorDir, "\\Gradients\\"), "*.png"))
                {
                    gradientImages.Add(Image.FromFile(file));
                }
                return gradientImages;
            }
            catch
            {
                return new List<Image>();
            }
        }
    }
}
