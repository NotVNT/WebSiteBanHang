using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Models.ViewModels;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RatingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RatingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? productId, string sortOrder, int pageNumber = 1)
        {
            const int pageSize = 10;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParam"] = String.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
            ViewData["RatingSortParam"] = sortOrder == "rating_desc" ? "rating" : "rating_desc";
            
            // Base query
            IQueryable<Rating> ratingsQuery = _context.Ratings
                .Include(r => r.User)
                .Include(r => r.Product);
                
            // Apply product filtering if provided
            if (productId.HasValue)
            {
                ratingsQuery = ratingsQuery.Where(r => r.ProductId == productId.Value);
                ViewData["FilteredProductId"] = productId.Value;
                ViewData["FilteredProductName"] = await _context.Products
                    .Where(p => p.Id == productId.Value)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();
            }
            
            // Apply sorting
            ratingsQuery = sortOrder switch
            {
                "date_asc" => ratingsQuery.OrderBy(r => r.DateCreated),
                "rating" => ratingsQuery.OrderBy(r => r.RatingValue),
                "rating_desc" => ratingsQuery.OrderByDescending(r => r.RatingValue),
                _ => ratingsQuery.OrderByDescending(r => r.DateCreated), // Default sort by newest first
            };
            
            var totalItems = await ratingsQuery.CountAsync();
            
            var ratings = await ratingsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            var paginatedList = new PaginatedList<Rating>(
                ratings, 
                totalItems, 
                pageNumber, 
                pageSize);
                
            // Get some statistics for the admin dashboard
            ViewBag.TotalRatings = totalItems;
            ViewBag.AverageRating = await _context.Ratings.AverageAsync(r => r.RatingValue);
            ViewBag.RatingStats = await _context.Ratings
                .GroupBy(r => r.RatingValue)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Rating)
                .ToDictionaryAsync(x => x.Rating, x => x.Count);
                
            return View(paginatedList);
        }
        
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var rating = await _context.Ratings
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (rating == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá này";
                return RedirectToAction(nameof(Index));
            }
            
            string productName = rating.Product?.Name ?? "sản phẩm";
            
            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã xóa đánh giá cho {productName}";
            
            return RedirectToAction(nameof(Index));
        }
    }
} 