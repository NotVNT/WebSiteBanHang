using System;
using System.ComponentModel.DataAnnotations;

namespace WebSiteBanHang.Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        public decimal DiscountAmount { get; set; }

        public bool IsPercentage { get; set; }

        public decimal? MinimumOrderAmount { get; set; }

        public int? MaxUseTimes { get; set; }

        public int UsedTimes { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 