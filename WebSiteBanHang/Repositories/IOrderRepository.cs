using WebSiteBanHang.Models;

namespace WebSiteBanHang.Repositories
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<Order> GetByIdAsync(int id);
        Task<Order> GetOrderWithItemsAsync(int id);
        Task<int> CreateOrderAsync(Order order);
        Task UpdateOrderStatusAsync(int id, OrderStatus status);
        Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<int> GetOrderCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task CancelOrderAsync(int id, string cancellationReason);
        Task<int> GetUniqueCustomerCountAsync();
    }
}
