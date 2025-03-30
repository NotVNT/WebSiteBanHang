using System;
using System.ComponentModel.DataAnnotations;

namespace WebSiteBanHang.Models
{
    public class Rating
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; }
        public string Comment { get; set; }
        public DateTime DateCreated { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Product Product { get; set; }
    }
}
