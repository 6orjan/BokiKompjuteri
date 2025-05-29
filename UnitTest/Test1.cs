using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Service.Tests
{
    [TestClass]
    public sealed class ProductServiceTests
    {
        private ApplicationDbContext _context;
        private IMapper _mapper;
        private ILogger<ProductService> _logger;
        private IProductService _productService;
        private string _dbName;
        private string _connectionString;
        private const string PostgresUsername = "postgres";
        private const string PostgresPassword = "0000";
        private const string PostgresHost = "localhost";

        [TestInitialize]
        public void Initialize()
        {
            // Create a unique database name for each test run to prevent conflicts
            _dbName = $"product_test_{Guid.NewGuid().ToString().Replace("-", "_")}";
            _connectionString = $"Host={PostgresHost};Database={_dbName};Username={PostgresUsername};Password={PostgresPassword}";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_connectionString)
                .Options;

            _context = new ApplicationDbContext(options);

            // Ensure database is created
            _context.Database.EnsureCreated();

            // Setup AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CreateUpdateProductDto, Product>();
                cfg.CreateMap<Product, ProductDto>()
                    .ForMember(dest => dest.CategoryNames, opt => opt.MapFrom(src =>
                        src.ProductCategories.Select(pc => pc.Category.Name).ToList()));
            });

            _mapper = mapperConfig.CreateMapper();

            // Setup Logger using NullLogger like in integration tests
            _logger = NullLogger<ProductService>.Instance;

            // Create service instance
            _productService = new ProductService(_context, _mapper, _logger);

            // Seed initial data
            SeedDatabase().Wait();
        }

        private async Task SeedDatabase()
        {
            // Add categories first
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" },
                new Category { Id = 2, Name = "Books", Description = "Reading materials" },
                new Category { Id = 3, Name = "Clothing", Description = "Apparel items" }
            };

            await _context.Categories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();

            // Add products
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Laptop",
                    Description = "Powerful laptop",
                    Price = 1200.00m,
                    Quantity = 10
                },
                new Product
                {
                    Name = "Programming Book",
                    Description = "Learn programming",
                    Price = 45.99m,
                    Quantity = 50
                }
            };

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            // Now create the many-to-many relationships
            var laptop = await _context.Products.FirstAsync(p => p.Name == "Laptop");
            var book = await _context.Products.FirstAsync(p => p.Name == "Programming Book");

            var productCategories = new List<ProductCategory>
            {
                new ProductCategory { ProductId = laptop.Id, CategoryId = 1 }, // Electronics
                new ProductCategory { ProductId = book.Id, CategoryId = 2 }    // Books
            };

            await _context.ProductCategories.AddRangeAsync(productCategories);
            await _context.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Dispose the context
            _context.Dispose();

            try
            {
                // Connect to the master database to drop the test database
                using (var masterConnection = new NpgsqlConnection(
                    $"Host={PostgresHost};Database=postgres;Username={PostgresUsername};Password={PostgresPassword}"))
                {
                    masterConnection.Open();

                    // Terminate all connections to the database
                    using (var terminateCommand = masterConnection.CreateCommand())
                    {
                        terminateCommand.CommandText = $@"
                            SELECT pg_terminate_backend(pg_stat_activity.pid)
                            FROM pg_stat_activity
                            WHERE pg_stat_activity.datname = '{_dbName}'
                            AND pid <> pg_backend_pid();";
                        terminateCommand.ExecuteNonQuery();
                    }

                    // Drop the database
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
                // Log the error but don't fail the test
                Console.WriteLine($"Error cleaning up test database: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task GetAllProductsAsync_ReturnsAllProducts()
        {
            // Act
            var result = await _productService.GetAllProductsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task GetProductByIdAsync_WithValidId_ReturnsProduct()
        {
            // Arrange
            var product = await _context.Products.FirstAsync(p => p.Name == "Laptop");

            // Act
            var result = await _productService.GetProductByIdAsync(product.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Laptop", result.Name);
            Assert.AreEqual(1200.00m, result.Price);
            Assert.IsTrue(result.CategoryNames.Contains("Electronics"));
        }

        [TestMethod]
        public async Task GetProductByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _productService.GetProductByIdAsync(999);

            // Assert
            Assert.IsNull(result);
        }

        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateProductAsync_WithDuplicateName_ThrowsArgumentException()
        {
            // Arrange
            var existingProduct = await _context.Products.FirstAsync();
            var newProduct = new CreateUpdateProductDto
            {
                Name = existingProduct.Name, // Use existing name to trigger exception
                Description = "Another product",
                Price = 999.99m,
                Quantity = 5,
                CategoryNames = new List<string> { "Electronics" }
            };

            // Act - This should throw ArgumentException
            await _productService.CreateProductAsync(newProduct);
        }

       

        [TestMethod]
        public async Task UpdateProductAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var updateDto = new CreateUpdateProductDto
            {
                Name = "Invalid Product",
                Price = 10.00m,
                CategoryNames = new List<string> { "Test" }
            };

            // Act
            var result = await _productService.UpdateProductAsync(999, updateDto);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteProductAsync_WithValidId_DeletesProduct()
        {
            // Arrange
            var productToDelete = await _context.Products.FirstAsync(p => p.Name == "Programming Book");

            // Act
            var result = await _productService.DeleteProductAsync(productToDelete.Id);

            // Assert
            Assert.IsTrue(result);

            // Verify the product was deleted from the database
            var deletedProduct = await _context.Products.FindAsync(productToDelete.Id);
            Assert.IsNull(deletedProduct);
        }

        [TestMethod]
        public async Task DeleteProductAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = await _productService.DeleteProductAsync(999);

            // Assert
            Assert.IsFalse(result);
        }

       
    }
}