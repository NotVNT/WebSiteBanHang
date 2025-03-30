using System.ComponentModel.DataAnnotations;

namespace WebSiteBanHang.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }
        
        [Range(typeof(decimal), "1000", "100000000", ErrorMessage = "Giá sản phẩm phải từ 1.000đ đến 100.000.000đ")]
        [Display(Name = "Giá (VNĐ)")]
        public decimal Price { get; set; }
        
        [Required]
        [Display(Name = "Mô tả sản phẩm")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Hãy chọn danh mục sản phẩm")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        
        public string? ImageUrl { get; set; }
        public List<ProductImage>? ImageUrls { get; set; } 
        public Category? Category { get; set; }
        
        // Navigation properties
        public ICollection<Favorite>? Favorites { get; set; }
        public ICollection<Rating>? Ratings { get; set; }
    }
}
