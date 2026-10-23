using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public class ProductRow
    {
        public products Product { get; set; }
        public string Name { get; set; }
        public string Info { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public partial class SellerProductsPage : Page
    {
        public SellerProductsPage()
        {
            InitializeComponent();
            // Список обновляется при каждом возврате на страницу
            Loaded += (s, e) => LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                var shop = SellerHelper.CurrentShop();
                if (shop == null)
                {
                    MessageBox.Show("У вас нет магазина. Создайте его в профиле.", "Магазин",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.Navigate(new ProfilePage());
                    return;
                }

                ShopTitle.Text = "Магазин «" + shop.shop_name + "»: товары";
                int shopId = shop.id;
                var rows = Core.Context.products.Where(p => p.seller_id == shopId)
                    .OrderBy(p => p.name).ToList()
                    .Select(p => new ProductRow
                    {
                        Product = p,
                        Name = p.name,
                        Info = p.categories.name + ", " + p.manufacturers.name
                               + (p.is_active ? "" : " (скрыт из каталога)"),
                        Price = p.price,
                        Stock = p.stock_quantity
                    }).ToList();

                ProductList.ItemsSource = rows;
                EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить товары: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductEditPage(null));
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as ProductRow;
            if (row != null)
            {
                NavigationService.Navigate(new ProductEditPage(row.Product));
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as ProductRow;
            if (row == null) return;
            var product = row.Product;

            var answer = MessageBox.Show("Удалить товар «" + product.name + "»? Он также исчезнет из корзин покупателей.",
                                         "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                // Товары, которые уже покупали, удалять нельзя: сломается история заказов
                int productId = product.id;
                if (Core.Context.order_items.Any(oi => oi.product_id == productId))
                {
                    var hide = MessageBox.Show("Этот товар есть в оформленных заказах, поэтому его нельзя удалить полностью. "
                                               + "Скрыть его из каталога?",
                                               "Товар нельзя удалить", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (hide == MessageBoxResult.Yes)
                    {
                        product.is_active = false;
                        Core.Context.SaveChanges();
                    }
                    LoadProducts();
                    return;
                }

                // Каскадное удаление: сначала убираем товар из всех корзин, затем сам товар
                foreach (var ci in Core.Context.cart_items.Where(c => c.product_id == productId).ToList())
                {
                    Core.Context.cart_items.Remove(ci);
                }
                Core.Context.products.Remove(product);
                Core.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось удалить товар: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadProducts();
        }
        
        private void NavProducts_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new SellerProductsPage()); }
        private void NavOrders_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new SellerOrdersPage()); }
        private void NavReports_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new ReportsPage()); }
    }
}
