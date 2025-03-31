using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhamTaManhLan_8888.Models
{
	public class ProductImage
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Đường dẫn ảnh không được để trống")]
		[Url(ErrorMessage = "Đường dẫn ảnh không hợp lệ")]
		public string Url { get; set; }

		// Khóa ngoại liên kết với Product
		[ForeignKey("Product")]
		public int ProductId { get; set; }

		public Product? Product { get; set; }
	}
}
