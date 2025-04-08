using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using WebSiteBanHang.Models.ViewModels;

namespace WebSiteBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IOrderRepository _orderRepository;

        public CartController(
            ApplicationDbContext context, 
            IProductRepository productRepository, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOrderRepository orderRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _orderRepository = orderRepository;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Kiểm tra nếu người dùng là Admin, chuyển hướng về trang chủ Admin
            if (User.IsInRole("Admin"))
            {
                TempData["InfoMessage"] = "Admin không thể sử dụng chức năng giỏ hàng.";
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();
            
            return View(cartItems);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int id, string returnUrl)
        {
            // Kiểm tra nếu người dùng là Admin, chuyển hướng về trang chủ Admin
            if (User.IsInRole("Admin"))
            {
                TempData["InfoMessage"] = "Admin không thể sử dụng chức năng giỏ hàng.";
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Check if the item is already in the cart
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == userId);

            if (cartItem != null)
            {
                // Update quantity if already in cart
                cartItem.Quantity += 1;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                // Add new item to cart
                cartItem = new CartItem
                {
                    ProductId = id,
                    UserId = userId ?? string.Empty, // Add null-coalescing operator here
                    Quantity = 1,
                    UnitPrice = product.Price
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Sản phẩm đã được thêm vào giỏ hàng";
            
            // If returnUrl is provided, redirect to that URL, otherwise go to product display
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("Display", "Product", new { id = id });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            // Kiểm tra nếu người dùng là Admin, chuyển hướng về trang chủ Admin
            if (User.IsInRole("Admin"))
            {
                TempData["InfoMessage"] = "Admin không thể sử dụng chức năng giỏ hàng.";
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (cartItem == null)
            {
                return NotFound();
            }
            
            if (quantity <= 0)
            {
                // Lưu tên sản phẩm trước khi xóa
                var productName = cartItem.Product?.Name ?? "Sản phẩm";
                
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                
                TempData["ErrorMessage"] = $"<strong class=\"product-name\">{productName}</strong> đã được xóa khỏi giỏ hàng";
            }
            else
            {
                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Số lượng sản phẩm đã được cập nhật";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            // Kiểm tra nếu người dùng là Admin, chuyển hướng về trang chủ Admin
            if (User.IsInRole("Admin"))
            {
                TempData["InfoMessage"] = "Admin không thể sử dụng chức năng giỏ hàng.";
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (cartItem == null)
            {
                return NotFound();
            }
            
            // Lưu tên sản phẩm trước khi xóa
            var productName = cartItem.Product?.Name ?? "Sản phẩm";
            
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            
            // Format the message with HTML to emphasize the product name
            TempData["ErrorMessage"] = $"<strong class=\"product-name\">{productName}</strong> đã được xóa khỏi giỏ hàng";
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Checkout(string selectedItems)
        {
            if (string.IsNullOrEmpty(selectedItems))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var selectedItemIds = selectedItems.Split(',').Select(int.Parse).ToList();

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && selectedItemIds.Contains(c.Id))
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng";
                return RedirectToAction(nameof(Index));
            }

            // Chuyển đổi sang ViewModel để hiển thị
            var cartItemViewModels = cartItems.Select(c => new CartItemViewModel
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product.Name,
                ProductImage = c.Product.ImageUrl,
                Quantity = c.Quantity,
                UnitPrice = c.UnitPrice
            }).ToList();

            decimal totalAmount = cartItems.Sum(c => c.Quantity * c.UnitPrice);

            // Lấy thông tin người dùng để điền mẫu
            var user = await _userManager.GetUserAsync(User);

            var checkoutViewModel = new MockCheckoutViewModel
            {
                FullName = user?.FullName,
                Email = user?.Email,
                PhoneNumber = user?.PhoneNumber,
                CartItems = cartItemViewModels ?? new List<CartItemViewModel>(),
                TotalAmount = totalAmount,
                PaymentMethod = "COD" // Mặc định
            };

            // Lưu danh sách sản phẩm đã chọn vào TempData để sử dụng sau này
            TempData["SelectedItemIds"] = string.Join(",", selectedItemIds);

            return View("CheckoutProcess", checkoutViewModel);
        }

        [HttpPost]
        [Authorize]
       [HttpPost]
[Authorize]
public async Task<IActionResult> PlaceOrder(MockCheckoutViewModel model)
{
    try
    {
        // Clear ModelState errors for optional fields
        ModelState.Remove("Notes");
        ModelState.Remove("PromotionCode");

        // Validate required fields
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError("FullName", "Vui lòng nhập họ tên");
        }
        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            ModelState.AddModelError("PhoneNumber", "Vui lòng nhập số điện thoại");
        }
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            ModelState.AddModelError("Email", "Vui lòng nhập email");
        }
        if (string.IsNullOrWhiteSpace(model.ShippingAddress))
        {
            ModelState.AddModelError("ShippingAddress", "Vui lòng nhập địa chỉ giao hàng");
        }
        if (string.IsNullOrWhiteSpace(model.PaymentMethod))
        {
            ModelState.AddModelError("PaymentMethod", "Vui lòng chọn phương thức thanh toán");
        }

        // Check if there are any validation errors for required fields
        if (!ModelState.IsValid)
        {
            // Reload cart items before returning the view
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentCartItems = await _context.CartItems
                .Where(c => c.UserId == currentUserId)
                .Include(c => c.Product)
                .ToListAsync();

            // Convert to view models
            model.CartItems = currentCartItems.Select(c => new CartItemViewModel
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product.Name,
                ProductImage = c.Product.ImageUrl,
                Quantity = c.Quantity,
                UnitPrice = c.UnitPrice
            }).ToList();

            return View("CheckoutProcess", model);
        }

        // Rest of your existing logic remains unchanged
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Get cart items
        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .ToListAsync();

        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng";
            return RedirectToAction(nameof(Index));
        }

        // Calculate order totals
        decimal orderTotalAmount = cartItems.Sum(c => c.Quantity * c.UnitPrice);
        
        // Process promotion code if provided
        int? promotionId = null;
        decimal discountAmount = 0;
        
        if (!string.IsNullOrEmpty(model.PromotionCode))
        {
            var promotionRepository = HttpContext.RequestServices.GetService<IPromotionRepository>();
            if (promotionRepository != null)
            {
                var promotion = await promotionRepository.GetByCodeAsync(model.PromotionCode);
                if (promotion != null && 
                    promotion.IsActive && 
                    promotion.StartDate <= DateTime.Now && 
                    (!promotion.EndDate.HasValue || promotion.EndDate >= DateTime.Now) &&
                    (!promotion.MaxUseTimes.HasValue || promotion.UsedTimes < promotion.MaxUseTimes.Value) &&
                    (!promotion.MinimumOrderAmount.HasValue || orderTotalAmount >= promotion.MinimumOrderAmount.Value))
                {
                    promotionId = promotion.Id;
                    if (promotion.IsPercentage)
                    {
                        discountAmount = Math.Round(orderTotalAmount * promotion.DiscountAmount / 100, 0);
                    }
                    else
                    {
                        discountAmount = promotion.DiscountAmount;
                    }
                    await promotionRepository.IncrementUsageAsync(promotion.Id);
                }
            }
        }
        else if (model.DiscountAmount > 0)
        {
            discountAmount = model.DiscountAmount;
        }

        // Create a new order
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now,
            TotalAmount = orderTotalAmount - discountAmount,
            FullName = model.FullName?.Trim() ?? "",
            Email = model.Email?.Trim() ?? "",
            PhoneNumber = model.PhoneNumber?.Trim() ?? "",
            ShippingAddress = model.ShippingAddress?.Trim() ?? "",
            Notes = model.Notes?.Trim() ?? "",
            PaymentMethod = model.PaymentMethod?.Trim() ?? "COD",
            PromotionId = promotionId,
            DiscountAmount = discountAmount,
            Status = OrderStatus.Pending,
            PaymentStatus = false,
            TrackingNumber = GenerateTrackingNumber(),
            CancellationReason = ""
        };
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        foreach (var cartItem in cartItems)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice
            };
            _context.OrderItems.Add(orderItem);
        }
        
        await _context.SaveChangesAsync();
        
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Đặt hàng thành công!";
        return RedirectToAction("OrderComplete", new { orderId = order.Id, amount = order.TotalAmount });
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", "Đã xảy ra lỗi khi đặt hàng. Vui lòng thử lại.");
        return View("CheckoutProcess", model);
    }
}

        public IActionResult OrderComplete(int orderId, decimal amount)
        {
            // Lấy thông tin đơn hàng từ database và bao gồm các entity liên quan
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefault(o => o.Id == orderId);
            
            // Truyền dữ liệu qua ViewBag
            ViewBag.OrderId = orderId;
            ViewBag.TotalAmount = amount;
            
            return View(order);
        }

        // Helper method to generate a tracking number
        private string GenerateTrackingNumber()
        {
            // Generate a simple tracking number: Current date + random number
            return $"TN{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCartAjax(int id, string returnUrl)
        {
            // Kiểm tra nếu người dùng là Admin, chuyển hướng về trang chủ Admin
            if (User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Admin không thể sử dụng chức năng giỏ hàng." });
            }
            
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Check if the item is already in the cart
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == userId);

            if (cartItem != null)
            {
                // Update quantity if already in cart
                cartItem.Quantity += 1;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                // Add new item to cart
                cartItem = new CartItem
                {
                    ProductId = id,
                    UserId = userId ?? string.Empty,
                    Quantity = 1,
                    UnitPrice = product.Price
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            
            // Get updated cart count
            int cartCount = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
            
            return Json(new { 
                success = true, 
                message = "Sản phẩm đã được thêm vào giỏ hàng", 
                cartCount = cartCount 
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ClearCart()
        {
            // Kiểm tra nếu người dùng là Admin, chuyển hướng về trang chủ Admin
            if (User.IsInRole("Admin"))
            {
                TempData["InfoMessage"] = "Admin không thể sử dụng chức năng giỏ hàng.";
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();
            
            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "Tất cả sản phẩm đã được xóa khỏi giỏ hàng";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromCartAjax(int id)
        {
            if (User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Admin không thể sử dụng chức năng giỏ hàng." });
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }
            
            var productName = cartItem.Product?.Name ?? "Sản phẩm";
            
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            
            // Get updated cart count
            int cartCount = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
            
            return Json(new { 
                success = true, 
                message = $"{productName} đã được xóa khỏi giỏ hàng", 
                cartCount = cartCount 
            });
        }

        // Add a new action method to validate promotion codes
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ValidatePromotion(string code, decimal amount)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { isValid = false, message = "Mã khuyến mãi không được để trống" });
            }

            try
            {
                // Trim code to remove any whitespace
                code = code.Trim();
                
                // Assuming you have a promotion repository or service
                var promotionRepository = HttpContext.RequestServices.GetService<IPromotionRepository>();
                
                if (promotionRepository == null)
                {
                    return Json(new { isValid = false, message = "Không thể xác thực mã khuyến mãi" });
                }

                // Get all promotions and filter case-insensitively
                var allPromotions = await promotionRepository.GetAllAsync();
                var promotion = allPromotions.FirstOrDefault(p => 
                    string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
                
                if (promotion == null)
                {
                    return Json(new { isValid = false, message = "Mã khuyến mãi không tồn tại" });
                }

                // Check if the promotion is active
                if (!promotion.IsActive)
                {
                    return Json(new { isValid = false, message = "Mã khuyến mãi không hoạt động" });
                }

                // Check if the promotion is within valid date range
                var now = DateTime.Now;
                
                if (promotion.StartDate > now)
                {
                    return Json(new { isValid = false, message = $"Mã khuyến mãi chỉ có hiệu lực từ {promotion.StartDate.ToString("dd/MM/yyyy")}" });
                }

                if (promotion.EndDate.HasValue && promotion.EndDate < now)
                {
                    return Json(new { isValid = false, message = $"Mã khuyến mãi đã hết hạn vào {promotion.EndDate.Value.ToString("dd/MM/yyyy")}" });
                }

                // Check usage limit
                if (promotion.MaxUseTimes.HasValue && promotion.UsedTimes >= promotion.MaxUseTimes.Value)
                {
                    return Json(new { isValid = false, message = "Mã khuyến mãi đã hết lượt sử dụng" });
                }

                // Check minimum order amount
                if (promotion.MinimumOrderAmount.HasValue && amount < promotion.MinimumOrderAmount.Value)
                {
                    return Json(new { 
                        isValid = false, 
                        message = $"Đơn hàng tối thiểu phải từ {promotion.MinimumOrderAmount.Value.ToString("N0")}đ để áp dụng mã này" 
                    });
                }

                // Calculate discount amount
                decimal discountAmount = 0;
                if (promotion.IsPercentage)
                {
                    // Apply percentage discount
                    discountAmount = Math.Round(amount * promotion.DiscountAmount / 100, 0);
                }
                else
                {
                    // Apply fixed amount discount
                    discountAmount = promotion.DiscountAmount;
                }
                
                // Return success with discount information
                return Json(new { 
                    isValid = true, 
                    message = $"Áp dụng mã giảm giá thành công! {(promotion.IsPercentage ? promotion.DiscountAmount + "%" : "")}",
                    discountAmount = discountAmount,
                    promotionId = promotion.Id,
                    isPercentage = promotion.IsPercentage,
                    discountValue = promotion.DiscountAmount
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { isValid = false, message = "Đã xảy ra lỗi khi xác thực mã khuyến mãi" });
            }
        }
    }
}