using Microsoft.AspNetCore.Mvc;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Register attempt with null request");
                    return BadRequest(new { error = "Request data is required" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Register attempt with invalid model state");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Attempting to register user with email: {Email}", request.Email);

                var response = await _authService.RegisterAsync(request);

                if (response == null)
                {
                    _logger.LogWarning("Registration failed for email: {Email}", request.Email);
                    return BadRequest(new { error = "Registration failed. Email might already be in use." });
                }

                _logger.LogInformation("User successfully registered with email: {Email}", request.Email);
                return CreatedAtAction(nameof(Register), new { email = response.Email }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering user with email: {Email}", request?.Email);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Login attempt with null request");
                    return BadRequest(new { error = "Request data is required" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login attempt with invalid model state");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                var response = await _authService.LoginAsync(request);

                if (response == null)
                {
                    _logger.LogWarning("Login failed for email: {Email}", request.Email);
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                _logger.LogInformation("User successfully logged in with email: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging in user with email: {Email}", request?.Email);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("user/{id}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching user with ID: {UserId}", id);

                var user = await _authService.GetUserByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                    return NotFound(new { error = "User not found" });
                }

                _logger.LogInformation("User found with ID: {UserId}", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user with ID: {UserId}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>User information</returns>
        [HttpGet("user/email/{email}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                _logger.LogInformation("Fetching user with email: {Email}", email);

                var user = await _authService.GetUserByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return NotFound(new { error = "User not found" });
                }

                _logger.LogInformation("User found with email: {Email}", email);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user with email: {Email}", email);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }
    }
}
