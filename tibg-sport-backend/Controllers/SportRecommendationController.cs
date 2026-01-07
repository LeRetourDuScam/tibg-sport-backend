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

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

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

            return System.Text.RegularExpressions.Regex.Replace(
                input, 
                @"[<>'"";(){}[\]\\]", 
                string.Empty
            ).Trim();
        }

        [HttpPost("training-plan")]
        public async Task<IActionResult> GetTrainingPlan([FromBody] TrainingPlanRequest request)
        {
            try
            {
                if (request == null || request.Profile == null || string.IsNullOrWhiteSpace(request.Sport))
                {
                    return BadRequest(new { error = "Profile data and sport are required" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

                request.Sport = SanitizeInput(request.Sport);

                _logger.LogInformation($"Received training plan request for sport: {request.Sport}");

                var trainingPlan = await _aiService.GetTrainingPlanAsync(request.Profile, request.Sport);

                if (trainingPlan != null)
                {
                    _logger.LogInformation($"Generated training plan for {request.Sport}");
                    return Ok(trainingPlan);
                }
                else
                {
                    _logger.LogError("Failed to generate training plan");
                    return StatusCode(500, new { error = "Failed to generate training plan. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating training plan");
                return StatusCode(500, new { error = "An error occurred while generating the training plan" });
            }
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
                    _logger.LogWarning("Ai service is unavailable");
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
