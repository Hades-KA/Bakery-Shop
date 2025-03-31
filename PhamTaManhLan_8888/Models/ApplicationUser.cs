using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PhamTaManhLan_8888.Models
{
	public class ApplicationUser : IdentityUser
	{
		[Required]
		public string FullName { get; set; }
		public string? Address { get; set; }
		public string? Age { get; set; }

		public string PhoneNumber { get; set; }
	}
}
