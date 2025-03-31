using System.Collections.Generic;
using System.Threading.Tasks;
using PhamTaManhLan_8888.Models;

namespace PhamTaManhLan_8888.Repositories
{
	public interface ICategoryRepository
	{
		Task<IEnumerable<Category>> GetAllAsync();
		Task<Category?> GetByIdAsync(int id);
		Task<Category?> GetByNameAsync(string name); // ✅ Thêm dòng này
		Task AddAsync(Category category);
		Task UpdateAsync(Category category);
		Task DeleteAsync(int id);
	}
}
