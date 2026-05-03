using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO; // Thư viện để xử lý Path và Directory
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/News
        public ActionResult Index()
        {
            // Sắp xếp tin mới nhất lên đầu
            var news = db.News.OrderByDescending(x => x.CreatedDate).ToList();
            return View(news);
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
        public ActionResult Create(News news, HttpPostedFileBase uploadImage)
        {
            if (ModelState.IsValid)
            {
                // Xử lý Upload ảnh
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadImage.FileName);
                    string folderPath = Server.MapPath("~/Content/Images/News/");
                    string path = Path.Combine(folderPath, fileName);

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    uploadImage.SaveAs(path);
                    news.ImagePath = "/Content/Images/News/" + fileName;
                }

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
                // Tìm đối tượng cũ trong DB để tránh mất dữ liệu ImagePath và CreatedDate
                var existingNews = db.News.AsNoTracking().FirstOrDefault(x => x.Id == news.Id);

                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    // Nếu chọn ảnh mới -> Xử lý upload
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadImage.FileName);
                    string folderPath = Server.MapPath("~/Content/Images/News/");
                    string path = Path.Combine(folderPath, fileName);

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    uploadImage.SaveAs(path);
                    news.ImagePath = "/Content/Images/News/" + fileName;
                }
                else
                {
                    // Nếu không chọn ảnh mới -> Giữ lại ảnh cũ
                    news.ImagePath = existingNews.ImagePath;
                }

                // Luôn giữ lại ngày tạo gốc
                news.CreatedDate = existingNews.CreatedDate;

                db.Entry(news).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(news);
        }

        // GET: Admin/News/Delete/5
        public ActionResult Delete(int? id)
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

        // POST: Admin/News/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            News news = db.News.Find(id);

            // (Tùy chọn) Xóa file ảnh vật lý trong thư mục để tiết kiệm bộ nhớ
            if (!string.IsNullOrEmpty(news.ImagePath))
            {
                string fullPath = Server.MapPath("~" + news.ImagePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

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