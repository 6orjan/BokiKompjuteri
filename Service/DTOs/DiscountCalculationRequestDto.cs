using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs
{
    public class DiscountCalculationRequestDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "Basket must contain at least one item.")]
        public List<BasketItemDto> Items { get; set; } = new List<BasketItemDto>();
    }
}
