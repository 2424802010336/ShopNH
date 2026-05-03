using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace WebApplication2.Models
{
    // Class người dùng mở rộng
    public class ApplicationUser : IdentityUser
    {
        // Thuộc tính để UserController kiểm tra quyền nhanh (nếu bạn dùng cột Role)
        public string Role { get; set; }

        // Thuộc tính khóa tài khoản thủ công (Nếu bạn dùng IsLocked thay vì LockoutEndDateUtc)
        public bool IsLocked { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note: authenticationType phải khớp với cái định nghĩa trong CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    // Lớp quản lý Database
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
            // Tự động cập nhật Database nếu có thay đổi Model trong quá trình phát triển
            Database.SetInitializer(new ApplicationDbInitializer());
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        // --- DANH SÁCH CÁC BẢNG TRONG HỆ THỐNG ---
        public virtual DbSet<Book> Books { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<Coupon> Coupons { get; set; }
        public virtual DbSet<News> News { get; set; }

        // Bảng trung gian áp dụng Voucher cho từng sản phẩm
        public virtual DbSet<ProductCoupon> ProductCoupons { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bạn có thể đổi tên bảng Identity tại đây để DB sạch hơn
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole>().ToTable("UserRoles");
        }
    }

    // Lớp khởi tạo dữ liệu mẫu (Seed Data)
    public class ApplicationDbInitializer : DropCreateDatabaseIfModelChanges<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            // Tạo Role Admin mặc định nếu chưa có
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            if (!roleManager.RoleExists("Admin"))
            {
                roleManager.Create(new IdentityRole("Admin"));
            }

            if (!roleManager.RoleExists("Customer"))
            {
                roleManager.Create(new IdentityRole("Customer"));
            }

            base.Seed(context);
        }
    }
}