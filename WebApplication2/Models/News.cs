using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class News
    {
        [Key]
        public int Id { get; set; }
        [Required, Display(Name = "Tiêu đề")]
        public string Title { get; set; }
        [Display(Name = "Hình ảnh")]
        public string ImagePath { get; set; }
        [Display(Name = "Mô tả ngắn")]
        public string ShortDescription { get; set; }
        [Display(Name = "Nội dung chi tiết")]
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}