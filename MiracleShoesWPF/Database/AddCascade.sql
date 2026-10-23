/* Каскадное удаление на уровне БД: при удалении товара или пользователя
   связанные строки корзин удаляются автоматически.
   (Для заказов, оплат и доставок каскад уже есть в TechStoreDB.sql:
   orders -> order_items, payments, deliveries; carts -> cart_items.) */
USE TechStoreDB;
GO

ALTER TABLE cart_items DROP CONSTRAINT FK_cart_items_products;
ALTER TABLE cart_items ADD CONSTRAINT FK_cart_items_products
    FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE;
GO

ALTER TABLE carts DROP CONSTRAINT FK_carts_users;
ALTER TABLE carts ADD CONSTRAINT FK_carts_users
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;
GO

/* Проверка: у этих внешних ключей должно быть CASCADE */
SELECT name, delete_referential_action_desc
FROM sys.foreign_keys
WHERE name IN (N'FK_cart_items_products', N'FK_carts_users', N'FK_cart_items_carts',
               N'FK_order_items_orders', N'FK_payments_orders', N'FK_deliveries_orders');
GO
