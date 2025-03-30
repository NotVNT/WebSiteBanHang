using Microsoft.AspNetCore.Mvc;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using WebSiteBanHang.Models.ViewModels;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        
        public CategoriesController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }
        
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            const int pageSize = 10; // Set the number of categories per page
            var allCategories = await _categoryRepository.GetAllAsync();
            
            // Apply search filter if search term is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                allCategories = allCategories.Where(c => c.Name.ToLower().Contains(searchTerm)).ToList();
                ViewBag.SearchTerm = searchTerm;
            }
            
            var paginatedCategories = PaginatedList<Category>.Create(allCategories, page, pageSize);
            return View(paginatedCategories);
        }
        
        public async Task<IActionResult> Display(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        
        public IActionResult Add()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Add(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepository.AddAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }
        
        public async Task<IActionResult> Update(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        
        [HttpPost]
        public async Task<IActionResult> Update(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                await _categoryRepository.UpdateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }
        
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            // Check if there are products using this category
            var products = await _productRepository.GetAllAsync();
            var productsInCategory = products.Where(p => p.CategoryId == id).ToList();
            
            ViewBag.HasProducts = productsInCategory.Any();
            ViewBag.ProductCount = productsInCategory.Count;
            
            return View(category);
        }
        
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Double-check if there are any products using this category
            var products = await _productRepository.GetAllAsync();
            if (products.Any(p => p.CategoryId == id))
            {
                TempData["ErrorMessage"] = "Cannot delete this category because it contains products.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

