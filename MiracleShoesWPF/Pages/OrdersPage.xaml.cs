using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    // Заказ для отображения
    public class OrderRow
    {
        public string Title { get; set; }
        public string Shop { get; set; }
        public string Status { get; set; }
        public string Items { get; set; }
        public string Payment { get; set; }
        public string Delivery { get; set; }
        public string Total { get; set; }
    }

    public partial class OrdersPage : Page
    {
        public OrdersPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                int userId = Core.AuthUser.id;
                List<orders> list = Core.Context.orders
                    .Where(o => o.user_id == userId)
                    .OrderByDescending(o => o.order_date)
                    .ToList();

                var rows = list.Select(o =>
                {
                    var pay = o.payments.FirstOrDefault();
                    var del = o.deliveries.FirstOrDefault();

                    return new OrderRow
                    {
                        Title = "Заказ №" + o.id + " от " + o.order_date.ToString("dd.MM.yyyy HH:mm"),
                        Shop = "Магазин: " + o.sellers.shop_name,
                        Status = "Статус: " + Labels.OrderStatus(o.status),
                        Items = string.Join(Environment.NewLine,
                            o.order_items.Select(oi => oi.products.name + " × " + oi.quantity
                                                       + " — " + string.Format("{0:N0} ₽", oi.price))),
                        Payment = pay == null ? "" :
                            "Оплата: " + Labels.PaymentMethod(pay.payment_method)
                            + " (" + Labels.PaymentStatus(pay.status) + ")",
                        Delivery = del == null ? "" :
                            "Доставка: " + Labels.DeliveryMethod(del.delivery_method)
                            + ", " + del.address + " (" + Labels.DeliveryStatus(del.status) + ")",
                        Total = string.Format("Сумма: {0:N0} ₽", o.order_items.Sum(oi => oi.quantity * oi.price))
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
    }
}
