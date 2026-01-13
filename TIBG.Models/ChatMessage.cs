using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    public class ChatMessage
    {
        [Required]
        public string Role { get; set; } = string.Empty; 

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime? Timestamp { get; set; }
    }

    public class ChatRequest
    {
        [Required]
        public UserProfile UserProfile { get; set; } = new UserProfile();

        [Required]
        public SportRecommendation Recommendation { get; set; } = new SportRecommendation();

        [Required]
        public List<ChatMessage> ConversationHistory { get; set; } = new List<ChatMessage>();

        [Required]
        public string UserMessage { get; set; } = string.Empty;
    }

    public class HealthChatRequest
    {
        [Required]
        public int ScorePercentage { get; set; }

        [Required]
        public string HealthLevel { get; set; } = string.Empty;

        public List<string> WeakCategories { get; set; } = new List<string>();

        public List<string> RiskFactors { get; set; } = new List<string>();

        public List<string> RecommendedExercises { get; set; } = new List<string>();

        public List<string> Recommendations { get; set; } = new List<string>();

        [Required]
        public List<ChatMessage> ConversationHistory { get; set; } = new List<ChatMessage>();

        [Required]
        public string UserMessage { get; set; } = string.Empty;

        public string Language { get; set; } = "fr";
    }

    public class ChatResponse
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
