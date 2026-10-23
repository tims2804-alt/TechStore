using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    // Строка корзины для отображения
    public class CartRow
    {
        public cart_items Item { get; set; }
        public string Name { get; set; }
        public string Shop { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    public partial class CartPage : Page
    {
        public CartPage()
        {
            InitializeComponent();
            LoadCart();
            Loaded += (s, e) => LoadCart();
        }

        private List<cart_items> GetItems()
        {
            int userId = Core.AuthUser.id;
            return Core.Context.cart_items.Where(ci => ci.carts.user_id == userId).ToList();
        }

        private void LoadCart()
        {
            try
            {
                List<cart_items> items = GetItems();

                var rows = items.Select(ci => new CartRow
                {
                    Item = ci,
                    Name = ci.products.name,
                    Shop = ci.products.sellers.shop_name,
                    Price = ci.products.price,
                    Quantity = ci.quantity,
                    Subtotal = ci.products.price * ci.quantity
                }).OrderBy(r => r.Shop).ThenBy(r => r.Name).ToList();

                CartList.ItemsSource = rows;

                bool empty = rows.Count == 0;
                EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
                CheckoutPanel.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;

                decimal total = rows.Sum(r => r.Subtotal);
                TotalText.Text = string.Format("Итого: {0:N0} ₽", total);

                int shops = items.Select(ci => ci.products.seller_id).Distinct().Count();
                ShopsNote.Text = shops > 1
                    ? "Товары из разных магазинов: для каждого магазина будет создан отдельный заказ (всего " + shops + ")."
                    : "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить корзину: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохраняет изменения корзины; при ошибке откатывает их
        private void SaveCart()
        {
            try
            {
                Core.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось изменить корзину: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadCart();
        }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as CartRow;
            if (row == null) return;

            int stock = row.Item.products.stock_quantity;
            if (row.Item.quantity + 1 > stock)
            {
                MessageBox.Show("На складе всего " + stock + " шт.", "Корзина",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            row.Item.quantity++;
            SaveCart();
        }

        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as CartRow;
            if (row == null) return;

            if (row.Item.quantity <= 1)
            {
                Core.Context.cart_items.Remove(row.Item);
            }
            else
            {
                row.Item.quantity--;
            }
            SaveCart();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as CartRow;
            if (row == null) return;

            Core.Context.cart_items.Remove(row.Item);
            SaveCart();
        }

        private void DeliveryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Событие приходит ещё во время построения страницы
            if (AddressBox == null) return;

            string method = (DeliveryBox.SelectedItem as ComboBoxItem)?.Tag as string;
            bool pickup = method == "pickup";
            AddressBox.IsEnabled = !pickup;
            AddressLabel.Opacity = pickup ? 0.5 : 1;
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            List<cart_items> items = GetItems();
            if (items.Count == 0)
            {
                MessageBox.Show("Корзина пуста.", "Заказ", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCart();
                return;
            }

            string deliveryMethod = (DeliveryBox.SelectedItem as ComboBoxItem)?.Tag as string;
            string paymentMethod = (PaymentBox.SelectedItem as ComboBoxItem)?.Tag as string;
            string address = AddressBox.Text.Trim();

            if (deliveryMethod != "pickup" && address.Length < 5)
            {
                MessageBox.Show("Укажите адрес доставки.", "Заказ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (deliveryMethod == "pickup")
            {
                address = "Самовывоз у продавца";
            }

            // Перед оформлением проверяем, что товары всё ещё доступны
            foreach (var ci in items)
            {
                var p = ci.products;
                if (!p.is_active || !p.sellers.is_active)
                {
                    MessageBox.Show("Товар «" + p.name + "» больше недоступен. Удалите его из корзины.",
                                    "Заказ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (ci.quantity > p.stock_quantity)
                {
                    MessageBox.Show("Товара «" + p.name + "» на складе осталось " + p.stock_quantity + " шт.",
                                    "Заказ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                DateTime now = DateTime.Now;
                bool paidNow = paymentMethod != "cash_on_delivery";
                int orderCount = 0;

                // Для каждого магазина создаём отдельный заказ
                foreach (var group in items.GroupBy(ci => ci.products.seller_id))
                {
                    var order = new orders
                    {
                        user_id = Core.AuthUser.id,
                        seller_id = group.Key,
                        order_date = now,
                        status = paidNow ? "paid" : "new"
                    };

                    decimal sum = 0;
                    foreach (var ci in group)
                    {
                        order.order_items.Add(new order_items
                        {
                            product_id = ci.product_id,
                            quantity = ci.quantity,
                            price = ci.products.price   // цена фиксируется на момент покупки
                        });
                        ci.products.stock_quantity -= ci.quantity;
                        sum += ci.quantity * ci.products.price;
                    }

                    order.payments.Add(new payments
                    {
                        payment_date = paidNow ? (DateTime?)now : null,
                        amount = sum,
                        payment_method = paymentMethod,
                        status = paidNow ? "paid" : "pending"
                    });

                    order.deliveries.Add(new deliveries
                    {
                        address = address,
                        delivery_method = deliveryMethod,
                        delivery_date = null,
                        status = "pending"
                    });

                    Core.Context.orders.Add(order);
                    orderCount++;
                }

                // Корзину очищаем
                foreach (var ci in items)
                {
                    Core.Context.cart_items.Remove(ci);
                }

                // Один SaveChanges выполняется в одной транзакции:
                // либо создаётся всё (заказы, оплаты, доставки, списание остатков), либо ничего
                Core.Context.SaveChanges();

                MessageBox.Show("Заказ оформлен. Создано заказов: " + orderCount + ".",
                                "Заказ", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(new OrdersPage());
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось оформить заказ: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCart();
            }
        }
    }
}
