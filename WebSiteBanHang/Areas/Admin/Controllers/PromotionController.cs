using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromotionController : Controller
    {
        private readonly IPromotionRepository _promotionRepository;

        public PromotionController(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        // Display list of promotions
        public async Task<IActionResult> Index()
        {
            var promotions = await _promotionRepository.GetAllAsync();
            return View(promotions);
        }

        // Display form to create a new promotion
        public IActionResult Create()
        {
            return View();
        }

        // Handle creating a new promotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate promotion code
                var existingPromotion = await _promotionRepository.GetByCodeAsync(promotion.Code);
                if (existingPromotion != null)
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại");
                    return View(promotion);
                }

                await _promotionRepository.AddAsync(promotion);
                TempData["SuccessMessage"] = "Tạo mã khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // Display form to edit a promotion
        public async Task<IActionResult> Edit(int id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // Handle updating a promotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check for duplicate code but exclude the current promotion
                var existingPromotion = await _promotionRepository.GetByCodeAsync(promotion.Code);
                if (existingPromotion != null && existingPromotion.Id != id)
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại");
                    return View(promotion);
                }

                await _promotionRepository.UpdateAsync(promotion);
                TempData["SuccessMessage"] = "Cập nhật mã khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // Display details of a promotion
        public async Task<IActionResult> Details(int id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // Display confirmation for deleting a promotion
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // Handle deleting a promotion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _promotionRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = "Xóa mã khuyến mãi thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Toggle promotion active status
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            promotion.IsActive = !promotion.IsActive;
            await _promotionRepository.UpdateAsync(promotion);
            
            return RedirectToAction(nameof(Index));
        }
    }
} 