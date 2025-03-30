using Microsoft.AspNetCore.Mvc;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace WebSiteBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;

        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        private List<Product> GetNewProducts(List<Product> allProducts, int count = 12)
        {
            var random = new Random();
            return allProducts.OrderBy(x => random.Next()).Take(count).ToList();
        }

        public async Task<IActionResult> Index()
        {
            var products = (await _productRepository.GetAllAsync())
                .OrderByDescending(p => p.Id)
                .ToList();
            
            // Get random products for "New Products" section
            ViewBag.NewProducts = GetNewProducts(products);

            return View(products);
        }
    }
}
