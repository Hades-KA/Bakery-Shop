using Microsoft.EntityFrameworkCore;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhamTaManhLan_8888.Repositories
{
	public class EFProductRepository : IProductRepository
	{
		private readonly ApplicationDbContext _context;

		public EFProductRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Product>> GetAllAsync()
		{
			return await _context.Products.Include(p => p.Category).ToListAsync();
		}

		public async Task<Product> GetByIdAsync(int id)
		{
			return await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
		}

		public async Task AddAsync(Product product)
		{
			_context.Products.Add(product);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Product product)
		{
			_context.Products.Update(product);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var product = await _context.Products.FindAsync(id);
			if (product != null)
			{
				_context.Products.Remove(product);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
		{
			if (string.IsNullOrEmpty(searchTerm))
			{
				return await _context.Products.Include(p => p.Category).ToListAsync();
			}
			return await _context.Products.Include(p => p.Category)
				.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
				.ToListAsync();
		}

		// ✅ Thêm chức năng lọc sản phẩm theo danh mục
		public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
		{
			return await _context.Products
				.Include(p => p.Category)
				.Where(p => p.CategoryId == categoryId)
				.ToListAsync();
		}

		// ✅ Thêm chức năng lấy sản phẩm mới nhất
		public async Task<IEnumerable<Product>> GetLatestProductsAsync(int count)
		{
			return await _context.Products
				.Include(p => p.Category)
				.OrderByDescending(p => p.Id) // Giả định sản phẩm mới có ID cao hơn
				.Take(count)
				.ToListAsync();
		}
	}
}
