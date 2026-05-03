namespace WebApplication2.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using WebApplication2.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<WebApplication2.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(WebApplication2.Models.ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // 1. Tạo quyền Admin nếu chưa có
            if (!roleManager.RoleExists("Admin"))
            {
                roleManager.Create(new IdentityRole("Admin"));
            }

            // 2. Tạo tài khoản Admin mặc định
            var adminEmail = "admin@shopNH.com";
            var user = userManager.FindByEmail(adminEmail);
            if (user == null)
            {
                user = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
                var result = userManager.Create(user, "Admin@123");
                if (result.Succeeded)
                {
                    userManager.AddToRole(user.Id, "Admin");
                }
            }
        }
    }
}