using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TIBG.API.Core.Configuration;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    public class GroqChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqChatService> _logger;
        private readonly IOptions<GroqSettings> _settings;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl;
        private const string DefaultBaseUrl = "https://api.groq.com/openai/v1/chat/completions";

        public GroqChatService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GroqChatService> logger,
            IOptions<GroqSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings;

            _apiKey = !string.IsNullOrWhiteSpace(_settings.Value.ApiKey)
                ? _settings.Value.ApiKey
                : configuration["Groq:ApiKey"] ?? throw new ArgumentException("GROQ_API_KEY not configured");

            _modelName = string.IsNullOrWhiteSpace(_settings.Value.ModelName)
                ? "llama-3.3-70b-versatile"
                : _settings.Value.ModelName;

            _baseUrl = string.IsNullOrWhiteSpace(_settings.Value.BaseUrl)
                ? DefaultBaseUrl
                : _settings.Value.BaseUrl;

            var timeoutSeconds = _settings.Value.TimeoutSeconds > 0 ? _settings.Value.TimeoutSeconds : 60;
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> GetChatResponseAsync(ChatRequest request)
        {
            try
            {
                var systemPrompt = BuildSystemPrompt(request.UserProfile, request.Recommendation);
                var messages = BuildMessageHistory(systemPrompt, request.ConversationHistory, request.UserMessage);

                var requestBody = new
                {
                    model = _modelName,
                    messages = messages,
                    max_completion_tokens = 800,
                    temperature = 0.7,
                    top_p = 0.9,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending chat request to Groq API with model: {Model}", _modelName);

                var response = await _httpClient.PostAsync(_baseUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Groq API error: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new HttpRequestException($"Groq API returned {response.StatusCode}");
                }

                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<GroqChatResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var aiResponse = result?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogError("Groq API returned empty content");
                    throw new GroqApiException("Groq API returned empty content");
                }

                _logger.LogInformation("Successfully received chat response from Groq API");
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response from Groq API");
                throw;
            }
        }

        private string BuildSystemPrompt(UserProfile profile, SportRecommendation recommendation)
        {
            var language = profile.Language ?? "en";
            var languageInstruction = language switch
            {
                "fr" => "Réponds TOUJOURS en français.",
                "en" => "Always respond in English.",
                _ => "Always respond in English."
            };

            return $@"You are FytAI, an expert sports coach and health advisor. Your role is to help users with questions about their personalized sport recommendation.

            {languageInstruction}

            === USER CONTEXT ===
            User Profile:
            - Age: {profile.Age}, Gender: {profile.Gender}
            - Fitness Level: {profile.FitnessLevel}
            - Main Goal: {profile.MainGoal}
            - Health Concerns: {GetHealthConcerns(profile)}
            - Available Time: {profile.AvailableTime}
            - Preferences: {profile.LocationPreference}, {profile.SocialPreference}

            Recommended Sport: {recommendation.Sport} (Score: {recommendation.Score}%)
            Reason: {recommendation.Reason}

            Key Benefits: {string.Join(", ", recommendation.Benefits.Take(3))}
            Important Precautions: {string.Join(", ", recommendation.Precautions.Take(2))}

            === YOUR ROLE ===
            - Answer questions about the recommended sport and why it suits the user
            - Provide practical advice on getting started
            - Suggest modifications based on health concerns
            - Explain exercises and training plan details
            - Offer motivation and address concerns
            - Be supportive, encouraging, and professional
            - Keep responses concise (2-4 paragraphs max)
            - If asked about medical issues, always remind users to consult healthcare professionals

            === COMMUNICATION STYLE ===
            Tone: {profile.PreferredTone ?? "Encouraging and professional"}
            Learning Style: {profile.LearningStyle ?? "Practical with examples"}";
        }

        private string GetHealthConcerns(UserProfile profile)
        {
            var concerns = new List<string>();
            if (profile.JointProblems) concerns.Add("Joint problems");
            if (profile.KneeProblems) concerns.Add("Knee problems");
            if (profile.BackProblems) concerns.Add("Back problems");
            if (profile.HeartProblems) concerns.Add("Heart problems");
            if (!string.IsNullOrWhiteSpace(profile.Injuries)) concerns.Add($"Injuries: {profile.Injuries}");
            
            return concerns.Any() ? string.Join(", ", concerns) : "None reported";
        }

        private object[] BuildMessageHistory(string systemPrompt, List<ChatMessage> history, string userMessage)
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var msg in history.TakeLast(10))
            {
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
            }

            messages.Add(new { role = "user", content = userMessage });

            return messages.ToArray();
        }

        private class GroqChatResponse
        {
            public Choice[]? Choices { get; set; }

            public class Choice
            {
                public Message? Message { get; set; }
            }

            public class Message
            {
                public string? Content { get; set; }
            }
        }
    }
}
