using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MiracleShoesWPF
{
    // Капча: случайный код рисуется на Canvas с наклоном букв и шумом
    internal static class CaptchaHelper
    {
        private static readonly Random Rnd = new Random();

        // Без похожих символов (0/O, 1/I)
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string NewCode(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = Alphabet[Rnd.Next(Alphabet.Length)];
            }
            return new string(chars);
        }

        private static Brush RandomBrush(int min, int max)
        {
            return new SolidColorBrush(Color.FromRgb(
                (byte)Rnd.Next(min, max), (byte)Rnd.Next(min, max), (byte)Rnd.Next(min, max)));
        }

        private static Line RandomLine(double width, double height, int min, int max, double thickness)
        {
            return new Line
            {
                X1 = Rnd.NextDouble() * width,
                Y1 = Rnd.NextDouble() * height,
                X2 = Rnd.NextDouble() * width,
                Y2 = Rnd.NextDouble() * height,
                Stroke = RandomBrush(min, max),
                StrokeThickness = thickness
            };
        }

        public static void Draw(Canvas canvas, string code)
        {
            canvas.Children.Clear();
            canvas.Background = Brushes.White;

            double width = canvas.Width;
            double height = canvas.Height;

            // Шум под буквами
            for (int i = 0; i < 10; i++)
            {
                canvas.Children.Add(RandomLine(width, height, 120, 220, 1));
            }

            // Буквы со случайным размером, наклоном и высотой
            double step = width / (code.Length + 1);
            for (int i = 0; i < code.Length; i++)
            {
                var letter = new TextBlock
                {
                    Text = code[i].ToString(),
                    FontSize = Rnd.Next(24, 34),
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = RandomBrush(0, 110),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(Rnd.Next(-25, 26))
                };
                Canvas.SetLeft(letter, step * (i + 0.5));
                Canvas.SetTop(letter, Rnd.Next(2, 12));
                canvas.Children.Add(letter);
            }

            // Линии поверх букв
            for (int i = 0; i < 3; i++)
            {
                canvas.Children.Add(RandomLine(width, height, 60, 160, 2));
            }
        }
    }
}
