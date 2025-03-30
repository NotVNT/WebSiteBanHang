using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebSiteBanHang.Models; // Only import Models namespace

namespace WebSiteBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class RatingsController : Controller
    {
        private readonly Models.ApplicationDbContext _context; // Explicitly use Models namespace
        private readonly UserManager<ApplicationUser> _userManager;

        public RatingsController(Models.ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var ratings = await _context.Ratings
                    .Where(r => r.UserId == userId)
                    .Include(r => r.Product)
                    .OrderByDescending(r => r.DateCreated)
                    .ToListAsync();
                
                ViewData["ActivePage"] = "Ratings";
                return View(ratings);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải đánh giá: " + ex.Message;
                return View(new List<Rating>());
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int ratingValue, string comment)
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
                
                // Validate rating value
                if (ratingValue < 1 || ratingValue > 5)
                {
                    TempData["ErrorMessage"] = "Giá trị đánh giá không hợp lệ (1-5)";
                    return RedirectToAction("Display", "Product", new { id = productId, area = "Customer" });
                }
                
                // Always create a new rating entry
                _context.Ratings.Add(new Rating
                {
                    UserId = userId,
                    ProductId = productId,
                    RatingValue = ratingValue,
                    Comment = comment,
                    DateCreated = DateTime.Now
                });
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm";
                
                return RedirectToAction("Display", "Product", new { id = productId, area = "Customer" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Display", "Product", new { id = productId, area = "Customer" });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
                    
                if (rating != null)
                {
                    _context.Ratings.Remove(rating);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã xóa đánh giá";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
