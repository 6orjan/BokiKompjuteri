using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace Data
{
    public class ApplicationDbContext : DbContext
    {
        // Define DbSet properties for each entity to be managed by EF Core
        // ** MODIFIED: Made DbSets virtual to allow mocking by Moq **
        public virtual DbSet<Category> Categories { get; set; } = null!;
        public virtual DbSet<Product> Products { get; set; } = null!;
        public virtual DbSet<ProductCategory> ProductCategories { get; set; } = null!;

        // Constructor used by dependency injection (and potentially by the factory if not parameterless)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // EF Core tools (like for migrations) also look for a parameterless constructor.
        // If your IDesignTimeDbContextFactory provides options, this might not be strictly needed
        // but can sometimes help with tooling.
        // public ApplicationDbContext() {}


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configure Many-to-Many Relationship between Product and Category ---
            modelBuilder.Entity<ProductCategory>()
                .HasKey(pc => new { pc.ProductId, pc.CategoryId });

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(pc => pc.ProductId);

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.Category)
                .WithMany(c => c.ProductCategories)
                .HasForeignKey(pc => pc.CategoryId);

            // --- Configure Product Price Precision ---
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18, 2)");

            // --- Add Unique Constraints ---
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Product>()
               .HasIndex(p => p.Name)
               .IsUnique();
        }
    }
}
