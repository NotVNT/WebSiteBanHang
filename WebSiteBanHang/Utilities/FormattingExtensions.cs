using System.Globalization;

namespace WebSiteBanHang.Utilities
{
    public static class FormattingExtensions
    {
        public static string ToVnd(this decimal price)
        {
            // Format as Vietnamese currency
            return string.Format(new CultureInfo("vi-VN"), "{0:C0}", price);
        }
        
        // Alternate method that directly formats with VNĐ suffix
        public static string ToVndCustom(this decimal price)
        {
            return string.Format("{0:#,##0} VNĐ", price);
        }
    }
}
