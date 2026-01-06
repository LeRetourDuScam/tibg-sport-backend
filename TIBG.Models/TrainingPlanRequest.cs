using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Request model for generating a training plan
    /// </summary>
    public class TrainingPlanRequest
    {
        [Required]
        public UserProfile Profile { get; set; } = new UserProfile();

        [Required]
        public string Sport { get; set; } = string.Empty;
    }
}
