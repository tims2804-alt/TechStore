using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MiracleShoesWPF.Pages;

namespace MiracleShoesWPF
{
    public partial class MainWindow : Window
    {
        private object _current;

        public MainWindow()
        {
            // Тема применяется до разметки, чтобы окно сразу открылось в нужных цветах
            ThemeManager.Initialize();
            InitializeComponent();
            MainFrame.Navigated += MainFrame_Navigated;
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            _current = e.Content;

            // На странице входа история переходов не нужна
            if (_current is LoginPage)
            {
                while (MainFrame.CanGoBack)
                {
                    MainFrame.RemoveBackEntry();
                }
            }

            // Заголовок окна соответствует текущей странице
            var page = _current as Page;
            Title = (page != null && !string.IsNullOrEmpty(page.Title)) ? "TechStore — " + page.Title : "TechStore";

            RefreshHeader();
        }

        // Обновляет шапку: имя пользователя, кнопки и «Назад»
        public void RefreshHeader()
        {
            object page = _current ?? MainFrame.Content;
            bool isLogin = page is LoginPage;
            bool isRegister = page is RegisterPage;
            bool isMain = page is MainPage;
            bool isAuth = Core.AuthUser != null;
            bool onAuthPage = isLogin || isRegister;

            BackButton.Visibility = (MainFrame.CanGoBack && !isLogin && !isMain)
                ? Visibility.Visible : Visibility.Collapsed;

            if (isAuth)
            {
                UserText.Text = Core.AuthUser.first_name + " " + Core.AuthUser.last_name;
                AuthButton.Content = "Выйти";
                AuthButton.ToolTip = "Выйти из аккаунта";
            }
            else
            {
                UserText.Text = "Гость";
                AuthButton.Content = "Войти";
                AuthButton.ToolTip = "Перейти на страницу входа";
            }

            CatalogButton.Visibility = onAuthPage ? Visibility.Collapsed : Visibility.Visible;
            // Корзина видна всем; гостя при нажатии попросят войти
            CartButton.Visibility = onAuthPage ? Visibility.Collapsed : Visibility.Visible;
            ProfileButton.Visibility = (isAuth && !onAuthPage) ? Visibility.Visible : Visibility.Collapsed;

            bool isSeller = false;
            if (isAuth && !onAuthPage)
            {
                isSeller = Core.AuthUser.sellers.Any();
            }
            SellerButton.Visibility = isSeller ? Visibility.Visible : Visibility.Collapsed;
            AuthButton.Visibility = onAuthPage ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }

        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage());
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (Core.AuthUser == null)
            {
                var answer = MessageBox.Show("Корзина доступна после входа в аккаунт. Перейти ко входу?",
                                             "Нужен вход", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Yes)
                {
                    MainFrame.Navigate(new LoginPage());
                }
                return;
            }
            MainFrame.Navigate(new CartPage());
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
        }

        private void SellerButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SellerProductsPage());
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
        }

        // Для гостя «Войти», для пользователя «Выйти» с подтверждением
        private void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            if (Core.AuthUser != null)
            {
                var answer = MessageBox.Show("Вы действительно хотите выйти из аккаунта?",
                                             "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;
            }
            MainFrame.Navigate(new LoginPage());
        }
    }
}
