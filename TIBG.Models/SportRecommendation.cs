using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Represents a complete sport recommendation with all details
    /// </summary>
    public class SportRecommendation
    {
        [Required]
        public string Sport { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public int Score { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(3)]
        public List<SportAlternative> Alternatives { get; set; } = new List<SportAlternative>();

        //[Required]
        //public TrainingPlan TrainingPlan { get; set; } = new TrainingPlan();
    }
}
