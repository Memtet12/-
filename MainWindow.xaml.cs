using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;
using Brush = System.Drawing.Brush;

namespace SandpileFractalWPF
{
    public partial class MainWindow : Window
    {
        private SandpileFractal sandpile;
        private DispatcherTimer timer;
        private int size = 400;
        private int scaleFactor = 2;

        public MainWindow()
        {
            InitializeComponent();
            sandpile = new SandpileFractal(size);

            // Настройка таймера для обновления изображения
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            sandpile.AddSand(size / 2, size / 2, 1000);
            UpdateImage();
        }

        private void UpdateImage()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                sandpile.SaveImage(memory, scaleFactor);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                SandpileImage.Source = bitmapImage;
            }
        }
    }

    public class SandpileFractal
    {
        private int[,] grid;
        private int size;

        public SandpileFractal(int size)
        {
            this.size = size;
            grid = new int[size, size];
        }

        public void AddSand(int x, int y, int amount)
        {
            grid[x, y] += amount;
            Topple();
        }

        private void Topple()
        {
            bool toppled = false;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (grid[i, j] >= 4)
                    {
                        int excess = grid[i, j] / 4;
                        grid[i, j] %= 4;
                        if (i > 0) grid[i - 1, j] += excess;
                        if (i < size - 1) grid[i + 1, j] += excess;
                        if (j > 0) grid[i, j - 1] += excess;
                        if (j < size - 1) grid[i, j + 1] += excess;
                        toppled = true;
                    }
                }
            }

            if (toppled)
            {
                Topple(); // Рекурсивный вызов
            }
        }

        public void SaveImage(Stream stream, int scaleFactor = 1)
        {
            int scaledSize = size * scaleFactor;
            using (Bitmap bitmap = new Bitmap(scaledSize, scaledSize))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            int value = grid[i, j];
                            Color color = GetColor(value);
                            using (Brush brush = new SolidBrush(color))
                            {
                                g.FillRectangle(brush, i * scaleFactor, j * scaleFactor, scaleFactor, scaleFactor);
                            }
                        }
                    }
                }
                bitmap.Save(stream, ImageFormat.Png);
            }
        }

        private Color GetColor(int value)
        {
            switch (value)
            {
                case 0:
                    return Color.FromArgb(240, 230, 140); // Светло-желтый
                case 1:
                    return Color.FromArgb(218, 165, 32);  // Золотой
                case 2:
                    return Color.FromArgb(184, 134, 11);  // Темно-золотой
                case 3:
                    return Color.FromArgb(139, 69, 19);   // Коричневый
                default:
                    return Color.FromArgb(255, 0, 0);     // Красный (для значений, превышающих 3)
            }
        }
    }
}