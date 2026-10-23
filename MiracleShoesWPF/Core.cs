using System;

namespace MiracleShoesWPF
{
    internal class Core
    {
        // Один общий контекст на всё приложение (нужен, чтобы связанные данные
        // подгружались и чтобы изменения сохранялись из любых страниц).
        // Если в Model1.Context.cs класс контекста называется иначе,
        // замените MiracleShoesEntities на его имя (в двух местах ниже).
        public static TechStoreDBEntities Context { get; } = new TechStoreDBEntities();

        // Текущий пользователь. null означает гостя.
        public static users AuthUser = null;
    }
}
