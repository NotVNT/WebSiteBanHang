using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace WebSiteBanHang.Models.ViewModels
{
    public class OrderCancellationViewModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lý do hủy đơn hàng")]
        [Display(Name = "Lý do hủy")]
        public string CancellationReasonId { get; set; }

        [Display(Name = "Lý do khác")]
        [StringLength(500, ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự")]
        public string CustomReason { get; set; } // No [Required] attribute

        public List<SelectListItem> CancellationReasons => new List<SelectListItem>
        {
            new SelectListItem { Value = "change_mind", Text = "Thay đổi ý định mua hàng" },
            new SelectListItem { Value = "wrong_info", Text = "Sai thông tin đơn hàng" },
            new SelectListItem { Value = "long_delivery", Text = "Thời gian giao hàng quá lâu" },
            new SelectListItem { Value = "financial", Text = "Tài chính không đủ" },
            new SelectListItem { Value = "not_needed", Text = "Sản phẩm không còn cần thiết" },
            new SelectListItem { Value = "price", Text = "Giá cả không hợp lý" },
            new SelectListItem { Value = "no_contact", Text = "Không liên lạc được với cửa hàng" },
            new SelectListItem { Value = "personal", Text = "Lý do cá nhân" },
            new SelectListItem { Value = "expectation", Text = "Sản phẩm không đúng kỳ vọng" },
            new SelectListItem { Value = "other", Text = "Lý do khác" }
        };

        public string GetFullCancellationReason()
        {
            if (CancellationReasonId == "other" && !string.IsNullOrWhiteSpace(CustomReason))
            {
                return CustomReason;
            }

            var selectedReason = CancellationReasons.Find(r => r.Value == CancellationReasonId);
            return selectedReason?.Text ?? "Không có lý do cụ thể";
        }
    }
}