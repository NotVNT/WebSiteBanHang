using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using WebSiteBanHang.Models.ViewModels;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // Display list of products
        public async Task<IActionResult> Index(int? categoryId, int page = 1)
        {
            const int pageSize = 10; // Show 10 products per page
            
            // Get all products
            var products = await _productRepository.GetAllAsync();
            
            // Filter by category if selected
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
            }

            // Get all categories for the dropdown
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.SelectedCategoryId = categoryId;
            
            var paginatedProducts = PaginatedList<Product>.Create(products, page, pageSize);
            return View(paginatedProducts);
        }

        // Display form to add a new product
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        // Handle adding a new product
        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageFile)
        {
            // Explicitly check for category selection
            if (product.CategoryId == 0)
            {
                ModelState.AddModelError("CategoryId", "Hãy chọn danh mục sản phẩm");
            }
            else
            {
                // Verify the category exists in the database
                var categoryExists = await _categoryRepository.GetByIdAsync(product.CategoryId);
                if (categoryExists == null)
                {
                    ModelState.AddModelError("CategoryId", "Danh mục không tồn tại");
                }
            }
            
            if (ModelState.IsValid)
            {
                // Process image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageFile);
                }
                else
                {
                    ModelState.AddModelError("", "Vui lòng chọn ảnh cho sản phẩm");
                    var categories = await _categoryRepository.GetAllAsync();
                    ViewBag.Categories = new SelectList(categories, "Id", "Name");
                    return View(product);
                }
                
                try
                {
                    await _productRepository.AddAsync(product);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the exception
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu sản phẩm: " + ex.Message);
                }
            }
            
            var categoriesList = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categoriesList, "Id", "Name", product.CategoryId);
            return View(product);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var savePath = Path.Combine("wwwroot/images", image.FileName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/images/" + image.FileName;
        }

        // Display product details
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Display form to update a product
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // Handle updating a product
        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Always retrieve the existing product to access its current ImageUrl
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // If model validation fails, preserve the existing image URL
            if (!ModelState.IsValid)
            {
                // Keep the existing image URL when validation fails
                product.ImageUrl = existingProduct.ImageUrl;
                
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                return View(product);
            }

            // Process the submitted form (valid case)
            if (imageFile != null && imageFile.Length > 0)
            {
                // If there's a new image, update the ImageUrl
                product.ImageUrl = await SaveImage(imageFile);
            }
            else
            {
                // If no new image provided, keep the existing one
                product.ImageUrl = existingProduct.ImageUrl;
            }

            // Update the product properties
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.ImageUrl = product.ImageUrl;

            await _productRepository.UpdateAsync(existingProduct);
            return RedirectToAction(nameof(Index));
        }

        // Display form to confirm product deletion
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Handle product deletion
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

