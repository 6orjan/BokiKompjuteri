using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.DTOs;
using Service.Interfaces;
namespace Service.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiscountService> _logger;
        private const decimal CategoryDiscountRate = 0.05m; // 5%

        public DiscountService(ApplicationDbContext context, ILogger<DiscountService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DiscountCalculationResultDto> CalculateDiscountAsync(DiscountCalculationRequestDto request)
        {
            var result = new DiscountCalculationResultDto();
            var productDetails = new Dictionary<int, (Product Product, List<Category> Categories)>();
            var categoryCounts = new Dictionary<int, int>();
            decimal originalTotal = 0m;
            decimal totalDiscount = 0m;

            foreach (var item in request.Items)
            {
                var product = await _context.Products
                                            .Include(p => p.ProductCategories)
                                                .ThenInclude(pc => pc.Category)
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Product with ID {item.ProductId} not found.";
                    _logger.LogWarning("Discount calculation failed: Product not found (ID: {ProductId})", item.ProductId);
                    return result;
                }

                if (product.Quantity < item.Quantity)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Not enough stock for product '{product.Name}'. Requested: {item.Quantity}, Available: {product.Quantity}.";
                    _logger.LogWarning("Discount calculation failed: Insufficient stock for {ProductName} (ID: {ProductId}). Requested: {RequestedQty}, Available: {AvailableQty}", product.Name, item.ProductId, item.Quantity, product.Quantity);
                    return result;
                }

                var categories = product.ProductCategories.Select(pc => pc.Category).ToList();
                productDetails[item.ProductId] = (product, categories);
                originalTotal += product.Price * item.Quantity;

                foreach (var category in categories)
                {
                    categoryCounts.TryGetValue(category.Id, out int currentCount);
                    categoryCounts[category.Id] = currentCount + item.Quantity;
                }
            }

            result.OriginalTotal = originalTotal;

            if (request.Items.Sum(i => i.Quantity) <= 1 && request.Items.Count <= 1) // Also check if only one type of product
            {
                result.FinalTotal = originalTotal;
                result.AppliedDiscountMessages.Add("No discount applied (single item or single product type).");
                return result;
            }

            var discountedProductIdsForCategory = new HashSet<int>();

            foreach (var item in request.Items)
            {
                var (product, categories) = productDetails[item.ProductId];
                for (int i = 0; i < item.Quantity; i++) 
                {
                    bool unitDiscountApplied = false;
                    foreach (var category in categories)
                    {
                        if (categoryCounts.TryGetValue(category.Id, out int count) && count > 1)
                        {
                           
                            if (i == 0 && !discountedProductIdsForCategory.Contains(product.Id))
                            {
                                decimal discountForItem = product.Price * CategoryDiscountRate;
                                totalDiscount += discountForItem;
                                result.AppliedDiscountMessages.Add($"Applied 5% discount ({discountForItem:C}) to first copy of '{product.Name}' for category '{category.Name}'.");
                                discountedProductIdsForCategory.Add(product.Id);
                                unitDiscountApplied = true;
                                break;
                            }
                        }
                    }
                    if (unitDiscountApplied && i == 0) break; 
                }
            }

            result.DiscountAmount = totalDiscount;
            result.FinalTotal = originalTotal - totalDiscount;

            if (totalDiscount == 0 && (request.Items.Sum(i => i.Quantity) > 1 || request.Items.Count > 1))
            {
                result.AppliedDiscountMessages.Add("No category discounts applicable based on basket contents.");
            }

            _logger.LogInformation("Discount calculation successful. Original: {OriginalTotal}, Discount: {DiscountAmount}, Final: {FinalTotal}", result.OriginalTotal, result.DiscountAmount, result.FinalTotal);
            return result;
        }
    }
}
