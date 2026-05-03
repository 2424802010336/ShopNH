using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Danh sách đơn hàng
        public ActionResult Index()
        {
            // Lấy đơn hàng mới nhất lên đầu
            var orders = db.Orders.OrderByDescending(o => o.OrderDate).ToList();
            return View(orders);
        }

        // Xem chi tiết đơn hàng
        public ActionResult Details(int id)
        {
            var order = db.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}