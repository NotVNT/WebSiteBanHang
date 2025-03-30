using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using WebSiteBanHang.Models;
using WebSiteBanHang.Services;

namespace WebSiteBanHang.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, ICustomEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user == null)
                    {
                        TempData["SuccessMessage"] = "Link đặt lại mật khẩu đã được gửi vào email của bạn. Vui lòng kiểm tra email.";
                        return Page();
                    }

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code, email = Input.Email },
                        protocol: Request.Scheme);

                    var emailSubject = "WebSiteBanHang";
                    var emailBody = $@"
                        <div style='font-family: Arial, sans-serif;'>
                            <h2>Xin chào {Input.Email},</h2>
                            <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản của mình tại WebSiteBanHang.</p>
                            <p>Vui lòng click vào nút bên dưới để đặt lại mật khẩu:</p>
                            <div style='margin: 20px 0;'>
                                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                                   style='background-color: #1a73e8; 
                                          color: white; 
                                          padding: 10px 20px; 
                                          text-decoration: none; 
                                          border-radius: 4px;
                                          display: inline-block;'>
                                    Đặt lại mật khẩu
                                </a>
                            </div>
                            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                            <hr style='margin: 20px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='margin-bottom: 5px;'>Trân trọng,</p>
                            <p style='margin-top: 0;'>WebSiteBanHang</p>
                        </div>";

                    await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailBody);

                    TempData["SuccessMessage"] = "Thông báo đặt lại mật khẩu đã được gửi vào email của bạn. Vui lòng kiểm tra email.";
                    return Page();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.");
                    return Page();
                }
            }

            return Page();
        }
    }
} 