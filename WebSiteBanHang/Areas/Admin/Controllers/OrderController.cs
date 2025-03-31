using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using WebSiteBanHang.Models.ViewModels;
using System.Linq;
using WebSiteBanHang.Utilities;

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

        public async Task<IActionResult> Index(OrderFilterViewModel filter, int page = 1)
        {
            const int pageSize = 10;
            
            // Initialize filter if it's null
            filter ??= new OrderFilterViewModel();
            
            // Apply date range filter if specified
            if (!string.IsNullOrEmpty(filter.DateRange))
            {
                filter.ApplyDateRangeFilter();
            }
            
            // Get all orders
            var orders = await _orderRepository.GetAllAsync();
            
            // Đếm số lượng đơn hàng theo trạng thái
            ViewBag.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            ViewBag.ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing);
            ViewBag.CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            ViewBag.CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
            
            // Apply filters
            if (filter.Status.HasValue)
            {
                orders = orders.Where(o => o.Status == filter.Status.Value).ToList();
            }
            
            if (filter.StartDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= filter.StartDate.Value).ToList();
            }
            
            if (filter.EndDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= filter.EndDate.Value).ToList();
            }
            
            // Apply Order ID filter
            if (filter.OrderId.HasValue)
            {
                orders = orders.Where(o => o.Id == filter.OrderId.Value).ToList();
            }
            
            // Apply Customer search with Vietnamese text normalization
            if (!string.IsNullOrWhiteSpace(filter.CustomerSearch))
            {
                string normalizedSearch = StringHelper.NormalizeVietnamese(filter.CustomerSearch);
                
                orders = orders.Where(o => 
                    (o.FullName != null && StringHelper.NormalizeVietnamese(o.FullName).Contains(normalizedSearch)) ||
                    (o.Email != null && StringHelper.NormalizeVietnamese(o.Email).Contains(normalizedSearch)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(filter.CustomerSearch))
                ).ToList();
            }
            
            // Apply price range filter
            if (filter.MinAmount.HasValue)
            {
                orders = orders.Where(o => o.TotalAmount >= filter.MinAmount.Value).ToList();
            }
            
            if (filter.MaxAmount.HasValue)
            {
                orders = orders.Where(o => o.TotalAmount <= filter.MaxAmount.Value).ToList();
            }
            
            // Apply sorting based on SortBy and SortDirection
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy)
                {
                    case "date":
                        orders = filter.SortDirection == "asc" 
                            ? orders.OrderBy(o => o.OrderDate).ToList()
                            : orders.OrderByDescending(o => o.OrderDate).ToList();
                        break;
                        
                    case "amount":
                        orders = filter.SortDirection == "asc" 
                            ? orders.OrderBy(o => o.TotalAmount).ToList()
                            : orders.OrderByDescending(o => o.TotalAmount).ToList();
                        break;
                        
                    case "id":
                        orders = filter.SortDirection == "asc" 
                            ? orders.OrderBy(o => o.Id).ToList()
                            : orders.OrderByDescending(o => o.Id).ToList();
                        break;
                        
                    case "status":
                        orders = filter.SortDirection == "asc" 
                            ? orders.OrderBy(o => o.Status).ToList()
                            : orders.OrderByDescending(o => o.Status).ToList();
                        break;
                        
                    default:
                        // Default sort by date (newest first)
                        orders = orders.OrderByDescending(o => o.OrderDate).ToList();
                        break;
                }
            }
            else
            {
                // Default sort by date (newest first) if no sort option specified
                orders = orders.OrderByDescending(o => o.OrderDate).ToList();
                filter.SortBy = "date";
                filter.SortDirection = "desc";
            }
            
            // Create paginated list
            var paginatedOrders = PaginatedList<Order>.Create(orders, page, pageSize);
            
            // Pass the filter back to the view
            ViewBag.Filter = filter;
            
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
            // Get the current order to check its status
            var order = await _orderRepository.GetOrderWithItemsAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            
            // Get the referer once at the beginning of the method
            string referer = Request.Headers["Referer"].ToString();
            
            // Check if the order is already completed or cancelled
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                string errorMessage = order.Status == OrderStatus.Completed 
                    ? "Đơn hàng đã hoàn thành không thể thay đổi trạng thái" 
                    : "Đơn hàng đã hủy không thể thay đổi trạng thái";
                
                // If it's an AJAX request, return JSON response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = false, 
                        message = errorMessage
                    });
                }
                
                // For direct form submissions, use TempData
                TempData["ErrorMessage"] = errorMessage;
                
                // Check if the request was from the list page or details page
                if (referer.Contains("/Admin/Order/Details"))
                {
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            // If order is not completed or cancelled, proceed with the update
            await _orderRepository.UpdateOrderStatusAsync(id, status);
            
            // Get appropriate status message
            string statusMessage = status switch
            {
                OrderStatus.Pending => "Đơn hàng đã được đặt lại trạng thái chờ xác nhận",
                OrderStatus.Processing => "Đơn hàng đã được xác nhận thành công",
                OrderStatus.Completed => "Đơn hàng đã được giao thành công",
                OrderStatus.Cancelled => "Đơn hàng đã bị hủy",
                _ => "Trạng thái đơn hàng đã được cập nhật"
            };
            
            // If it's an AJAX request, return JSON response
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Set TempData here too for consistent behavior when page reloads
                TempData["SuccessMessage"] = statusMessage;
                
                return Json(new { 
                    success = true, 
                    message = statusMessage,
                    newStatus = status.ToString(),
                    statusClass = GetStatusClass(status),
                    statusText = GetStatusText(status)
                });
            }
            
            // For direct form submissions, use TempData
            TempData["SuccessMessage"] = statusMessage;
            
            // Check if the request was from the list page or details page
            if (referer.Contains("/Admin/Order/Details"))
            {
                return RedirectToAction(nameof(Details), new { id });
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Helper methods for status styling
        private string GetStatusClass(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "bg-warning text-dark",
                OrderStatus.Processing => "bg-info text-dark",
                OrderStatus.Completed => "bg-success",
                OrderStatus.Cancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }

        private string GetStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xác nhận",
                OrderStatus.Processing => "Đã xác nhận",
                OrderStatus.Completed => "Đã giao hàng",
                OrderStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        private decimal CalculateOrderTotal(Order order)
        {
            return order.Items.Sum(item => item.Quantity * item.UnitPrice);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id, string cancellationReason, string otherReason)
        {
            // Handle other reason if selected
            if (cancellationReason == "other" && !string.IsNullOrEmpty(otherReason))
            {
                cancellationReason = otherReason;
            }
            
            await _orderRepository.CancelOrderAsync(id, cancellationReason);
            
            // Change to SuccessMessage for consistent styling
            TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
            
            // Check if the request came from details page
            string referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Admin/Order/Details"))
            {
                return RedirectToAction(nameof(Details), new { id });
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
