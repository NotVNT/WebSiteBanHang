namespace WebSiteBanHang.Models
{
    public enum OrderStatus
    {
        Pending, // Chờ xác nhận
        Processing, // Đã xác nhận
        Completed, // Đã hoàn thành
        Cancelled // Hủy đơn hàng
    }
} 