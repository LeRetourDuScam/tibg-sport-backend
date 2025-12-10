using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Represents a training plan for a sport
    /// </summary>
    public class TrainingPlan
    {
        [Required]
        public string Goal { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<string> Equipment { get; set; } = new List<string>();

        [Required]
        [MinLength(3)]
        [MaxLength(5)]
        public List<string> ProgressionTips { get; set; } = new List<string>();
    }
}
