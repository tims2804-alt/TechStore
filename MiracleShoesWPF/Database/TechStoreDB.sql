/* ============================================================
   TechStoreDB - база данных интернет-магазина электроники
   СУБД: Microsoft SQL Server
   Роли: покупатель и продавец. Отдельной роли нет: пользователь
   становится продавцом, когда для него создана запись в sellers.
   ВНИМАНИЕ: скрипт удаляет и пересоздаёт базу TechStoreDB.
   ============================================================ */

USE master;
GO

IF DB_ID(N'TechStoreDB') IS NOT NULL
BEGIN
    ALTER DATABASE TechStoreDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TechStoreDB;
END
GO

CREATE DATABASE TechStoreDB COLLATE Cyrillic_General_CI_AS;
GO

USE TechStoreDB;
GO

/* ---------- 1. Таблицы ---------- */

-- Пользователи (каждый пользователь может покупать)
CREATE TABLE users (
    id            INT IDENTITY(1,1) CONSTRAINT PK_users PRIMARY KEY,
    first_name    NVARCHAR(100) NOT NULL,
    last_name     NVARCHAR(100) NOT NULL,
    email         NVARCHAR(150) NOT NULL CONSTRAINT UQ_users_email UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    phone         NVARCHAR(20)  NULL,
    created_at    DATETIME2(0)  NOT NULL CONSTRAINT DF_users_created DEFAULT SYSDATETIME(),
    CONSTRAINT CK_users_email CHECK (email LIKE N'%_@_%._%')
);
GO

-- Продавцы (профиль продавца, создаётся из профиля пользователя)
CREATE TABLE sellers (
    id          INT IDENTITY(1,1) CONSTRAINT PK_sellers PRIMARY KEY,
    user_id     INT NOT NULL CONSTRAINT UQ_sellers_user UNIQUE
                    CONSTRAINT FK_sellers_users REFERENCES users(id),
    shop_name   NVARCHAR(100) NOT NULL CONSTRAINT UQ_sellers_shop UNIQUE,
    description NVARCHAR(MAX) NULL,
    is_active   BIT NOT NULL CONSTRAINT DF_sellers_active DEFAULT 1,
    created_at  DATETIME2(0) NOT NULL CONSTRAINT DF_sellers_created DEFAULT SYSDATETIME()
);
GO

CREATE TABLE categories (
    id          INT IDENTITY(1,1) CONSTRAINT PK_categories PRIMARY KEY,
    name        NVARCHAR(100) NOT NULL CONSTRAINT UQ_categories_name UNIQUE,
    description NVARCHAR(MAX) NULL
);
GO

CREATE TABLE manufacturers (
    id      INT IDENTITY(1,1) CONSTRAINT PK_manufacturers PRIMARY KEY,
    name    NVARCHAR(100) NOT NULL CONSTRAINT UQ_manufacturers_name UNIQUE,
    country NVARCHAR(100) NULL
);
GO

CREATE TABLE products (
    id              INT IDENTITY(1,1) CONSTRAINT PK_products PRIMARY KEY,
    seller_id       INT NOT NULL CONSTRAINT FK_products_sellers REFERENCES sellers(id),
    category_id     INT NOT NULL CONSTRAINT FK_products_categories REFERENCES categories(id),
    manufacturer_id INT NOT NULL CONSTRAINT FK_products_manufacturers REFERENCES manufacturers(id),
    name            NVARCHAR(200) NOT NULL,
    description     NVARCHAR(MAX) NULL,
    price           DECIMAL(12,2) NOT NULL,
    stock_quantity  INT NOT NULL CONSTRAINT DF_products_stock DEFAULT 0,
    warranty_months INT NULL,
    is_active       BIT NOT NULL CONSTRAINT DF_products_active DEFAULT 1,
    created_at      DATETIME2(0) NOT NULL CONSTRAINT DF_products_created DEFAULT SYSDATETIME(),
    CONSTRAINT CK_products_price CHECK (price >= 0),
    CONSTRAINT CK_products_stock CHECK (stock_quantity >= 0),
    CONSTRAINT CK_products_warranty CHECK (warranty_months IS NULL OR warranty_months >= 0)
);
GO

CREATE TABLE carts (
    id         INT IDENTITY(1,1) CONSTRAINT PK_carts PRIMARY KEY,
    user_id    INT NOT NULL CONSTRAINT UQ_carts_user UNIQUE
                   CONSTRAINT FK_carts_users REFERENCES users(id),
    created_at DATETIME2(0) NOT NULL CONSTRAINT DF_carts_created DEFAULT SYSDATETIME()
);
GO

