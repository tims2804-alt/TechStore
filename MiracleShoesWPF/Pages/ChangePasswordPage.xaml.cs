using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiracleShoesWPF.Pages
{
    public partial class ChangePasswordPage : Page
    {
        public ChangePasswordPage()
        {
            InitializeComponent();
        }

        private void Show(string text, bool isError)
        {
            MessageText.Text = text;
            MessageText.Foreground = (Brush)new BrushConverter().ConvertFromString(isError ? "#DC2626" : "#16A34A");
            MessageText.Visibility = Visibility.Visible;
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            string oldPass = OldBox.Password;
            string newPass = NewBox.Password;
            string confirm = ConfirmBox.Password;

            if (!PasswordHelper.Verify(oldPass, Core.AuthUser.password_hash))
            {
                Show("Текущий пароль указан неверно", true);
                return;
            }
            if (newPass.Length < 6)
            {
                Show("Новый пароль должен быть не короче 6 символов", true);
                return;
            }
            if (newPass == oldPass)
            {
                Show("Новый пароль должен отличаться от текущего", true);
                return;
            }
            if (newPass != confirm)
            {
                Show("Пароли не совпадают", true);
                return;
            }

            try
            {
                Core.AuthUser.password_hash = PasswordHelper.Hash(newPass);
                Core.Context.SaveChanges();

                OldBox.Password = "";
                NewBox.Password = "";
                ConfirmBox.Password = "";
                Show("Пароль изменён", false);
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                Show("Не удалось сменить пароль: " + ex.GetBaseException().Message, true);
            }
        }
    }
}
