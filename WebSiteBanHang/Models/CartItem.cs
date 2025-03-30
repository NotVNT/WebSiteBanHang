namespace WebSiteBanHang.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        // Navigation property
        public Product Product { get; set; }

        // Calculated property
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
