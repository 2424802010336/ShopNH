using System;
using System.Collections.Generic;

namespace WebApplication2.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }

        // Đảm bảo có 4 dòng này để khớp với logic thanh toán
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Note { get; set; }
        public string UserId { get; set; }

        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}