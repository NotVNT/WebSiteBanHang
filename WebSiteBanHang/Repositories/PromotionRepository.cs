using Microsoft.EntityFrameworkCore;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly ApplicationDbContext _context;

        public PromotionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Promotion>> GetAllAsync()
        {
            return await _context.Promotions.ToListAsync();
        }

        public async Task<Promotion?> GetByIdAsync(int id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public async Task<Promotion?> GetByCodeAsync(string code)
        {
            return await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.Promotions
                .Where(p => p.IsActive && 
                           p.StartDate <= now && 
                           (p.EndDate == null || p.EndDate >= now))
                .ToListAsync();
        }

        public async Task AddAsync(Promotion promotion)
        {
            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsValidPromotionAsync(string code, decimal orderAmount)
        {
            var now = DateTime.Now;
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == code &&
                                         p.IsActive &&
                                         p.StartDate <= now &&
                                         (p.EndDate == null || p.EndDate >= now));

            if (promotion == null)
                return false;

            // Check minimum order amount
            if (promotion.MinimumOrderAmount.HasValue && orderAmount < promotion.MinimumOrderAmount.Value)
                return false;

            // Check usage limit
            if (promotion.MaxUseTimes.HasValue && promotion.UsedTimes >= promotion.MaxUseTimes.Value)
                return false;

            return true;
        }

        public async Task IncrementUsageAsync(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                promotion.UsedTimes++;
                await _context.SaveChangesAsync();
            }
        }
    }
} 