using System;
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
                    pictureBox1.Image = (Image) InputImage;                 // Display input image
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)

            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

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
            // example: create a negative image
            int threshold = FindThreshold(Image);
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int grey = (pixelColor.R + pixelColor.B + pixelColor.G) / 3;        //turn into grey pixel
                    //float Hue = pixelColor.GetHue();
                    //Color updatedColor = (grey < threshold) ? Color.FromArgb(255, 255, 255) : Color.FromArgb(0, 0, 0); // Threshold Image
                    Color updatedColor = Color.FromArgb(grey, grey, grey);
                    Image[x, y] = updatedColor;
                    //Image[x, y] = (pixelColor.B < 222) ? Color.Black : Color.White;                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }

            Image = Prewitt(Image);
            
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
            progressBar.Visible = false;                                    // Hide progress bar
        }
        
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        /* 
            Our own functions 
        */
        private Color[,] DetectObjects(Color[,] Image)
        {
            return null;
        }

        private Color[,] Prewitt (Color[,] Image)
        {
            Kernel prewittHorizontal = new Kernel(-1, 0, 1, -1, 0, 1, -1, 0, 1);
            Color[,] Gx = prewittHorizontal.Apply(Image);

            Kernel prewittVertical = new Kernel(-1, -1, -1, 0, 0, 0, 1, 1, 1);
            Color[,] Gy = prewittVertical.Apply(Image);

            return Gy;
        }

        // ========== THRESHOLDING ==========
        //finds the threshold by looking at minimum between the 2 highest peaks
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
            for (int i = 1; i < freqs.Length; i++)
            {
                //checks if its a top and if it is in the range of the array
                if (freqs[i - 1] < freqs[i] && (i + 1) < 256 && freqs[i + 1] < freqs[i])
                {
                    peaks.Add(new Tuple<int, int>(i, freqs[i]));
                    i += 35;        //To skip peaks too near to eachother
                }
            }

            return minimumThreshold(peaks, freqs);
        }

            //finds the right threshold   
            private int minimumThreshold(List<Tuple<int,int>> peaks, int[] freqs)
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
            for(int i = lowestIndex; i < highestIndex; i++)
            {
                if (freqs[i] < minimum.Item2)
                    minimum = new Tuple<int, int>(i, freqs[i]);
            }

            return minimum.Item1;
        }

        // ========== ERODE, CLOSE ===========
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
                    updatedImage[x, y] = newColor(POI, Color.FromArgb(255, 255, 255, 255));
                }
            }

            return updatedImage;
        }

        private Color[,] Close(Color[,] Image)
        {
            Color[,] updatedImage = new Color[Image.GetLength(0), Image.GetLength(1)];

            for (int x = 1; x < Image.GetLength(0) - 1; x++)         //Ignore the sides of the image
            {
                for (int y = 1; y < Image.GetLength(1) - 1; y++)
                {
                    Color[] POI = new Color[] { Image[x - 1, y - 1], Image[x, y - 1], Image[x + 1, y - 1],
                                                Image[x - 1, y], Image[x, y], Image[x + 1, y],
                                                Image[x - 1, y + 1], Image[x, y + 1], Image[x + 1, y + 1]}; // Pixels of Interest
                    updatedImage[x, y] = newColor(POI, Color.FromArgb(255, 0, 0, 0));
                }
            }

            return updatedImage;
        }

        private Color newColor(Color[] POI, Color output)
        {
            for(int i = 0; i < POI.Length; i++)
            {
                if (POI[i] == output)
                    return output;
            }

            return Color.FromArgb(255 - output.R, 255 - output.G, 255 - output.B);
        }
    }
}
