using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public string? Brand { get; set; }
        public double Rating { get; set; } = 4.5;
        public int ReviewCount { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public decimal DiscountPercentage =>
            OriginalPrice.HasValue && OriginalPrice > 0
                ? Math.Round((1 - Price / OriginalPrice.Value) * 100)
                : 0;

        // ──> Added: Connects this product model to your customer review dataset
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    }
}