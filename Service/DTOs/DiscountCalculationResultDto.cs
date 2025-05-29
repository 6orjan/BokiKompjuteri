using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs
{
    public class DiscountCalculationResultDto
    {
        public decimal OriginalTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public List<string> AppliedDiscountMessages { get; set; } = new List<string>(); 
        public bool IsSuccess { get; set; } = true; 
        public string? ErrorMessage { get; set; } 
    }
}
