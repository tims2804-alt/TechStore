using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiracleShoesWPF.Pages
{
    public partial class ProductEditPage : Page
    {
        private readonly products _product;   // null - добавление нового товара

        public ProductEditPage(products product)
        {
            InitializeComponent();
            _product = product;

            try
            {
                CategoryBox.ItemsSource = Core.Context.categories.OrderBy(c => c.name).ToList();
                ManufacturerBox.ItemsSource = Core.Context.manufacturers.OrderBy(m => m.name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить справочники: " + ex.GetBaseException().Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (_product == null)
            {
                HeaderText.Text = "Новый товар";
            }
            else
            {
                HeaderText.Text = "Редактирование товара";
                NameBox.Text = _product.name;
                DescBox.Text = _product.description ?? "";
                CategoryBox.SelectedValue = _product.category_id;
                ManufacturerBox.SelectedValue = _product.manufacturer_id;
                PriceBox.Text = _product.price.ToString("0.##");
                StockBox.Text = _product.stock_quantity.ToString();
                WarrantyBox.Text = _product.warranty_months.HasValue ? _product.warranty_months.Value.ToString() : "";
                ActiveBox.IsChecked = _product.is_active;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            if (name.Length < 2)
            {
                ShowError("Название должно быть не короче 2 символов");
                return;
            }
            if (CategoryBox.SelectedValue == null)
            {
                ShowError("Выберите категорию");
                return;
            }
            if (ManufacturerBox.SelectedValue == null)
            {
                ShowError("Выберите производителя");
                return;
            }

            // Принимаем и запятую, и точку в цене
            string priceText = PriceBox.Text.Trim().Replace('.', ',');
            decimal price;
            if (!decimal.TryParse(priceText, NumberStyles.Number, new CultureInfo("ru-RU"), out price) || price < 0)
            {
                ShowError("Цена должна быть числом не меньше 0");
                return;
            }

            int stock;
            if (!int.TryParse(StockBox.Text.Trim(), out stock) || stock < 0)
            {
                ShowError("Количество на складе должно быть целым числом не меньше 0");
                return;
            }

            int? warranty = null;
            if (WarrantyBox.Text.Trim() != "")
            {
                int w;
                if (!int.TryParse(WarrantyBox.Text.Trim(), out w) || w < 0)
                {
                    ShowError("Гарантия должна быть целым числом не меньше 0");
                    return;
                }
                warranty = w;
            }

            try
            {
                var shop = SellerHelper.CurrentShop();
                if (shop == null)
                {
                    ShowError("У вас нет магазина");
                    return;
                }

                var p = _product;
                if (p == null)
                {
                    p = new products { seller_id = shop.id, created_at = DateTime.Now };
                    Core.Context.products.Add(p);
                }

                p.name = name;
                p.description = DescBox.Text.Trim() == "" ? null : DescBox.Text.Trim();
                p.category_id = (int)CategoryBox.SelectedValue;
                p.manufacturer_id = (int)ManufacturerBox.SelectedValue;
                p.price = price;
                p.stock_quantity = stock;
                p.warranty_months = warranty;
                p.is_active = ActiveBox.IsChecked == true;

                Core.Context.SaveChanges();
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                ShowError("Не удалось сохранить товар: " + ex.GetBaseException().Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Отменяем несохранённые правки существующего товара
            DbHelper.DiscardChanges();
            NavigationService.GoBack();
        }
    }
}
