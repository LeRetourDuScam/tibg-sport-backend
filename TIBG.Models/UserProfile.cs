using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Simplified UserProfile with only essential fields (15 max)
    /// All fields are required except health conditions and injuries
    /// </summary>
    public class UserProfile
    {
        // Physical metrics (REQUIRED)
        [Required(ErrorMessage = "Age is required")]
        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
        public int Age { get; set; }
        
        [Required(ErrorMessage = "Gender is required")]
        [MaxLength(20, ErrorMessage = "Gender cannot exceed 20 characters")]
        public string Gender { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Height is required")]
        [Range(100, 250, ErrorMessage = "Height must be between 100 and 250 cm")]
        public double Height { get; set; }
        
        [Required(ErrorMessage = "Weight is required")]
        [Range(30, 300, ErrorMessage = "Weight must be between 30 and 300 kg")]
        public double Weight { get; set; }

        // Fitness & Health (REQUIRED)
        [Required(ErrorMessage = "Fitness level is required")]
        [MaxLength(50, ErrorMessage = "Fitness level cannot exceed 50 characters")]
        public string FitnessLevel { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Exercise frequency is required")]
        [MaxLength(50, ErrorMessage = "Exercise frequency cannot exceed 50 characters")]
        public string ExerciseFrequency { get; set; } = string.Empty;
        
        // Health issues (OPTIONAL - can be empty)
        [MaxLength(500, ErrorMessage = "Health conditions cannot exceed 500 characters")]
        public string? HealthConditions { get; set; }
        
        [MaxLength(500, ErrorMessage = "Injuries cannot exceed 500 characters")]
        public string? Injuries { get; set; }

        // Goals (REQUIRED)
        [Required(ErrorMessage = "Main goal is required")]
        [MaxLength(200, ErrorMessage = "Main goal cannot exceed 200 characters")]
        public string MainGoal { get; set; } = string.Empty;

        // Availability (REQUIRED)
        [Required(ErrorMessage = "Available time is required")]
        [MaxLength(50, ErrorMessage = "Available time cannot exceed 50 characters")]
        public string AvailableTime { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Number of available days is required")]
        [Range(1, 7, ErrorMessage = "Available days must be between 1 and 7")]
        public int AvailableDays { get; set; }

        // Preferences (REQUIRED)
        [Required(ErrorMessage = "Location preference is required")]
        [MaxLength(50, ErrorMessage = "Location preference cannot exceed 50 characters")]
        public string LocationPreference { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Team preference is required")]
        [MaxLength(50, ErrorMessage = "Team preference cannot exceed 50 characters")]
        public string TeamPreference { get; set; } = string.Empty;

        // Experience (REQUIRED)
        [MaxLength(500, ErrorMessage = "Practiced sports cannot exceed 500 characters")]
        public string? PractisedSports { get; set; }

        // Language (REQUIRED)
        [Required(ErrorMessage = "Language is required")]
        [MaxLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
        public string Language { get; set; } = "en";
    }
}
