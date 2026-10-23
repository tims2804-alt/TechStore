using System.Windows;

namespace MiracleShoesWPF
{
    // Присоединяемое свойство для плейсхолдера (серой подсказки) в TextBox.
    // Использование в XAML: app:Hint.Text="Введите email"
    public static class Hint
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(Hint),
                new PropertyMetadata(string.Empty));

        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(TextProperty, value);
        }
    }
}
