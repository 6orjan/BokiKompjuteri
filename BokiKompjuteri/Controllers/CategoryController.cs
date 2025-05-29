using Microsoft.AspNetCore.Mvc;
using Service.DTOs;
using Service.Interfaces;



namespace BokiKompjuteri.Controllers
{
    [Route("api/[controller]")] // Route prefix: /api/categories
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        // GET: api/categories
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        // GET: api/categories/5
        [HttpGet("{id:int}")] // Route constraint for integer ID
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);

            if (category == null)
            {
                _logger.LogInformation("Category with ID: {CategoryId} not found.", id);
                return NotFound($"Category with ID {id} not found."); // Friendly error message
            }

            return Ok(category);
        }

        // POST: api/categories
        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // For validation errors or duplicate names
        public async Task<ActionResult<CategoryDto>> PostCategory([FromBody] CreateUpdateCategoryDto categoryDto)
        {
            // ModelState validation is automatically handled by [ApiController] attribute for basic validation rules

            try
            {
                var createdCategory = await _categoryService.CreateCategoryAsync(categoryDto);
                // If service throws exception for duplicate name, it will be caught below

                // Return 201 Created status with the location of the new resource and the resource itself
                return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.Id }, createdCategory);
            }
            catch (ArgumentException ex) // Catch specific exceptions from the service (e.g., duplicate name)
            {
                _logger.LogWarning("Failed to create category: {ErrorMessage}", ex.Message);
                // Return a 400 Bad Request with the specific error message
                return BadRequest(new { message = ex.Message }); // Return structured error
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred while creating a category.");
                // Return a generic 500 Internal Server Error
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        // PUT: api/categories/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Success, no content to return
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation errors or ID mismatch
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Category not found
        public async Task<IActionResult> PutCategory(int id, [FromBody] CreateUpdateCategoryDto categoryDto)
        {
            // Basic validation handled by [ApiController]

            // Optional: Check if the ID in the route matches an ID potentially in the DTO (if any)
            // if (id != categoryDto.Id) return BadRequest("ID mismatch");

            try
            {
                var success = await _categoryService.UpdateCategoryAsync(id, categoryDto);

                if (!success)
                {
                    // Determine if it was not found or another issue (like duplicate name)
                    // The service layer should ideally differentiate, but for now, assume not found if Get fails
                    var exists = await _categoryService.GetCategoryByIdAsync(id);
                    if (exists == null)
                    {
                        _logger.LogWarning("Attempted to update non-existent category with ID: {CategoryId}", id);
                        return NotFound($"Category with ID {id} not found.");
                    }
                    else
                    {
                        // Assume other failure (e.g., duplicate name handled in service)
                        _logger.LogWarning("Failed to update category with ID: {CategoryId}. Possible duplicate name or concurrency issue.", id);
                        return BadRequest("Failed to update category. Check for duplicate names or try again."); // More specific error if possible
                    }
                }

                return NoContent(); // Standard response for successful PUT
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred while updating category ID {CategoryId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Success, no content
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Not found
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // e.g., Category in use
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var success = await _categoryService.DeleteCategoryAsync(id);

                if (!success)
                {
                    // Check if it exists to differentiate between "Not Found" and "Cannot Delete" (e.g., in use)
                    var exists = await _categoryService.GetCategoryByIdAsync(id);
                    if (exists == null)
                    {
                        _logger.LogWarning("Attempted to delete non-existent category with ID: {CategoryId}", id);
                        return NotFound($"Category with ID {id} not found.");
                    }
                    else
                    {
                        // Assume deletion failed because it's in use (based on service logic)
                        _logger.LogWarning("Failed to delete category ID {CategoryId}, likely because it is in use.", id);
                        return BadRequest($"Cannot delete category ID {id} as it is currently associated with products.");
                    }
                }

                return NoContent(); // Standard response for successful DELETE
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting category ID {CategoryId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}
