using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TIBG.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for communicating with Hugging Face API for sport recommendations
    /// </summary>
    public class HuggingFaceAiService : IAiRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HuggingFaceAiService> _logger;
        private readonly string _apiKey;
        private readonly string _modelName;
        private const string BaseUrl = "https://router.huggingface.co/v1/chat/completions";

        public HuggingFaceAiService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<HuggingFaceAiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["HuggingFace:ApiKey"] ?? throw new ArgumentException("HUGGINGFACE_TOKEN not configured");
            _modelName = configuration["HuggingFace:ModelName"] ?? "Qwen/Qwen2.5-7B-Instruct";
            
            _httpClient.Timeout = TimeSpan.FromSeconds(300);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<SportRecommendation?> GetSportRecommendationAsync(UserProfile profile)
        {
            try
            {
                var prompt = BuildPrompt(profile);
                var language = profile.Language ?? "en";

                var requestBody = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { 
                            role = "system", 
                            content = $"You are an expert sports recommendation assistant. You can return any type of sport dont only do natation and cyclisme. You MUST respond ONLY with valid JSON matching the provided schema. All text content (sport names, descriptions, benefits, etc) MUST be in {language} language. Only JSON keys stay in English. Return ONLY the JSON object, no markdown, no explanation."
                        },
                        new { 
                            role = "user", 
                            content = prompt 
                        }
                    },
                    max_tokens = 8192,
                    temperature = 0.3,
                    top_p = 0.9,
                    response_format = new { type = "json_object" },
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(BaseUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Hugging Face API error: {response.StatusCode} - {error}");
                    return null;
                }

                var responseText = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<HuggingFaceResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Choices != null && result.Choices.Count > 0)
                {
                    var aiResponse = result.Choices[0].Message?.Content;
                    if (!string.IsNullOrEmpty(aiResponse))
                    {
                        return ParseAiResponse(aiResponse);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sport recommendation from Hugging Face");
                return null;
            }
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
                    max_tokens = 10
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(BaseUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Hugging Face service availability");
                return false;
            }
        }

        private string BuildPrompt(UserProfile profile)
        {
            // Physical metrics
            var heightM = (profile.Height ?? 170) / 100;
            var bmi = (profile.Weight ?? 70) / (heightM * heightM);
            var legLength = profile.LegLength?.ToString() ?? "N/A";
            var armLength = profile.ArmLength?.ToString() ?? "N/A";
            var waistSize = profile.WaistSize?.ToString() ?? "N/A";

            // Health conditions
            var healthIssues = new List<string>();
            if (profile.JointProblems) healthIssues.Add("joint problems");
            if (profile.KneeProblems) healthIssues.Add("knee problems");
            if (profile.BackProblems) healthIssues.Add("back problems");
            if (profile.HeartProblems) healthIssues.Add("heart problems");
            if (profile.HealthConditions != null) healthIssues.AddRange(profile.HealthConditions);
            if (!string.IsNullOrEmpty(profile.OtherHealthIssues)) healthIssues.Add(profile.OtherHealthIssues);
            if (!string.IsNullOrEmpty(profile.Injuries)) healthIssues.Add($"injuries: {profile.Injuries}");
            if (!string.IsNullOrEmpty(profile.Allergies)) healthIssues.Add($"allergies: {profile.Allergies}");

            var healthText = healthIssues.Any() ? string.Join(", ", healthIssues) : "no particular health issues";

            // Goals and motivations
            var mainGoal = profile.MainGoal ?? "general fitness";
            var specificGoals = profile.SpecificGoals != null && profile.SpecificGoals.Any() ? string.Join(", ", profile.SpecificGoals) : "none specified";
            var motivations = profile.Motivations != null && profile.Motivations.Any() ? string.Join(", ", profile.Motivations) : "none specified";
            var fears = profile.Fears != null && profile.Fears.Any() ? string.Join(", ", profile.Fears) : "none";

            // Lifestyle and availability
            var availableTime = profile.AvailableTime ?? "flexible";
            var preferredTime = profile.PreferredTime ?? "any time";
            var availableDays = profile.AvailableDays ?? 1;
            var workType = profile.WorkType ?? "not specified";
            var sleepQuality = profile.SleepQuality ?? "normal";
            var stressLevel = profile.StressLevel ?? "moderate";
            var lifestyle = profile.Lifestyle ?? "not specified";

            // Preferences
            var exercisePreferences = profile.ExercisePreferences != null && profile.ExercisePreferences.Any() ? string.Join(", ", profile.ExercisePreferences) : "no preferences";
            var exerciseAversions = profile.ExerciseAversions != null && profile.ExerciseAversions.Any() ? string.Join(", ", profile.ExerciseAversions) : "none";
            var equipmentAvailable = profile.EquipmentAvailable != null && profile.EquipmentAvailable.Any() ? string.Join(", ", profile.EquipmentAvailable) : "none";
            var musicPreference = profile.MusicPreference ?? "any";
            var socialPreference = profile.SocialPreference ?? "flexible";

            // Experience
            var practisedSports = profile.PractisedSports != null && profile.PractisedSports.Any() ? string.Join(", ", profile.PractisedSports) : "None";
            var favoriteActivity = profile.FavoriteActivity ?? "not specified";
            var pastExperience = profile.PastExperienceWithFitness ?? "beginner";
            var successFactors = profile.SuccessFactors != null && profile.SuccessFactors.Any() ? string.Join(", ", profile.SuccessFactors) : "not specified";

            // Challenges and support
            var challenges = profile.PrimaryChallenges != null && profile.PrimaryChallenges.Any() ? string.Join(", ", profile.PrimaryChallenges) : "none";
            var supportSystem = profile.SupportSystem ?? "not specified";

            return $@"You are an expert in sports recommendation. Analyze this comprehensive profile and recommend ONE sport that perfectly matches this person.

PHYSICAL PROFILE:
Age: {profile.Age ?? 0} years | Gender: {profile.Gender ?? "N/A"}
Height: {profile.Height ?? 0}cm | Weight: {profile.Weight ?? 0}kg | BMI: {bmi:F1}
Leg Length: {legLength}cm | Arm Length: {armLength}cm | Waist: {waistSize}cm

FITNESS & HEALTH:
Fitness Level: {profile.FitnessLevel ?? "beginner"} | Activity Level: {profile.ActivityLevel ?? "sedentary"}
Exercise Frequency: {profile.ExerciseFrequency ?? "never"}
Health Issues: {healthText}

GOALS & MOTIVATION:
Main Goal: {mainGoal}
Specific Goals: {specificGoals}
Motivations: {motivations}
Fears/Concerns: {fears}

AVAILABILITY & LIFESTYLE:
Available Time: {availableTime} | Preferred Time: {preferredTime} | Days/Week: {availableDays}
Work Type: {workType} | Sleep Quality: {sleepQuality} | Stress Level: {stressLevel}
Lifestyle: {lifestyle}

PREFERENCES:
Exercise Preferences: {exercisePreferences}
Exercise Aversions: {exerciseAversions}
Location: {profile.LocationPreference ?? "any"} | Team/Solo: {profile.TeamPreference ?? "flexible"}
Equipment Available: {equipmentAvailable}
Music: {musicPreference} | Social: {socialPreference}

EXPERIENCE:
Practiced Sports: {practisedSports}
Favorite Activity: {favoriteActivity}
Past Experience: {pastExperience}
Success Factors: {successFactors}

CHALLENGES:
Primary Challenges: {challenges}
Support System: {supportSystem}

CRITICAL INSTRUCTIONS:
1. Analyze ALL the information above to make the BEST recommendation
2. Consider physical metrics, health issues, goals, lifestyle, preferences, and experience
3. IMPORTANT: ALL text content MUST be written in this language: ""{profile.Language}""
   - Sport name, reason, explanation, benefits, precautions, exercise names/descriptions
   - For example: if language is ""pt"" → ""Natação"" not ""Swimming""
   - if ""fr"" → ""Natation"", if ""es"" → ""Natación"", if ""de"" → ""Schwimmen""
   - Only JSON keys stay in English
4. Preferred Tone: {profile.PreferredTone ?? "encouraging"}
5. Learning Style: {profile.LearningStyle ?? "visual"}

CRITICAL JSON FORMATTING:
- Return ONLY valid JSON, no markdown, no explanation before or after
- benefits MUST be an array of exactly 5 strings: [""benefit1"", ""benefit2"", ""benefit3"", ""benefit4"", ""benefit5""]
- precautions MUST be an array of exactly 4 strings: [""precaution1"", ""precaution2"", ""precaution3"", ""precaution4""]
- exercises MUST be an array of exactly 3 objects with name, description, duration, repetitions, videoUrl
- alternatives MUST be an array of 2-3 objects, each with: sport (string), score (number 0-100), reason (string), benefits (array of 3-5 strings), precautions (array of 2-4 strings)
- trainingPlan MUST be an object with: goal (string), equipment (array of strings), progressionTips (array of 3-5 strings)
- ALL arrays of strings must contain actual string values, NOT nested objects
- Example of CORRECT alternatives format:
  ""alternatives"": [
    {{
      ""sport"": ""Yoga"",
      ""score"": 85,
      ""reason"": ""Great alternative"",
      ""benefits"": [""benefit1"", ""benefit2"", ""benefit3""],
      ""precautions"": [""precaution1"", ""precaution2""]
    }}
  ]

START your response with {{ and END with }}";
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

        private class HuggingFaceResponse
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
