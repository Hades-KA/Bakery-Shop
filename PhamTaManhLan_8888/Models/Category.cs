using System.ComponentModel.DataAnnotations;

namespace PhamTaManhLan_8888.Models
{
	public class Category
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Tên danh mục không được để trống")]
		[StringLength(50, ErrorMessage = "Tên danh mục không được dài quá 50 ký tự")]
		public string Name { get; set; }

		// Quan hệ 1-N với Product
		public List<Product>? Products { get; set; }
	}
}
