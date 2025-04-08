using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhamTaManhLan_8888.Models
{
    public enum PaymentMethod
    {
        CreditCard,
        Paypal,
        COD
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalPrice { get; set; }

        [Required(ErrorMessage = "Shipping address is required")]
        public string ShippingAddress { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please select a payment method")]
        public PaymentMethod PaymentMethod { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
    }
}
