using System.ComponentModel.DataAnnotations;

namespace PhamTaManhLan_8888.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Chờ xử lý")]
        Pending = 0,

        [Display(Name = "Đang xử lý")]
        Processing = 1,

        [Display(Name = "Đang giao")]
        Shipped = 2,

        [Display(Name = "Đã giao")]
        Delivered = 3,

        [Display(Name = "Đã hủy")]
        Cancelled = 4,

        [Display(Name = "Hoàn thành")]
        Completed = 5
    }
}
