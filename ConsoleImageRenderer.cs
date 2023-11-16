using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using static System.Formats.Asn1.AsnWriter;
using System.Diagnostics;
using System.Security;

public class OutOfConsoleWidth: Exception
{
    public OutOfConsoleWidth(string message)
        : base(message) { }
}




class ConsoleImageRenderer
{
    private StringBuilder currentLine = new StringBuilder();
    public int maxWidth{ get; set; }
    public int maxHeight { get; set; }
    public List<(int, int, int)[,]> Layers = new List<(int, int, int)[,]>();
    public bool Colorful { get; set; } = false;
    public ConsoleImageRenderer(int Width = 944, int Height = 354, bool colorful = false)
    {
        this.maxWidth = Width;
        this.maxHeight = Height;
        this.Colorful = colorful;
    }
    private void Print(int red, int green, int blue)
    {
        if (Colorful)
        {
            currentLine.Append($"\x1B[48;2;{red};{green};{blue}m  ");
        }
        else
        {
            Console.ResetColor();
            char[] symbols = @"$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\|()1{}[]?-_+~<>i!lI;:,""^`'. ".ToCharArray();
            Array.Reverse(symbols);
            int brigthness = red + green + blue;
            currentLine.Append(new string(symbols[brigthness / symbols.Length], 2));
            //Console.Write(new string(symbols[brigthness/ symbols.Length],2));
            
        }
    }
    public (int, int, int)[,] GeneratePlainBackground(int red,int green, int blue)
    {
        (int, int, int)[,] background = new (int, int, int) [maxHeight, maxWidth];
        for (int consoleY = 0; consoleY < maxHeight; consoleY++)
        {
            for (int consoleX = 0; consoleX < maxWidth; consoleX++)
            {
                background[consoleY, consoleX] = (red, green, blue);
            }
        }
        return background;
    }
    public void RenderAllLayers()
    {
        Console.SetCursorPosition(0, 0);

        // reversing Layers while rendering to take the higher layer on each pixel
        Layers.Reverse();
        for (int consoleY = 0; consoleY < maxHeight; consoleY++)
        {
            for (int consoleX = 0; consoleX < maxWidth; consoleX++)
            {
                foreach((int, int, int)[,] Layer in Layers)
                {
                    (int, int, int) curPixel = Layer[consoleY, consoleX];
                    if (curPixel != (-1, -1, -1))
                    {
                        //Console.Write($"\x1B[48;2;{curPixel.Item1};{curPixel.Item2};{curPixel.Item3}m  ");
                        Print(curPixel.Item1, curPixel.Item2, curPixel.Item3);
                        //Console.Out.WriteAsync($"\x1B[48;2;{curPixel.Item1};{curPixel.Item2};{curPixel.Item3}m  ");
                        //Debug.Write($"\x1B[48;2;{curPixel.Item1};{curPixel.Item2};{curPixel.Item3}m  ");
                        break;
                    }
                }
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(currentLine.ToString());
            
            //Console.WriteLine();
            currentLine.Clear();
        }
        // returning layers to rigth position
        Console.ResetColor();
        Layers.Reverse();
    }
    public void AddNewLayer((int, int, int)[,] Layer)
    {
        Layers.Add(Layer);
    }
    private (int, int, int)[,] GetScaledPhoto(Bitmap image, int scale)
    {
        //scale 2
        int photoHeigth = image.Height;
        int photoWidth = image.Width;

        (int, int, int)[,] currentLayer = new (int, int, int)[photoHeigth / scale, photoWidth / scale];
        for (int yOnPhoto = 0; yOnPhoto < photoHeigth/scale; yOnPhoto ++)
        {
            for(int xOnPhoto = 0; xOnPhoto < photoWidth / scale; xOnPhoto ++)
            {
                if((yOnPhoto*scale)+scale>photoHeigth|| (xOnPhoto * scale) + scale > photoWidth)
                {
                    continue;
                }
                int red = 0;
                int green = 0;
                int blue = 0;
                for (int x = 0; x < scale; x++)
                {
                    for (int y = 0; y < scale; y++)
                    {
                        if ((xOnPhoto * scale) + x < photoWidth && (yOnPhoto*scale) + y < photoHeigth)
                        {
                            Color col = image.GetPixel((xOnPhoto * scale) + x, (yOnPhoto * scale) + y);
                            red += col.R;
                            green += col.G;
                            blue += col.B;
                        }

                    }
                }
                red /= scale * scale;
                green /= scale* scale;
                blue /= scale* scale;
                //if (red > 255 || green > 255 || blue > 255)
                //{
                //    Console.WriteLine("ighergergerguhru");
                //}
                //Console.Write(photoHeigth + "  :  ");
                //Console.WriteLine(xOnPhoto / scale);
                
                currentLayer[yOnPhoto, xOnPhoto] = (red, green, blue);
            }
        }
        
        return currentLayer;
    }
    public (int, int, int)[,] GenerateLayerByPhoto(  
                                            string path,
                                            int scale = 1,
                                            int offsetX = 0,
                                            int offsetY = 0)
    {
        path = path.Replace("\"", "");
        Bitmap image = new Bitmap(path);
        (int, int, int)[,] photo = GetScaledPhoto(image, scale);

        (int, int, int)[,] currentLayer = new (int, int, int)[maxHeight, maxWidth];
        for (int consoleY = 0; consoleY < maxHeight; consoleY ++)
        {
            for (int consoleX = 0; consoleX < maxWidth; consoleX ++)
            {
                
                // перевірка чи ми в потрібному місці
                if(consoleY > offsetY && consoleX > offsetX &&
                    consoleY< photo.GetLength(0)+offsetY && consoleX < photo.GetLength(1)+offsetX)
                {
                    // current pixel on photo
                    (int, int, int) currentPixel = photo[consoleY - offsetY, consoleX - offsetX];

                    currentLayer[consoleY, consoleX] = 
                        (currentPixel.Item1, currentPixel.Item2, currentPixel.Item3);
                }
                else
                {
                    // -1 це прозорий кольор
                    currentLayer[consoleY, consoleX] = (-1, -1, -1);
                }
            }
        }
        return currentLayer;
    }
}

