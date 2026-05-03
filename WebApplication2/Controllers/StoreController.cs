using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using PagedList;

namespace WebApplication2.Controllers
{
    public class StoreController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        /// <summary>
        /// Trang danh mục sản phẩm hỗ trợ lọc AJAX đa năng
        /// </summary>
        /// <param name="brand">Chuỗi thương hiệu (VD: "Yonex,Lining")</param>
        /// <param name="group">Mã nhóm sản phẩm (VD: "Vot", "Giay")</param>
        /// <param name="sort">Kiểu sắp xếp (VD: "price_asc")</param>
        /// <param name="minPrice">Giá tối thiểu</param>
        /// <param name="maxPrice">Giá tối đa</param>
        /// <param name="page">Số trang hiện tại</param>
        /// <param name="spec">Chuỗi thông số kỹ thuật (VD: "4U,39,Nam")</param>
        public ActionResult Category(string brand, string group, string sort, decimal? minPrice, decimal? maxPrice, int? page, string spec)
        {
            // Bắt đầu truy vấn từ bảng Books (Sản phẩm)
            var products = db.Books.AsQueryable();

            // 1. LỌC THEO NHÓM (GROUP) - Đồng bộ với Mega Menu và Sidebar
            if (!string.IsNullOrEmpty(group))
            {
                if (group == "PhuKien")
                {
                    // Nếu là phụ kiện chung, lấy tất cả các mã danh mục thuộc nhóm phụ kiện
                    var listPhuKienCodes = new[] { "TuiVot", "Balo", "Ao", "Quan", "Vay", "PhuKien", "Soc", "Cuoc", "Vo", "QuanCan", "QuaCau", "ChanMoHoi" };
                    products = products.Where(p => listPhuKienCodes.Any(code => p.Category.Contains(code)));
                }
                else
                {
                    // Lọc sản phẩm bắt đầu bằng mã nhóm (VD: Category là "VotYonex" sẽ khớp với group "Vot")
                    products = products.Where(p => p.Category.StartsWith(group));
                }
            }

            // 2. LỌC THEO THƯƠNG HIỆU (Hỗ trợ chọn nhiều checkbox cùng lúc)
            if (!string.IsNullOrEmpty(brand))
            {
                var brandList = brand.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                // Lọc sản phẩm có cột Category chứa bất kỳ tên hãng nào trong danh sách
                products = products.Where(p => brandList.Any(b => p.Category.Contains(b)));
            }

            // 3. LỌC THEO THÔNG SỐ KỸ THUẬT (SPEC) - Tự động nhận diện Size, Trọng lượng, Form...
            if (!string.IsNullOrEmpty(spec))
            {
                var specList = spec.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in specList)
                {
                    string trimSpec = s.Trim();
                    // Tìm kiếm từ khóa 'spec' trong tất cả các cột kỹ thuật liên quan
                    products = products.Where(p =>
                        (p.Weight != null && p.Weight.Contains(trimSpec)) ||
                        (p.HandleLength != null && p.HandleLength.Contains(trimSpec)) ||
                        (p.SwingWeight != null && p.SwingWeight.Contains(trimSpec)) ||
                        (p.SizeList != null && p.SizeList.Contains(trimSpec)) ||
                        (p.FormType != null && p.FormType.Contains(trimSpec)) ||
                        (p.Description != null && p.Description.Contains(trimSpec))
                    );
                }
            }

            // 4. LỌC THEO GIÁ (Ưu tiên lấy SalePrice nếu có, nếu không thì lấy Price)
            if (minPrice.HasValue)
            {
                products = products.Where(p => (p.SalePrice ?? p.Price) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => (p.SalePrice ?? p.Price) <= maxPrice.Value);
            }

            // 5. SẮP XẾP DỮ LIỆU
            switch (sort)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.SalePrice ?? p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.SalePrice ?? p.Price);
                    break;
                default:
                    // Mặc định hiện sản phẩm mới nhất lên đầu
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            // 6. XỬ LÝ PHÂN TRANG
            int pageSize = 12; // Số sản phẩm trên 1 trang
            int pageNumber = (page ?? 1);

            // Gửi các tham số lọc về ViewBag để Sidebar giữ trạng thái Checkbox/Radio
            ViewBag.CurrentGroup = group;
            ViewBag.CurrentBrand = brand;
            ViewBag.CurrentSpec = spec;
            ViewBag.CurrentSort = sort;

            // Chuyển đổi mã Group thành tên tiếng Việt hiển thị trên tiêu đề trang
            ViewBag.GroupName = GetFriendlyGroupName(group);

            // Trả về View cùng với danh sách sản phẩm đã được phân trang
            return View(products.ToPagedList(pageNumber, pageSize));
        }

        /// <summary>
        /// Hàm chuyển đổi mã danh mục thành tên hiển thị thân thiện
        /// </summary>
        private string GetFriendlyGroupName(string group)
        {
            if (string.IsNullOrEmpty(group)) return "Tất cả sản phẩm";
            switch (group)
            {
                case "Vot": return "Vợt Cầu Lông";
                case "Giay": return "Giày Cầu Lông";
                case "Ao": return "Áo Cầu Lông";
                case "Quan": return "Quần Cầu Lông";
                case "Vay": return "Váy Cầu Lông";
                case "TuiVot": return "Túi Vợt Cầu Lông";
                case "Balo": return "Balo Cầu Lông";
                case "PhuKien": return "Phụ Kiện Cầu Lông";
                default: return group;
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