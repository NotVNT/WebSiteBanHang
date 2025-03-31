using Microsoft.EntityFrameworkCore;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            try
            {
                // Sử dụng AsNoTracking để tối ưu hiệu suất khi chỉ đọc
                var result = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new Order
                    {
                        Id = o.Id,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status
                        // Chỉ lấy các trường cần thiết, tránh NULL
                    })
                    .ToListAsync();
                    
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserOrdersAsync: {ex.Message}");
                return new List<Order>();
            }
        }

        public async Task<Order> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> GetOrderWithItemsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<int> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order.Id;
        }

        public async Task UpdateOrderStatusAsync(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                // Validate that the status is one of the allowed values
                if (!Enum.IsDefined(typeof(OrderStatus), status))
                {
                    throw new ArgumentException("Invalid order status", nameof(status));
                }
                
                // Update payment status automatically when order is completed
                if (status == OrderStatus.Completed && order.PaymentMethod == "COD")
                {
                    order.PaymentStatus = true;
                }
                
                order.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<int> GetOrderCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task CancelOrderAsync(int id, string cancellationReason)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = cancellationReason;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUniqueCustomerCountAsync()
        {
            return await _context.Orders
                .Select(o => o.UserId)
                .Distinct()
                .CountAsync();
        }
    }
}
