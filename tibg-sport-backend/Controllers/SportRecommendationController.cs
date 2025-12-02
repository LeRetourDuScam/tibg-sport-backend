using Microsoft.AspNetCore.Mvc;
using TIBG.API.Core.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for sport recommendation endpoints
    /// </summary>
    [ApiController]
    [Route("api")]
    public class SportRecommendationController : ControllerBase
    {
        private readonly IAiRecommendationService _aiService;
        private readonly ILogger<SportRecommendationController> _logger;

        public SportRecommendationController(
            IAiRecommendationService aiService,
            ILogger<SportRecommendationController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes user profile and returns sport recommendation
        /// </summary>
        /// <param name="profile">User profile with all relevant information</param>
        /// <returns>Sport recommendation with exercises and training plan</returns>
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeProfile([FromBody] UserProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    return BadRequest(new { error = "Profile data is required" });
                }

                _logger.LogInformation("Received profile analysis request");

                var recommendation = await _aiService.GetSportRecommendationAsync(profile);

                if (recommendation != null)
                {
                    _logger.LogInformation($"Generated recommendation: {recommendation.Sport}");
                    return Ok(recommendation);
                }
                else
                {
                    _logger.LogError("Failed to generate recommendation");
                    return StatusCode(500, new { error = "Failed to generate recommendation. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing profile");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Health check endpoint to verify API and AI service availability
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var isAiAvailable = await _aiService.IsServiceAvailableAsync();

                var response = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        api = "operational",
                        huggingface = isAiAvailable ? "operational" : "unavailable"
                    }
                };

                if (!isAiAvailable)
                {
                    _logger.LogWarning("Hugging Face service is unavailable");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }
    }
}
