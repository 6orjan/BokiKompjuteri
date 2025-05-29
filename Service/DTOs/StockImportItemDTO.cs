using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs
{
    public class StockImportItemDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<string> Categories { get; set; } = new List<string>();

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)] 
        public int Quantity { get; set; }
    }
}
