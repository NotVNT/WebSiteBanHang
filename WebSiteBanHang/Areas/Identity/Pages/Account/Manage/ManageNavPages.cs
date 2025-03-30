using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebSiteBanHang.Areas.Identity.Pages.Account.Manage
{
    public static class ManageNavPages
    {
        public static string Index => "Index";
        public static string ChangePassword => "ChangePassword";
        public static string ExternalLogins => "ExternalLogins";
        public static string PersonalData => "PersonalData";
        public static string Favorites => "Favorites";
        public static string Ratings => "Ratings";

        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);
        public static string ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);
        public static string FavoritesNavClass(ViewContext viewContext) => PageNavClass(viewContext, Favorites);
        public static string RatingsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Ratings);

        // General method for any page
        public static string GetNavClass(ViewContext viewContext, string page) => PageNavClass(viewContext, page);

        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }

        public static bool IsActivePage(ViewContext viewContext, string page)
        {
            var currentPage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(currentPage, page, StringComparison.OrdinalIgnoreCase);
        }
    }
}
