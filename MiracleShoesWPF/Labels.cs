namespace MiracleShoesWPF
{
    // Русские подписи для кодов статусов и способов из базы данных
    internal static class Labels
    {
        public static string OrderStatus(string code)
        {
            switch (code)
            {
                case "new": return "Новый";
                case "paid": return "Оплачен";
                case "shipped": return "Отправлен";
                case "delivered": return "Доставлен";
                case "cancelled": return "Отменён";
                default: return code;
            }
        }

        public static string PaymentMethod(string code)
        {
            switch (code)
            {
                case "card": return "Банковская карта";
                case "sbp": return "СБП";
                case "cash_on_delivery": return "При получении";
                default: return code;
            }
        }

        public static string PaymentStatus(string code)
        {
            switch (code)
            {
                case "pending": return "ожидает оплаты";
                case "paid": return "оплачен";
                case "failed": return "ошибка оплаты";
                case "refunded": return "возвращён";
                default: return code;
            }
        }

        public static string DeliveryMethod(string code)
        {
            switch (code)
            {
                case "courier": return "Курьер";
                case "pickup": return "Самовывоз";
                case "post": return "Почта";
                default: return code;
            }
        }

        public static string DeliveryStatus(string code)
        {
            switch (code)
            {
                case "pending": return "готовится";
                case "in_transit": return "в пути";
                case "delivered": return "доставлена";
                case "cancelled": return "отменена";
                default: return code;
            }
        }
    }
}
