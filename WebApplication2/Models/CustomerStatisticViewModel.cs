using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.Models
{
    public class CustomerStatisticViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}