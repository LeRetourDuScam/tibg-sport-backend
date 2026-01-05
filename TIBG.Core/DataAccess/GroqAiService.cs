using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TIBG.API.Core.Configuration;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for communicating with Groq API for sport recommendations
    /// </summary>
    public class GroqAiService : IAiRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqAiService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IOptions<GroqSettings> _settings;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl;
        private const string DefaultBaseUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const int DefaultMaxRetries = 3;
        private const int DefaultCacheDurationMinutes = 60;

        public GroqAiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GroqAiService> logger,
            IMemoryCache cache,
            IOptions<GroqSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
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

            var timeoutSeconds = _settings.Value.TimeoutSeconds > 0 ? _settings.Value.TimeoutSeconds : 120;
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<SportRecommendation?> GetSportRecommendationAsync(UserProfile profile)
        {
            var cacheKey = GenerateProfileHash(profile);

            if (_cache.TryGetValue(cacheKey, out SportRecommendation? cached))
            {
                _logger.LogInformation("Cache hit for profile {Hash}", cacheKey);
                return cached;
            }

            var maxRetries = _settings.Value.MaxRetries > 0 ? _settings.Value.MaxRetries : DefaultMaxRetries;
            var cacheDuration = _settings.Value.CacheDurationMinutes > 0 ? _settings.Value.CacheDurationMinutes : DefaultCacheDurationMinutes;

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await CallGroqApiAsync(profile);

                    if (result != null)
                    {
                        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(cacheDuration));
                    }

                    return result;
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "Groq API attempt {Attempt} failed, retrying in {Delay}s", attempt, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
                catch (TaskCanceledException ex) when (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "Groq API attempt {Attempt} timed out, retrying in {Delay}s", attempt, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }

            throw new GroqApiException("Failed after max retries");
        }

        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                var requestBody = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "user", content = "Hello" }
                    },
                    max_completion_tokens = 10
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_baseUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Groq service availability");
                return false;
            }
        }

        private async Task<SportRecommendation?> CallGroqApiAsync(UserProfile profile)
        {
            var prompt = BuildPrompt(profile);
            var language = profile.Language ?? "en";

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $"You are an expert sports recommendation assistant. Respond with valid JSON only. All content must be in {language} language (keys stay English)."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_completion_tokens = 1024,
                temperature = 1,
                top_p = 1,
                stream = false,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to Groq API with model: {Model}", _modelName);

            var response = await _httpClient.PostAsync(_baseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Groq API error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"Groq API returned {response.StatusCode}");
            }

            var responseText = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GroqResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var aiResponse = result?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                _logger.LogError("Groq API returned empty content");
                throw new GroqApiException("Groq API returned empty content");
            }

            return ParseAiResponse(aiResponse);
        }

        private string BuildPrompt(UserProfile profile)
        {
            var bmi = CalculateBmi(profile);

            var sections = new StringBuilder();

            sections.AppendLine("=== USER PROFILE DATA ===");
            sections.AppendLine(JsonSerializer.Serialize(new
            {
                physical = new
                {
                    profile.Age,
                    profile.Gender,
                    profile.Height,
                    profile.Weight,
                    BMI = bmi
                },
                fitness = new
                {
                    profile.FitnessLevel,
                    profile.ExerciseFrequency
                },
                health = new
                {
                    profile.HealthConditions,
                    profile.Injuries
                },
                goals = new { profile.MainGoal },
                availability = new
                {
                    profile.AvailableTime,
                    profile.AvailableDays
                },
                preferences = new
                {
                    profile.LocationPreference,
                    profile.TeamPreference,
                    profile.PractisedSports
                }
            }, new JsonSerializerOptions { WriteIndented = true }));

            sections.AppendLine("\n=== TASK ===");
            sections.AppendLine("Analyze this profile and return ONE optimal sport recommendation.");
            sections.AppendLine($"Response MUST be in {profile.Language ?? "en"} language.");

            sections.AppendLine("\n=== OUTPUT SCHEMA (STRICT) ===");
            sections.AppendLine(@"{
              ""sport"": ""string (required)"",
              ""score"": number 0-100 (required),
              ""reason"": ""string min 100 chars max 150 chars (required)"",
              ""explanation"": ""string min 100 chars max 150 chars (required)"",
              ""benefits"": [""string"", ""string"", ""string"", ""string"", ""string""] (exactly 5),
              ""precautions"": [""string"", ""string"", ""string"", ""string""] (exactly 4),
              ""exercises"": [
                {""name"": ""string"", ""description"": ""string"", ""duration"": ""string"", ""repetitions"": ""string""}
              ] (exactly 3),
              ""alternatives"": [
                {""sport"": ""string"", ""score"": number, ""reason"": ""string"", ""benefits"": [""string"", ...], ""precautions"": [""string"", ...]}
              ] (2-3 items),
              ""trainingPlan"": {
                ""goal"": ""string (required)"",
                ""equipment"": [""string (required)"", ...],
                ""progressionTips"": [""string (required)"", ""string (required)"", ""string (required)""]
              }
            }");

            sections.AppendLine("\n=== MEDICAL SAFETY RULES ===");
            if (!string.IsNullOrWhiteSpace(profile.HealthConditions) || !string.IsNullOrWhiteSpace(profile.Injuries))
            {
                sections.AppendLine("IMPORTANT: User has reported health concerns or injuries.");
                sections.AppendLine("Consider LOW-IMPACT sports and progressive intensity.");
                sections.AppendLine("Prioritize safety and recommend medical clearance when appropriate.");
            }

            return sections.ToString();
        }

        private static double CalculateBmi(UserProfile profile)
        {
            if (profile.Height <= 0 || profile.Weight <= 0)
            {
                return 0;
            }

            var heightM = profile.Height / 100.0;
            return Math.Round(profile.Weight / (heightM * heightM), 1);
        }

        private static string GenerateProfileHash(UserProfile profile)
        {
            var json = JsonSerializer.Serialize(profile);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hash);
        }

        private SportRecommendation? ParseAiResponse(string aiResponse)
        {
            try
            {
                var cleaned = aiResponse.Trim();

                if (cleaned.StartsWith("```json"))
                    cleaned = cleaned.Substring(7);
                if (cleaned.StartsWith("```"))
                    cleaned = cleaned.Substring(3);
                if (cleaned.EndsWith("```"))
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);

                cleaned = cleaned.Trim();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };

                try
                {
                    var recommendation = JsonSerializer.Deserialize<SportRecommendation>(cleaned, options);
                    
                    if (recommendation == null)
                    {
                        _logger.LogError("Deserialization returned null");
                        return null;
                    }

                    if (string.IsNullOrEmpty(recommendation.Sport))
                    {
                        _logger.LogError("Sport field is missing or empty");
                        return null;
                    }

                    if (recommendation.Alternatives == null || recommendation.Alternatives.Count < 2)
                    {
                        _logger.LogWarning($"Alternatives count is {recommendation.Alternatives?.Count ?? 0}, expected at least 2");
                    }

                    if (recommendation.Exercises == null || recommendation.Exercises.Count < 3)
                    {
                        _logger.LogWarning($"Exercises count is {recommendation.Exercises?.Count ?? 0}, expected 3");
                    }


                    return recommendation;
                }
                catch (JsonException jsonEx)
                {
                    if (!string.IsNullOrEmpty(jsonEx.Path))
                    {
                        _logger.LogError($"Problem at JSON path: {jsonEx.Path}");
                    }
                    
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing AI response. First 1000 chars: {aiResponse.Substring(0, Math.Min(1000, aiResponse.Length))}");
                return null;
            }
        }

        private class GroqResponse
        {
            public List<Choice>? Choices { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string? Content { get; set; }
        }
    }
}
