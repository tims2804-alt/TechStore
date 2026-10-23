using System.Linq;

namespace MiracleShoesWPF
{
    internal static class SellerHelper
    {
        // Магазин текущего пользователя или null, если магазина нет
        public static sellers CurrentShop()
        {
            if (Core.AuthUser == null) return null;
            int userId = Core.AuthUser.id;
            return Core.Context.sellers.FirstOrDefault(s => s.user_id == userId);
        }
    }
}
