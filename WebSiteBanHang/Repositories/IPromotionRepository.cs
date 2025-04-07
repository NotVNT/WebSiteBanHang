using WebSiteBanHang.Models;

namespace WebSiteBanHang.Repositories
{
    public interface IPromotionRepository
    {
        Task<IEnumerable<Promotion>> GetAllAsync();
        Task<Promotion?> GetByIdAsync(int id);
        Task<Promotion?> GetByCodeAsync(string code);
        Task<IEnumerable<Promotion>> GetActivePromotionsAsync();
        Task AddAsync(Promotion promotion);
        Task UpdateAsync(Promotion promotion);
        Task DeleteAsync(int id);
        Task<bool> IsValidPromotionAsync(string code, decimal orderAmount);
        Task IncrementUsageAsync(int id);
    }
} 