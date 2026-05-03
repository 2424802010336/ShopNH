using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using System.IO;
using PagedList;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. TRANG TỔNG QUAN (DASHBOARD)
        public ActionResult AdminDashboard()
        {
            ViewBag.TotalProducts = db.Books.Count();
            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.TotalOrders = db.Orders.Count();
            ViewBag.TotalRevenue = db.Orders.Where(o => o.Status == "Đã giao").Sum(o => (decimal?)o.TotalAmount) ?? 0;
            return View();
        }

        // 2. DANH SÁCH SẢN PHẨM (INDEX)
        public ActionResult Index(string searchString, string category, int? page)
        {
            var books = db.Books.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(s => s.Title.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                // Sử dụng Contains để khi chọn "Vot" sẽ ra cả "VotYonex", "VotVictor"...
                books = books.Where(s => s.Category.Contains(category));
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(books.OrderByDescending(x => x.Id).ToPagedList(pageNumber, pageSize));
        }

        // 3. THÊM MỚI SẢN PHẨM
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Book book, HttpPostedFileBase uploadImage, string CategoryGroup, string Brand, string SubCategory, string Target, string Weight, string SwingWeight, string SizeList, string FormType)
        {
            if (ModelState.IsValid)
            {
                // 1. Xử lý gộp mã danh mục để bộ lọc AJAX bên ngoài chạy đúng
                if (CategoryGroup == "PhuKien")
                {
                    book.Category = SubCategory; // Ví dụ: "QuanCan"
                }
                else
                {
                    book.Category = CategoryGroup + Brand; // Ví dụ: "VotYonex"
                }

                // 2. Gộp thông số vào Description hoặc cột Spec (Nếu database bạn có cột riêng thì gán thẳng)
                // Ví dụ: lưu size và form chân vào Description để tìm kiếm dễ hơn
                if (!string.IsNullOrEmpty(SizeList))
                    book.Description += "\nSize: " + SizeList;
                if (!string.IsNullOrEmpty(FormType))
                    book.Description += "\nForm: " + FormType;

                // XỬ LÝ HÌNH ẢNH
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(uploadImage.FileName);
                    string extension = Path.GetExtension(uploadImage.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yymmddhhmmssfff") + extension;

                    string folderPath = Server.MapPath("~/Content/Images/");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string path = Path.Combine(folderPath, fileName);
                    uploadImage.SaveAs(path);

                    book.ImagePath = "~/Content/Images/" + fileName;
                }
                else
                {
                    book.ImagePath = "~/Content/Images/no-image.png";
                }

                db.Books.Add(book);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(book);
        }

        // 4. CHỈNH SỬA SẢN PHẨM
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Book book = db.Books.Find(id);
            if (book == null) return HttpNotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Book book, HttpPostedFileBase uploadImage, string CategoryGroup, string Brand, string SubCategory)
        {
            if (ModelState.IsValid)
            {
                // Tìm bản ghi gốc trong DB để tránh lỗi mất dữ liệu các trường không có trong Form
                var bookInDb = db.Books.Find(book.Id);
                if (bookInDb == null) return HttpNotFound();

                // Cập nhật thông tin cơ bản
                bookInDb.Title = book.Title;
                bookInDb.Price = book.Price;
                bookInDb.SalePrice = book.SalePrice;
                bookInDb.Description = book.Description;
                bookInDb.Weight = book.Weight;
                bookInDb.SwingWeight = book.SwingWeight;
                bookInDb.HandleLength = book.HandleLength;
                bookInDb.SizeList = book.SizeList;
                bookInDb.FormType = book.FormType;

                // Cập nhật lại danh mục đồng bộ
                if (CategoryGroup == "PhuKien")
                {
                    bookInDb.Category = SubCategory;
                }
                else if (!string.IsNullOrEmpty(CategoryGroup) && !string.IsNullOrEmpty(Brand))
                {
                    bookInDb.Category = CategoryGroup + Brand;
                }

                // Xử lý ảnh mới nếu có upload
                if (uploadImage != null && uploadImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(uploadImage.FileName);
                    string extension = Path.GetExtension(uploadImage.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yymmddhhmmssfff") + extension;

                    string folderPath = Server.MapPath("~/Content/Images/");
                    string path = Path.Combine(folderPath, fileName);
                    uploadImage.SaveAs(path);

                    bookInDb.ImagePath = "~/Content/Images/" + fileName;
                }

                db.Entry(bookInDb).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(book);
        }

        // 5. CHI TIẾT SẢN PHẨM
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Book book = db.Books.Find(id);
            if (book == null) return HttpNotFound();
            return View(book);
        }

        // 6. XÓA SẢN PHẨM
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Book book = db.Books.Find(id);
            if (book == null) return HttpNotFound();
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Book book = db.Books.Find(id);
            if (book != null)
            {
                db.Books.Remove(book);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}