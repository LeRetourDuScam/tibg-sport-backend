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


        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeProfile([FromBody] UserProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    return BadRequest(new { error = "Profile data is required" });
                }

                // Validation des données
                var validationError = ValidateUserProfile(profile);
                if (validationError != null)
                {
                    return BadRequest(new { error = validationError });
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

        private string? ValidateUserProfile(UserProfile profile)
        {
            if (profile.Age < 18 || profile.Age > 120)
            {
                return "Age must be between 18 and 120 years";
            }

            if (profile.Height < 100 || profile.Height > 250)
            {
                return "Height must be between 100 and 250 cm";
            }

            if (profile.Weight < 30 || profile.Weight > 300)
            {
                return "Weight must be between 30 and 300 kg";
            }

            if (profile.LegLength < 40 || profile.LegLength > 150)
            {
                return "Leg length must be between 40 and 150 cm";
            }

            if (profile.ArmLength < 40 || profile.ArmLength > 120)
            {
                return "Arm length must be between 40 and 120 cm";
            }

            if (profile.WaistSize < 40 || profile.WaistSize > 200)
            {
                return "Waist size must be between 40 and 200 cm";
            }

            if (string.IsNullOrWhiteSpace(profile.MainGoal))
            {
                return "Main goal is required";
            }

            if (string.IsNullOrWhiteSpace(profile.FitnessLevel))
            {
                return "Fitness level is required";
            }

            if (string.IsNullOrWhiteSpace(profile.Gender))
            {
                return "Gender is required";
            }

            if (profile.AvailableDays < 0 || profile.AvailableDays > 7)
            {
                return "Available days must be between 1 and 7";
            }

            return null; 
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
