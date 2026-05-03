using System;

namespace WebApplication2.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice
        {
            get { return Price * Quantity; }
        }


    }
}