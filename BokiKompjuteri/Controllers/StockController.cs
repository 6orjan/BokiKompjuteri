using Microsoft.AspNetCore.Mvc;
using Service.DTOs;
using Service.Interfaces;



namespace BokiKompjuteri.Controllers
{
    [Route("api/[controller]")] // Route prefix: /api/stock
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        // POST: api/stock/import
        [HttpPost("import")] // Specific action route: /api/stock/import
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)] // Returns a status message
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid input data
        public async Task<IActionResult> ImportStock([FromBody] List<StockImportItemDto> stockItems)
        {
            // Basic validation (e.g., is the list null?) - [ApiController] helps with model binding issues
            if (stockItems == null || !stockItems.Any())
            {
                return BadRequest("No stock items provided in the request body.");
            }
            // More detailed validation happens within the DTOs and the service

            try
            {
                _logger.LogInformation("Starting stock import for {ItemCount} items.", stockItems.Count);
                var resultMessage = await _stockService.ImportStockAsync(stockItems);
                _logger.LogInformation("Stock import finished. Result: {ResultMessage}", resultMessage);
                return Ok(new { message = resultMessage }); // Return status message in a JSON object
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during stock import.");
                // Provide a generic error message to the client
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during stock import. Please check logs for details.");
            }
        }
    }
}
