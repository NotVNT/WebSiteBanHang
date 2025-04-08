using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebSiteBanHang.Models.ViewModels
{
    public class OrderFilterViewModel
    {
        // Status filter
        public OrderStatus? Status { get; set; }
        
        // Cancellation Type filter
        [Display(Name = "Người hủy")]
        public CancellationType? CancellationType { get; set; }
        
        // Date range filters
        [DataType(DataType.Date)]
        [Display(Name = "Từ ngày")]
        public DateTime? StartDate { get; set; }
        
        [DataType(DataType.Date)]
        [Display(Name = "Đến ngày")]
        public DateTime? EndDate { get; set; }
        
        // Quick date filter options
        public string DateRange { get; set; }
        
        // New filters
        [Display(Name = "Tìm khách hàng")]
        public string CustomerSearch { get; set; }
        
        [Display(Name = "Mã đơn hàng")]
        public int? OrderId { get; set; }
        
        // New price range filter
        [Display(Name = "Giá từ")]
        public decimal? MinAmount { get; set; }
        
        [Display(Name = "Giá đến")]
        public decimal? MaxAmount { get; set; }
        
        // New sorting properties
        [Display(Name = "Sắp xếp theo")]
        public string SortBy { get; set; }
        
        [Display(Name = "Thứ tự")]
        public string SortDirection { get; set; }
        
        // Helper properties for UI
        public List<SelectListItem> StatusOptions
        {
            get
            {
                var options = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tất cả trạng thái" }
                };
                
                foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
                {
                    string statusText = status switch
                    {
                        OrderStatus.Pending => "Chờ xác nhận",
                        OrderStatus.Confirmed => "Đã xác nhận",
                        OrderStatus.Completed => "Đã hoàn thành",
                        OrderStatus.Cancelled => "Đã hủy",
                        _ => status.ToString()
                    };
                    
                    options.Add(new SelectListItem
                    {
                        Value = ((int)status).ToString(),
                        Text = statusText
                    });
                }
                
                return options;
            }
        }
        
        public List<SelectListItem> CancellationTypeOptions
        {
            get
            {
                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tất cả" },
                    new SelectListItem { Value = ((int)Models.CancellationType.Customer).ToString(), Text = "Khách hàng hủy" },
                    new SelectListItem { Value = ((int)Models.CancellationType.Admin).ToString(), Text = "Admin hủy" }
                };
            }
        }
        
        public List<SelectListItem> DateRangeOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Tùy chỉnh" },
            new SelectListItem { Value = "today", Text = "Hôm nay" },
            new SelectListItem { Value = "yesterday", Text = "Hôm qua" },
            new SelectListItem { Value = "last7days", Text = "7 ngày qua" },
            new SelectListItem { Value = "last30days", Text = "30 ngày qua" },
            new SelectListItem { Value = "thismonth", Text = "Tháng này" },
            new SelectListItem { Value = "lastmonth", Text = "Tháng trước" },
            new SelectListItem { Value = "all", Text = "Tất cả thời gian" }
        };
        
        public List<SelectListItem> SortOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "date", Text = "Ngày đặt hàng" },
            new SelectListItem { Value = "amount", Text = "Tổng tiền" },
            new SelectListItem { Value = "id", Text = "Mã đơn hàng" },
            new SelectListItem { Value = "status", Text = "Trạng thái" }
        };
        
        public List<SelectListItem> SortDirectionOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "desc", Text = "Giảm dần" },
            new SelectListItem { Value = "asc", Text = "Tăng dần" }
        };
        
        // Method to apply the date range filter based on the selected option
        public void ApplyDateRangeFilter()
        {
            if (string.IsNullOrEmpty(DateRange))
                return;
                
            var today = DateTime.Today;
            
            switch (DateRange)
            {
                case "today":
                    StartDate = today;
                    EndDate = today.AddDays(1).AddSeconds(-1);
                    break;
                    
                case "yesterday":
                    StartDate = today.AddDays(-1);
                    EndDate = today.AddSeconds(-1);
                    break;
                    
                case "last7days":
                    StartDate = today.AddDays(-7);
                    EndDate = today.AddDays(1).AddSeconds(-1);
                    break;
                    
                case "last30days":
                    StartDate = today.AddDays(-30);
                    EndDate = today.AddDays(1).AddSeconds(-1);
                    break;
                    
                case "thismonth":
                    StartDate = new DateTime(today.Year, today.Month, 1);
                    EndDate = StartDate.Value.AddMonths(1).AddSeconds(-1);
                    break;
                    
                case "lastmonth":
                    StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    EndDate = new DateTime(today.Year, today.Month, 1).AddSeconds(-1);
                    break;
                    
                case "all":
                    StartDate = null;
                    EndDate = null;
                    break;
            }
        }
    }
} 