using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Service.DTOs;
using Service.Interfaces;
using Service.Services;

namespace IntegrationTest
{
    [TestClass]
    public sealed class StockServiceIntegrationTests
    {
        private ApplicationDbContext _context;
        private IMapper _mapper;
        private ILogger<StockService> _logger;
        private IProductService _productService;
        private StockService _stockService;
        private string _dbName;
        private string _connectionString;
        private const string PostgresUsername = "postgres";
        private const string PostgresPassword = "0000";
        private const string PostgresHost = "localhost";

        [TestInitialize]
        public void Initialize()
        {
            
            _dbName = $"computerstore_test_{Guid.NewGuid().ToString().Replace("-", "_")}";
            _connectionString = $"Host={PostgresHost};Database={_dbName};Username={PostgresUsername};Password={PostgresPassword}";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_connectionString)
                .Options;

            _context = new ApplicationDbContext(options);

          
            _context.Database.EnsureCreated();

         
            var mapperConfig = new MapperConfiguration(cfg =>
            {
               
                cfg.CreateMap<CreateUpdateProductDto, Product>();
                cfg.CreateMap<Product, ProductDto>();
                cfg.CreateMap<Category, CategoryDto>();
            });
            _mapper = mapperConfig.CreateMapper();

           
            _logger = NullLogger<StockService>.Instance;
            var productLogger = NullLogger<ProductService>.Instance;

           
            _productService = new ProductService(_context, _mapper, productLogger);

           
            _stockService = new StockService(_context, _mapper, _logger, _productService);

            
            SeedCategories().Wait();
        }

