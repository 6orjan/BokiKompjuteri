using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.DTOs;
using Service.Interfaces;
namespace Service.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ApplicationDbContext context, IMapper mapper, ILogger<CategoryService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateUpdateCategoryDto categoryDto)
        {
            
            if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == categoryDto.Name.ToLower()))
            {
                _logger.LogWarning("Attempted to create category with duplicate name: {CategoryName}", categoryDto.Name);
                
                throw new ArgumentException($"Category with name '{categoryDto.Name}' already exists.");
            }

            var category = _mapper.Map<Category>(categoryDto);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new category with ID: {CategoryId}", category.Id);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Attempted to delete non-existent category with ID: {CategoryId}", id);
                return false;
            }

            bool isInUse = await _context.ProductCategories.AnyAsync(pc => pc.CategoryId == id);
            if (isInUse)
            {
                _logger.LogWarning("Attempted to delete category ID {CategoryId} which is in use by products.", id);
                return false;
            }

            _context.Categories.Remove(category);
            var result = await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted category with ID: {CategoryId}", id);
            return result > 0;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return null;
            }
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> UpdateCategoryAsync(int id, CreateUpdateCategoryDto categoryDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Attempted to update non-existent category with ID: {CategoryId}", id);
                return false;
            }

            if (category.Name.ToLower() != categoryDto.Name.ToLower() &&
                await _context.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == categoryDto.Name.ToLower()))
            {
                _logger.LogWarning("Attempted to update category ID {CategoryId} to a duplicate name: {CategoryName}", id, categoryDto.Name);
                return false;
            }

            _mapper.Map(categoryDto, category);
            category.Id = id;

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated category with ID: {CategoryId}", id);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating category ID {CategoryId}", id);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating category ID {CategoryId}", id);
                return false;
            }
        }
    }
}
