using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Models.ViewModels;
using WebSiteBanHang.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public async Task<IActionResult> Index(int page = 1, string status = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _orderRepository.GetUserOrdersAsync(userId);

            // Lọc theo trạng thái nếu có
            OrderStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int statusValue))
            {
                statusEnum = (OrderStatus)statusValue;
                orders = orders.Where(o => o.Status == statusEnum.Value).ToList();
            }

            // Tạo danh sách trạng thái cho dropdown
            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Tất cả trạng thái", Value = "" },
                new SelectListItem { 
                    Text = "Chờ xác nhận", 
                    Value = ((int)OrderStatus.Pending).ToString(), 
                    Selected = statusEnum == OrderStatus.Pending 
                },
                new SelectListItem { 
                    Text = "Đã xác nhận", 
                    Value = ((int)OrderStatus.Confirmed).ToString(), 
                    Selected = statusEnum == OrderStatus.Confirmed 
                },
                new SelectListItem { 
                    Text = "Đã hoàn thành", 
                    Value = ((int)OrderStatus.Completed).ToString(), 
                    Selected = statusEnum == OrderStatus.Completed 
                },
                new SelectListItem { 
                    Text = "Đã hủy", 
                    Value = ((int)OrderStatus.Cancelled).ToString(), 
                    Selected = statusEnum == OrderStatus.Cancelled 
                }
            };

            ViewBag.StatusList = statusList;
            ViewBag.CurrentStatus = status;

            int pageSize = 10; // Số đơn hàng trên mỗi trang
            var paginatedOrders = PaginatedList<Order>.Create(orders, page, pageSize);
            return View(paginatedOrders);
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
