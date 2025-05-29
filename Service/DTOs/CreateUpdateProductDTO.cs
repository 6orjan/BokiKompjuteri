using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs
{
    public class CreateUpdateProductDto
    {
        [Required(ErrorMessage = "Product name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Product price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Product quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; } 

        [Required(ErrorMessage = "At least one category is required.")]
        [MinLength(1, ErrorMessage = "At least one category is required.")]
        public List<string> CategoryNames { get; set; } = new List<string>(); 
    }
}
