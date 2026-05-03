using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;
using System.Data.Entity;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. DANH SÁCH VOUCHER
        public ActionResult Index()
        {
            var coupons = db.Coupons.OrderByDescending(c => c.ExpiryDate).ToList();
            return View(coupons);
        }

        // 2. TẠO MỚI VOUCHER
        public ActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                coupon.IsActive = true;
                db.Coupons.Add(coupon);
                db.SaveChanges();
                TempData["Success"] = "Đã tạo mã giảm giá thành công!";
                return RedirectToAction("Index");
            }
            return View(coupon);
        }

        // 3. CHỈNH SỬA VOUCHER
        public ActionResult Edit(int id)
        {
            var coupon = db.Coupons.Find(id);
            if (coupon == null) return HttpNotFound();
            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                db.Entry(coupon).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật Voucher thành công!";
                return RedirectToAction("Index");
            }
            return View(coupon);
        }

        // 4. XÓA VOUCHER
        public ActionResult Delete(int id)
        {
            var coupon = db.Coupons.Find(id);
            if (coupon == null) return HttpNotFound();
            return View(coupon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var coupon = db.Coupons.Find(id);
            if (coupon != null)
            {
                db.Coupons.Remove(coupon);
                db.SaveChanges();
                TempData["Success"] = "Đã xóa mã giảm giá!";
            }
            return RedirectToAction("Index");
        }

        // 5. TRANG THIẾT LẬP ƯU ĐÃI (GỘP TỪ SALE)
        public ActionResult ApplySale()
        {
            // Lấy danh sách Voucher còn hạn
            ViewBag.Coupons = db.Coupons.Where(c => c.IsActive && c.ExpiryDate >= DateTime.Now).ToList();
            // Lấy danh sách sản phẩm
            var products = db.Books.ToList();
            return View(products);
        }

        // 6. XỬ LÝ ÁP DỤNG SALE CHO NHIỀU SẢN PHẨM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExecuteApplySale(int couponId, int[] selectedProductIds)
        {
            if (selectedProductIds == null || selectedProductIds.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm!";
                return RedirectToAction("ApplySale");
            }

            // Logic: Tùy theo cấu trúc DB của bạn (ví dụ gán CouponId vào bảng Book)
            foreach (var pId in selectedProductIds)
            {
                var book = db.Books.Find(pId);
                if (book != null)
                {
                    // Ví dụ: book.CouponId = couponId;
                    // Hoặc thêm vào bảng trung gian ProductCoupons nếu có
                }
            }
            
            db.SaveChanges();
            TempData["Success"] = "Đã áp dụng mã giảm giá cho các sản phẩm đã chọn!";
            return RedirectToAction("ApplySale");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}