        private async Task SeedCategories()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "CPU", Description = "Processors" },
                new Category { Id = 2, Name = "Keyboard", Description = "Input devices" },
                new Category { Id = 3, Name = "Periphery", Description = "External devices" },
                new Category { Id = 4, Name = "Category1", Description = "Test category" }
            };

            await _context.Categories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
           
            _context.Dispose();

         
            try
            {
           
                using (var masterConnection = new NpgsqlConnection(
                    $"Host={PostgresHost};Database=postgres;Username={PostgresUsername};Password={PostgresPassword}"))
                {
                    masterConnection.Open();

               
                    using (var terminateCommand = masterConnection.CreateCommand())
                    {
                        terminateCommand.CommandText = $@"
                            SELECT pg_terminate_backend(pg_stat_activity.pid)
                            FROM pg_stat_activity
                            WHERE pg_stat_activity.datname = '{_dbName}'
                            AND pid <> pg_backend_pid();";
                        terminateCommand.ExecuteNonQuery();
                    }

                 
                    using (var command = masterConnection.CreateCommand())
                    {
                        command.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\";";
                        command.ExecuteNonQuery();
                    }

                    masterConnection.Close();
                }
            }
            catch (Exception ex)
            {
              
                Console.WriteLine($"Error cleaning up test database: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task ImportStockAsync_WithValidItems_CreatesProducts()
        {
   
            var stockItems = new List<StockImportItemDto>
            {
                new StockImportItemDto
                {
                    Name = "Intel's Core i9-9900K",
                    Categories = new List<string> { "CPU" },
                    Price = 475.99m,
                    Quantity = 2
                },
                new StockImportItemDto
                {
                    Name = "Razer BlackWidow Keyboard",
                    Categories = new List<string> { "Keyboard", "Periphery" },
                    Price = 89.99m,
                    Quantity = 10
                }
            };

        
            var result = await _stockService.ImportStockAsync(stockItems);

      
            Assert.IsTrue(result.Contains("Stock import finished"));
            Assert.IsTrue(result.Contains("Processed: 2"));
            Assert.IsTrue(result.Contains("Products Created: 2"));

    
            var intelProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == "Intel's Core i9-9900K");
            var razerProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == "Razer BlackWidow Keyboard");

            Assert.IsNotNull(intelProduct);
            Assert.IsNotNull(razerProduct);
            Assert.AreEqual(475.99m, intelProduct.Price);
            Assert.AreEqual(2, intelProduct.Quantity);
            Assert.AreEqual(89.99m, razerProduct.Price);
            Assert.AreEqual(10, razerProduct.Quantity);

            
            var intelCategories = await _context.ProductCategories
                .Where(pc => pc.ProductId == intelProduct.Id)
                .Select(pc => pc.CategoryId)
                .ToListAsync();

            var razerCategories = await _context.ProductCategories
                .Where(pc => pc.ProductId == razerProduct.Id)
                .Select(pc => pc.CategoryId)
                .ToListAsync();

            Assert.AreEqual(1, intelCategories.Count);
            
            Assert.AreEqual(2, razerCategories.Count);
        }

        [TestMethod]
        public async Task ImportStockAsync_WithEmptyList_ReturnsAppropriateMessage()
        {
        
            var emptyList = new List<StockImportItemDto>();

    
            var result = await _stockService.ImportStockAsync(emptyList);

          
            Assert.AreEqual("No stock items provided for import.", result);

         
            var productCount = await _context.Products.CountAsync();
            Assert.AreEqual(0, productCount);
        }

        [TestMethod]
        public async Task ImportStockAsync_WithExistingProduct_UpdatesProduct()
        {
            
            
            var existingProduct = new Product
            {
                Id = 1,
                Name = "Existing Product",
                Price = 50m,
                Quantity = 2,
                Description = "Existing description"
            };

            await _context.Products.AddAsync(existingProduct);

          
            var productCategory = new ProductCategory
            {
                ProductId = existingProduct.Id,
                CategoryId = 4
            };

            await _context.ProductCategories.AddAsync(productCategory);
            await _context.SaveChangesAsync();

            
            var stockItem = new StockImportItemDto
            {
                Name = "Existing Product",
                Categories = new List<string> { "Category1", "CPU" }, // Add a new category
                Price = 100m, 
                Quantity = 5   
            };

           
            var result = await _stockService.ImportStockAsync(new List<StockImportItemDto> { stockItem });

          
            Assert.IsTrue(result.Contains("Products Updated: 1"));

            var updatedProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == existingProduct.Id);

            Assert.IsNotNull(updatedProduct);
            Assert.AreEqual(100m, updatedProduct.Price);
            Assert.AreEqual(5, updatedProduct.Quantity);
            Assert.AreEqual("Existing description", updatedProduct.Description);

            
            var productCategories = await _context.ProductCategories
                .Where(pc => pc.ProductId == existingProduct.Id)
                .Select(pc => pc.CategoryId)
                .ToListAsync();

            Assert.AreEqual(2, productCategories.Count);
            CollectionAssert.Contains(productCategories, 1); 
            CollectionAssert.Contains(productCategories, 4); 
        }

        [TestMethod]
        public async Task ImportStockAsync_WithInvalidItem_SkipsInvalidItems()
        {
          
            var stockItems = new List<StockImportItemDto>
            {
                new StockImportItemDto
                {
                    Name = "Valid Product",
                    Categories = new List<string> { "CPU" },
                    Price = 100m,
                    Quantity = 5
                },
                new StockImportItemDto
                {
                    Name = null, 
                    Categories = new List<string> { "CPU" },
                    Price = 100m,
                    Quantity = 5
                },
                new StockImportItemDto
                {
                    Name = "Invalid Categories",
                    Categories = new List<string>(), 
                    Price = 100m,
                    Quantity = 5
                }
            };

           
            var result = await _stockService.ImportStockAsync(stockItems);

           
            Assert.IsTrue(result.Contains("Processed: 1"));

            
            var products = await _context.Products.ToListAsync();
            Assert.AreEqual(1, products.Count);
            Assert.AreEqual("Valid Product", products[0].Name);
        }

        
    }
}