using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Entities
{
    public class ProductCategory
    {
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!; 

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;
    }
}
