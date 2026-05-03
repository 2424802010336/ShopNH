CREATE DATABASE ShopNH;
GO

USE ShopNH;
GO

-- Bảng lưu thông tin quyền (Admin, User...)
CREATE TABLE [dbo].[AspNetRoles] (
    [Id]   NVARCHAR (128) NOT NULL,
    [Name] NVARCHAR (256) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Bảng lưu thông tin tài khoản người dùng
CREATE TABLE [dbo].[AspNetUsers] (
    [Id]                   NVARCHAR (128) NOT NULL,
    [Email]                NVARCHAR (256) NULL,
    [EmailConfirmed]       BIT            NOT NULL,
    [PasswordHash]         NVARCHAR (MAX) NULL,
    [SecurityStamp]        NVARCHAR (MAX) NULL,
    [PhoneNumber]          NVARCHAR (MAX) NULL,
    [PhoneNumberConfirmed] BIT            NOT NULL,
    [TwoFactorEnabled]     BIT            NOT NULL,
    [LockoutEndDateUtc]    DATETIME       NULL,
    [LockoutEnabled]       BIT            NOT NULL,
    [AccessFailedCount]    INT            NOT NULL,
    [UserName]             NVARCHAR (256) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Bảng trung gian gán quyền cho người dùng (Nhiều - Nhiều)
CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] NVARCHAR (128) NOT NULL,
    [RoleId] NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);

-- Các bảng phụ trợ khác của Identity
CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [UserId]     NVARCHAR (128) NOT NULL,
    [ClaimType]  NVARCHAR (MAX) NULL,
    [ClaimValue] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] NVARCHAR (128) NOT NULL,
    [ProviderKey]   NVARCHAR (128) NOT NULL,
    [UserId]        NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_dbo.AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC, [UserId] ASC),
    CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);

-- Bảng Danh mục (Cầu lông)
CREATE TABLE [dbo].[Categories] (
    [Id]   INT            IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (100) NOT NULL,
    [Icon] NVARCHAR (50)  NULL,
    CONSTRAINT [PK_dbo.Categories] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Bảng Sản phẩm (Books/Dụng cụ)
-- Sửa lại bảng Books để liên kết với Categories chuẩn hơn
CREATE TABLE [dbo].[Books] (
    [Id]          INT             IDENTITY (1, 1) NOT NULL,
    [Title]       NVARCHAR (250)  NOT NULL,
    [ImagePath]   NVARCHAR (MAX)  NULL,
    [Price]       DECIMAL (18, 2) NOT NULL,
    [SalePrice]   DECIMAL (18, 2) NULL,
    [CategoryId]  INT             NOT NULL, -- Dùng ID thay vì NVARCHAR
    [Description] NVARCHAR (MAX)  NULL,
    [Stock]       INT             DEFAULT (0), -- Thêm tồn kho
    CONSTRAINT [PK_dbo.Books] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.Books_dbo.Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id])
);

-- Bảng Đơn hàng
CREATE TABLE [dbo].[Orders] (
    [Id]           INT             IDENTITY (1, 1) NOT NULL,
    [OrderDate]    DATETIME        NOT NULL,
    [CustomerName] NVARCHAR (100)  NOT NULL,
    [Phone]        NVARCHAR (20)   NOT NULL,
    [Address]      NVARCHAR (MAX)  NOT NULL,
    [TotalAmount]  DECIMAL (18, 2) NOT NULL,
    [Status]       NVARCHAR (50)   DEFAULT (N'Chờ xử lý'),
    [UserId]       NVARCHAR (128)  NULL,
    CONSTRAINT [PK_dbo.Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.Orders_dbo.AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id])
);

-- Bảng Chi tiết đơn hàng
CREATE TABLE [dbo].[OrderDetails] (
    [Id]        INT             IDENTITY (1, 1) NOT NULL,
    [OrderId]   INT             NOT NULL,
    [ProductId] INT             NOT NULL,
    [Quantity]  INT             NOT NULL,
    [UnitPrice] DECIMAL (18, 2) NOT NULL,
    CONSTRAINT [PK_dbo.OrderDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.OrderDetails_dbo.Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.OrderDetails_dbo.Books] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Books] ([Id])
);

-- Bảng Lưu thông tin mã giảm giá (Ưu đãi)
CREATE TABLE [dbo].[Coupons] (
    [Id]               INT             IDENTITY (1, 1) NOT NULL,
    [Code]             NVARCHAR (50)   NOT NULL, -- Ví dụ: KM2026, YONEX10
    [Description]      NVARCHAR (MAX)  NULL,     -- Mô tả chương trình
    [DiscountPercent]  INT             NOT NULL, -- Giảm theo % (Ví dụ: 10, 20, 50)
    [ExpiryDate]       DATETIME        NOT NULL, -- Ngày hết hạn
    [IsActive]         BIT             DEFAULT (1), -- Còn hoạt động hay không
    CONSTRAINT [PK_dbo.Coupons] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Bảng Bảng trung gian liên kết Sản phẩm và Mã giảm giá
CREATE TABLE [dbo].[ProductCoupons] (
    [ProductId] INT NOT NULL,
    [CouponId]  INT NOT NULL,
    CONSTRAINT [PK_dbo.ProductCoupons] PRIMARY KEY CLUSTERED ([ProductId] ASC, [CouponId] ASC),
    CONSTRAINT [FK_dbo.ProductCoupons_dbo.Books] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Books] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.ProductCoupons_dbo.Coupons] FOREIGN KEY ([CouponId]) REFERENCES [dbo].[Coupons] ([Id]) ON DELETE CASCADE
);