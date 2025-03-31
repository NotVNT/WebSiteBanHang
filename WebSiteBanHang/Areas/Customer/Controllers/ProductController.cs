using Microsoft.AspNetCore.Mvc;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace WebSiteBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly Models.ApplicationDbContext _context;

        public ProductController(
            IProductRepository productRepository, 
            ICategoryRepository categoryRepository,
            Models.ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                // Get the favorite if it exists
                var favorite = await GetFavorite(id, userId);
                ViewBag.IsFavorite = favorite != null;
                ViewBag.FavoriteId = favorite?.Id;
                ViewBag.UserRating = await GetUserRating(id, userId);
            }

            return View(product);
        }

        public async Task<IActionResult> Index(int? categoryId, string sortBy = null, int page = 1)
        {
            const int pageSize = 9; // Show 9 products per page
            
            // Get all products
            var products = await _productRepository.GetAllAsync();
            
            // Filter by category if selected
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
                
                // Get category name for display
                var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
                ViewBag.CategoryName = category?.Name ?? "Unknown Category";
            }

            // Apply sorting
            switch (sortBy)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price).ToList();
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price).ToList();
                    break;
                default:
                    // Default sorting can remain unchanged or you can set a default
                    break;
            }
            
            // Get all categories for sidebar
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.CurrentSortBy = sortBy; // Save current sort for UI state
            
            var paginatedProducts = WebSiteBanHang.Models.ViewModels.PaginatedList<Product>.Create(products, page, pageSize);
            return View(paginatedProducts);
        }

        public async Task<IActionResult> Search(string searchTerm, string sortBy = null, int page = 1)
        {
            const int pageSize = 9;
            
            ViewBag.SearchTerm = searchTerm;
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }
            
            // Get all products
            var allProducts = await _productRepository.GetAllAsync();
            
            // Improve search logic
            var searchTermLower = searchTerm.ToLower().Trim();
            
            // Log all product names to help diagnose the issue
            System.Diagnostics.Debug.WriteLine($"Total products in database: {allProducts.Count()}");
            System.Diagnostics.Debug.WriteLine($"Searching for products containing '{searchTermLower}' in name");
            
            var filteredProducts = allProducts
                .Where(p => p.Name != null && p.Name.ToLower().Contains(searchTermLower))
                .ToList();

            // Apply sorting
            switch (sortBy)
            {
                case "price_asc":
                    filteredProducts = filteredProducts.OrderBy(p => p.Price).ToList();
                    break;
                case "price_desc":
                    filteredProducts = filteredProducts.OrderByDescending(p => p.Price).ToList();
                    break;
                default:
                    // Default sorting remains unchanged
                    break;
            }
            
            // Log detailed information about search results
            System.Diagnostics.Debug.WriteLine($"Found {filteredProducts.Count} products containing '{searchTermLower}':");
            foreach (var product in filteredProducts)
            {
                System.Diagnostics.Debug.WriteLine($"- ID: {product.Id}, Name: \"{product.Name}\"");
            }
            
            // Get all categories for sidebar
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = categories;
            ViewBag.SearchResults = filteredProducts.Count;
            ViewBag.CurrentSortBy = sortBy; // Save current sort for UI state
            
            var paginatedProducts = WebSiteBanHang.Models.ViewModels.PaginatedList<Product>.Create(
                filteredProducts, page, pageSize);
                
            return View("Index", paginatedProducts);
        }

        private async Task<Favorite> GetFavorite(int productId, string userId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(f => f.ProductId == productId && f.UserId == userId);
        }

        private async Task<Rating> GetUserRating(int productId, string userId)
        {
            return await _context.Ratings
                .Where(r => r.ProductId == productId && r.UserId == userId)
                .OrderByDescending(r => r.DateCreated)  // Get the most recent rating
                .FirstOrDefaultAsync();
        }
    }
}
