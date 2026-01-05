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
        [Range(1, 52)]
        public int DurationWeeks { get; set; }

        [Required]
        [MinLength(1)]
        public List<WeekPlan> Weeks { get; set; } = new List<WeekPlan>();

        [Required]
        [MinLength(1)]
        public List<string> Equipment { get; set; } = new List<string>();

        [Required]
        [MinLength(3)]
        [MaxLength(5)]
        public List<string> ProgressionTips { get; set; } = new List<string>();
    }
    public class WeekPlan
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int WeekNumber { get; set; }

        [Required]
        public string Focus { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<SessionPlan> Sessions { get; set; } = new List<SessionPlan>();
    }
    public class SessionPlan
    {
        [Required]
        public string Day { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Duration { get; set; }

        [Required]
        public string Intensity { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<string> Exercises { get; set; } = new List<string>();

        public string? Notes { get; set; }
    }
}
