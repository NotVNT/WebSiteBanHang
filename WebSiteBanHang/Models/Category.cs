using System.ComponentModel.DataAnnotations;

namespace WebSiteBanHang.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required, StringLength(50)]
        public String Name { get; set; }
        List<Product> Products { get; set; } = new List<Product>();
    }
}
