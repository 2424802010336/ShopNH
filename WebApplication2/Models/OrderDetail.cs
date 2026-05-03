namespace WebApplication2.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        // Dùng đúng tên ProductId để lưu ID sản phẩm
        public int ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Liên kết điều hướng
        public virtual Order Order { get; set; }

        // Chỉ định ProductId là khóa ngoại cho bảng Books
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("ProductId")]
        public virtual Book Book { get; set; }
    }
}