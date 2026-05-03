using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        // CÁC TRƯỜNG MỚI (Thêm vào nếu chưa có)
        public string Weight { get; set; }
        public string HandleLength { get; set; }
        public string SwingWeight { get; set; }
        public string SizeList { get; set; }
        public string FormType { get; set; }
    }
}