using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.Models
{
    public class ProductCoupon
    {
        public int Id { get; set; }
        public int BookId { get; set; } // Liên kết sản phẩm
        public int CouponId { get; set; } // Liên kết mã giảm giá

        public virtual Book Book { get; set; }
        public virtual Coupon Coupon { get; set; }
    }
}