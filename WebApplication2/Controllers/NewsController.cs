using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using PagedList;

namespace WebApplication2.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. TRANG DANH SÁCH TIN TỨC (Thay thế cho Home/News)
        // URL: /News/Index hoặc /News
        public ActionResult Index(int? page)
        {
            // Lấy danh sách tin tức từ DB, sắp xếp mới nhất lên đầu
            var newsList = db.News.OrderByDescending(x => x.CreatedDate).ToList();

            int pageSize = 8;
            int pageNumber = (page ?? 1);

            // Trả về View tại /Views/News/Index.cshtml
            return View(newsList.ToPagedList(pageNumber, pageSize));
        }

        // 2. TRANG CHI TIẾT TIN TỨC
        // URL: /News/Details/5
        public ActionResult Details(int id)
        {
            var news = db.News.Find(id);
            if (news == null)
            {
                return HttpNotFound();
            }

            // Lấy thêm 3 bài viết liên quan (bỏ qua bài hiện tại)
            ViewBag.RelatedNews = db.News.Where(x => x.Id != id).Take(3).ToList();

            return View(news);
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