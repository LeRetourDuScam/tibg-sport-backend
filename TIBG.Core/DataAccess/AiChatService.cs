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
    public class AiChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiChatService> _logger;
        private readonly IOptions<AiSettings> _settings;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl;
        private const string DefaultBaseUrl = "https://api.groq.com/openai/v1/chat/completions";

        public AiChatService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AiChatService> logger,
            IOptions<AiSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings;

            _apiKey = !string.IsNullOrWhiteSpace(_settings.Value.ApiKey)
                ? _settings.Value.ApiKey
                : configuration["Ai:ApiKey"] ?? throw new ArgumentException("Ai_API_KEY not configured");

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

        private class AiChatResponse
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

        public async Task<string> GetHealthChatResponseAsync(HealthChatRequest request)
        {
            try
            {
                var systemPrompt = BuildHealthSystemPrompt(request);
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

                _logger.LogInformation("Sending health chat request to AI API with model: {Model}", _modelName);

                var response = await _httpClient.PostAsync(_baseUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI API error: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new HttpRequestException($"AI API returned {response.StatusCode}");
                }

                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<AiChatResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var aiResponse = result?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogError("AI API returned empty content for health chat");
                    throw new AiApiException("AI API returned empty content");
                }

                _logger.LogInformation("Successfully received health chat response from AI API");
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health chat response from AI API");
                throw;
            }
        }

        private string BuildHealthSystemPrompt(HealthChatRequest request)
        {
            var languageInstruction = request.Language switch
            {
                "fr" => "Réponds TOUJOURS en français.",
                "en" => "Always respond in English.",
                _ => "Réponds TOUJOURS en français."
            };

            var weakCategories = request.WeakCategories.Any() 
                ? string.Join(", ", request.WeakCategories) 
                : "Aucune catégorie faible";

            var riskFactors = request.RiskFactors.Any() 
                ? string.Join(", ", request.RiskFactors) 
                : "Aucun facteur de risque identifié";

            var exercises = request.RecommendedExercises.Any() 
                ? string.Join(", ", request.RecommendedExercises.Take(8)) 
                : "Exercices adaptés au profil";

            var recommendations = request.Recommendations.Any() 
                ? string.Join("\n- ", request.Recommendations.Take(5)) 
                : "Suivre les conseils généraux de santé";

            return $@"Tu es FytAI, un coach santé et bien-être expert. Tu aides les utilisateurs à comprendre leurs résultats de questionnaire de santé et à améliorer leur bien-être.

            {languageInstruction}

            === RÉSULTATS DU QUESTIONNAIRE DE SANTÉ ===
            - Score global de santé: {request.ScorePercentage}%
            - Niveau de santé: {request.HealthLevel}
            - Catégories à améliorer: {weakCategories}
            - Facteurs de risque identifiés: {riskFactors}
            - Exercices recommandés: {exercises}
            - Recommandations personnalisées:
            - {recommendations}

            === TON RÔLE ===
            - Répondre aux questions sur les résultats du questionnaire de santé
            - Expliquer le score et le niveau de santé de manière bienveillante
            - Donner des conseils pratiques et personnalisés pour améliorer les catégories faibles
            - Guider l'utilisateur sur les exercices recommandés
            - Motiver et encourager l'utilisateur dans sa démarche de santé
            - Être empathique, professionnel et encourageant
            - Garder les réponses concises (2-4 paragraphes maximum)
            - Toujours rappeler de consulter un professionnel de santé pour des problèmes médicaux

            === STYLE DE COMMUNICATION ===
            Ton: Encourageant, bienveillant et professionnel
            Approche: Pratique avec des exemples concrets et des étapes réalisables";
        }
    }
}
