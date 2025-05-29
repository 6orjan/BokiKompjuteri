using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, IMapper mapper, ILogger<ProductService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProductDto> CreateProductAsync(CreateUpdateProductDto productDto)
        {
            if (await _context.Products.AnyAsync(p => p.Name.ToLower() == productDto.Name.ToLower()))
            {
                _logger.LogWarning("Product with name '{ProductName}' already exists.", productDto.Name);
                throw new ArgumentException($"Product with name '{productDto.Name}' already exists.");
            }

            var product = _mapper.Map<Product>(productDto);
            await EnsureCategoriesExistAsync(productDto.CategoryNames);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await AddProductCategoriesAsync(product, productDto.CategoryNames);

            var createdProduct = await GetProductWithCategoriesByIdAsync(product.Id);
            return _mapper.Map<ProductDto>(createdProduct);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Attempted to delete non-existent product with ID: {ProductId}", id);
                return false;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted product with ID: {ProductId}", id);
            return true;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await GetProductWithCategoriesByIdAsync(id);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<bool> UpdateProductAsync(int id, CreateUpdateProductDto productDto)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update.", id);
                return false;
            }

            // Compare names in a case-insensitive way but with a method that EF Core can translate
            if (!string.Equals(product.Name, productDto.Name, StringComparison.OrdinalIgnoreCase)
                && await _context.Products.AnyAsync(p => p.Id != id && p.Name.ToLower() == productDto.Name.ToLower()))
            {
                _logger.LogWarning("Product name conflict during update. ID: {ProductId}, New Name: {ProductName}", id, productDto.Name);
                return false;
            }

            _mapper.Map(productDto, product);
            await EnsureCategoriesExistAsync(productDto.CategoryNames);
            await UpdateProductCategoriesAsync(product, productDto.CategoryNames);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product with ID {ProductId} updated successfully.", id);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating product ID {ProductId}", id);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating product ID {ProductId}", id);
                return false;
            }
        }

        private async Task<Product?> GetProductWithCategoriesByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task EnsureCategoriesExistAsync(List<string> categoryNames)
        {
            var distinctNames = categoryNames
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in distinctNames)
            {
                // Convert to method that EF Core can translate
                var lowercaseName = name.ToLower();
                if (!await _context.Categories.AnyAsync(c => c.Name.ToLower() == lowercaseName))
                {
                    _context.Categories.Add(new Category { Name = name });
                    _logger.LogInformation("Created new category: {CategoryName}", name);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task AddProductCategoriesAsync(Product product, List<string> categoryNames)
        {
            var distinctNames = categoryNames
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in distinctNames)
            {
                // Use ToLower() for comparison instead of Equals with StringComparison
                var lowercaseName = name.ToLower();
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == lowercaseName);

                if (category != null)
                {
                    product.ProductCategories.Add(new ProductCategory
                    {
                        Product = product,
                        Category = category
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateProductCategoriesAsync(Product product, List<string> categoryNames)
        {
            var desiredNames = categoryNames
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // First materialize the categories we need to work with
            var productCategories = product.ProductCategories.ToList();
            var existingCategoryNames = productCategories
                .Select(pc => pc.Category.Name.ToLower())
                .ToList();

            var desiredLowerNames = desiredNames
                .Select(name => name.ToLower())
                .ToList();

            // Find categories to remove
            var toRemove = productCategories
                .Where(pc => !desiredLowerNames.Contains(pc.Category.Name.ToLower()))
                .ToList();

            foreach (var item in toRemove)
            {
                _context.ProductCategories.Remove(item);
            }

            // Find categories to add
            foreach (var name in desiredNames)
            {
                var lowercaseName = name.ToLower();
                if (!existingCategoryNames.Contains(lowercaseName))
                {
                    var category = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == lowercaseName);

                    if (category != null)
                    {
                        product.ProductCategories.Add(new ProductCategory
                        {
                            Product = product,
                            Category = category
                        });
                    }
                }
            }
        }
    }
}