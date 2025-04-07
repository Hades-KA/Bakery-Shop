using System.ComponentModel.DataAnnotations.Schema;

namespace PhamTaManhLan_8888.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Order Order { get; set; }
        public Product Product { get; set; }

        // Lấy ảnh sản phẩm
        [NotMapped]
        public string ProductImageUrl => Product?.ImageUrl ?? "/images/default.jpg";
    }
}