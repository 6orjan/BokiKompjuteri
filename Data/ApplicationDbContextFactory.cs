using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
           
            string connectionString = "Host=localhost;Port=5432;Database=InternetServicesDb_Dev_FromFactory;Username=postgres;Password=0000;";

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string is null or empty in ApplicationDbContextFactory. Please set it directly for design-time tools.");
            }

            Console.WriteLine($"DESIGN-TIME Factory: Using HARDCODED connection string to Database: InternetServicesDb_Dev_FromFactory");

            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}