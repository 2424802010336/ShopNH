using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication2.Models;
using PagedList;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        // Sử dụng ApplicationDbContext để kết nối Database ShopNH
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // --- TRANG CHỦ SHOP NH ---
        public ActionResult Index()
        {
            // Lấy 4 sản phẩm mới nhất
            ViewBag.NewArrivals = db.Books.OrderByDescending(x => x.Id).Take(4).ToList();

            // Lấy 4 sản phẩm đang giảm giá
            ViewBag.SaleOff = db.Books.Where(x => x.SalePrice.HasValue && x.SalePrice > 0)
                                      .OrderByDescending(x => x.Id).Take(4).ToList();

            // Lấy dữ liệu cho các mục hiển thị riêng biệt trên trang chủ
            // Lưu ý: Category trong Database phải nhập đúng các mã này
            ViewBag.VotCauLong = db.Books.Where(x => x.Category.Contains("Vot")).Take(4).ToList();
            ViewBag.GiayCauLong = db.Books.Where(x => x.Category.Contains("Giay")).Take(4).ToList();
            ViewBag.AoCauLong = db.Books.Where(x => x.Category.Contains("Ao")).Take(4).ToList();
            ViewBag.QuanCauLong = db.Books.Where(x => x.Category.Contains("Quan")).Take(4).ToList();
            ViewBag.VayCauLong = db.Books.Where(x => x.Category.Contains("Vay")).Take(4).ToList();
            ViewBag.TuiCauLong = db.Books.Where(x => x.Category.Contains("Tui")).Take(4).ToList();
            ViewBag.BaloCauLong = db.Books.Where(x => x.Category.Contains("Balo")).Take(4).ToList();
            ViewBag.PhuKienCauLong = db.Books.Where(x => x.Category.Contains("PhuKien")).Take(4).ToList();

            return View();
        }

        // --- CHI TIẾT SẢN PHẨM ---
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var product = db.Books.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // --- TRANG TIN TỨC & KỸ THUẬT CẦU LÔNG ---
        public ActionResult News(int? page)
        {
            int pageSize = 12; // 12 bài mỗi trang (4 bài/dòng x 3 dòng)
            int pageNumber = (page ?? 1);

            var newsList = db.News.OrderByDescending(x => x.CreatedDate)
                          .ToPagedList(pageNumber, pageSize);

            return View(newsList);
        }

        // --- TRANG GIỚI THIỆU SHOP NH ---
        public ActionResult About()
        {
            ViewBag.Message = "Hệ thống cửa hàng cầu lông chính hãng ShopNH.";
            return View();
        }

        // --- TRANG LIÊN HỆ ---
        public ActionResult Contact()
        {
            ViewBag.Message = "Thông tin liên hệ ShopNH.";
            return View();
        }

        // Giải phóng bộ nhớ Database khi kết thúc yêu cầu
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