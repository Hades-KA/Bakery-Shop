using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace PhamTaManhLan_8888.Models
{
	public class Order
	{
		public int Id { get; set; }
		public string UserId { get; set; }
		public DateTime OrderDate { get; set; }
		public decimal TotalPrice { get; set; }
		public string ShippingAddress { get; set; }
		public string Notes { get; set; }

		[ForeignKey("UserId")]
		[ValidateNever]
		public ApplicationUser ApplicationUser { get; set; }

		public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

		public OrderStatus Status { get; set; } = OrderStatus.Pending;
	}
}
