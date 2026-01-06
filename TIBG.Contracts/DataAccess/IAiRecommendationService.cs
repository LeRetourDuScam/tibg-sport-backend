using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Interface for AI-powered sport recommendation service
    /// </summary>
    public interface IAiRecommendationService
    {
        /// <summary>
        /// Generates a sport recommendation based on user profile
        /// </summary>
        Task<SportRecommendation?> GetSportRecommendationAsync(UserProfile profile);

        /// <summary>
        /// Generates a personalized training plan for a recommended sport
        /// </summary>
        Task<TrainingPlan?> GetTrainingPlanAsync(UserProfile profile, string sport);

        /// <summary>
        /// Checks if the AI service is available
        /// </summary>
        Task<bool> IsServiceAvailableAsync();
    }
}
