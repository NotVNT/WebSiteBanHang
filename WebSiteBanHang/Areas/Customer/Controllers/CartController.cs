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
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (cartItem == null)
            {
                return NotFound();
            }
            
            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
            }
            
            await _context.SaveChangesAsync();
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
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (cartItem == null)
            {
                return NotFound();
            }
            
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            
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
        public async Task<IActionResult> PlaceOrder(MockCheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CheckoutProcess", model);
            }

            // Lấy danh sách sản phẩm đã chọn từ TempData
            var selectedItemsString = TempData["SelectedItemIds"]?.ToString();
            if (string.IsNullOrEmpty(selectedItemsString))
            {
                return RedirectToAction(nameof(Index));
            }

            var selectedItemIds = selectedItemsString.Split(',').Select(int.Parse).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && selectedItemIds.Contains(c.Id))
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào trong giỏ hàng để đặt hàng";
                return RedirectToAction(nameof(Index));
            }

            // Tính tổng tiền đơn hàng
            decimal totalAmount = cartItems.Sum(c => c.Quantity * c.UnitPrice);

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                PaymentStatus = false,
                PaymentMethod = model.PaymentMethod,
                TrackingNumber = GenerateTrackingNumber(),
                FullName = model.FullName,
                ShippingAddress = model.ShippingAddress,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Notes = model.Notes
            };

            // Thêm các sản phẩm vào đơn hàng
            foreach (var item in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            // Lưu đơn hàng vào database
            var orderId = await _orderRepository.CreateOrderAsync(order);

            // Xóa các mục đã chọn khỏi giỏ hàng
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Thêm thông báo thành công
            TempData["SuccessMessage"] = "Đặt hàng thành công! Cảm ơn bạn đã mua hàng.";

            // Chuyển hướng đến trang OrderComplete
            return RedirectToAction(nameof(OrderComplete), new { orderId = orderId, amount = totalAmount });
        }

        public IActionResult OrderComplete(int orderId, decimal amount)
        {
            // Lấy thông tin đơn hàng từ database
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            
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
    }
}