using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public partial class LoginPage : Page
    {
        private const int MaxAttemptsWithoutCaptcha = 3;

        // Счётчик неверных попыток живёт, пока открыто приложение
        private static int _failedAttempts = 0;

        private string _captchaCode;

        public LoginPage()
        {
            InitializeComponent();

            // Любой заход на страницу входа сбрасывает пользователя
            Core.AuthUser = null;

            if (_failedAttempts >= MaxAttemptsWithoutCaptcha)
            {
                ShowCaptcha();
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ShowCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Visible;
            NewCaptcha();
        }

        private void NewCaptcha()
        {
            _captchaCode = CaptchaHelper.NewCode(5);
            CaptchaHelper.Draw(CaptchaCanvas, _captchaCode);
            CaptchaBox.Text = "";
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            NewCaptcha();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordInput.Password;

            if (email == "" || password == "")
            {
                ShowError("Введите email и пароль");
                return;
            }

            // Если капча включена, сначала проверяем её (неверная капча не считается попыткой входа)
            if (CaptchaPanel.Visibility == Visibility.Visible)
            {
                if (!string.Equals(CaptchaBox.Text.Trim(), _captchaCode, StringComparison.OrdinalIgnoreCase))
                {
                    ShowError("Неверный код с картинки. Попробуйте ещё раз.");
                    NewCaptcha();
                    return;
                }
            }

            try
            {
                var user = Core.Context.users.FirstOrDefault(u => u.email == email);

                // Сообщение одинаковое для неверного email и пароля
                if (user == null || !PasswordHelper.Verify(password, user.password_hash))
                {
                    _failedAttempts++;
                    PasswordInput.Password = "";

                    if (_failedAttempts >= MaxAttemptsWithoutCaptcha)
                    {
                        ShowCaptcha();
                        ShowError("Неверный email или пароль. Подтвердите, что вы не робот.");
                    }
                    else
                    {
                        ShowError("Неверный email или пароль. Осталось попыток до проверки: "
                                  + (MaxAttemptsWithoutCaptcha - _failedAttempts));
                    }
                    return;
                }

                _failedAttempts = 0;
                Core.AuthUser = user;
                NavigationService.Navigate(new MainPage());
            }
            catch (Exception ex)
            {
                ShowError("Не удалось обратиться к базе данных: " + ex.GetBaseException().Message);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            Core.AuthUser = null;
            NavigationService.Navigate(new MainPage());
        }
    }
}
