using Data;
using Microsoft.OpenApi.Models;
using Service.Interfaces;
using Service.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.EntityFrameworkCore.InMemory;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Configuration ---
        var configuration = builder.Configuration;

        // --- Add services to the container ---

        // 1. Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback or error handling if connection string is missing
            // For development, could use InMemory database or throw an error
            Console.WriteLine("Warning: DefaultConnection string not found in configuration. Using InMemory database.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("InternetServicesDb"));
            // OR: throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
        }


        // 2. AutoMapper
        // Requires AutoMapper.Extensions.Microsoft.DependencyInjection NuGet package
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); // Scans assemblies for profiles (like MappingProfile)

        // 3. Custom Services (Dependency Injection)
        // Register interfaces and their implementations
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IStockService, StockService>();
        builder.Services.AddScoped<IDiscountService, DiscountService>();
        // AddScoped: Instance per HTTP request
        // AddTransient: New instance every time requested
        // AddSingleton: Single instance for the application lifetime

        // 4. Controllers
        builder.Services.AddControllers()
            // Optional: Configure JSON options if needed (e.g., handling reference loops)
            .AddJsonOptions(options =>
            {
                // Example: Ignore reference loops if using complex object graphs directly in API responses
                // options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });


        // 5. API Explorer and Swagger (for API documentation and testing UI)
        // Requires Swashbuckle.AspNetCore NuGet package
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Internet Services API", Version = "v1" });
            // Optional: Add XML comments path if you use /// comments for documentation
            // var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            // var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            // c.IncludeXmlComments(xmlPath);
        });

        // 6. Logging (already configured by default, but can be customized)
        builder.Services.AddLogging();

        // 7. CORS (Cross-Origin Resource Sharing) - If your frontend is hosted separately
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", // Define a policy name
                policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://yourfrontenddomain.com") // Allow specific origins
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                    // For development, you might use AllowAnyOrigin(), but be cautious in production.
                    // policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
        });


        // --- Build the application ---
        var app = builder.Build();

        // --- Configure the HTTP request pipeline ---

        // Development specific configurations
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage(); // More detailed error page for devs
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internet Services API V1");
                c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
            });
        }
        else
        {
            // Production error handling (e.g., redirect to an error page)
            app.UseExceptionHandler("/Error"); // Needs an Error page/handler
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // Standard middleware pipeline
        app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS

        app.UseRouting(); // Must come before UseCors and UseAuthentication/UseAuthorization

        // Apply CORS policy - Use the specific policy name defined above
        app.UseCors("AllowSpecificOrigin");
        // Or use a default policy if configured differently

        // Add Authentication/Authorization middleware if needed (not in requirements, but common)
        // app.UseAuthentication();
        app.UseAuthorization();

        // Map controller endpoints
        app.MapControllers();

        // --- Run the application ---
        app.Run();
    }
}