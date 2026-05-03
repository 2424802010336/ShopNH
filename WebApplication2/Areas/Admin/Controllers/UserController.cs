using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;
using Microsoft.AspNet.Identity;
using PagedList;
using Microsoft.AspNet.Identity.Owin;
using System.Web;

namespace WebApplication2.Areas.Admin.Controllers
{
    // ViewModel dùng để hiển thị dữ liệu ra Table
    public class CustomerStatisticViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; } // Sẽ lấy từ UserName
        public string Email { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult ManageAdmins()
        {
            // Tìm ID của quyền Admin
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole == null) return View(new List<ApplicationUser>());

            var adminUserIds = adminRole.Users.Select(u => u.UserId).ToList();

            var admins = db.Users.Where(u => adminUserIds.Contains(u.Id)).ToList();

            return View(admins);
        }

        public ActionResult ManageCustomers(int? page, int? month, int? year)
        {
            // Thiết lập giá trị lọc mặc định nếu không có
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            // Lấy danh sách khách hàng và tính toán thống kê theo Tháng/Năm
            var customers = db.Users.Select(u => new CustomerStatisticViewModel
            {
                UserId = u.Id,
                FullName = u.UserName,
                Email = u.Email,
                // Tổng đơn hàng khớp với Tháng/Năm lọc
                OrderCount = db.Orders.Count(o => o.UserId == u.Id
                                            && o.OrderDate.Month == selectedMonth
                                            && o.OrderDate.Year == selectedYear),
                // Tổng chi tiêu khớp với Tháng/Năm lọc và trạng thái "Đã giao"
                TotalSpent = db.Orders
                    .Where(o => o.UserId == u.Id
                             && o.Status == "Đã giao"
                             && o.OrderDate.Month == selectedMonth
                             && o.OrderDate.Year == selectedYear)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToPagedList(pageNumber, pageSize);

            return View(customers);
        }

        public ActionResult ToggleLock(string id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                if (user.LockoutEndDateUtc > DateTime.UtcNow)
                    user.LockoutEndDateUtc = null;
                else
                    user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(100);

                db.SaveChanges();
            }
            return RedirectToAction("ManageCustomers");
        }

        public ActionResult Index()
        {
            var allUsers = db.Users.ToList();
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Admin");
            var adminIds = adminRole != null ? adminRole.Users.Select(u => u.UserId).ToList() : new List<string>();

            ViewBag.Admins = allUsers.Where(u => adminIds.Contains(u.Id)).ToList();
            ViewBag.NormalUsers = allUsers.Where(u => !adminIds.Contains(u.Id)).ToList();

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}