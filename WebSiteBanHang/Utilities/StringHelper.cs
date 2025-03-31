using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebSiteBanHang.Utilities
{
    public static class StringHelper
    {
        /// <summary>
        /// Normalizes a string by removing diacritics (accent marks) and converting to lowercase
        /// </summary>
        /// <param name="text">The input text to normalize</param>
        /// <returns>Normalized text without diacritics in lowercase</returns>
        public static string NormalizeVietnamese(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Convert to lowercase first
            string formD = text.ToLower().Normalize(NormalizationForm.FormD);
            
            // Create a StringBuilder with enough capacity to improve performance
            var sb = new StringBuilder(formD.Length);

            // Iterate through each character and exclude combining diacritics
            foreach (char c in formD)
            {
                // Skip combining diacritics
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Convert specific Vietnamese letters to their non-accent equivalents
            return sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'd');
        }
    }
} 