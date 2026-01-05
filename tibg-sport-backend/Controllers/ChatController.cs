using Microsoft.AspNetCore.Mvc;
using TIBG.API.Core.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for chatbot endpoints
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request data is required" });
                }

                if (string.IsNullOrWhiteSpace(request.UserMessage))
                {
                    return BadRequest(new { error = "User message is required" });
                }

                if (request.UserProfile == null)
                {
                    return BadRequest(new { error = "User profile is required" });
                }

                if (request.Recommendation == null)
                {
                    return BadRequest(new { error = "Recommendation is required" });
                }

                _logger.LogInformation("Received chat request with message: {Message}", 
                    request.UserMessage.Length > 50 ? request.UserMessage[..50] + "..." : request.UserMessage);

                var responseMessage = await _chatService.GetChatResponseAsync(request);

                if (!string.IsNullOrWhiteSpace(responseMessage))
                {
                    _logger.LogInformation("Successfully generated chat response");
                    return Ok(new ChatResponse { Message = responseMessage });
                }
                else
                {
                    _logger.LogError("Chat service returned empty response");
                    return StatusCode(500, new { error = "Failed to generate response" });
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling chat service");
                return StatusCode(503, new { error = "AI service temporarily unavailable. Please try again." });
            }
            catch (GroqApiException ex)
            {
                _logger.LogError(ex, "Groq API error while processing chat");
                return StatusCode(500, new { error = "Failed to process your message. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing chat request");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpGet("chat/health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "chat" });
        }
    }
}
