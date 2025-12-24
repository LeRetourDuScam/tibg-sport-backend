using System.Collections.Generic;

namespace TIBG.Models
{

    public class UserProfile
    {
        // Physical metrics
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public double? LegLength { get; set; }
        public double? ArmLength { get; set; }
        public double? WaistSize { get; set; }

        // Fitness & Health
        public string? FitnessLevel { get; set; }
        public string? ExerciseFrequency { get; set; }
        public bool JointProblems { get; set; }
        public bool KneeProblems { get; set; }
        public bool BackProblems { get; set; }
        public bool HeartProblems { get; set; }
        public string? HealthConditions { get; set; }
        public string? OtherHealthIssues { get; set; }
        public string? Injuries { get; set; }
        public string? Allergies { get; set; }

        // Goals & Motivation
        public string? MainGoal { get; set; }
        public string? SpecificGoals { get; set; }
        public string? Motivations { get; set; }
        public string? Fears { get; set; }

        // Availability & Lifestyle
        public string? AvailableTime { get; set; }
        public string? PreferredTime { get; set; }
        public int? AvailableDays { get; set; }
        public string? WorkType { get; set; }
        public string? SleepQuality { get; set; }
        public string? StressLevel { get; set; }
        public string? Lifestyle { get; set; }

        // Preferences
        public string? ExercisePreferences { get; set; }
        public string? ExerciseAversions { get; set; }
        public string? LocationPreference { get; set; }
        public string? TeamPreference { get; set; }
        public string? EquipmentAvailable { get; set; }
        public string? MusicPreference { get; set; }
        public string? SocialPreference { get; set; }

        // Experience
        public string? PractisedSports { get; set; }
        public string? FavoriteActivity { get; set; }
        public string? PastExperienceWithFitness { get; set; }
        public string? SuccessFactors { get; set; }

        // Challenges & Support
        public string? PrimaryChallenges { get; set; }
        public string? SupportSystem { get; set; }

        // Language and preferences
        public string Language { get; set; } = "en";
        public string? PreferredTone { get; set; }
        public string? LearningStyle { get; set; }
    }
}
