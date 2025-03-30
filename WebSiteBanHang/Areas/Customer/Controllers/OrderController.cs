using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Models.ViewModels;
using WebSiteBanHang.Repositories;

namespace WebSiteBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;

        public OrderController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _orderRepository.GetUserOrdersAsync(userId);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderRepository.GetOrderWithItemsAsync(id);
            
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }
            
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderRepository.GetByIdAsync(id);
            
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }
            
            // Only allow cancellation if the order is in Pending status
            if (order.Status == OrderStatus.Pending)
            {
                await _orderRepository.UpdateOrderStatusAsync(id, OrderStatus.Cancelled);
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này do trạng thái hiện tại.";
            }
            
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
