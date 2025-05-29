using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18, 2)")] 
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")] 
        public decimal Price { get; set; }

        
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; } = 0; 

        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    }
}
