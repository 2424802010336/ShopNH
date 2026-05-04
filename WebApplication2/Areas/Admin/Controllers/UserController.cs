using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;
using Microsoft.AspNet.Identity;
using PagedList;
using System.Data.Entity;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using System.Web;

namespace WebApplication2.Areas.Admin.Controllers
{
    // ViewModel để hiển thị thống kê (Giữ nguyên để không lỗi build)
    public class CustomerStatisticViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. TRANG DANH SÁCH ADMIN
        public ActionResult ManageAdmins()
        {
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole == null) return View(new List<ApplicationUser>());

            var adminUserIds = adminRole.Users.Select(u => u.UserId).ToList();
            var admins = db.Users.Where(u => adminUserIds.Contains(u.Id)).ToList();

            return View(admins);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // 2. Xử lý lưu Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAdmin(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };

                var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user.Id, "Admin");
                    TempData["Success"] = "Tạo Admin thành công!";
                    return RedirectToAction("ManageAdmins");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            return View("Create", model);
        }

        // 3. XỬ LÝ NÚT KHÓA / MỞ KHÓA
        public ActionResult ToggleLock(string id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                // Logic khóa: Nếu đang bị khóa thì mở, nếu không thì khóa 100 năm
                if (user.LockoutEndDateUtc > DateTime.UtcNow)
                {
                    user.LockoutEndDateUtc = null;
                }
                else
                {
                    user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(100);
                }
                db.SaveChanges();
            }

            // Kiểm tra nếu là Admin thì quay về trang Admin, nếu là Khách thì về trang Khách
            bool isAdmin = db.Roles.Any(r => r.Name == "Admin" && r.Users.Any(u => u.UserId == id));
            if (isAdmin)
            {
                return RedirectToAction("ManageAdmins");
            }
            return RedirectToAction("ManageCustomers");
        }

        // 4. QUẢN LÝ KHÁCH HÀNG (Giữ lại để không lỗi link)
        public ActionResult ManageCustomers(int? month, int? year, string searchString, int? page)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            var customerRole = db.Roles.FirstOrDefault(r => r.Name == "Customer");
            string customerRoleId = customerRole?.Id ?? "";

            var customersQuery = db.Users.Where(u => u.Roles.Any(r => r.RoleId == customerRoleId)).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                customersQuery = customersQuery.Where(u => u.Email.Contains(searchString) || u.UserName.Contains(searchString));
            }

            var statistics = customersQuery.ToList().Select(u => new CustomerStatisticViewModel
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                OrderCount = db.Orders.Count(o => o.UserId == u.Id && o.OrderDate.Month == selectedMonth && o.OrderDate.Year == selectedYear),
                TotalSpent = db.Orders.Where(o => o.UserId == u.Id && o.OrderDate.Month == selectedMonth && o.OrderDate.Year == selectedYear).Sum(o => (decimal?)o.TotalAmount) ?? 0
            }).ToList();

            ViewBag.GrandTotal = statistics.Sum(s => s.TotalSpent);
            return View(statistics.ToPagedList(page ?? 1, 10));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}