using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using PagedList;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/News
        public ActionResult Index(string searchString, int? page)
        {
            var news = db.News.AsQueryable();

            // 1. Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                news = news.Where(s => s.Title.Contains(searchString));
            }

            // 2. Sắp xếp mới nhất lên đầu
            news = news.OrderByDescending(x => x.CreatedDate);

            // 3. Phân trang: 10 bản ghi mỗi trang
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(news.ToPagedList(pageNumber, pageSize));
        }

        // GET: Admin/News/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            News news = db.News.Find(id);
            if (news == null)
            {
                return HttpNotFound();
            }
            return View(news);
        }

        // GET: Admin/News/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sử dụng [ValidateInput(false)] nếu bạn dùng trình soạn thảo văn bản giàu (CKEditor/TinyMCE) cho trường Content
        public ActionResult Create(News news, HttpPostedFileBase uploadImage)
        {
            if (ModelState.IsValid)
            {
                // 1. Xử lý Upload ảnh
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    // Tạo tên file duy nhất bằng Guid để tránh trùng lặp
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadImage.FileName);
                    string folderPath = Server.MapPath("~/Content/Images/News/");
                    string path = Path.Combine(folderPath, fileName);

                    // Tự động tạo thư mục nếu chưa tồn tại trên Server
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    uploadImage.SaveAs(path);
                    // Lưu đường dẫn ảo vào database (dùng dấu / để tương thích web)
                    news.ImagePath = "/Content/Images/News/" + fileName;
                }

                // 2. Thiết lập các thông số mặc định
                news.CreatedDate = DateTime.Now;

                db.News.Add(news);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(news);
        }

        // GET: Admin/News/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            News news = db.News.Find(id);
            if (news == null)
            {
                return HttpNotFound();
            }
            return View(news);
        }

        // POST: Admin/News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(News news, HttpPostedFileBase uploadImage)
        {
            if (ModelState.IsValid)
            {
                // Truy vấn đối tượng cũ từ DB (không theo dõi) để lấy lại thông tin cũ nếu không thay đổi
                var existingNews = db.News.AsNoTracking().FirstOrDefault(x => x.Id == news.Id);

                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    // Xử lý thay ảnh mới
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadImage.FileName);
                    string folderPath = Server.MapPath("~/Content/Images/News/");
                    string path = Path.Combine(folderPath, fileName);

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // (Tùy chọn) Xóa ảnh cũ trên server để tránh rác bộ nhớ
                    if (existingNews != null && !string.IsNullOrEmpty(existingNews.ImagePath))
                    {
                        string oldPath = Server.MapPath("~" + existingNews.ImagePath);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    uploadImage.SaveAs(path);
                    news.ImagePath = "/Content/Images/News/" + fileName;
                }
                else
                {
                    // Giữ lại đường dẫn ảnh cũ nếu người dùng không chọn ảnh mới
                    news.ImagePath = existingNews?.ImagePath;
                }

                // Luôn giữ lại ngày tạo gốc để không bị cập nhật thành ngày sửa
                news.CreatedDate = existingNews?.CreatedDate ?? DateTime.Now;

                db.Entry(news).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(news);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            News news = db.News.Find(id);
            if (news == null) return HttpNotFound();
            return View(news);
        }

        // 2. Hàm thực hiện xóa khi bấm nút xác nhận
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            News news = db.News.Find(id);
            db.News.Remove(news);
            db.SaveChanges();
            return RedirectToAction("Index");
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