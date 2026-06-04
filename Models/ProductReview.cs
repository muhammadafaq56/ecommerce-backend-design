using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Review text cannot exceed 1000 characters.")]
        public string ReviewText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}