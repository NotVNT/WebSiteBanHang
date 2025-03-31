using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebSiteBanHang.Repositories;
using Microsoft.AspNetCore.Identity;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            UserManager<ApplicationUser> userManager)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get total orders count
            ViewBag.TotalOrders = await _orderRepository.GetOrderCountAsync();

            // Get count of customers who have placed orders (not just registered users)
            ViewBag.TotalCustomers = await _orderRepository.GetUniqueCustomerCountAsync();

            // Get total products count
            ViewBag.TotalProducts = await _productRepository.GetProductCountAsync();

            // Get total revenue from completed orders
            ViewBag.TotalRevenue = await _orderRepository.GetTotalRevenueAsync();

            return View();
        }
    }
}
