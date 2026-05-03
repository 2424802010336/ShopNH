using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models;
using Microsoft.AspNet.Identity;

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

        // 2. LẤY DỮ LIỆU CHO MINI CART (DROPDOWN)
        public ActionResult GetMiniCart()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return PartialView("_MiniCartPartial", cart);
        }

        // 3. THÊM VÀO GIỎ HÀNG BẰNG AJAX (Dùng cho trang chủ/chi tiết)
        [HttpPost]
        public JsonResult AddToCartAjax(int id)
        {
            var book = db.Books.Find(id);
            if (book == null) return Json(new { success = false });

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = id,
                    Title = book.Title,
                    Quantity = 1,
                    Price = book.SalePrice ?? book.Price,
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

        // 4. AJAX: CẬP NHẬT SỐ LƯỢNG (Dùng chung cho nút + / - ở cả trang chính và Mini Cart)
        [HttpPost]
        public JsonResult UpdateQuantity(int id, int delta)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null) return Json(new { success = false });

            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null)
            {
                item.Quantity += delta;
                // Nếu giảm xuống 0 hoặc nhỏ hơn thì xóa sản phẩm khỏi danh sách
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            Session["Cart"] = cart;

            // Trả về JSON để JavaScript cập nhật giao diện ngay lập tức
            return Json(new
            {
                success = true,
                newQty = item?.Quantity ?? 0, // Trả về 0 nếu item đã bị xóa
                itemTotal = item != null ? (item.Price * item.Quantity).ToString("N0") + " đ" : "0 đ",
                cartTotal = cart.Sum(x => x.Price * x.Quantity).ToString("N0") + " đ",
                cartCount = cart.Sum(x => x.Quantity)
            });
        }

        // 5. XÓA SẢN PHẨM KHỎI GIỎ HÀNG (Dùng cho nút Xóa/Thùng rác)
        public ActionResult RemoveFromCart(int id)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductId == id);
                if (item != null) cart.Remove(item);
                Session["Cart"] = cart;
            }
            return RedirectToAction("Index");
        }

        // 6. TRANG THANH TOÁN (Yêu cầu đăng nhập)
        [Authorize]
        public ActionResult Checkout()
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any()) return RedirectToAction("Index", "Home");
            return View(cart);
        }

        // 7. XỬ LÝ ĐẶT HÀNG (Lưu vào Database)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessOrder(string CustomerName, string Phone, string Address, string Note)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any()) return RedirectToAction("Index", "Home");

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new Order
                    {
                        UserId = User.Identity.GetUserId(),
                        CustomerName = CustomerName,
                        Phone = Phone,
                        Address = Address,
                        Note = Note,
                        OrderDate = DateTime.Now,
                        Status = "Chờ xử lý",
                        TotalAmount = cart.Sum(x => x.Price * x.Quantity)
                    };

                    db.Orders.Add(order);
                    db.SaveChanges();

                    foreach (var item in cart)
                    {
                        db.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price
                        });
                    }
                    db.SaveChanges();
                    transaction.Commit();

                    // Xóa giỏ hàng sau khi đặt thành công
                    Session["Cart"] = null;

                    return View("OrderSuccess");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return RedirectToAction("Index");
                }
            }
        }
    }
}