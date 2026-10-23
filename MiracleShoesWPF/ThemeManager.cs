using System;
using System.IO;
using System.Windows;

namespace MiracleShoesWPF
{
    // Переключение светлой и тёмной темы. Выбранная тема запоминается между запусками.
    internal static class ThemeManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TechStore", "theme.txt");

        private static ResourceDictionary _current;

        public static bool IsDark { get; private set; }

        // Вызывается один раз при запуске, до создания разметки окна
        public static void Initialize()
        {
            bool dark = false;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    dark = File.ReadAllText(SettingsPath).Trim() == "dark";
                }
            }
            catch (Exception)
            {
                // Не удалось прочитать настройку - остаётся светлая тема
            }
            Apply(dark);
        }

        public static void Toggle()
        {
            Apply(!IsDark);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, IsDark ? "dark" : "light");
            }
            catch (Exception)
            {
                // Тема переключена, но запомнить её не удалось - не критично
            }
        }

        private static void Apply(bool dark)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Themes/" + (dark ? "DarkTheme" : "LightTheme") + ".xaml",
                                 UriKind.Absolute)
            };

            var merged = Application.Current.Resources.MergedDictionaries;
            if (_current != null)
            {
                merged.Remove(_current);
            }
            merged.Add(dictionary);

            _current = dictionary;
            IsDark = dark;
        }
    }
}
