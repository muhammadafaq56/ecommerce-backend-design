using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Areas.Admin.Models.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public string? Brand { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }

        public List<CategoryOption> Categories { get; set; } = new();
    }

    public class CategoryOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}