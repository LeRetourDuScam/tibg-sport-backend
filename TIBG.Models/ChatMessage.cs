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

    public class ChatResponse
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
