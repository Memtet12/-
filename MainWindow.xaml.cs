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
        private DispatcherTimer elapsedTimer;
        private int size = 400;
        private int scaleFactor = 2;
        private int sandAmount = 1000;
        private DateTime startTime;
        private TimeSpan totalElapsedTime;

        public MainWindow()
        {
            InitializeComponent();
            sandpile = new SandpileFractal(size);

            // Настройка таймера для обновления изображения
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
            timer.Tick += Timer_Tick;

            // Настройка таймера для отслеживания времени работы
            elapsedTimer = new DispatcherTimer();
            elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            elapsedTimer.Tick += ElapsedTimer_Tick;

            // Настройка начальных значений слайдеров
            SizeSlider.Value = size;
            ScaleFactorSlider.Value = scaleFactor;
            SandAmountSlider.Value = sandAmount;

            // Подписка на события изменения значений слайдеров
            SizeSlider.ValueChanged += (s, e) => UpdateParameters();
            ScaleFactorSlider.ValueChanged += (s, e) => UpdateParameters();
            SandAmountSlider.ValueChanged += (s, e) => UpdateParameters();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            sandpile.AddSand(size / 2, size / 2, sandAmount);
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

        private void UpdateParameters()
        {
            size = (int)SizeSlider.Value;
            scaleFactor = (int)ScaleFactorSlider.Value;
            sandAmount = (int)SandAmountSlider.Value;

            // Пересоздаем сетку фрактала при изменении размера
            sandpile = new SandpileFractal(size);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            elapsedTimer.Start();
            startTime = DateTime.Now;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            elapsedTimer.Stop();
            totalElapsedTime += DateTime.Now - startTime;
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            elapsedTimer.Start();
            startTime = DateTime.Now;
        }

        private void ElapsedTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan currentElapsedTime = DateTime.Now - startTime;
            TimerTextBlock.Text = $"Время работы: {totalElapsedTime + currentElapsedTime:hh\\:mm\\:ss}";
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
            if (x < 0 || x >= size || y < 0 || y >= size)
            {
                throw new ArgumentOutOfRangeException("x or y", "Индекс находился вне границ массива.");
            }

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