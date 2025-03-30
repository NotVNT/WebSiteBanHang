using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebSiteBanHang.Services
{
    // Sử dụng trực tiếp interface từ Microsoft.AspNetCore.Identity.UI.Services
    public interface ICustomEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
} 