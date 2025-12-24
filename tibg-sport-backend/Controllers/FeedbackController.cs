using Microsoft.AspNetCore.Mvc;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(IFeedbackService feedbackService, ILogger<FeedbackController> logger)
        {
            _feedbackService = feedbackService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] UserFeedback feedback)
        {
            if (feedback == null)
            {
                return BadRequest(new { error = "Feedback data is required" });
            }

            if (string.IsNullOrWhiteSpace(feedback.Rating))
            {
                return BadRequest(new { error = "Rating is required" });
            }

            if (string.IsNullOrWhiteSpace(feedback.Sport))
            {
                return BadRequest(new { error = "Sport is required" });
            }

            var validRatings = new[] { "perfect", "good", "meh", "bad" };
            if (!validRatings.Contains(feedback.Rating.ToLower()))
            {
                return BadRequest(new { error = "Invalid rating value" });
            }

            feedback.CreatedAt = DateTime.UtcNow;

            var success = await _feedbackService.SaveFeedbackAsync(feedback);

            if (!success)
            {
                return StatusCode(500, new { error = "Failed to save feedback" });
            }

            return Ok(new { message = "Feedback submitted successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks()
        {
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync();
            return Ok(feedbacks);
        }
    }
}
