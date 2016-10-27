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
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int grey = (pixelColor.R + pixelColor.B + pixelColor.G) / 3;
                    Color updatedColor = (grey < 196 || grey > 208) ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255); // Threshold Image
                    Image[x, y] = updatedColor;
                    //Image[x, y] = (pixelColor.B < 222) ? Color.Black : Color.White;                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }

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
            progressBar.Visible = false;                                    // Hide progress bar
        }
        
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        /* Our own functions */
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
