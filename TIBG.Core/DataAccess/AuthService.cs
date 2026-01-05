using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.ENTITIES;
using TIBG.Models;
using BCrypt.Net;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Authentication service for user registration and login
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly FytAiDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            FytAiDbContext context, 
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning($"Registration attempt with existing email: {request.Email}");
                    return null;
                }

                // Hash password
                var passwordHash = HashPassword(request.Password);

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User registered successfully: {user.Email}");

                // Generate JWT token
                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Username);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                return new AuthResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return null;
            }
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Failed login attempt for email: {request.Email}");
                    return null;
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"Login attempt for inactive user: {request.Email}");
                    return null;
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User logged in successfully: {user.Email}");

                // Generate JWT token
                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Username);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                return new AuthResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
        }

        /// <summary>
        /// Hash password using BCrypt with automatic salt generation
        /// </summary>
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verify password against BCrypt hash
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
