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
        public string Explanation { get; set; } = string.Empty;

        [Required]
        [MinLength(5)]
        [MaxLength(5)]
        public List<string> Benefits { get; set; } = new List<string>();

        [Required]
        [MinLength(4)]
        [MaxLength(4)]
        public List<string> Precautions { get; set; } = new List<string>();

        [Required]
        [MinLength(3)]
        [MaxLength(3)]
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();

        [Required]
        [MinLength(2)]
        [MaxLength(3)]
        public List<SportAlternative> Alternatives { get; set; } = new List<SportAlternative>();

        [Required]
        public TrainingPlan TrainingPlan { get; set; } = new TrainingPlan();
    }
}
