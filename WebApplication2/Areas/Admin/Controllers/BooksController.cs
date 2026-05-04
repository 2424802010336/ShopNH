using System;
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
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. TRANG TỔNG QUAN (DASHBOARD)
        public ActionResult AdminDashboard()
        {
            // 1. Thống kê cơ bản
            ViewBag.TotalProducts = db.Books.Count();
            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.TotalOrders = db.Orders.Count();

            // 2. TÍNH DOANH THU: Khớp chính xác với chữ "Đã giao" (có dấu) trong ảnh của bạn
            // Nếu vẫn hiện 0đ, hãy chắc chắn cột TotalAmount trong DB của đơn #1 có giá trị 5,000,000
            ViewBag.TotalRevenue = db.Orders
                .Where(o => o.Status == "Đã giao" || o.Status == "Hoàn thành")
                .Select(o => (decimal?)o.TotalAmount)
                .Sum() ?? 0m;

            // 3. ĐẾM TRẠNG THÁI: Chỉ giữ lại 3 mục theo yêu cầu
            ViewBag.Status_ChoDuyet = db.Orders.Count(o => o.Status == "Chờ duyệt" || o.Status == "Đang xử lý");
            ViewBag.Status_DaGiao = db.Orders.Count(o => o.Status == "Đã giao" || o.Status == "Hoàn thành");
            ViewBag.Status_DaHuy = db.Orders.Count(o => o.Status == "Đã hủy");

            // 4. PHÂN BỔ DANH MỤC: Đếm sản phẩm theo loại
            ViewBag.Cat_Vot = db.Books.Count(b => b.Category.Contains("Vot"));
            ViewBag.Cat_Giay = db.Books.Count(b => b.Category.Contains("Giay"));
            ViewBag.Cat_AoQuan = db.Books.Count(b => b.Category.Contains("Ao") || b.Category.Contains("Quan"));
            ViewBag.Cat_PhuKien = db.Books.Count(b => b.Category.Contains("PhuKien") || b.Category.Contains("Can"));

            return View();
        }

        // 2. DANH SÁCH SẢN PHẨM (INDEX)
        public ActionResult Index(string searchString, string category, int? page)
        {
            // Tối ưu hiệu suất đọc dữ liệu
            var books = db.Books.AsNoTracking().AsQueryable();

            // Bộ lọc tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                books = books.Where(s => s.Title.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                books = books.Where(s => s.Category.Contains(category));
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Lưu trạng thái lọc để hiển thị lại trên View
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategory = category;

            return View(books.OrderByDescending(x => x.Id).ToPagedList(pageNumber, pageSize));
        }

        // 3. THÊM MỚI SẢN PHẨM
        public ActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Book book, HttpPostedFileBase uploadImage, string CategoryGroup, string Brand, string SubCategory)
        {
            if (ModelState.IsValid)
            {
                // Xử lý gộp mã danh mục theo logic nghiệp vụ
                book.Category = ProcessCategory(CategoryGroup, Brand, SubCategory);

                // Xử lý tải ảnh lên
                book.ImagePath = HandleImageUpload(uploadImage);

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
                var bookInDb = db.Books.Find(book.Id);
                if (bookInDb == null) return HttpNotFound();

                // Cập nhật thông tin chi tiết
                UpdateBookDetails(bookInDb, book);

                // Cập nhật lại Category nếu có thay đổi từ Form
                if (!string.IsNullOrEmpty(CategoryGroup))
                {
                    bookInDb.Category = ProcessCategory(CategoryGroup, Brand, SubCategory);
                }

                // Chỉ cập nhật ảnh mới nếu người dùng có tải ảnh lên
                string newImagePath = HandleImageUpload(uploadImage);
                if (newImagePath != "~/Content/Images/no-image.png")
                {
                    bookInDb.ImagePath = newImagePath;
                }

                db.Entry(bookInDb).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(book);
        }

        // 5. CHI TIẾT & XÓA (Gộp logic tìm kiếm cơ bản)
        public ActionResult Details(int? id) => GetBookView(id);
        public ActionResult Delete(int? id) => GetBookView(id);

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

        #region Helper Methods (Các phương thức hỗ trợ tái sử dụng code)

        // Logic xử lý chuỗi danh mục
        private string ProcessCategory(string group, string brand, string sub)
        {
            if (group == "PhuKien") return sub;
            return group + brand;
        }

        // Logic xử lý file ảnh vật lý
        private string HandleImageUpload(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength <= 0) return "~/Content/Images/no-image.png";

            string fileName = Path.GetFileNameWithoutExtension(file.FileName);
            string extension = Path.GetExtension(file.FileName);
            // Tạo tên file duy nhất bằng timestamp
            fileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";

            string folderPath = Server.MapPath("~/Content/Images/");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string path = Path.Combine(folderPath, fileName);
            file.SaveAs(path);

            return "~/Content/Images/" + fileName;
        }

        // Cập nhật các thuộc tính sản phẩm
        private void UpdateBookDetails(Book target, Book source)
        {
            target.Title = source.Title;
            target.Price = source.Price;
            target.SalePrice = source.SalePrice;
            target.Description = source.Description;
            target.Weight = source.Weight;
            target.SwingWeight = source.SwingWeight;
            target.HandleLength = source.HandleLength;
            target.SizeList = source.SizeList;
            target.FormType = source.FormType;
        }

        private ActionResult GetBookView(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Book book = db.Books.Find(id);
            if (book == null) return HttpNotFound();
            return View(book);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}