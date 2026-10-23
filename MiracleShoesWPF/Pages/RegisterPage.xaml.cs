using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        // Возвращает текст ошибки или null, если всё в порядке
        private string Validate(string firstName, string lastName, string email, string phone,
                                string password, string confirm)
        {
            if (firstName == "" || lastName == "")
                return "Укажите имя и фамилию";

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Введите корректный email";

            if (phone != "" && !Regex.IsMatch(phone, @"^\+?[0-9\s\-\(\)]{10,20}$"))
                return "Телефон указан неверно (пример: +7 900 111-22-33)";

            if (password.Length < 6)
                return "Пароль должен быть не короче 6 символов";

            if (password != confirm)
                return "Пароли не совпадают";

            return null;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameBox.Text.Trim();
            string lastName = LastNameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();
            string password = PasswordInput.Password;
            string confirm = ConfirmInput.Password;

            string error = Validate(firstName, lastName, email, phone, password, confirm);
            if (error != null)
            {
                ShowError(error);
                return;
            }

            users user = null;
            try
            {
                if (Core.Context.users.Any(u => u.email == email))
                {
                    ShowError("Пользователь с таким email уже зарегистрирован");
                    return;
                }

                user = new users
                {
                    first_name = firstName,
                    last_name = lastName,
                    email = email,
                    phone = phone == "" ? null : phone,
                    password_hash = PasswordHelper.Hash(password),
                    created_at = DateTime.Now
                };

                Core.Context.users.Add(user);
                Core.Context.SaveChanges();

                Core.AuthUser = user;
                NavigationService.Navigate(new MainPage());
            }
            catch (Exception ex)
            {
                // Убираем неудавшуюся запись из контекста, чтобы она не мешала дальше
                if (user != null)
                {
                    Core.Context.Entry(user).State = System.Data.EntityState.Detached;
                }
                ShowError("Не удалось зарегистрироваться: " + ex.GetBaseException().Message);
            }
        }

        private void ToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }
    }
}
