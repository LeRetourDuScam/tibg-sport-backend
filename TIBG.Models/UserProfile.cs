using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{

    public class UserProfile
    {
        // Physical metrics
        public int? Age { get; set; }
        
        [MaxLength(50)]
        public string? Gender { get; set; }
        
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public double? LegLength { get; set; }
        public double? ArmLength { get; set; }
        public double? WaistSize { get; set; }

        // Fitness & Health
        [MaxLength(100)]
        public string? FitnessLevel { get; set; }
        
        [MaxLength(100)]
        public string? ExerciseFrequency { get; set; }
        
        public bool JointProblems { get; set; }
        public bool KneeProblems { get; set; }
        public bool BackProblems { get; set; }
        public bool HeartProblems { get; set; }
        
        [MaxLength(500)]
        public string? HealthConditions { get; set; }
        
        [MaxLength(500)]
        public string? OtherHealthIssues { get; set; }
        
        [MaxLength(500)]
        public string? Injuries { get; set; }
        
        [MaxLength(500)]
        public string? Allergies { get; set; }

        // Goals & Motivation
        [MaxLength(200)]
        public string? MainGoal { get; set; }
        
        [MaxLength(1000)]
        public string? SpecificGoals { get; set; }
        
        [MaxLength(1000)]
        public string? Motivations { get; set; }
        
        [MaxLength(1000)]
        public string? Fears { get; set; }

        // Availability & Lifestyle
        [MaxLength(100)]
        public string? AvailableTime { get; set; }
        
        [MaxLength(100)]
        public string? PreferredTime { get; set; }
        
        public int? AvailableDays { get; set; }
        
        [MaxLength(100)]
        public string? WorkType { get; set; }
        
        [MaxLength(100)]
        public string? SleepQuality { get; set; }
        
        [MaxLength(100)]
        public string? StressLevel { get; set; }
        
        [MaxLength(500)]
        public string? Lifestyle { get; set; }

        // Preferences
        [MaxLength(500)]
        public string? ExercisePreferences { get; set; }
        
        [MaxLength(500)]
        public string? ExerciseAversions { get; set; }
        
        [MaxLength(100)]
        public string? LocationPreference { get; set; }
        
        [MaxLength(100)]
        public string? TeamPreference { get; set; }
        
        [MaxLength(500)]
        public string? EquipmentAvailable { get; set; }
        
        [MaxLength(100)]
        public string? MusicPreference { get; set; }
        
        [MaxLength(100)]
        public string? SocialPreference { get; set; }

        // Experience
        [MaxLength(500)]
        public string? PractisedSports { get; set; }
        
        [MaxLength(200)]
        public string? FavoriteActivity { get; set; }
        
        [MaxLength(100)]
        public string? PastExperienceWithFitness { get; set; }
        
        [MaxLength(500)]
        public string? SuccessFactors { get; set; }

        // Challenges & Support
        [MaxLength(500)]
        public string? PrimaryChallenges { get; set; }
        
        [MaxLength(500)]
        public string? SupportSystem { get; set; }

        // Language and preferences
        [MaxLength(10)]
        public string Language { get; set; } = "en";
        
        [MaxLength(100)]
        public string? PreferredTone { get; set; }
        
        [MaxLength(100)]
        public string? LearningStyle { get; set; }
    }
}
