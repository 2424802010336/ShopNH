using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using PagedList; // Thêm thư viện phân trang

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. DANH SÁCH ĐƠN HÀNG (Tích hợp Tìm kiếm và Phân trang)
        public ActionResult Index(string searchString, int? page)
        {
            var orders = db.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                int orderId;

                if (int.TryParse(searchString, out orderId))
                {
                    orders = orders.Where(o => o.Id == orderId);
                }
                else
                {
                    orders = orders.Where(o => false);
                }
            }

            var orderedList = orders.OrderByDescending(o => o.OrderDate);
            ViewBag.CurrentFilter = searchString;

            return View(orderedList.ToPagedList(page ?? 1, 10));
        }

        // 2. XEM CHI TIẾT ĐƠN HÀNG
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                          .Include(o => o.OrderDetails.Select(d => d.Book))
                          .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // 3. CẬP NHẬT TRẠNG THÁI (AJAX)
        [HttpPost]
        public JsonResult UpdateStatus(int id, string status)
        {
            try
            {
                var order = db.Orders.Find(id);
                if (order != null)
                {
                    order.Status = status;
                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. XÓA ĐƠN HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteOrder(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = db.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == id);
                    if (order != null)
                    {
                        db.OrderDetails.RemoveRange(order.OrderDetails);
                        db.Orders.Remove(order);
                        db.SaveChanges();
                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    return HttpNotFound();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return RedirectToAction("Index", new { error = "Không thể xóa đơn hàng này." });
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}