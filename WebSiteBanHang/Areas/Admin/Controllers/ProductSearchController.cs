using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductSearchController : Controller
    {
        private readonly IProductRepository _productRepository;

        public ProductSearchController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm" });
            }

            var products = await _productRepository.GetAllAsync();
            var searchResults = products
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price.ToString("N0") + " đ",
                    image = p.ImageUrl ?? "/images/no-image.png"
                })
                .ToList();

            return Json(new { success = true, results = searchResults });
        }
    }
} 