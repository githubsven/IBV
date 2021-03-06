﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        private Color BackgroundColor = Color.Black;
        private Color FocusColor = Color.White;

        public INFOIBV()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage != null) InputImage.Dispose();               // Reset image
                InputImage = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                    pictureBox1.Image = (Image)InputImage;                 // Display input image
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)

            /* Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;*/

            // Copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            Image = MakeGrey(Image);
            Color[,] secondImage = Image;

            Image = Prewitt(Image);
            secondImage = Threshold(secondImage);
      
            Image = Max(Image, secondImage);
            Image = Threshold(Image, true);
            Image = Close(Dilate(Close((Erode(Image)))));

            int[,] intImage = colorToInt(Image);

            intImage = labeling(intImage);
            intImage = removeBorderShapes(intImage);
            intImage = removeAfterElongation(intImage);
            Image = colorLabeling(intImage);

            Image = Close(Image);
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            //progressBar.Visible = false;                                  // Hide progress bar
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        /* Our own functions */
        private Color[,] DetectObjects(Color[,] Image)
        {
            return null;
        }

        #region Max, Substract
        private Color[,] Max(Color[,] Image1, Color[,] Image2)
        {
            if (Image1.GetLength(0) != Image2.GetLength(0) || Image1.GetLength(1) != Image2.GetLength(1))
                throw new Exception("Make sure both images are of equal size");

            Color[,] result = new Color[Image1.GetLength(0), Image1.GetLength(1)];
            for (int x = 0; x < Image1.GetLength(0); x++)
            {
                for (int y = 0; y < Image1.GetLength(1); y++)
                {
                    int color = Math.Max(Image1[x, y].R, Image2[x, y].R);
                    result[x, y] = Color.FromArgb(color, color, color);
                }
            }

            return result;
        }

        private Color[,] Substract(Color[,] Image1, Color[,] Image2)
        {
            if (Image1.GetLength(0) != Image2.GetLength(0) || Image1.GetLength(1) != Image2.GetLength(1))
                throw new Exception("Make sure both images are of equal size");

            Color[,] result = new Color[Image1.GetLength(0), Image1.GetLength(1)];
            for (int x = 0; x < Image1.GetLength(0); x++)
            {
                for (int y = 0; y < Image1.GetLength(1); y++)
                {
                    int color = Math.Max(0, Image1[x, y].R - Image2[x, y].R);
                    result[x, y] = Color.FromArgb(color, color, color);
                }
            }

            return result;
        }
        #endregion

        #region Blur
        private Color[,] MakeGrey(Color[,] Image)
        {
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    int grey = (Image[x, y].R + Image[x, y].B + Image[x, y].G) / 3;
                    Image[x, y] = Color.FromArgb(grey, grey, grey);
                }
            }

            return Image;
        }
        #endregion

        #region Edge Detection
        private Color[,] Prewitt(Color[,] Image)
        {
            Kernel prewittHorizontal = new Kernel(-1, 0, 1, -1, 0, 1, -1, 0, 1);
            Color[,] Gx = prewittHorizontal.Apply(Image);

            Kernel prewittVertical = new Kernel(-1, -1, -1, 0, 0, 0, 1, 1, 1);
            Color[,] Gy = prewittVertical.Apply(Image);

            Color[,] result = Gx;
            for (int x = 0; x < result.GetLength(0); x++)
            {
                for (int y = 0; y < result.GetLength(1); y++)
                {
                    int colorIntensity = Math.Max(result[x, y].R, Gy[x, y].R);
                    result[x, y] = Color.FromArgb(colorIntensity, colorIntensity, colorIntensity);
                }
            }

            return result;
        }
        #endregion

        #region Threshold
        //finds the threshold by looking at minimum between the 2 highest peaks
        private Color[,] Threshold(Color[,] Image, bool inverse = false, int threshold = 0)
        {
            Color[,] result = new Color[Image.GetLength(0), Image.GetLength(1)];

            if (threshold == 0)
                threshold = FindThreshold(Image);

            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    Color pixelColor = Image[x, y];
                    int grey = (pixelColor.R + pixelColor.B + pixelColor.G) / 3;
                    Color updatedColor = (grey < threshold) ? Color.FromArgb(255, 255, 255) : Color.FromArgb(0, 0, 0);
                    if (inverse) updatedColor = Color.FromArgb(255 - updatedColor.R, 255 - updatedColor.G, 255 - updatedColor.B);
                    result[x, y] = updatedColor;
                }
            }

            return result;
        }

        private int FindThreshold(Color[,] Image)
        {
            //fills the array with the amount of pixels per grey value (histogram)
            int[] frequencies = new int[256];
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    frequencies[(Image[x, y].R + Image[x, y].B + Image[x, y].G) / 3]++;
                }
            }

            return findPeaks(frequencies);
        }

        //searches for all maxima (peaks)
        private int findPeaks(int[] freqs)
        {
            List<Tuple<int, int>> peaks = new List<Tuple<int, int>>();
            for (int i = 2; i < freqs.Length - 2; i++)
            {
                //checks if its a top and if it is in the range of the array
                if (freqs[i - 1] < freqs[i] && (i + 1) < 256 && freqs[i + 1] < freqs[i] && 
                    freqs[i - 2] < freqs[i-1] && freqs[i + 2] < freqs[i+1])
                {
                    peaks.Add(new Tuple<int, int>(i, freqs[i]));
                    i += 35;        //To skip peaks too close to eachother
                }
            }

            return minimumThreshold(peaks, freqs);
        }

        //finds the right threshold   
        private int minimumThreshold(List<Tuple<int, int>> peaks, int[] freqs)
        {
            //finds the 2 highest peaks
            Tuple<int, int> firstTop = new Tuple<int, int>(0, 0), secondTop = new Tuple<int, int>(0, 0);
            for (int i = 0; i < peaks.Count; i++)
            {
                if (peaks[i].Item2 > firstTop.Item2)
                {
                    secondTop = firstTop;
                    firstTop = peaks[i];
                }
                else if (peaks[i].Item2 > secondTop.Item2)
                    secondTop = peaks[i];
            }

            //finds the minimum between the 2 highest peaks
            Tuple<int, int> minimum = firstTop;
            int lowestIndex = Math.Min(firstTop.Item1, secondTop.Item1);
            int highestIndex = Math.Max(firstTop.Item1, secondTop.Item1);
            for (int i = lowestIndex; i < highestIndex; i++)
            {
                if (freqs[i] < minimum.Item2)
                    minimum = new Tuple<int, int>(i, freqs[i]);
            }

            return minimum.Item1;
        }
        #endregion

        #region Erode, Dilate
        private Color[,] Erode(Color[,] Image)
        {
            Color[,] updatedImage = new Color[Image.GetLength(0), Image.GetLength(1)];

            for (int x = 1; x < Image.GetLength(0) - 1; x++)         //Ignore the sides of the image
            {
                for (int y = 1; y < Image.GetLength(1) - 1; y++)
                {
                    Color[] POI = new Color[] { Image[x - 1, y - 1], Image[x, y - 1], Image[x + 1, y - 1],
                                                Image[x - 1, y], Image[x, y], Image[x + 1, y],
                                                Image[x - 1, y + 1], Image[x, y + 1], Image[x + 1, y + 1]}; // Pixels of Interest

                    updatedImage[x, y] = newColorErode(POI); // If one of the pixels in POI is the background color, make this pixel the background color as well
                }
            }

            return updatedImage;
        }

        private Color[,] Dilate(Color[,] Image)
        {
            Color[,] updatedImage = new Color[Image.GetLength(0), Image.GetLength(1)];

            for (int x = 2; x < Image.GetLength(0) - 2; x++)         //Ignore the sides of the image
            {
                for (int y = 2; y < Image.GetLength(1) - 2; y++)
                {
                    Color[] POI = new Color[] {Image[x - 1, y - 2], Image[x, y - 2], Image[x + 1, y - 2], Image[x - 2, y - 2], Image[x + 2, y - 2],
                                                Image[x - 1, y - 1], Image[x, y - 1], Image[x + 1, y - 1], Image[x-2,y-1], Image[x+2,y-1],
                                                Image[x - 1, y], Image[x, y], Image[x + 1, y], Image[x - 2, y], Image[x + 2, y],
                                                Image[x - 1, y + 1], Image[x, y + 1], Image[x + 1, y + 1], Image[x - 2, y + 1], Image[x + 2, y + 1],
                                                Image[x - 1, y + 2], Image[x, y + 2], Image[x + 1, y + 2], Image[x - 2, y + 2], Image[x + 2, y + 2]
                                                }; // Pixels of Interest

                    updatedImage[x, y] = newColorDilate(POI); // If one of the pixels in POI is the foreground color, make this pixel the foreground color as well
                }
            }

            return updatedImage;
        }

        private Color newColorDilate(Color[] POI)
        {
            int max = 0;

            for (int i = 0; i < POI.Length; i++)
            {
                if (POI[i].R > max)
                    max = POI[i].R;
            }

            return Color.FromArgb(max, max, max);
        }

        private Color newColorErode(Color[] POI)
        {
            int min = 255;

            for (int i = 0; i < POI.Length; i++)
            {
                if (POI[i].R < min)
                    min = POI[i].R;
            }

            return Color.FromArgb(min, min, min);
        }
    

        private Color [,] Close(Color[,] Image)
        {
            return Erode(Dilate(Image));
        }

        private Color[,] Open(Color[,] Image)
        {
            return Dilate(Erode(Image));
        }
        #endregion

        #region Labeling 
        private int [,] labeling(int [,] Image)
        {
            Dictionary<int, List<int>> diffLabels = new Dictionary<int, List <int>> ();
            int [,] labelImage = new int [Image.GetLength(0), Image.GetLength(1)];

            //set whole labelImage to zero
            for (int i = 0; i < Image.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < Image.GetLength(1) - 1; j++)
                {
                    labelImage[i, j] = 0;
                }
            }

            int label = 0;
            List <int> neighbors = new List <int> ();

            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if(Image[x,y] == 255)
                    {
                        neighbors.Clear();

                        //find labels of neighbors left, left up, up and right up
                        if (x > 0 && labelImage[x - 1, y] > 0)
                            neighbors.Add(labelImage[x - 1, y]);
                        if (x > 0 && y > 0 && labelImage[x - 1, y - 1] > 0)
                            neighbors.Add(labelImage[x - 1, y - 1]);
                        if (y > 0 && labelImage[x, y - 1] > 0)
                            neighbors.Add(labelImage[x, y - 1]);
                        if (y > 0 && x < Image.GetLength(0) && labelImage[x + 1, y - 1] > 0)
                            neighbors.Add(labelImage[x + 1, y - 1]);

                        //if there are no neighbors, give the pixel a new label and add it to the diffTabel
                        if(neighbors.Count == 0)
                        {
                            label++;
                            List<int> labelList = new List<int>();
                            labelList.Add(label);
                            diffLabels.Add(label,labelList);
                            labelImage[x, y] = label;
                        }

                        else
                        {
                            //take over the lowest label of all the neighbors
                            labelImage[x,y] = neighbors.Min();

                            //add the current label to the lists of the neighboring labels
                            foreach (int neighborLabel in neighbors)
                            {
                                if (!diffLabels[neighborLabel].Contains(labelImage[x,y]))
                                    diffLabels[neighborLabel].Add(labelImage[x, y]);
                            }
                        }
                    }
                }
            }

            labelImage = secondPass(Image, labelImage, diffLabels);
           
            return labelImage;
        }

        //second round to make sure all adjoining shapes are of the same lowest label
        private int [,] secondPass(int [,] Image, int [,] labelImage, Dictionary<int,List <int>> diffLabels)
        {
            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (Image[x, y] == 255)
                        labelImage[x, y] = findLowestLabel(diffLabels, labelImage[x, y]);
                }
            }

            return labelImage;
        }

        //function to look for the adjoining figure with the lowest label
        private int findLowestLabel (Dictionary<int, List<int>> table,int label)
        {
            int newLabel = table[label].Min();

            if (newLabel == table[newLabel].Min())
                return newLabel;

            else
                newLabel = findLowestLabel(table, table[newLabel].Min());

            return newLabel;
        }

        //makes a list of all shapes that intersect with the border
        private List<int> findBorderShapes(int [,] Image)
        {
            List<int> borderShapes = new List<int>();

            int z = 3;

            //check which shapes intersect with the vertical borders and with how much pixels
            for (int y = 3; y < Image.GetLength(1) - 3; y++)
            {
                if (Image[z, y] > 0 && !borderShapes.Contains(Image[z, y]))
                    borderShapes.Add(Image[z, y]);

                if (y == Image.GetLength(1) - 4 && z != Image.GetLength(0) - 4)
                {
                    z = Image.GetLength(0) - 4;
                    y = 2;
                }
            }

            int t = 3;

            //check which shapes intersect with the horizontal borders and with how much pixels
            for (int x = 3; x < Image.GetLength(0) - 3; x++)
            {
                if (Image[x, t] > 0 && !borderShapes.Contains(Image[z, t]))
                        borderShapes.Add(Image[x,t]);

                if (x == Image.GetLength(0) - 4 && t != Image.GetLength(1) - 4)
                {
                    t = Image.GetLength(1) - 4;
                    x = 2;
                }
            }

            return borderShapes;
        }

        //removes the shapes that intersect with the borders
        private int [,] removeBorderShapes(int [,] Image)
        {
            List<int> removeShapes = findBorderShapes(Image);

            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (removeShapes.Contains(Image[x,y]))
                        Image[x,y] = 0;                  
                }
            }

            return Image;
        }


        //give all shapes in the image a different grey color
        private Color[,] colorLabeling(int [,] Image)
        {
            Color[,] updatedImage = new Color[Image.GetLength(0), Image.GetLength(1)];
            Dictionary<int, int> greyValues = new Dictionary<int, int>();
            greyValues = findGreyvalueLabel(Image);

            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (Image[x, y] != 0)
                    {
                        int greyColor = greyValues[Image[x, y]];
                        updatedImage[x, y] = Color.FromArgb(greyColor,greyColor,greyColor);
                    }
                    else
                        updatedImage[x, y] = Color.FromArgb(0, 0, 0);
                }
            }

            return updatedImage;
        }

        //make a dictionary with the labels and the grey colors that belong to that labels
        private Dictionary<int, int> findGreyvalueLabel(int [,] Image)
        {
            List<int> labels = new List<int>();

            //make a list of 
            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (Image[x,y] != 0 && !labels.Contains(Image[x,y]))
                        labels.Add(Image[x, y]);

                }
            }

            int grey = 0;
            //determine steps for the different grey values (the shapes colors start from 55 to make sure they stand out from the background)
            if (labels.Count > 0)
                grey = 200/labels.Count;

            //fill the dictionary with the value combined with the right grey color
            Dictionary<int, int> greyValues = new Dictionary<int, int>();
            int totalGrey = 55 + grey;

            foreach (int label in labels)
            {
                greyValues.Add(label, totalGrey);
                totalGrey += grey;
            }

            return greyValues;
        }
        #endregion

        #region Area Functions
        private int[,] removeAfterElongation(int[,] Image)
        {
            List<int> goodShapes = new List<int>();
            List<int> badShapes = new List<int>();

            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (Image[x, y] > 0)
                    {
                        //shape is already tested and must be removed
                        if (badShapes.Contains(Image[x, y]))
                            Image[x, y] = 0;
                        //shape is not yet tested
                        else if (!goodShapes.Contains(Image[x, y]))
                        {
                            //shape is good
                            if (calculateElongation(Image, Image[x, y]))
                                goodShapes.Add(Image[x, y]);
                            //shape is bad
                            else
                            {
                                badShapes.Add(Image[x, y]);
                                Image[x, y] = 0;
                            }
                        }
                    }
                }
            }

            return Image;
        }

        //returns true if the shape passes, false if it is to be removed
        private bool calculateElongation(int [,] Image, int label)
        {
            double xMax = 0, yMax = 0, xMin = 512, yMin = 512;

            // find min and max values for x and y
            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (Image[x, y] == label)
                    {
                        if (x > xMax)
                            xMax = x;
                        if (x < xMin)
                            xMin = x;
                        if (y > yMax)
                            yMax = y;
                        if (y < yMin)
                            yMin = y;
                    }
                }
            }

            double xLength = xMax - xMin;
            double yLength = yMax - yMin;

            double elongation = xLength/ yLength;

            if(elongation > 0.85 || elongation < 0.55)
                return false;

            return true;
        }

        #endregion

        #region Switch between int [,] and Color [,]
        /* These function were made, becuase doing calculations on int[,] has lower memory cost than doing calculations on Color[,] */
        private int[,] colorToInt(Color[,] Image)
        {
            int[,] updatedImage = new int[Image.GetLength(0), Image.GetLength(1)];


            for (int i = 0; i < Image.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < Image.GetLength(1) - 1; j++)
                {
                    updatedImage[i, j] = Image[i, j].R;
                }
            }

            return updatedImage;
        }

        private Color [,] intToColor(int [,] Image)
        {
            Color[,] updatedImage = new Color[Image.GetLength(0), Image.GetLength(1)];

            for (int y = 0; y < Image.GetLength(1); y++)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                {
                    if (Image[x, y] == 255)
                        updatedImage[x, y] = Color.FromArgb(255, 255, 255);
                    else
                        updatedImage[x, y] = Color.FromArgb(0, 0, 0);

                }
            }

            return updatedImage;
        }
        #endregion
    }
}
