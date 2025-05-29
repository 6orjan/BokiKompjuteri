using Microsoft.AspNetCore.Mvc;
using Service.DTOs;
using Service.Interfaces;

namespace BokiKompjuteri.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class BasketController : ControllerBase
    {
        private readonly IDiscountService _discountService;
        private readonly ILogger<BasketController> _logger;

        public BasketController(IDiscountService discountService, ILogger<BasketController> logger)
        {
            _discountService = discountService;
            _logger = logger;
        }

        // POST: api/basket/calculate-discount
        [HttpPost("calculate-discount")]
        [ProducesResponseType(typeof(DiscountCalculationResultDto), StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        public async Task<ActionResult<DiscountCalculationResultDto>> CalculateDiscount([FromBody] DiscountCalculationRequestDto request)
        {
            

            try
            {
                _logger.LogInformation("Calculating discount for basket with {ItemCount} item types.", request.Items?.Count ?? 0);
                var result = await _discountService.CalculateDiscountAsync(request);

                if (!result.IsSuccess)
                {
                   
                    _logger.LogWarning("Discount calculation failed: {ErrorMessage}", result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage }); // Return structured error
                }

                // If calculation was successful (IsSuccess is true), return OK with the full result DTO
                _logger.LogInformation("Discount calculation successful. Final Total: {FinalTotal}", result.FinalTotal);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during discount calculation.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while calculating discounts. Please try again later.");
            }
        }
    }
}