CREATE TABLE cart_items (
    id         INT IDENTITY(1,1) CONSTRAINT PK_cart_items PRIMARY KEY,
    cart_id    INT NOT NULL CONSTRAINT FK_cart_items_carts REFERENCES carts(id) ON DELETE CASCADE,
    product_id INT NOT NULL CONSTRAINT FK_cart_items_products REFERENCES products(id),
    quantity   INT NOT NULL CONSTRAINT DF_cart_items_qty DEFAULT 1,
    CONSTRAINT UQ_cart_items UNIQUE (cart_id, product_id),
    CONSTRAINT CK_cart_items_qty CHECK (quantity > 0)
);
GO

-- Заказ оформляется на одного продавца: корзина с товарами нескольких
-- продавцов при оформлении разбивается на несколько заказов
CREATE TABLE orders (
    id         INT IDENTITY(1,1) CONSTRAINT PK_orders PRIMARY KEY,
    user_id    INT NOT NULL CONSTRAINT FK_orders_users REFERENCES users(id),
    seller_id  INT NOT NULL CONSTRAINT FK_orders_sellers REFERENCES sellers(id),
    order_date DATETIME2(0) NOT NULL CONSTRAINT DF_orders_date DEFAULT SYSDATETIME(),
    status     NVARCHAR(30) NOT NULL CONSTRAINT DF_orders_status DEFAULT N'new',
    CONSTRAINT CK_orders_status CHECK (status IN (N'new', N'paid', N'shipped', N'delivered', N'cancelled'))
);
GO

CREATE TABLE order_items (
    id         INT IDENTITY(1,1) CONSTRAINT PK_order_items PRIMARY KEY,
    order_id   INT NOT NULL CONSTRAINT FK_order_items_orders REFERENCES orders(id) ON DELETE CASCADE,
    product_id INT NOT NULL CONSTRAINT FK_order_items_products REFERENCES products(id),
    quantity   INT NOT NULL,
    price      DECIMAL(12,2) NOT NULL,   -- цена на момент покупки
    CONSTRAINT CK_order_items_qty CHECK (quantity > 0),
    CONSTRAINT CK_order_items_price CHECK (price >= 0)
);
GO

CREATE TABLE payments (
    id             INT IDENTITY(1,1) CONSTRAINT PK_payments PRIMARY KEY,
    order_id       INT NOT NULL CONSTRAINT UQ_payments_order UNIQUE
                       CONSTRAINT FK_payments_orders REFERENCES orders(id) ON DELETE CASCADE,
    payment_date   DATETIME2(0) NULL,
    amount         DECIMAL(12,2) NOT NULL,
    payment_method NVARCHAR(50) NOT NULL,
    status         NVARCHAR(30) NOT NULL,
    CONSTRAINT CK_payments_amount CHECK (amount >= 0),
    CONSTRAINT CK_payments_method CHECK (payment_method IN (N'card', N'sbp', N'cash_on_delivery')),
    CONSTRAINT CK_payments_status CHECK (status IN (N'pending', N'paid', N'failed', N'refunded'))
);
GO

CREATE TABLE deliveries (
    id              INT IDENTITY(1,1) CONSTRAINT PK_deliveries PRIMARY KEY,
    order_id        INT NOT NULL CONSTRAINT UQ_deliveries_order UNIQUE
                        CONSTRAINT FK_deliveries_orders REFERENCES orders(id) ON DELETE CASCADE,
    address         NVARCHAR(255) NOT NULL,
    delivery_method NVARCHAR(50) NOT NULL,
    delivery_date   DATETIME2(0) NULL,
    status          NVARCHAR(30) NOT NULL,
    CONSTRAINT CK_deliveries_method CHECK (delivery_method IN (N'courier', N'pickup', N'post')),
    CONSTRAINT CK_deliveries_status CHECK (status IN (N'pending', N'in_transit', N'delivered', N'cancelled'))
);
GO

/* ---------- 2. Индексы для частых запросов ---------- */

CREATE INDEX IX_products_category ON products(category_id);
CREATE INDEX IX_products_seller   ON products(seller_id);
CREATE INDEX IX_products_name     ON products(name);
CREATE INDEX IX_orders_user       ON orders(user_id);
CREATE INDEX IX_orders_seller     ON orders(seller_id);
CREATE INDEX IX_order_items_order ON order_items(order_id);
GO

/* ---------- 3. Триггеры целостности ---------- */

