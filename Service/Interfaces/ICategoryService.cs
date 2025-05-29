using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.DTOs;
namespace Service.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int id); 
        Task<CategoryDto> CreateCategoryAsync(CreateUpdateCategoryDto categoryDto);
        Task<bool> UpdateCategoryAsync(int id, CreateUpdateCategoryDto categoryDto); 
        Task<bool> DeleteCategoryAsync(int id); 
    }
}
