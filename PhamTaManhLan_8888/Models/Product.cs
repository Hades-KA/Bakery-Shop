using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhamTaManhLan_8888.Models
{
	public class Product
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Tên sản phẩm không được để trống")]
		[StringLength(100, ErrorMessage = "Tên sản phẩm không được dài quá 100 ký tự")]
		public string Name { get; set; }

		[Required(ErrorMessage = "Giá sản phẩm không được để trống")]
		[Range(1000, 500000, ErrorMessage = "Giá sản phẩm phải nằm trong khoảng 0.01 - 10,000.00")]
		public decimal Price { get; set; }

		[StringLength(500, ErrorMessage = "Mô tả không được dài quá 500 ký tự")]
		public string? Description { get; set; }

		[Url(ErrorMessage = "Đường dẫn ảnh không hợp lệ")]
		public string? ImageUrl { get; set; }

		// Danh sách ảnh liên kết với sản phẩm
		public List<ProductImage>? Images { get; set; }

		// Khóa ngoại liên kết danh mục
		public int CategoryId { get; set; }
		[ForeignKey("CategoryId")]
		public Category? Category { get; set; }
	}
}