-- Нельзя покупать товары у самого себя
CREATE TRIGGER trg_orders_no_self_purchase
ON orders
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN sellers s ON s.id = i.seller_id
        WHERE s.user_id = i.user_id
    )
    BEGIN
        RAISERROR (N'Нельзя оформить заказ на товары собственного магазина', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Товар в заказе должен принадлежать продавцу этого заказа
CREATE TRIGGER trg_order_items_same_seller
ON order_items
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN orders o   ON o.id = i.order_id
        JOIN products p ON p.id = i.product_id
        WHERE p.seller_id <> o.seller_id
    )
    BEGIN
        RAISERROR (N'Товар принадлежит другому продавцу, чем заказ', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

/* ---------- 4. Тестовые данные ---------- */
-- password_hash в тестовых данных - заглушки. Реальные хеши
-- (SHA-256 с солью) формирует приложение при регистрации.

INSERT INTO users (first_name, last_name, email, password_hash, phone) VALUES
(N'Иван',  N'Петров',  N'ivan@example.com',  N'HASH_PLACEHOLDER_1', N'+79001112233'),
(N'Мария', N'Сидорова', N'maria@example.com', N'HASH_PLACEHOLDER_2', N'+79002223344'),
(N'Алексей', N'Козлов', N'alex@example.com',  N'HASH_PLACEHOLDER_3', N'+79003334455'),
(N'Ольга', N'Смирнова', N'olga@example.com',  N'HASH_PLACEHOLDER_4', NULL);

-- Продавцами становятся пользователи 2 и 3
INSERT INTO sellers (user_id, shop_name, description) VALUES
(2, N'TechWorld', N'Смартфоны, ноутбуки и аксессуары'),
(3, N'GadgetHub', N'Гаджеты и умная электроника');

INSERT INTO categories (name, description) VALUES
(N'Смартфоны',   N'Мобильные телефоны'),
(N'Ноутбуки',    N'Портативные компьютеры'),
(N'Наушники',    N'Проводные и беспроводные наушники'),
(N'Планшеты',    N'Планшетные компьютеры'),
(N'Умные часы',  N'Смарт-часы и фитнес-браслеты');

INSERT INTO manufacturers (name, country) VALUES
(N'Apple',   N'США'),
(N'Samsung', N'Южная Корея'),
(N'Xiaomi',  N'Китай'),
(N'Lenovo',  N'Китай'),
(N'Sony',    N'Япония');

-- Товары 1-4 у продавца 1 (TechWorld), товары 5-8 у продавца 2 (GadgetHub)
INSERT INTO products (seller_id, category_id, manufacturer_id, name, description, price, stock_quantity, warranty_months) VALUES
(1, 1, 1, N'iPhone 15 128 ГБ',            N'Смартфон Apple',               79990, 10, 12),
(1, 2, 1, N'MacBook Air 13 M2',           N'Ноутбук Apple',               109990,  5, 12),
(1, 3, 5, N'Sony WH-1000XM5',             N'Беспроводные наушники',        29990, 15, 12),
(1, 4, 2, N'Samsung Galaxy Tab S9',       N'Планшет Samsung',              69990,  7, 12),
(2, 1, 3, N'Xiaomi 13T 256 ГБ',           N'Смартфон Xiaomi',              44990, 20, 12),
(2, 2, 4, N'Lenovo IdeaPad 5',            N'Ноутбук Lenovo',               64990,  8, 12),
(2, 5, 2, N'Samsung Galaxy Watch 6',      N'Умные часы Samsung',           24990, 12, 12),
(2, 3, 3, N'Xiaomi Buds 4 Pro',           N'Беспроводные наушники Xiaomi',  9990, 30,  6);

-- Корзина пользователя 1
INSERT INTO carts (user_id) VALUES (1);
INSERT INTO cart_items (cart_id, product_id, quantity) VALUES (1, 4, 1), (1, 8, 2);

-- Два заказа пользователя 1: по одному на каждого продавца
INSERT INTO orders (user_id, seller_id, status) VALUES
(1, 1, N'paid'),
(1, 2, N'new');

INSERT INTO order_items (order_id, product_id, quantity, price)
SELECT 1, id, 1, price FROM products WHERE id IN (1, 3);

INSERT INTO order_items (order_id, product_id, quantity, price)
SELECT 2, id, 2, price FROM products WHERE id = 5;

INSERT INTO payments (order_id, payment_date, amount, payment_method, status)
SELECT 1, SYSDATETIME(), SUM(quantity * price), N'card', N'paid'
FROM order_items WHERE order_id = 1;

INSERT INTO payments (order_id, payment_date, amount, payment_method, status)
SELECT 2, NULL, SUM(quantity * price), N'cash_on_delivery', N'pending'
FROM order_items WHERE order_id = 2;

INSERT INTO deliveries (order_id, address, delivery_method, delivery_date, status) VALUES
(1, N'г. Москва, ул. Ленина, д. 1, кв. 5', N'courier', NULL, N'pending'),
(2, N'г. Москва, ул. Ленина, д. 1, кв. 5', N'post',    NULL, N'pending');
GO

/* ---------- 5. Проверка ---------- */
SELECT s.shop_name, p.name, p.price
FROM products p JOIN sellers s ON s.id = p.seller_id
ORDER BY s.shop_name, p.name;
GO
