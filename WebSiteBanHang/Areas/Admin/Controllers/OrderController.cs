using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using WebSiteBanHang.Models.ViewModels;
using System.Linq;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;

        public OrderController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var orders = await _orderRepository.GetAllAsync();
            
            var paginatedOrders = PaginatedList<Order>.Create(orders, page, pageSize);
            return View(paginatedOrders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepository.GetOrderWithItemsAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            await _orderRepository.UpdateOrderStatusAsync(id, status);
            return RedirectToAction(nameof(Details), new { id });
        }

        private decimal CalculateOrderTotal(Order order)
        {
            return order.Items.Sum(item => item.Quantity * item.UnitPrice);
        }
    }
}
