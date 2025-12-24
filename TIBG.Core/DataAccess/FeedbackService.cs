using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using TIBG.Contracts.DataAccess;
using TIBG.Models;
using TIBG.ENTITIES;

namespace TIBG.API.Core.DataAccess
{
    public class FeedbackService : IFeedbackService
    {
        private readonly FytAiDbContext _dbContext; 
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(FytAiDbContext dbContext, ILogger<FeedbackService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> SaveFeedbackAsync(UserFeedback feedback)
        {
            try
            {
                _dbContext.Feedbacks.Add(feedback);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Feedback saved to database: Rating={Rating}, Sport={Sport}", 
                    feedback.Rating, feedback.Sport);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feedback to database");
                return false;
            }
        }

        public async Task<List<UserFeedback>> GetAllFeedbacksAsync()
        {
            try
            {
                return await _dbContext.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedbacks from database");
                return new List<UserFeedback>();
            }
        }
    }
}
