using Microsoft.AspNetCore.Mvc;
using TIBG.API.Core.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for sport recommendation endpoints
    /// </summary>
    [ApiController]
    [Route("api/v1")]
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


        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeProfile([FromBody] UserProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    return BadRequest(new { error = "Profile data is required" });
                }

                // Validate ModelState (checks Required and Range attributes automatically)
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

                // Additional sanitization for optional text fields
                if (!string.IsNullOrWhiteSpace(profile.HealthConditions))
                {
                    profile.HealthConditions = SanitizeInput(profile.HealthConditions);
                }
                if (!string.IsNullOrWhiteSpace(profile.Injuries))
                {
                    profile.Injuries = SanitizeInput(profile.Injuries);
                }
                if (!string.IsNullOrWhiteSpace(profile.PractisedSports))
                {
                    profile.PractisedSports = SanitizeInput(profile.PractisedSports);
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
        /// Sanitize user input to prevent injection attacks
        /// </summary>
        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove potentially dangerous characters
            return System.Text.RegularExpressions.Regex.Replace(
                input, 
                @"[<>'"";(){}[\]\\]", 
                string.Empty
            ).Trim();
        }

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
                        groq = isAiAvailable ? "operational" : "unavailable"
                    }
                };

                if (!isAiAvailable)
                {
                    _logger.LogWarning("Groq service is unavailable");
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
