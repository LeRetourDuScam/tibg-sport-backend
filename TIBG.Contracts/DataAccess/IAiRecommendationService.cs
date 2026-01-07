using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Interface for AI-powered sport recommendation service
    /// </summary>
    public interface IAiRecommendationService
    {
        Task<SportRecommendation?> GetSportRecommendationAsync(UserProfile profile);

        Task<TrainingPlan?> GetTrainingPlanAsync(UserProfile profile, string sport);

        Task<bool> IsServiceAvailableAsync();
    }
}
