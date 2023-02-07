using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
//using System.Windows.Forms;

namespace MyFirst
{
    abstract class Filters
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);

        public int[] histogram = new int[256];
        public int[] histogramR = new int[256];
        public int[] histogramG = new int[256];
        public int[] histogramB = new int[256];
        public String ti;
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
        public virtual Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker) 
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            
            for (int i=0; i<sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / sourceImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }
            
            return resultImage;
        }
        
    }

    class InvertFilter : Filters 
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            //throw new NotImplementedException();
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R,
                                                255 - sourceColor.G,
                                                255 - sourceColor.B);
            return resultColor;
        }
    }

    class MatrixFilter : Filters
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel)
        {
            this.kernel = kernel;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for(int l = -radiusY; l<=radiusY; l++)
                for(int k = -radiusX; k<=radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(
                        Clamp((int)resultR, 0, 255),
                        Clamp((int)resultG, 0, 255),
                        Clamp((int)resultB, 0, 255)
                        );
        }
    }

    class BlurFilter : MatrixFilter
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
        }
    }

    class GaussianFilter : MatrixFilter
    {
        public void createGaussianKernel(int radius, float sigma)
        {
            int size = 2 * radius + 1;
            kernel = new float[size, size];
            float norm = 0;
            for(int i=-radius; i<=radius; i++)
                for(int j=-radius; j<=radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;
        }

        public GaussianFilter()
        {
            createGaussianKernel(3, 2);
        }
    }

    class GrayScaleFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            //throw new NotImplementedException();
            Color sourceColor = sourceImage.GetPixel(x, y);
            double intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B;
            Color resultColor = Color.FromArgb((int)(intensity),
                                                (int)(intensity),
                                                (int)(intensity));
            
            return resultColor;
        }
    }

    class SobelFilter : MatrixFilter
    {
        public SobelFilter(bool b)
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            if (b)
            {
                kernel[0, 0] = -1.0f;
                kernel[0, 1] = -2.0f;
                kernel[0, 2] = -1.0f;
                kernel[1, 0] = 0.0f;
                kernel[1, 1] = 0.0f;
                kernel[1, 2] = 0.0f;
                kernel[2, 0] = 1.0f;
                kernel[2, 1] = 2.0f;
                kernel[2, 2] = 1.0f;
            }
            else
            {
                kernel[0, 0] = -1.0f;
                kernel[0, 1] = 0.0f;
                kernel[0, 2] = 1.0f;
                kernel[1, 0] = -2.0f;
                kernel[1, 1] = 0.0f;
                kernel[1, 2] = 2.0f;
                kernel[2, 0] = -1.0f;
                kernel[2, 1] = 0.0f;
                kernel[2, 2] = 1.0f;
            }


        }
    }

    class SharpnessFilter : MatrixFilter
    {
        public SharpnessFilter()
        {
            kernel = new float[3, 3];
            kernel[0, 0] = -1.0f;
            kernel[0, 1] = -1.0f;
            kernel[0, 2] = -1.0f;
            kernel[1, 0] = -1.0f;
            kernel[1, 1] = 9.0f;
            kernel[1, 2] = -1.0f;
            kernel[2, 0] = -1.0f;
            kernel[2, 1] = -1.0f;
            kernel[2, 2] = -1.0f;
        }
    }

    class EmbossFilter : Filters
    {
        float[,] kernel = new float[3, 3];

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0.0f;
            kernel[0, 1] = 1.0f;
            kernel[0, 2] = 0.0f;
            kernel[1, 0] = 1.0f;
            kernel[1, 1] = 0.0f;
            kernel[1, 2] = -1.0f;
            kernel[2, 0] = 0.0f;
            kernel[2, 1] = -1.0f;
            kernel[2, 2] = 0.0f;

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0; 
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                        Clamp((int)((resultIntensity+255)/2), 0, 255),
                        Clamp((int)((resultIntensity + 255) / 2), 0, 255),
                        Clamp((int)((resultIntensity + 255) / 2), 0, 255)
                        );
        }
    }

   class MedianFilter : Filters
    {
        float[,] kernel = new float[3, 3];

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    kernel[i, j] = 1.0f;

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            double resultR = 0;
            double resultG = 0;
            double resultB = 0;
            double[] cor = new double[9];
            int[] corR = new int[9];
            int[] corG = new int[9];
            int[] corB = new int[9];
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    corR[3 * (k + radiusX) + (l + radiusY)] = neighbourColor.R;
                    corG[3 * (k + radiusX) + (l + radiusY)] = neighbourColor.G;
                    corB[3 * (k + radiusX) + (l + radiusY)] = neighbourColor.B;
                    cor[3 * (k+radiusX) + (l+radiusY)] = 0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B;
                }
            for(int i=0;i<9;i++)
                for(int j=0; j<8-i;j++)
                {
                    if (cor[j] < cor[j + 1])
                    {
                        double t = cor[j];
                        cor[j] = cor[j + 1];
                        cor[j + 1] = t;
                    }
                    if(corR[j]<corR[j+1])
                    {
                        int t = corR[j];
                        corR[j] = corR[j + 1];
                        corR[j + 1] = t;
                    }
                    if (corG[j] < corG[j + 1])
                    {
                        int t = corG[j];
                        corG[j] = corG[j + 1];
                        corG[j + 1] = t;
                    }
                    if (corB[j] < corB[j + 1])
                    {
                        int t = corB[j];
                        corB[j] = corB[j + 1];
                        corB[j + 1] = t;
                    }
                }
                    
            resultR = corR[4];
            resultG = corG[4];
            resultB = corB[4];
            return Color.FromArgb(
                        Clamp((int)(resultR), 0, 255),
                        Clamp((int)(resultG), 0, 255),
                        Clamp((int)(resultB), 0, 255)
                        );
        }
    }

    class LinearFilter : Filters
    {
        public void calculateHistogram(Bitmap sourceImage)
        {
            for (int i = 0; i < 256; i++)
            {
                histogramR[i] = 0;
                histogramG[i] = 0;
                histogramB[i] = 0;
                histogram[i] = 0;
            }
                
            for (int i = 0; i < sourceImage.Width; i++)
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    Color sourColor = sourceImage.GetPixel(i, j);
                    int inTensity = Clamp((int)(0.299 * sourColor.R + 0.587 * sourColor.G + 0.114 * sourColor.B), 0, 255);
                    histogram[inTensity] += 1;
                    histogram[sourColor.R] += 1;
                    histogram[sourColor.G] += 1;
                    histogram[sourColor.B] += 1;
                }
        }
        
    protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            int min = 0;
            int max = 0;
            int i = 0;
            bool found = false;
            while(i<256 & !found)
            {
                if (histogram[i] == 0)
                    i++;
                else
                    found = true;
            }
            min = i;
            i = 255;
            found = false;
            while (i >=0  & !found)
            {
                if (histogram[i] == 0)
                    i--;
                else
                    found = true;
            }
            max = i;
            found = false;


            Color sourceColor = sourceImage.GetPixel(x, y);
            int intensity = Clamp((int)(0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B), 0, 255);
            
            if (max > min)
            {
                float f = (float)(intensity - min) / (float)(max - min);
                int newIntensity = (int)(255 * f);
                return Color.FromArgb(newIntensity, newIntensity, newIntensity);
            }
            else
                return sourceColor;
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            calculateHistogram(sourceImage);

           
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / (sourceImage.Width) * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

    }

    class DilationFilter : Filters
    {
        int[,] kernel = new int[3, 3];

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0;
            kernel[0, 1] = 1;
            kernel[0, 2] = 0;
            kernel[1, 0] = 1;
            kernel[1, 1] = 1;
            kernel[1, 2] = 1;
            kernel[2, 0] = 0;
            kernel[2, 1] = 1;
            kernel[2, 2] = 0;

            int[] toSortR = new int[9];
            int[] toSortG = new int[9];
            int[] toSortB = new int[9];

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    //resultIntensity = (float)(0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B);
                    if (kernel[k + radiusX, l + radiusY] > 0)
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.R;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.G;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.B;
                    }
                    else
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = 0;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = 0;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = 0;
                    }


                }

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 8 - i; j++)
                {
                    if (toSortR[j] < toSortR[j + 1])
                    {
                        int t = toSortR[j];
                        toSortR[j] = toSortR[j + 1];
                        toSortR[j + 1] = t;
                    }
                    if (toSortG[j] < toSortG[j + 1])
                    {
                        int t = toSortG[j];
                        toSortG[j] = toSortG[j + 1];
                        toSortG[j + 1] = t;
                    }
                    if (toSortB[j] < toSortB[j + 1])
                    {
                        int t = toSortB[j];
                        toSortB[j] = toSortB[j + 1];
                        toSortB[j + 1] = t;
                    }
                }
            //float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                Clamp((int)(toSortR[0]), 0, 255),
                Clamp((int)(toSortG[0]), 0, 255),
                Clamp((int)(toSortB[0]), 0, 255)
                );
        }
    }

    class ErosionFilter : Filters
    {
        int[,] kernel = new int[3, 3];

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0;
            kernel[0, 1] = 1;
            kernel[0, 2] = 0;
            kernel[1, 0] = 1;
            kernel[1, 1] = 1;
            kernel[1, 2] = 1;
            kernel[2, 0] = 0;
            kernel[2, 1] = 1;
            kernel[2, 2] = 0;

            int[] toSortR = new int[9];
            int[] toSortG = new int[9];
            int[] toSortB = new int[9];

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    //resultIntensity = (float)(0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B);
                    if (kernel[k + radiusX, l + radiusY] > 0)
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.R;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.G;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.B;
                    }
                    else
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = 255;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = 255;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = 255;
                    }


                }

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 8 - i; j++)
                {
                    if (toSortR[j] < toSortR[j + 1])
                    {
                        int t = toSortR[j];
                        toSortR[j] = toSortR[j + 1];
                        toSortR[j + 1] = t;
                    }
                    if (toSortG[j] < toSortG[j + 1])
                    {
                        int t = toSortG[j];
                        toSortG[j] = toSortG[j + 1];
                        toSortG[j + 1] = t;
                    }
                    if (toSortB[j] < toSortB[j + 1])
                    {
                        int t = toSortB[j];
                        toSortB[j] = toSortB[j + 1];
                        toSortB[j + 1] = t;
                    }
                }
            //float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                Clamp((int)(toSortR[8]), 0, 255),
                Clamp((int)(toSortG[8]), 0, 255),
                Clamp((int)(toSortB[8]), 0, 255)
                );
        }
    }

    class OpeningFilter : Filters
    {
        int[,] kernel = new int[3, 3];

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0;
            kernel[0, 1] = 1;
            kernel[0, 2] = 0;
            kernel[1, 0] = 1;
            kernel[1, 1] = 1;
            kernel[1, 2] = 1;
            kernel[2, 0] = 0;
            kernel[2, 1] = 1;
            kernel[2, 2] = 0;

            int[] toSortR = new int[9];
            int[] toSortG = new int[9];
            int[] toSortB = new int[9];

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    //resultIntensity = (float)(0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B);
                    if (kernel[k + radiusX, l + radiusY] > 0)
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.R;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.G;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.B;
                    }
                    else
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = 255;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = 255;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = 255;
                    }


                }

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 8 - i; j++)
                {
                    if (toSortR[j] < toSortR[j + 1])
                    {
                        int t = toSortR[j];
                        toSortR[j] = toSortR[j + 1];
                        toSortR[j + 1] = t;
                    }
                    if (toSortG[j] < toSortG[j + 1])
                    {
                        int t = toSortG[j];
                        toSortG[j] = toSortG[j + 1];
                        toSortG[j + 1] = t;
                    }
                    if (toSortB[j] < toSortB[j + 1])
                    {
                        int t = toSortB[j];
                        toSortB[j] = toSortB[j + 1];
                        toSortB[j + 1] = t;
                    }
                }
            //float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                Clamp((int)(toSortR[8]), 0, 255),
                Clamp((int)(toSortG[8]), 0, 255),
                Clamp((int)(toSortB[8]), 0, 255)
                );
        }

        protected Color calculateAgainNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0;
            kernel[0, 1] = 1;
            kernel[0, 2] = 0;
            kernel[1, 0] = 1;
            kernel[1, 1] = 1;
            kernel[1, 2] = 1;
            kernel[2, 0] = 0;
            kernel[2, 1] = 1;
            kernel[2, 2] = 0;

            int[] toSortR = new int[9];
            int[] toSortG = new int[9];
            int[] toSortB = new int[9];

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    //resultIntensity = (float)(0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B);
                    if (kernel[k + radiusX, l + radiusY] > 0)
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.R;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.G;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.B;
                    }
                    else
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = 0;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = 0;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = 0;
                    }


                }

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 8 - i; j++)
                {
                    if (toSortR[j] < toSortR[j + 1])
                    {
                        int t = toSortR[j];
                        toSortR[j] = toSortR[j + 1];
                        toSortR[j + 1] = t;
                    }
                    if (toSortG[j] < toSortG[j + 1])
                    {
                        int t = toSortG[j];
                        toSortG[j] = toSortG[j + 1];
                        toSortG[j + 1] = t;
                    }
                    if (toSortB[j] < toSortB[j + 1])
                    {
                        int t = toSortB[j];
                        toSortB[j] = toSortB[j + 1];
                        toSortB[j + 1] = t;
                    }
                }
            //float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                Clamp((int)(toSortR[0]), 0, 255),
                Clamp((int)(toSortG[0]), 0, 255),
                Clamp((int)(toSortB[0]), 0, 255)
                );
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            Bitmap exchangeImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / (sourceImage.Width) * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    exchangeImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / (sourceImage.Width) * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateAgainNewPixelColor(exchangeImage, i, j));
                }
            }

            return resultImage;
        }
    }

    class ClosingFilter : Filters
    {
        int[,] kernel = new int[3, 3];

        protected  Color calculateAgainNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0;
            kernel[0, 1] = 1;
            kernel[0, 2] = 0;
            kernel[1, 0] = 1;
            kernel[1, 1] = 1;
            kernel[1, 2] = 1;
            kernel[2, 0] = 0;
            kernel[2, 1] = 1;
            kernel[2, 2] = 0;

            int[] toSortR = new int[9];
            int[] toSortG = new int[9];
            int[] toSortB = new int[9];

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    //resultIntensity = (float)(0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B);
                    if (kernel[k + radiusX, l + radiusY] > 0)
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.R;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.G;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.B;
                    }
                    else
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = 255;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = 255;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = 255;
                    }


                }

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 8 - i; j++)
                {
                    if (toSortR[j] < toSortR[j + 1])
                    {
                        int t = toSortR[j];
                        toSortR[j] = toSortR[j + 1];
                        toSortR[j + 1] = t;
                    }
                    if (toSortG[j] < toSortG[j + 1])
                    {
                        int t = toSortG[j];
                        toSortG[j] = toSortG[j + 1];
                        toSortG[j + 1] = t;
                    }
                    if (toSortB[j] < toSortB[j + 1])
                    {
                        int t = toSortB[j];
                        toSortB[j] = toSortB[j + 1];
                        toSortB[j + 1] = t;
                    }
                }
            //float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                Clamp((int)(toSortR[8]), 0, 255),
                Clamp((int)(toSortG[8]), 0, 255),
                Clamp((int)(toSortB[8]), 0, 255)
                );
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // throw new NotImplementedException();

            kernel[0, 0] = 0;
            kernel[0, 1] = 1;
            kernel[0, 2] = 0;
            kernel[1, 0] = 1;
            kernel[1, 1] = 1;
            kernel[1, 2] = 1;
            kernel[2, 0] = 0;
            kernel[2, 1] = 1;
            kernel[2, 2] = 0;

            int[] toSortR = new int[9];
            int[] toSortG = new int[9];
            int[] toSortB = new int[9];

            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    //resultIntensity = (float)(0.299 * neighbourColor.R + 0.587 * neighbourColor.G + 0.114 * neighbourColor.B);
                    if (kernel[k + radiusX, l + radiusY] > 0)
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.R;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.G;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = kernel[k + radiusX, l + radiusY] * neighbourColor.B;
                    }
                    else
                    {
                        toSortR[3 * (k + radiusX) + (l + radiusY)] = 0;
                        toSortG[3 * (k + radiusX) + (l + radiusY)] = 0;
                        toSortB[3 * (k + radiusX) + (l + radiusY)] = 0;
                    }


                }

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 8 - i; j++)
                {
                    if (toSortR[j] < toSortR[j + 1])
                    {
                        int t = toSortR[j];
                        toSortR[j] = toSortR[j + 1];
                        toSortR[j + 1] = t;
                    }
                    if (toSortG[j] < toSortG[j + 1])
                    {
                        int t = toSortG[j];
                        toSortG[j] = toSortG[j + 1];
                        toSortG[j + 1] = t;
                    }
                    if (toSortB[j] < toSortB[j + 1])
                    {
                        int t = toSortB[j];
                        toSortB[j] = toSortB[j + 1];
                        toSortB[j + 1] = t;
                    }
                }
            //float resultIntensity = (float)(0.299 * resultR + 0.587 * resultG + 0.114 * resultB);
            return Color.FromArgb(
                Clamp((int)(toSortR[0]), 0, 255),
                Clamp((int)(toSortG[0]), 0, 255),
                Clamp((int)(toSortB[0]), 0, 255)
                );
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            

            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            Bitmap exchangeImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / (sourceImage.Width) * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    exchangeImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / (sourceImage.Width) * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateAgainNewPixelColor(exchangeImage, i, j));
                }
            }


            return resultImage;
        }

        
    }

    class GlassFilter : Filters
    {
        Random rnd = new Random();
        public double rand()
        {
            return rnd.NextDouble();
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            //throw new NotImplementedException();
            
            int k = Clamp(x + (int)(10 * (rand() - 0.5)), 0, sourceImage.Width - 1);
            int l = Clamp(y + (int)(10 * (rand() - 0.5)), 0, sourceImage.Height - 1);
            Color resultColor = sourceImage.GetPixel(k, l);

            return resultColor;
        }
    }

    class Wave1Filter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            //throw new NotImplementedException();
            int k = x + (int)(20 * Math.Sin(2 * Math.PI * x / 90));
            Color resultColor = Color.FromArgb(0, 0, 0);
            if (k >= 0 & k <= sourceImage.Width - 1)
                resultColor = sourceImage.GetPixel(k, y);

            return resultColor;
        }
    }

    class Wave2Filter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            //throw new NotImplementedException();
            int k = x + (int)(20 * Math.Sin(2 * Math.PI * y / 90));
            Color resultColor = Color.FromArgb(0, 0, 0);
            if (k >= 0 & k <= sourceImage.Width - 1)
                resultColor = sourceImage.GetPixel(k, y);

            return resultColor;
        }
    }

    class GrayWorldFilter : Filters
    {
        float AvR;
        float AvG;
        float AvB;
        float Average;
        public void calculateAverage(Bitmap sourceImage)
        {
            AvR = 0;
            AvG = 0;
            AvB = 0;
            Average = 0;
            
            for (int i = 0; i < sourceImage.Width; i++)
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    Color sourColor = sourceImage.GetPixel(i, j);
                    AvR += (float)sourColor.R / (sourceImage.Width * sourceImage.Height);
                    AvG += (float)sourColor.G / (sourceImage.Width * sourceImage.Height);
                    AvB += (float)sourColor.B / (sourceImage.Width * sourceImage.Height);
                }
            Average = (AvR + AvG + AvB) / 3;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            Color sourceColor = sourceImage.GetPixel(x, y);
            

            if (AvR > 0 & AvG > 0 & AvB > 0)
            {
                float newR = sourceColor.R * Average / AvR;
                float newG = sourceColor.G * Average / AvG;
                float newB = sourceColor.B * Average / AvB;
                return Color.FromArgb(Clamp((int)newR, 0, 255),
                                        Clamp((int)newG, 0, 255),
                                        Clamp((int)newB, 0, 255));
            }
            else
                return Color.FromArgb(50, 50, 50);
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            calculateAverage(sourceImage);


            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / (sourceImage.Width) * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

    }


}

