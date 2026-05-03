using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SaleController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            ViewBag.Coupons = db.Coupons.Where(c => c.IsActive && c.ExpiryDate >= DateTime.Now).ToList();
            var products = db.Books.ToList();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplySale(int couponId, int[] selectedProductIds)
        {
            if (selectedProductIds == null || selectedProductIds.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm!";
                return RedirectToAction("Index");
            }

            // Xóa mã cũ và áp mã mới cho các sản phẩm đã chọn
            var oldLinks = db.ProductCoupons.Where(pc => selectedProductIds.Contains(pc.BookId)).ToList();
            db.ProductCoupons.RemoveRange(oldLinks);

            foreach (var pId in selectedProductIds)
            {
                db.ProductCoupons.Add(new ProductCoupon { BookId = pId, CouponId = couponId });
            }

            db.SaveChanges();
            TempData["Success"] = "Đã áp dụng ưu đãi cho các sản phẩm được chọn!";
            return RedirectToAction("Index");
        }
    }
}