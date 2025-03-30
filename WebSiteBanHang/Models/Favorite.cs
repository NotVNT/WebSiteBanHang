using System;
using System.ComponentModel.DataAnnotations;

namespace WebSiteBanHang.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int ProductId { get; set; }
        public DateTime DateAdded { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Product Product { get; set; }
    }
}
