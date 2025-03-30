using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly Models.ApplicationDbContext _context; // Explicitly use Models namespace
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(Models.ApplicationDbContext context, UserManager<ApplicationUser> userManager) // Explicitly use Models namespace
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var favorites = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Product)
                    .ToListAsync();
                
                ViewData["ActivePage"] = "Favorites";
                return View(favorites);
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách yêu thích: " + ex.Message;
                return View(new List<Favorite>());
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Home", new { area = "Customer" });
                }
                
                // Check if already in favorites
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
                    
                if (existingFavorite == null)
                {
                    // Add to favorites
                    _context.Favorites.Add(new Favorite
                    {
                        UserId = userId,
                        ProductId = productId,
                        DateAdded = DateTime.Now
                    });
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã thêm vào danh sách yêu thích";
                }
                else
                {
                    TempData["InfoMessage"] = "Sản phẩm này đã có trong danh sách yêu thích";
                }
                
                return RedirectToAction("Display", "Product", new { id = productId, area = "Customer" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Display", "Product", new { id = productId, area = "Customer" });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int id, int? returnToProduct = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
                    
                if (favorite != null)
                {
                    // Store the product ID before removing the favorite
                    var productId = favorite.ProductId;
                    
                    _context.Favorites.Remove(favorite);
                    await _context.SaveChangesAsync();
                    
                    // Check if it's an AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích" });
                    }
                    
                    TempData["SuccessMessage"] = "Đã xóa khỏi danh sách yêu thích";
                    
                    // If returnToProduct parameter is provided, return to the product display page
                    if (returnToProduct.HasValue)
                    {
                        return RedirectToAction("Display", "Product", new { area = "Customer", id = productId });
                    }
                }
                else if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Không tìm thấy mục yêu thích" });
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
                }
                
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
