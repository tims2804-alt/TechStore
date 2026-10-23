using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public partial class MainPage : Page
    {
        // Пока страница строится, обработчики фильтров ничего не делают
        private bool _ready = false;

        public MainPage()
        {
            InitializeComponent();
            Loaded += (s, e) => Filter();

            try
            {
                var allCategories = Core.Context.categories.OrderBy(c => c.name).ToList();
                // Пункт «Все категории» нужен только для списка, в базу он не попадает
                allCategories.Insert(0, new categories { id = 0, name = "Все категории" });
                FilterBox.ItemsSource = allCategories;
                FilterBox.SelectedIndex = 0;

                _ready = true;
                Filter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить каталог: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Собираем запрос к БД по поиску, категории и сортировке
        private void Filter()
        {
            if (!_ready) return;

            IQueryable<products> query = Core.Context.products
                .Where(p => p.is_active && p.sellers.is_active);

            string text = SearchBox.Text.Trim();
            if (text != "")
            {
                query = query.Where(p => p.name.Contains(text));
            }

            var category = FilterBox.SelectedItem as categories;
            if (category != null && category.id != 0)
            {
                int categoryId = category.id;
                query = query.Where(p => p.category_id == categoryId);
            }

            switch (SortBox.SelectedIndex)
            {
                case 1:
                    query = query.OrderBy(p => p.price);
                    break;
                case 2:
                    query = query.OrderByDescending(p => p.price);
                    break;
                case 3:
                    query = query.OrderBy(p => p.name);
                    break;
                default:
                    query = query.OrderBy(p => p.id);
                    break;
            }

            List<products> result = query.ToList();
            CatalogListBox.ItemsSource = result;
            CountText.Text = "Найдено товаров: " + result.Count;
            EmptyText.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filter();
        }

        private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Filter();
        }

        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Filter();
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button)?.DataContext as products;
            if (product == null) return;

            // Гость не может пользоваться корзиной
            if (Core.AuthUser == null)
            {
                var answer = MessageBox.Show("Чтобы добавить товар в корзину, нужно войти в аккаунт. Перейти ко входу?",
                                             "Нужен вход", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Yes)
                {
                    NavigationService.Navigate(new LoginPage());
                }
                return;
            }

            // Свои товары покупать нельзя (в БД это тоже запрещено триггером)
            if (Core.AuthUser.sellers.Any(s => s.id == product.seller_id))
            {
                MessageBox.Show("Нельзя купить товар из собственного магазина.",
                                "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                int userId = Core.AuthUser.id;
                var cart = Core.Context.carts.FirstOrDefault(c => c.user_id == userId);
                if (cart == null)
                {
                    cart = new carts { user_id = userId, created_at = DateTime.Now };
                    Core.Context.carts.Add(cart);
                }

                var item = cart.cart_items.FirstOrDefault(ci => ci.product_id == product.id);
                int alreadyInCart = item == null ? 0 : item.quantity;

                if (alreadyInCart + 1 > product.stock_quantity)
                {
                    MessageBox.Show("На складе всего " + product.stock_quantity + " шт., в корзине уже " + alreadyInCart + ".",
                                    "Корзина", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (item == null)
                {
                    cart.cart_items.Add(new cart_items { product_id = product.id, quantity = 1 });
                }
                else
                {
                    item.quantity++;
                }

                Core.Context.SaveChanges();
                MessageBox.Show("Товар «" + product.name + "» добавлен в корзину.",
                                "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось добавить товар: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
