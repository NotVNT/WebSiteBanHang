using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebSiteBanHang.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày giao hàng")]
        public DateTime? ShippingDate { get; set; }

        [Display(Name = "Tổng tiền")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [NotMapped]
        public decimal OrderTotal 
        { 
            get => TotalAmount; 
            set => TotalAmount = value; 
        }

        [Display(Name = "Trạng thái")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        [NotMapped]
        public OrderStatus OrderStatus 
        { 
            get => Status; 
            set => Status = value; 
        }

        [Display(Name = "Thanh toán")]
        public bool PaymentStatus { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD";

        [Display(Name = "Mã vận đơn")]
        [Required]
        public string TrackingNumber { get; set; }

        [Display(Name = "Họ tên người nhận")]
        [Required]
        public string FullName { get; set; }
        
        [NotMapped]
        public string ShippingName 
        { 
            get => FullName; 
            set => FullName = value; 
        }

        [Display(Name = "Địa chỉ giao hàng")]
        [Required]
        public string ShippingAddress { get; set; }

        [Display(Name = "Điện thoại")]
        [Required]
        public string PhoneNumber { get; set; }

        [Display(Name = "Email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Ghi chú")]
        [Required(AllowEmptyStrings = true)]
        public string Notes { get; set; } = "";

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string CancellationReason { get; set; } = "";

        // New property for Promotion reference
        public int? PromotionId { get; set; }
        
        // New property for discount amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        public virtual Promotion Promotion { get; set; }
    }

    public enum OrderStatus
    {
        Pending,    // Chờ xác nhận
        Processing, // Đã xác nhận / đang xử lý
        Confirmed,  // Đã xác nhận đơn hàng
        Shipping,   // Đang giao hàng
        Delivered,  // Đã giao hàng
        Completed,  // Đã hoàn thành
        Cancelled   // Đã hủy đơn hàng
    }
} 