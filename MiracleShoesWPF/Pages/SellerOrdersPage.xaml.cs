using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public class ShopOrderRow
    {
        public orders Order { get; set; }
        public string Title { get; set; }
        public string Buyer { get; set; }
        public string Status { get; set; }
        public string Items { get; set; }
        public string Payment { get; set; }
        public string Delivery { get; set; }
        public string Total { get; set; }
        public string NextText { get; set; }
        public Visibility NextVisibility { get; set; }
        public Visibility CancelVisibility { get; set; }
    }

    public partial class SellerOrdersPage : Page
    {
        public SellerOrdersPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                var shop = SellerHelper.CurrentShop();
                if (shop == null) return;
                int shopId = shop.id;

                var rows = Core.Context.orders.Where(o => o.seller_id == shopId)
                    .OrderByDescending(o => o.order_date).ToList()
                    .Select(o =>
                    {
                        var pay = o.payments.FirstOrDefault();
                        var del = o.deliveries.FirstOrDefault();
                        bool canNext = o.status == "new" || o.status == "paid" || o.status == "shipped";
                        bool canCancel = o.status == "new" || o.status == "paid";

                        return new ShopOrderRow
                        {
                            Order = o,
                            Title = "Заказ №" + o.id + " от " + o.order_date.ToString("dd.MM.yyyy HH:mm"),
                            Buyer = "Покупатель: " + o.users.first_name + " " + o.users.last_name
                                    + (string.IsNullOrEmpty(o.users.phone) ? "" : ", " + o.users.phone),
                            Status = "Статус: " + Labels.OrderStatus(o.status),
                            Items = string.Join(Environment.NewLine,
                                o.order_items.Select(oi => oi.products.name + " × " + oi.quantity
                                                           + " — " + string.Format("{0:N0} ₽", oi.price))),
                            Payment = pay == null ? "" : "Оплата: " + Labels.PaymentMethod(pay.payment_method)
                                                         + " (" + Labels.PaymentStatus(pay.status) + ")",
                            Delivery = del == null ? "" : "Доставка: " + Labels.DeliveryMethod(del.delivery_method)
                                                          + ", " + del.address + " (" + Labels.DeliveryStatus(del.status) + ")",
                            Total = string.Format("Сумма: {0:N0} ₽", o.order_items.Sum(oi => oi.quantity * oi.price)),
                            NextText = o.status == "shipped" ? "Отметить доставленным" : "Отправить заказ",
                            NextVisibility = canNext ? Visibility.Visible : Visibility.Collapsed,
                            CancelVisibility = canCancel ? Visibility.Visible : Visibility.Collapsed
                        };
                    }).ToList();

                OrdersList.ItemsSource = rows;
                EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить заказы: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as ShopOrderRow;
            if (row == null) return;
            var order = row.Order;

            try
            {
                var pay = order.payments.FirstOrDefault();
                var del = order.deliveries.FirstOrDefault();

                if (order.status == "shipped")
                {
                    order.status = "delivered";
                    if (del != null)
                    {
                        del.status = "delivered";
                        del.delivery_date = DateTime.Now;
                    }
                    // Оплата при получении фиксируется в момент доставки
                    if (pay != null && pay.status == "pending")
                    {
                        pay.status = "paid";
                        pay.payment_date = DateTime.Now;
                    }
                }
                else
                {
                    order.status = "shipped";
                    if (del != null) del.status = "in_transit";
                }

                Core.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось изменить статус: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadOrders();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.DataContext as ShopOrderRow;
            if (row == null) return;
            var order = row.Order;

            var answer = MessageBox.Show("Отменить заказ №" + order.id + "? Товары вернутся на склад.",
                                         "Подтверждение отмены", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                foreach (var oi in order.order_items)
                {
                    oi.products.stock_quantity += oi.quantity;
                }

                order.status = "cancelled";
                var del = order.deliveries.FirstOrDefault();
                if (del != null) del.status = "cancelled";
                var pay = order.payments.FirstOrDefault();
                if (pay != null) pay.status = pay.status == "paid" ? "refunded" : "failed";

                Core.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось отменить заказ: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadOrders();
        }
        
        private void NavProducts_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new SellerProductsPage()); }
        private void NavOrders_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new SellerOrdersPage()); }
        private void NavReports_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new ReportsPage()); }
    }
}
