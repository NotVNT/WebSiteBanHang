using System;
using System.ComponentModel.DataAnnotations;

namespace WebSiteBanHang.Models
{
    public class Content
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string Slug { get; set; }

        [Required]
        public string Body { get; set; }

        [StringLength(500)]
        public string MetaDescription { get; set; }

        [StringLength(100)]
        public string MetaKeywords { get; set; }

        public bool Published { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string ContentType { get; set; } = "Page"; // Page, Policy, Guide, etc.

        public int DisplayOrder { get; set; } = 0;
    }
} 