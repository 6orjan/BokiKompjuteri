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
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StockService> _logger;
        private readonly IProductService _productService;

        public StockService(ApplicationDbContext context, IMapper mapper, ILogger<StockService> logger, IProductService productService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _productService = productService;
        }

        public async Task<string> ImportStockAsync(IEnumerable<StockImportItemDto> stockItems)
        {
            int itemsProcessed = 0;
            int productsCreated = 0;
            int productsUpdated = 0;
            

            if (stockItems == null || !stockItems.Any())
            {
                return "No stock items provided for import.";
            }

            foreach (var item in stockItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name) || item.Categories == null || !item.Categories.Any())
                {
                    _logger.LogWarning("Skipping invalid stock import item: {@StockItem}", item);
                    continue;
                }

                try
                {
                    var existingProduct = await _context.Products
                                                        .FirstOrDefaultAsync(p => p.Name.ToLower() == item.Name.ToLower());

                    if (existingProduct == null)
                    {
                        var createDto = new CreateUpdateProductDto
                        {
                            Name = item.Name,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            CategoryNames = item.Categories.Select(c => c.Trim()).ToList(),
                            Description = null
                        };
                        await _productService.CreateProductAsync(createDto);
                        productsCreated++;
                        _logger.LogInformation("Created new product via stock import: {ProductName}", item.Name);
                    }
                    else
                    {
                 
                        var updateDto = new CreateUpdateProductDto
                        {
                            Name = item.Name, 
                            Price = item.Price,
                            Quantity = item.Quantity,
                            CategoryNames = item.Categories.Select(c => c.Trim()).ToList(),
                            Description = existingProduct.Description 
                        };
                        bool updateSuccess = await _productService.UpdateProductAsync(existingProduct.Id, updateDto);
                        if (updateSuccess)
                        {
                            productsUpdated++;
                            _logger.LogInformation("Updated existing product via stock import: {ProductName}", item.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to update product {ProductName} during stock import.", item.Name);
                        }
                    }
                    itemsProcessed++;
                }
                catch (ArgumentException ex) 
                {
                    _logger.LogError(ex, "Error processing stock import item (likely validation error from ProductService): {ProductName}", item.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing stock import item: {ProductName}", item.Name);
                }
            }

            _logger.LogInformation("Stock import finished. Processed: {ItemsProcessed}, Products Created: {ProductsCreated}, Products Updated: {ProductsUpdated}",
                itemsProcessed, productsCreated, productsUpdated);

            return $"Stock import finished. Processed: {itemsProcessed}, Products Created: {productsCreated}, Products Updated: {productsUpdated}";
        }
    }
}
