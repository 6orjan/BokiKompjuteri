using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.DTOs;
namespace Service.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateUpdateProductDto productDto);
        Task<bool> UpdateProductAsync(int id, CreateUpdateProductDto productDto);
        Task<bool> DeleteProductAsync(int id);
    }
}
