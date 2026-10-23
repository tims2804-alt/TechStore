using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public class BarRow
    {
        public string Label { get; set; }
        public string ValueText { get; set; }
        public double BarWidth { get; set; }
    }

    public partial class ReportsPage : Page
    {
        private const double MaxBar = 340;

        public ReportsPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadReports();
        }

        // Строит строки диаграммы: ширина столбца пропорциональна значению
        private static List<BarRow> Bars(IEnumerable<Tuple<string, decimal, string>> data)
        {
            var list = data.ToList();
            decimal max = list.Count == 0 ? 0 : list.Max(d => d.Item2);
            return list.Select(d => new BarRow
            {
                Label = d.Item1,
                ValueText = d.Item3,
                BarWidth = max > 0 ? Math.Max(2, (double)(d.Item2 / max) * MaxBar) : 0
            }).ToList();
        }

        private void LoadReports()
        {
            try
            {
                var shop = SellerHelper.CurrentShop();
                if (shop == null) return;
                int shopId = shop.id;

                var items = Core.Context.order_items
                    .Where(oi => oi.orders.seller_id == shopId && oi.orders.status != "cancelled").ToList();
                var allOrders = Core.Context.orders.Where(o => o.seller_id == shopId).ToList();

                decimal revenue = items.Sum(oi => oi.quantity * oi.price);
                int sold = items.Sum(oi => oi.quantity);
                int activeOrders = allOrders.Count(o => o.status != "cancelled");
                SummaryText.Text = string.Format("Выручка: {0:N0} ₽   |   Заказов: {1}   |   Продано единиц: {2}",
                                                 revenue, activeOrders, sold);

                ProductBars.ItemsSource = Bars(items
                    .GroupBy(oi => oi.products.name)
                    .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.quantity), Sum = g.Sum(x => x.quantity * x.price) })
                    .OrderByDescending(x => x.Sum)
                    .Select(x => Tuple.Create(x.Name + " (" + x.Qty + " шт.)", x.Sum, string.Format("{0:N0} ₽", x.Sum))));

                CategoryBars.ItemsSource = Bars(items
                    .GroupBy(oi => oi.products.categories.name)
                    .Select(g => new { Name = g.Key, Sum = g.Sum(x => x.quantity * x.price) })
                    .OrderByDescending(x => x.Sum)
                    .Select(x => Tuple.Create(x.Name, x.Sum, string.Format("{0:N0} ₽", x.Sum))));

                StatusBars.ItemsSource = Bars(allOrders
                    .GroupBy(o => o.status)
                    .Select(g => new { Name = Labels.OrderStatus(g.Key), Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Select(x => Tuple.Create(x.Name, (decimal)x.Count, x.Count.ToString())));

                EmptyText.Visibility = allOrders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось построить отчёты: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void NavProducts_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new SellerProductsPage()); }
        private void NavOrders_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new SellerOrdersPage()); }
        private void NavReports_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new ReportsPage()); }
    }
}
