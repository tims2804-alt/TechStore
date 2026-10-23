using System.Linq;

namespace MiracleShoesWPF
{
    internal static class DbHelper
    {
        // Откатывает несохранённые изменения общего контекста после неудачного SaveChanges:
        // добавленные записи убираем, изменённые и удалённые перечитываем из БД.
        public static void DiscardChanges()
        {
            foreach (var entry in Core.Context.ChangeTracker.Entries().ToList())
            {
                switch (entry.State)
                {
                    case System.Data.EntityState.Added:
                        entry.State = System.Data.EntityState.Detached;
                        break;
                    case System.Data.EntityState.Modified:
                    case System.Data.EntityState.Deleted:
                        entry.Reload();
                        break;
                }
            }
        }
    }
}
