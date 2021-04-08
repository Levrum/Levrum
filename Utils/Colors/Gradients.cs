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

        public List<Color> ExtractPrimaryColorsFromGradient(List<Color> gradient)
        {
            //This can fail if the number of intervals is too high or the differences between primary colors is too small
            //I'll add a bit of logic to determine what the minimum distance is between primary colors and use that to find missing values...
            if(gradient.Count <= 2)
            {
                return gradient;
            }

            List<Color> primaryColors = new List<Color>();

            Color previousColor = Color.Empty;
            int aSlope = 0;
            int rSlope = 0;
            int gSlope = 0;
            int bSlope = 0;
            //This works off the idea that if the slope of any given color changes, we are moving towards a different color
            //If we are moving towards a different color, the previous color was a primary color...
            foreach(Color c in gradient)
            {
                if(previousColor != Color.Empty)
                {
                    bool primaryFound = false;
                    int colorDiff = 0;

                    //Alpha
                    int currentSlope = c.A - previousColor.A;
                    if (Math.Abs(aSlope - currentSlope) > 1)
                    {
                        primaryFound = true;
                    }
                    colorDiff += Math.Abs(aSlope - currentSlope);
                    aSlope = currentSlope;

                    //Red
                    currentSlope = c.R - previousColor.R;
                    if (rSlope - currentSlope > 1)
                    {
                        primaryFound = true;
                    }
                    rSlope = currentSlope;
                    colorDiff += Math.Abs(rSlope - currentSlope);

                    //Green
                    currentSlope = c.G - previousColor.G;
                    if (gSlope - currentSlope > 1)
                    {
                        primaryFound = true;
                    }
                    colorDiff += Math.Abs(gSlope - currentSlope);
                    gSlope = currentSlope;

                    //Blue
                    currentSlope = c.B - previousColor.B;
                    if (Math.Abs(bSlope - currentSlope) > 1)
                    {
                        primaryFound = true;
                    }
                    colorDiff += Math.Abs(bSlope - currentSlope);
                    bSlope = currentSlope;

                    if (primaryFound || colorDiff > 4)
                    {
                        //Slope changed. Therefore the previous color was a primary color.
                        primaryColors.Add(previousColor);
                    }
                    previousColor = c;
                }
                else
                {
                    //First element. Always will be a primary value
                    previousColor = c;
                    primaryColors.Add(previousColor);
                }
            }

            //Last element. Will always be a primary value
            primaryColors.Add(previousColor);
            return primaryColors;
        }

        public List<Color> InvertColorGradient(List<Color> gradient)
        {
            try
            {
                List<Color> inversedGradient = new List<Color>();
                for (int i = gradient.Count - 1; i >= 0; i--)
                {
                    inversedGradient.Add(gradient[i]);
                }
                return inversedGradient;
            }
            catch
            {
                return gradient;
            }
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
                if(gradientImages.Count == 0)
                {
                    //Set the default colors...
                    List<Color> viridis = new List<Color> { Color.FromArgb(255, 68, 1, 84), Color.FromArgb(255, 72, 20, 102), Color.FromArgb(255, 72, 37, 118), Color.FromArgb(255, 69, 53, 128), Color.FromArgb(255, 65, 67, 135), Color.FromArgb(255, 59, 82, 139), Color.FromArgb(255, 52, 95, 141), Color.FromArgb(255, 46, 108, 142), Color.FromArgb(255, 42, 120, 142), Color.FromArgb(255, 37, 132, 142), Color.FromArgb(255, 32, 144, 140), Color.FromArgb(255, 30, 156, 137), Color.FromArgb(255, 35, 168, 132), Color.FromArgb(255, 47, 180, 123), Color.FromArgb(255, 68, 191, 112), Color.FromArgb(255, 93, 201, 98), Color.FromArgb(255, 122, 209, 81), Color.FromArgb(255, 154, 217, 61), Color.FromArgb(255, 188, 223, 39), Color.FromArgb(255, 222, 227, 25) };
                    viridis = CreateNColorGradient(viridis, 1000);
                    //viridis = InvertColorGradient(viridis);
                    gradientImages.Add(CreateGradientImage(viridis));
                    SaveGradient(CreateGradientImage(viridis), "L1 viridis");
                    List<Color> seabreeze = new List<Color> { Color.White, Color.FromArgb(255, 133, 203, 207), Color.FromArgb(255, 57, 132, 182), Color.FromArgb(255, 29, 46, 129), Color.Black };
                    seabreeze = CreateNColorGradient(seabreeze, 1000);
                    seabreeze = InvertColorGradient(seabreeze);
                    gradientImages.Add(CreateGradientImage(seabreeze));
                    SaveGradient(CreateGradientImage(seabreeze), "L2 seabreeze");
                    List<Color> oldschool = new List<Color> { Color.FromArgb(0, 148, 0), Color.Yellow, Color.Red };
                    oldschool = CreateNColorGradient(oldschool, 1000);
                    oldschool = InvertColorGradient(oldschool);
                    gradientImages.Add(CreateGradientImage(oldschool));
                    SaveGradient(CreateGradientImage(oldschool), "L3 oldschool");
                    List<Color> spectral = new List<Color> { Color.FromArgb(255, 215, 25, 28), Color.FromArgb(255, 223, 55, 42), Color.FromArgb(255, 230, 85, 56), Color.FromArgb(255, 238, 115, 69), Color.FromArgb(255, 245, 144, 83), Color.FromArgb(255, 253, 174, 97), Color.FromArgb(255, 253, 190, 116), Color.FromArgb(255, 254, 206, 135), Color.FromArgb(255, 254, 223, 153), Color.FromArgb(255, 255, 239, 172), Color.FromArgb(255, 255, 255, 191), Color.FromArgb(255, 238, 248, 186), Color.FromArgb(255, 221, 241, 180), Color.FromArgb(255, 204, 235, 175), Color.FromArgb(255, 188, 228, 169), Color.FromArgb(255, 171, 221, 164), Color.FromArgb(255, 145, 203, 168), Color.FromArgb(255, 120, 185, 173), Color.FromArgb(255, 94, 167, 177), Color.FromArgb(255, 68, 149, 182) };
                    spectral = CreateNColorGradient(spectral, 1000);
                    //spectral = InvertColorGradient(spectral);
                    gradientImages.Add(CreateGradientImage(spectral));
                    SaveGradient(CreateGradientImage(spectral), "L5 spectral");
                    List<Color> magma = new List<Color> { Color.FromArgb(255, 0, 0, 4), Color.FromArgb(255, 7, 6, 28), Color.FromArgb(255, 21, 14, 55), Color.FromArgb(255, 38, 17, 86), Color.FromArgb(255, 59, 15, 111), Color.FromArgb(255, 80, 18, 123), Color.FromArgb(255, 100, 26, 128), Color.FromArgb(255, 120, 34, 130), Color.FromArgb(255, 140, 41, 129), Color.FromArgb(255, 161, 48, 126), Color.FromArgb(255, 182, 54, 122), Color.FromArgb(255, 203, 62, 114), Color.FromArgb(255, 221, 74, 105), Color.FromArgb(255, 237, 89, 95), Color.FromArgb(255, 246, 112, 92), Color.FromArgb(255, 251, 136, 97), Color.FromArgb(255, 254, 160, 109), Color.FromArgb(255, 254, 183, 126), Color.FromArgb(255, 254, 206, 145), Color.FromArgb(255, 253, 230, 167) };
                    magma = CreateNColorGradient(magma, 1000);
                    //magma = InvertColorGradient(magma);
                    gradientImages.Add(CreateGradientImage(magma));
                    SaveGradient(CreateGradientImage(magma), "L6 magma");
                    List<Color> inferno = new List<Color> { Color.FromArgb(255, 0, 0, 4), Color.FromArgb(255, 8, 6, 29), Color.FromArgb(255, 23, 11, 58), Color.FromArgb(255, 44, 11, 88), Color.FromArgb(255, 66, 10, 104), Color.FromArgb(255, 87, 15, 109), Color.FromArgb(255, 107, 23, 110), Color.FromArgb(255, 127, 31, 108), Color.FromArgb(255, 147, 38, 103), Color.FromArgb(255, 168, 46, 95), Color.FromArgb(255, 187, 55, 85), Color.FromArgb(255, 204, 66, 72), Color.FromArgb(255, 221, 81, 57), Color.FromArgb(255, 234, 98, 42), Color.FromArgb(255, 243, 119, 26), Color.FromArgb(255, 249, 142, 10), Color.FromArgb(255, 252, 165, 11), Color.FromArgb(255, 251, 190, 35), Color.FromArgb(255, 246, 214, 69), Color.FromArgb(255, 242, 238, 114) };
                    inferno = CreateNColorGradient(inferno, 1000);
                    //inferno = InvertColorGradient(inferno);
                    gradientImages.Add(CreateGradientImage(inferno));
                    SaveGradient(CreateGradientImage(inferno), "L7 inferno");
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
