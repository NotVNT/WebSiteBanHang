using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.Models.ViewModels
{
    public class ContentViewModel
    {
        public Content Content { get; set; }
        public List<SelectListItem> ContentTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Page", Text = "Trang tĩnh" },
            new SelectListItem { Value = "AboutUs", Text = "Giới thiệu" },
            new SelectListItem { Value = "Contact", Text = "Liên hệ" },
            new SelectListItem { Value = "PrivacyPolicy", Text = "Chính sách bảo mật" },
            new SelectListItem { Value = "TermsOfUse", Text = "Điều khoản sử dụng" },
            new SelectListItem { Value = "PurchaseGuide", Text = "Hướng dẫn mua hàng" },
            new SelectListItem { Value = "FAQ", Text = "Câu hỏi thường gặp" },
            new SelectListItem { Value = "ShippingPolicy", Text = "Chính sách vận chuyển" },
            new SelectListItem { Value = "ReturnPolicy", Text = "Chính sách đổi trả" }
        };
    }

    public class ContentListViewModel
    {
        public List<Content> Contents { get; set; }
        public string SearchTerm { get; set; }
        public string ContentType { get; set; }
        public List<SelectListItem> ContentTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Tất cả" },
            new SelectListItem { Value = "Page", Text = "Trang tĩnh" },
            new SelectListItem { Value = "AboutUs", Text = "Giới thiệu" },
            new SelectListItem { Value = "Contact", Text = "Liên hệ" },
            new SelectListItem { Value = "PrivacyPolicy", Text = "Chính sách bảo mật" },
            new SelectListItem { Value = "TermsOfUse", Text = "Điều khoản sử dụng" },
            new SelectListItem { Value = "PurchaseGuide", Text = "Hướng dẫn mua hàng" },
            new SelectListItem { Value = "FAQ", Text = "Câu hỏi thường gặp" },
            new SelectListItem { Value = "ShippingPolicy", Text = "Chính sách vận chuyển" },
            new SelectListItem { Value = "ReturnPolicy", Text = "Chính sách đổi trả" }
        };
    }
} 