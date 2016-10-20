using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace INFOIBV
{
    class Kernel
    {
        private float[,] values; //The entire matrix that represents the kernel

        public float[] TopRow { get { return new float[3]{ values[0, 0], values[0, 1], values[0, 2] }; } } //Returns the top row of values
        public float[] CenterRow { get { return new float[3] { values[1, 0], values[1, 1], values[1, 2] }; } } //Returns the top row of values
        public float[] BottomRow { get { return new float[3] { values[2, 0], values[2, 1], values[2, 2] }; } } //Returns the top row of values

        /// <summary>
        /// Creates a 3x3 Kernel.
        /// The default Kernel is the Identity Kernel.
        /// </summary>
        public Kernel(float UpperLeft = 0, float UpperMiddle = 0, float UpperRight = 0,
                        float CenterLeft = 0, float CenterMiddle = 1, float CenterRight = 0,
                        float BottomLeft = 0, float BottomMiddle = 0, float BottomRight = 0)
        {
            values = new float[3, 3];

            /* Top Row */
            values[0, 0] = UpperLeft;
            values[0, 1] = UpperMiddle;
            values[0, 2] = UpperRight;

            /*Middle Row */
            values[1, 0] = CenterLeft;
            values[1, 1] = CenterMiddle;
            values[1, 2] = CenterRight;

            /*Bottom Row */
            values[2, 0] = BottomLeft;
            values[2, 1] = BottomMiddle;
            values[2, 2] = BottomRight;
        }

        /// <summary>
        /// Multiplies the entire kernel with a number.
        /// </summary>
        public void Multiply(float number)
        {
            for (int x = 0; x < values.GetLength(0); x++)
                for (int y = 0; y < values.GetLength(1); y++)
                    values[x, y] *= number;
        }

        public Color[,] Apply(Color[,] Image)
        {
            Color[,] updatedImage = new Color[Image.GetLength(0), Image.GetLength(1)];
            for(int x = 1; x < Image.GetLength(0) - 1; x++)         //Ignore the sides of the image
            {
                for(int y = 1; y < Image.GetLength(1) - 1; y++)
                {
                    //TODO
                }
            }

            return null;
        }
    }
}
