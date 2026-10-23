using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiracleShoesWPF.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadProfile();
        }

        private static void Show(TextBlock target, string text, bool isError)
        {
            target.Text = text;
            target.Foreground = isError
                ? (Brush)new BrushConverter().ConvertFromString("#B91C1C")
                : (Brush)new BrushConverter().ConvertFromString("#15803D");
            target.Visibility = Visibility.Visible;
        }

        private void LoadProfile()
        {
            var user = Core.AuthUser;

            EmailText.Text = "Email: " + user.email;
            RegisteredText.Text = "Дата регистрации: " + user.created_at.ToString("dd.MM.yyyy");
            FirstNameBox.Text = user.first_name;
            LastNameBox.Text = user.last_name;
            PhoneBox.Text = user.phone ?? "";

            try
            {
                int userId = user.id;
                var shop = Core.Context.sellers.FirstOrDefault(s => s.user_id == userId);

                if (shop == null)
                {
                    BecomeSellerPanel.Visibility = Visibility.Visible;
                    SellerPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BecomeSellerPanel.Visibility = Visibility.Collapsed;
                    SellerPanel.Visibility = Visibility.Visible;
                    ShopNameText.Text = shop.shop_name;
                    ShopDescText.Text = string.IsNullOrWhiteSpace(shop.description) ? "Описание не указано" : shop.description;
                    ShopStatusText.Text = shop.is_active ? "Магазин активен" : "Магазин отключён";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные магазина: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameBox.Text.Trim();
            string lastName = LastNameBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();

            if (firstName == "" || lastName == "")
            {
                Show(ProfileMessage, "Укажите имя и фамилию", true);
                return;
            }
            if (phone != "" && !Regex.IsMatch(phone, @"^\+?[0-9\s\-\(\)]{10,20}$"))
            {
                Show(ProfileMessage, "Телефон указан неверно (пример: +7 900 111-22-33)", true);
                return;
            }

            try
            {
                Core.AuthUser.first_name = firstName;
                Core.AuthUser.last_name = lastName;
                Core.AuthUser.phone = phone == "" ? null : phone;
                Core.Context.SaveChanges();

                Show(ProfileMessage, "Данные сохранены", false);
                (Application.Current.MainWindow as MainWindow)?.RefreshHeader();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                LoadProfile();
                Show(ProfileMessage, "Не удалось сохранить: " + ex.GetBaseException().Message, true);
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ChangePasswordPage());
        }

        private void SellerProducts_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SellerProductsPage());
        }

        private void SellerOrders_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SellerOrdersPage());
        }

        private void SellerReports_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportsPage());
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new OrdersPage());
        }

        private void CreateShop_Click(object sender, RoutedEventArgs e)
        {
            string name = ShopNameBox.Text.Trim();
            string description = ShopDescBox.Text.Trim();

            if (name.Length < 3)
            {
                Show(ShopMessage, "Название магазина должно быть не короче 3 символов", true);
                return;
            }

            sellers shop = null;
            try
            {
                if (Core.Context.sellers.Any(s => s.shop_name == name))
                {
                    Show(ShopMessage, "Магазин с таким названием уже существует", true);
                    return;
                }

                shop = new sellers
                {
                    users = Core.AuthUser,
                    shop_name = name,
                    description = description == "" ? null : description,
                    is_active = true,
                    created_at = DateTime.Now
                };

                Core.Context.sellers.Add(shop);
                Core.Context.SaveChanges();

                MessageBox.Show("Магазин «" + name + "» создан. Теперь вы можете продавать товары.",
                                "Магазин", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadProfile();
                (Application.Current.MainWindow as MainWindow)?.RefreshHeader();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                Show(ShopMessage, "Не удалось создать магазин: " + ex.GetBaseException().Message, true);
            }
        }
    }
}
