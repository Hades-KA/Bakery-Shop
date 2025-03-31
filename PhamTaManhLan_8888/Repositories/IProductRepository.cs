using PhamTaManhLan_8888.Models;

public interface IProductRepository
{
	Task<IEnumerable<Product>> GetAllAsync();
	Task<Product> GetByIdAsync(int id);
	Task AddAsync(Product product);
	Task UpdateAsync(Product product);
	Task DeleteAsync(int id);
	Task<IEnumerable<Product>> SearchAsync(string searchTerm);

	// ✅ Thêm chức năng lọc sản phẩm theo danh mục
	Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);

	// ✅ Thêm chức năng lấy sản phẩm mới nhất
	Task<IEnumerable<Product>> GetLatestProductsAsync(int count);
}
