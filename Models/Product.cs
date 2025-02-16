using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Wireframe.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        [Display(Name = "Price")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal price { get; set; }

        [Display(Name = "Product Image")]
        public string Imgurl { get; set; }

        public DateOnly Date { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
