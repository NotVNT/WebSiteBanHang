using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using WebSiteBanHang.Models.ViewModels;
using System.Linq;
using WebSiteBanHang.Utilities;
using WebSiteBanHang.Services;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomEmailSender _emailSender;

        public OrderController(IOrderRepository orderRepository, ICustomEmailSender emailSender)
        {
            _orderRepository = orderRepository;
            _emailSender = emailSender;
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
            var allOrders = await _orderRepository.GetAllAsync();
            
            // Đếm số lượng đơn hàng theo trạng thái
            ViewBag.TotalOrders = allOrders.Count; // Tổng số đơn hàng thực tế
            ViewBag.PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending);
            ViewBag.ProcessingOrders = allOrders.Count(o => o.Status == OrderStatus.Confirmed);
            ViewBag.CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed);
            ViewBag.CancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled);
            
            // Copy danh sách đơn hàng để áp dụng bộ lọc mà không ảnh hưởng đến tổng ban đầu
            var filteredOrders = allOrders.ToList();
            
            // Apply filters
            if (filter.Status.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.Status == filter.Status.Value).ToList();
            }
            
            // Apply cancellation type filter
            if (filter.CancellationType.HasValue && filter.Status == OrderStatus.Cancelled)
            {
                filteredOrders = filteredOrders.Where(o => o.CancellationType == filter.CancellationType.Value).ToList();
            }
            
            if (filter.StartDate.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.OrderDate >= filter.StartDate.Value).ToList();
            }
            
            if (filter.EndDate.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.OrderDate <= filter.EndDate.Value).ToList();
            }
            
            // Apply Order ID filter
            if (filter.OrderId.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.Id == filter.OrderId.Value).ToList();
            }
            
            // Apply customer search filter
            if (!string.IsNullOrEmpty(filter.CustomerSearch))
            {
                string search = filter.CustomerSearch.ToLower();
                filteredOrders = filteredOrders.Where(o => 
                    (o.FullName != null && o.FullName.ToLower().Contains(search)) ||
                    (o.Email != null && o.Email.ToLower().Contains(search)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(search))
                ).ToList();
            }
            
            // Apply price range filter
            if (filter.MinAmount.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.TotalAmount >= filter.MinAmount.Value).ToList();
            }
            
            if (filter.MaxAmount.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.TotalAmount <= filter.MaxAmount.Value).ToList();
            }
            
            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy)
                {
                    case "date":
                        filteredOrders = filter.SortDirection == "asc" 
                            ? filteredOrders.OrderBy(o => o.OrderDate).ToList()
                            : filteredOrders.OrderByDescending(o => o.OrderDate).ToList();
                        break;
                        
                    case "amount":
                        filteredOrders = filter.SortDirection == "asc" 
                            ? filteredOrders.OrderBy(o => o.TotalAmount).ToList()
                            : filteredOrders.OrderByDescending(o => o.TotalAmount).ToList();
                        break;
                        
                    case "id":
                        filteredOrders = filter.SortDirection == "asc" 
                            ? filteredOrders.OrderBy(o => o.Id).ToList()
                            : filteredOrders.OrderByDescending(o => o.Id).ToList();
                        break;
                        
                    case "status":
                        filteredOrders = filter.SortDirection == "asc" 
                            ? filteredOrders.OrderBy(o => o.Status).ToList()
                            : filteredOrders.OrderByDescending(o => o.Status).ToList();
                        break;
                        
                    default:
                        // Default sort by date (newest first)
                        filteredOrders = filteredOrders.OrderByDescending(o => o.OrderDate).ToList();
                        break;
                }
            }
            else
            {
                // Default sort by date (newest first) if no sort option specified
                filteredOrders = filteredOrders.OrderByDescending(o => o.OrderDate).ToList();
                filter.SortBy = "date";
                filter.SortDirection = "desc";
            }
            
            // Create paginated list
            var paginatedOrders = PaginatedList<Order>.Create(filteredOrders, page, pageSize);
            
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
                OrderStatus.Confirmed => "Đơn hàng đã được xác nhận thành công",
                OrderStatus.Completed => "Đơn hàng đã được giao thành công",
                OrderStatus.Cancelled => "Đơn hàng đã bị hủy",
                _ => "Trạng thái đơn hàng đã được cập nhật"
            };

            // Send email notification when order is confirmed
            if (status == OrderStatus.Confirmed)
            {
                try 
                {
                    await SendOrderConfirmationEmail(order);
                    // Add success message for email
                    statusMessage += " và email xác nhận đã được gửi đến khách hàng";
                }
                catch (Exception ex)
                {
                    // Log the error but don't stop the process
                    Console.WriteLine($"Error sending confirmation email: {ex.Message}");
                    // Add warning about email
                    statusMessage += " nhưng không thể gửi email đến khách hàng";
                }
            }
            
            // Send email notification when order is completed
            if (status == OrderStatus.Completed)
            {
                try 
                {
                    await SendOrderCompletedEmail(order);
                    // Add success message for email
                    statusMessage += " và email thông báo đã được gửi đến khách hàng";
                }
                catch (Exception ex)
                {
                    // Log the error but don't stop the process
                    Console.WriteLine($"Error sending completed order email: {ex.Message}");
                    // Add warning about email
                    statusMessage += " nhưng không thể gửi email đến khách hàng";
                }
            }
            
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
                OrderStatus.Confirmed => "bg-info text-dark",
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
                OrderStatus.Confirmed => "Đã xác nhận",
                OrderStatus.Completed => "Đã hoàn thành",
                OrderStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        private decimal CalculateOrderTotal(Order order)
        {
            return order.Items.Sum(item => item.Quantity * item.UnitPrice);
        }

        private async Task SendOrderCancellationEmail(Order order, string cancellationReason)
        {
            if (string.IsNullOrEmpty(order.Email))
                return;

            string subject = $"Đơn hàng #{order.Id} đã bị hủy";
            
            // Create HTML for order items
            string orderItemsHtml = "";
            if (order.Items != null && order.Items.Any())
            {
                orderItemsHtml = "<table class='product-list'>" +
                    "<tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr>";

                foreach (var item in order.Items)
                {
                    orderItemsHtml += $"<tr>" +
                        $"<td>{item.Product?.Name ?? "Sản phẩm"}</td>" +
                        $"<td>{item.Quantity}</td>" +
                        $"<td>{item.UnitPrice:N0} đ</td>" +
                        $"<td>{(item.UnitPrice * item.Quantity):N0} đ</td>" +
                        $"</tr>";
                }

                orderItemsHtml += "</table>";
            }
            
            string message = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #dc3545; color: white; padding: 10px 20px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    .order-details {{ margin: 20px 0; }}
                    .product-list {{ border-collapse: collapse; width: 100%; margin: 15px 0; }}
                    .product-list th, .product-list td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    .product-list th {{ background-color: #f2f2f2; }}
                    .total {{ font-weight: bold; text-align: right; margin-top: 10px; }}
                    .btn {{ display: inline-block; padding: 10px 20px; background-color: #dc3545; color: white; 
                           text-decoration: none; border-radius: 4px; }}
                    .highlight {{ background-color: #f8d7da; padding: 10px; border-radius: 5px; margin: 10px 0; }}
                    .reason-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Thông báo hủy đơn hàng</h2>
                    </div>
                    <div class='content'>
                        <p>Chào <strong>{order.FullName}</strong>,</p>
                        
                        <div class='highlight'>
                            <p>Đơn hàng <strong>#{order.Id}</strong> của bạn đã bị hủy.</p>
                        </div>
                        
                        <div class='reason-box'>
                            <h3 style='margin-top: 0;'>Lý do hủy đơn:</h3>
                            <p style='margin-bottom: 0;'>{cancellationReason}</p>
                        </div>
                        
                        <div class='order-details'>
                            <h3>Thông tin đơn hàng:</h3>
                            <p><strong>Mã đơn hàng:</strong> #{order.Id}</p>
                            <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Ngày hủy:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Tổng tiền:</strong> {order.TotalAmount:N0} đ</p>
                            <p><strong>Phương thức thanh toán:</strong> {(order.PaymentMethod == "COD" ? "Thanh toán khi nhận hàng (COD)" : order.PaymentMethod)}</p>
                        </div>
                        
                        <h3>Chi tiết đơn hàng đã hủy:</h3>
                        {orderItemsHtml}
                        <p class='total'>Tổng giá trị đơn hàng: <strong>{order.TotalAmount:N0} đ</strong></p>
                        
                        <p>Nếu bạn có bất kỳ thắc mắc nào hoặc cần hỗ trợ thêm, vui lòng liên hệ với chúng tôi qua email hoặc số điện thoại được cung cấp trên trang web.</p>
                        
                        <p>Cảm ơn bạn đã quan tâm đến sản phẩm của GocNhoDecor!</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 GocNhoDecor - Nơi mang đến không gian sống đẹp cho ngôi nhà của bạn</p>
                    </div>
                </div>
            </body>
            </html>";

            await _emailSender.SendEmailAsync(order.Email, subject, message);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id, string cancellationReason, string otherReason)
        {
            var order = await _orderRepository.GetOrderWithItemsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Combine reasons if "Other" is selected
            string finalReason = cancellationReason;
            if (cancellationReason == "other" && !string.IsNullOrEmpty(otherReason))
            {
                finalReason = otherReason;
            }

            // Update order status to cancelled with admin cancellation type
            await _orderRepository.CancelOrderAsync(id, finalReason, CancellationType.Admin);

            try 
            {
                // Send cancellation email
                await SendOrderCancellationEmail(order, finalReason);
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy và email thông báo đã được gửi đến khách hàng";
            }
            catch (Exception ex)
            {
                // Log the error but don't stop the process
                Console.WriteLine($"Error sending cancellation email: {ex.Message}");
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy nhưng không thể gửi email đến khách hàng";
            }

            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = TempData["SuccessMessage"] });
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to send order confirmation email
        private async Task SendOrderConfirmationEmail(Order order)
        {
            if (string.IsNullOrEmpty(order.Email))
                return;

            string subject = $"Đơn hàng #{order.Id} đã được xác nhận!";
            
            // Create HTML for order items
            string orderItemsHtml = "";
            if (order.Items != null && order.Items.Any())
            {
                orderItemsHtml = "<table class='product-list'>" +
                    "<tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr>";

                foreach (var item in order.Items)
                {
                    orderItemsHtml += $"<tr>" +
                        $"<td>{item.Product?.Name ?? "Sản phẩm"}</td>" +
                        $"<td>{item.Quantity}</td>" +
                        $"<td>{item.UnitPrice:N0} đ</td>" +
                        $"<td>{(item.UnitPrice * item.Quantity):N0} đ</td>" +
                        $"</tr>";
                }

                orderItemsHtml += "</table>";
            }
            
            string message = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #0d6efd; color: white; padding: 10px 20px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    .order-details {{ margin: 20px 0; }}
                    .product-list {{ border-collapse: collapse; width: 100%; margin: 15px 0; }}
                    .product-list th, .product-list td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    .product-list th {{ background-color: #f2f2f2; }}
                    .total {{ font-weight: bold; text-align: right; margin-top: 10px; }}
                    .btn {{ display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: white; 
                           text-decoration: none; border-radius: 4px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Đơn hàng của bạn đã được xác nhận!</h2>
                    </div>
                    <div class='content'>
                        <p>Chào <strong>{order.FullName}</strong>,</p>
                        <p>Cảm ơn bạn đã mua sắm tại GocNhoDecor. Đơn hàng #{order.Id} của bạn đã được xác nhận và sẽ được xử lý ngay!</p>
                        
                        <div class='order-details'>
                            <h3>Thông tin đơn hàng:</h3>
                            <p><strong>Mã đơn hàng:</strong> #{order.Id}</p>
                            <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Tổng tiền:</strong> {order.TotalAmount:N0} đ</p>
                            <p><strong>Phương thức thanh toán:</strong> {(order.PaymentMethod == "COD" ? "Thanh toán khi nhận hàng (COD)" : order.PaymentMethod)}</p>
                        </div>
                        
                        <h3>Chi tiết đơn hàng:</h3>
                        {orderItemsHtml}
                        <p class='total'>Tổng thanh toán: <strong>{order.TotalAmount:N0} đ</strong></p>
                        
                        <p>Đơn hàng của bạn sẽ được giao đến:</p>
                        <p>
                            <strong>Địa chỉ:</strong> {order.ShippingAddress}<br/>
                            <strong>Số điện thoại:</strong> {order.PhoneNumber}
                        </p>
                        
                        <p>Bạn có thể theo dõi trạng thái đơn hàng trong phần <strong>Đơn hàng của tôi</strong> trên website.</p>
                        
                        <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc số điện thoại được cung cấp trên trang web.</p>
                        
                        <p>Cảm ơn bạn đã chọn mua sắm tại GocNhoDecor!</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 GocNhoDecor - Nơi mang đến không gian sống đẹp cho ngôi nhà của bạn</p>
                    </div>
                </div>
            </body>
            </html>";

            await _emailSender.SendEmailAsync(order.Email, subject, message);
        }

        // Helper method to send order completed email
        private async Task SendOrderCompletedEmail(Order order)
        {
            if (string.IsNullOrEmpty(order.Email))
                return;

            string subject = $"Đơn hàng #{order.Id} đã giao thành công!";
            
            // Create HTML for order items
            string orderItemsHtml = "";
            if (order.Items != null && order.Items.Any())
            {
                orderItemsHtml = "<table class='product-list'>" +
                    "<tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr>";

                foreach (var item in order.Items)
                {
                    orderItemsHtml += $"<tr>" +
                        $"<td>{item.Product?.Name ?? "Sản phẩm"}</td>" +
                        $"<td>{item.Quantity}</td>" +
                        $"<td>{item.UnitPrice:N0} đ</td>" +
                        $"<td>{(item.UnitPrice * item.Quantity):N0} đ</td>" +
                        $"</tr>";
                }

                orderItemsHtml += "</table>";
            }
            
            string message = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #28a745; color: white; padding: 10px 20px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    .order-details {{ margin: 20px 0; }}
                    .product-list {{ border-collapse: collapse; width: 100%; margin: 15px 0; }}
                    .product-list th, .product-list td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    .product-list th {{ background-color: #f2f2f2; }}
                    .total {{ font-weight: bold; text-align: right; margin-top: 10px; }}
                    .btn {{ display: inline-block; padding: 10px 20px; background-color: #28a745; color: white; 
                           text-decoration: none; border-radius: 4px; }}
                    .highlight {{ background-color: #e8f5e9; padding: 10px; border-radius: 5px; margin: 10px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Đơn hàng đã giao thành công!</h2>
                    </div>
                    <div class='content'>
                        <p>Chào <strong>{order.FullName}</strong>,</p>
                        <p>Cảm ơn bạn đã mua sắm tại GocNhoDecor. Chúng tôi rất vui thông báo rằng đơn hàng của bạn đã được giao thành công!</p>
                        
                        <div class='highlight'>
                            <p>Đơn hàng <strong>#{order.Id}</strong> đã được giao thành công. Chúng tôi hy vọng bạn hài lòng với sản phẩm!</p>
                        </div>
                        
                        <div class='order-details'>
                            <h3>Thông tin đơn hàng:</h3>
                            <p><strong>Mã đơn hàng:</strong> #{order.Id}</p>
                            <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Ngày giao:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Tổng tiền:</strong> {order.TotalAmount:N0} đ</p>
                            <p><strong>Phương thức thanh toán:</strong> {(order.PaymentMethod == "COD" ? "Thanh toán khi nhận hàng (COD)" : order.PaymentMethod)}</p>
                        </div>
                        
                        <h3>Chi tiết đơn hàng:</h3>
                        {orderItemsHtml}
                        <p class='total'>Tổng thanh toán: <strong>{order.TotalAmount:N0} đ</strong></p>
                        
                        <p>Nếu bạn có bất kỳ câu hỏi gì hoặc cần hỗ trợ về sản phẩm, vui lòng liên hệ với chúng tôi qua email hoặc số điện thoại được cung cấp trên trang web.</p>
                        
                        <p>Cảm ơn bạn đã chọn mua sắm tại GocNhoDecor!</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 GocNhoDecor - Nơi mang đến không gian sống đẹp cho ngôi nhà của bạn</p>
                    </div>
                </div>
            </body>
            </html>";

            await _emailSender.SendEmailAsync(order.Email, subject, message);
        }
    }
}
