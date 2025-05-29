using Microsoft.AspNetCore.Mvc;
using Service.DTOs;
using Service.Interfaces;

namespace BokiKompjuteri.Controllers
{
    [Route("api/[controller]")] // Route prefix: /api/products
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: api/products
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        // GET: api/products/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                _logger.LogInformation("Product with ID: {ProductId} not found.", id);
                return NotFound($"Product with ID {id} not found.");
            }

            return Ok(product);
        }

        // POST: api/products
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        public async Task<ActionResult<ProductDto>> PostProduct([FromBody] CreateUpdateProductDto productDto)
        {
            // Basic validation handled by [ApiController]

            try
            {
                var createdProduct = await _productService.CreateProductAsync(productDto);
               

                return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
            }
            catch (ArgumentException ex) 
            {
                _logger.LogWarning("Failed to create product: {ErrorMessage}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred while creating a product.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        // PUT: api/products/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation, ID mismatch, other errors
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutProduct(int id, [FromBody] CreateUpdateProductDto productDto)
        {
            // Basic validation handled by [ApiController]

            try
            {
                var success = await _productService.UpdateProductAsync(id, productDto);

                if (!success)
                {
                    // Check if the product exists to return 404, otherwise assume 400
                    var exists = await _productService.GetProductByIdAsync(id);
                    if (exists == null)
                    {
                        _logger.LogWarning("Attempted to update non-existent product with ID: {ProductId}", id);
                        return NotFound($"Product with ID {id} not found.");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update product with ID: {ProductId}. Possible duplicate name or invalid categories.", id);
                        // More specific error message if possible (e.g., based on exception type from service)
                        return BadRequest("Failed to update product. Check input data (e.g., duplicate name, valid categories).");
                    }
                }
                return NoContent();
            }
            catch (ArgumentException ex) // Catch specific known issues from the service during update
            {
                _logger.LogWarning("Failed to update product ID {ProductId}: {ErrorMessage}", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred while updating product ID {ProductId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        // DELETE: api/products/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);

                if (!success)
                {
                    // If deletion failed, it's most likely because the product wasn't found.
                    _logger.LogWarning("Attempted to delete non-existent product with ID: {ProductId} or delete failed.", id);
                    return NotFound($"Product with ID {id} not found or could not be deleted.");
                }

                return NoContent();
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting product ID {ProductId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}
