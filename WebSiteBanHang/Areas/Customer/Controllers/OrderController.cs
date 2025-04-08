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

        // GET: Customer/Order/CancelOrder/5
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderRepository.GetByIdAsync(id);
            
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }
            
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng ở trạng thái chờ xác nhận.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            var viewModel = new OrderCancellationViewModel
            {
                OrderId = id
            };
            
            return View(viewModel);
        }

        // POST: Customer/Order/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(OrderCancellationViewModel model)
        {
            // Remove CustomReason from ModelState validation if not "other"
            if (model.CancellationReasonId != "other")
            {
                ModelState.Remove("CustomReason");
            }

            // Add custom validation for CustomReason when "other" is selected
            if (model.CancellationReasonId == "other" && string.IsNullOrWhiteSpace(model.CustomReason))
            {
                ModelState.AddModelError("CustomReason", "Vui lòng nhập lý do cụ thể khi chọn 'Lý do khác'.");
            }

            if (!ModelState.IsValid)
            {
                return View("CancelOrder", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderRepository.GetByIdAsync(model.OrderId);
            
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }
            
            // Only allow cancellation if the order is in Pending status
            if (order.Status == OrderStatus.Pending)
            {
                string cancellationReason = model.GetFullCancellationReason();
                await _orderRepository.CancelOrderAsync(model.OrderId, cancellationReason, CancellationType.Customer);
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này do trạng thái hiện tại.";
            }
            
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }
    }
}
