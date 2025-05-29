using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs
{
    public class BasketItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "the quantity needs to be at least 1")]
        public int Quantity { get; set; }
    }
}
