using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Areas.Admin.Models.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }

        public int ProductCount { get; set; }
    }
}