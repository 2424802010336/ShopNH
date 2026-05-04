using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Models;
using Microsoft.AspNet.Identity;
using System.Data.Entity;

namespace WebApplication2.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. TRANG GIỎ HÀNG CHÍNH
        public ActionResult Index()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View(cart);
        }

        // 2. LẤY DỮ LIỆU CHO MINI CART (DROPDOWN TRÊN NAVBAR)
        public ActionResult GetMiniCart()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return PartialView("_MiniCartPartial", cart);
        }

        // 3. THÊM VÀO GIỎ HÀNG BẰNG AJAX
        [HttpPost]
        public JsonResult AddToCartAjax(int id)
        {
            try
            {
                var book = db.Books.Find(id);
                if (book == null) return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

                var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == id);

                if (item == null)
                {
                    cart.Add(new CartItem
                    {
                        ProductId = id,
                        Title = book.Title,
                        Quantity = 1,
                        // Ưu tiên lấy giá khuyến mãi nếu có
                        Price = book.SalePrice.HasValue && book.SalePrice > 0 ? book.SalePrice.Value : book.Price,
                        ImagePath = book.ImagePath
                    });
                }
                else
                {
                    item.Quantity++;
                }

                Session["Cart"] = cart;

                return Json(new
                {
                    success = true,
                    newCount = cart.Sum(x => x.Quantity),
                    totalPrice = cart.Sum(x => x.Price * x.Quantity).ToString("N0") + " đ"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. CẬP NHẬT SỐ LƯỢNG BẰNG AJAX (+ / -)
        [HttpPost]
        public JsonResult UpdateQuantity(int id, int delta)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null) return Json(new { success = false });

            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null)
            {
                item.Quantity += delta;

                // Nếu số lượng giảm xuống <= 0 thì xóa khỏi giỏ
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            Session["Cart"] = cart;

            return Json(new
            {
                success = true,
                newQty = item?.Quantity ?? 0,
                itemTotal = item != null ? (item.Price * item.Quantity).ToString("N0") + " đ" : "0 đ",
                cartTotal = cart.Sum(x => x.Price * x.Quantity).ToString("N0") + " đ",
                cartCount = cart.Sum(x => x.Quantity)
            });
        }

        // 5. XÓA SẢN PHẨM KHỎI GIỎ HÀNG
        public ActionResult RemoveFromCart(int id)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductId == id);
                if (item != null)
                {
                    cart.Remove(item);
                }
                Session["Cart"] = cart;
            }
            return RedirectToAction("Index");
        }

        // 6. TRANG NHẬP THÔNG TIN THANH TOÁN
        [Authorize]
        public ActionResult Checkout()
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }
            return View(cart);
        }

        // 7. XỬ LÝ ĐẶT HÀNG (LƯU DATABASE)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessOrder(string CustomerName, string Phone, string Address, string Note)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // Sử dụng Transaction để đảm bảo dữ liệu được lưu đủ hoặc không lưu gì nếu lỗi
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tạo hóa đơn mới
                    var order = new Order
                    {
                        UserId = User.Identity.GetUserId(),
                        CustomerName = CustomerName,
                        Phone = Phone,
                        Address = Address,
                        Note = Note,
                        OrderDate = DateTime.Now,
                        Status = "Chờ duyệt", // Trạng thái mặc định
                        TotalAmount = cart.Sum(x => x.Price * x.Quantity)
                    };

                    db.Orders.Add(order);
                    db.SaveChanges(); // Lưu để lấy ID của Order vừa tạo

                    // 2. Lưu chi tiết hóa đơn
                    foreach (var item in cart)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price
                        };
                        db.OrderDetails.Add(orderDetail);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    // 3. Xóa giỏ hàng sau khi đặt thành công
                    Session["Cart"] = null;

                    return RedirectToAction("OrderSuccess");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại!");
                    return View("Checkout", cart);
                }
            }
        }

        // 8. TRANG THÔNG BÁO THÀNH CÔNG
        public ActionResult OrderSuccess()
        {
            return View();
        }

        [Authorize]
        public ActionResult MyOrders()
        {
            // Lấy ID của người dùng hiện tại
            string userId = User.Identity.GetUserId();

            // Lấy danh sách đơn hàng của người đó, sắp xếp mới nhất lên đầu
            var orders = db.Orders
                           .Where(o => o.UserId == userId)
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();

            return View(orders);
        }

        // Trang chi tiết đơn hàng cho khách hàng
        [Authorize]
        public ActionResult MyOrderDetails(int id)
        {
            string userId = User.Identity.GetUserId();

            // Tìm đơn hàng của đúng người dùng đó
            var order = db.Orders
                          .Include(o => o.OrderDetails.Select(d => d.Book))
                          .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Reorder(int id)
        {
            string userId = User.Identity.GetUserId();

            // 1. Tìm đơn hàng cũ (phải thuộc về user này và có trạng thái Đã hủy)
            var oldOrder = db.Orders
                             .Include(o => o.OrderDetails)
                             .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (oldOrder == null)
            {
                return HttpNotFound();
            }

            try
            {
                // 2. Tạo đối tượng đơn hàng mới
                var newOrder = new Order
                {
                    UserId = userId,
                    CustomerName = oldOrder.CustomerName,
                    Phone = oldOrder.Phone,
                    Address = oldOrder.Address,
                    Note = "Đặt lại từ đơn hàng #" + oldOrder.Id,
                    OrderDate = DateTime.Now,
                    TotalAmount = oldOrder.TotalAmount,
                    Status = "Chờ xử lý" // Reset trạng thái về ban đầu
                };

                // 3. Sao chép chi tiết đơn hàng
                foreach (var detail in oldOrder.OrderDetails)
                {
                    var newDetail = new OrderDetail
                    {
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    };
                    newOrder.OrderDetails.Add(newDetail);
                }

                db.Orders.Add(newOrder);
                db.SaveChanges();

                // Trả về thông báo thành công và ID đơn mới
                return Json(new { success = true, newOrderId = newOrder.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi đặt hàng lại: " + ex.Message });
            }
        }


        // Giải phóng bộ nhớ
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