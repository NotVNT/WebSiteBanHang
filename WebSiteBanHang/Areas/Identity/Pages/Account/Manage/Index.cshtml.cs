using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            [StringLength(100, ErrorMessage = "Họ tên không được vượt quá {1} ký tự")]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [RegularExpression(@"^(0\d{9,10})$", ErrorMessage = "Số điện thoại phải có 10 hoặc 11 chữ số và bắt đầu bằng số 0")]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
            [DataType(DataType.Date)]
            [Display(Name = "Ngày sinh")]
            public DateTime BirthDate { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Input = new InputModel
            {
                FullName = user.FullName,
                PhoneNumber = phoneNumber,
                Email = email,
                BirthDate = user.BirthDate
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải thông tin người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải thông tin người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Lỗi khi cập nhật số điện thoại.";
                    return RedirectToPage();
                }
            }

            // Update FullName
            if (user.FullName != Input.FullName)
            {
                user.FullName = Input.FullName;
            }

            // Update BirthDate
            if (user.BirthDate != Input.BirthDate)
            {
                user.BirthDate = Input.BirthDate;
            }

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thông tin tài khoản của bạn đã được cập nhật thành công";
            return RedirectToPage();
        }
    }
}
