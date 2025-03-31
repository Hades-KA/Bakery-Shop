using Microsoft.EntityFrameworkCore;
using PhamTaManhLan_8888.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhamTaManhLan_8888.Repositories
{
	public class EFCategoryRepository : ICategoryRepository
	{
		private readonly ApplicationDbContext _context;

		public EFCategoryRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Category>> GetAllAsync()
		{
			return await _context.Categories
				.Include(c => c.Products) // Load danh sách sản phẩm của từng danh mục
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Category?> GetByIdAsync(int id)
		{
			var category = await _context.Categories
				.Include(c => c.Products)
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == id);

			if (category == null)
			{
				throw new KeyNotFoundException($"Category with id {id} not found.");
			}

			return category;
		}

		public async Task<Category?> GetByNameAsync(string name) // ✅ Thêm phương thức này
		{
			return await _context.Categories
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Name == name);
		}

		public async Task AddAsync(Category category)
		{
			await _context.Categories.AddAsync(category);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Category category)
		{
			_context.Categories.Update(category);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var category = await _context.Categories.FindAsync(id);
			if (category != null)
			{
				_context.Categories.Remove(category);
				await _context.SaveChangesAsync();
			}
		}
	}
}
