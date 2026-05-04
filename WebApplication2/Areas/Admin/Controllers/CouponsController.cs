using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;
using System.Data.Entity;
using PagedList;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. DANH SÁCH VOUCHER (Tìm kiếm tuyệt đối & Phân trang)
        public ActionResult Index(string searchString, int? page)
        {
            var coupons = db.Coupons.AsQueryable();

            // Tìm kiếm theo mã Voucher
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                // SỬA TẠI ĐÂY: Dùng == thay cho .Contains để tìm chính xác và ẩn các mã khác
                coupons = coupons.Where(c => c.Code == searchString);
            }

            // Sắp xếp theo ngày hết hạn (mới nhất lên đầu)
            var orderedList = coupons.OrderByDescending(c => c.ExpiryDate);

            // Phân trang 10 bản ghi
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.CurrentFilter = searchString;

            return View(orderedList.ToPagedList(pageNumber, pageSize));
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

        public ActionResult ApplySale(string searchString, string category, int? page)
        {
            // 1. Lấy danh sách Voucher cho Bước 1
            ViewBag.Coupons = db.Coupons.Where(c => c.IsActive && c.ExpiryDate >= DateTime.Now).ToList();

            var products = db.Books.AsQueryable();

            // 2. Lọc theo tên sản phẩm (nếu có)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                products = products.Where(p => p.Title.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            // 3. Lọc theo danh mục (nếu có)
            if (!string.IsNullOrEmpty(category))
            {
                // Vì trường Category của bạn lưu dạng chuỗi (VotYonex, GiayLining...)
                products = products.Where(p => p.Category.Contains(category));
                ViewBag.CurrentCategory = category;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(products.OrderByDescending(x => x.Id).ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExecuteApplySale(int couponId, int[] selectedProductIds)
        {
            if (selectedProductIds == null || selectedProductIds.Length == 0)
            {
                TempData["Error"] = "Vui lòng tích chọn ít nhất một sản phẩm!";
                return RedirectToAction("ApplySale");
            }

            try
            {
                var coupon = db.Coupons.Find(couponId);
                if (coupon == null) return HttpNotFound();

                foreach (var pId in selectedProductIds)
                {
                    var book = db.Books.Find(pId);
                    if (book != null)
                    {
                        // Cập nhật giá khuyến mãi (SalePrice) dựa trên % giảm của Coupon
                        // Lưu ý: Hậu hãy chắc chắn Model Book đã có cột SalePrice (kiểu decimal)
                        book.SalePrice = book.Price - (book.Price * coupon.DiscountPercent / 100);
                        db.Entry(book).State = EntityState.Modified;
                    }
                }

                db.SaveChanges();
                TempData["Success"] = "Đã áp dụng mã " + coupon.Code + " cho các sản phẩm thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction("ApplySale");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}