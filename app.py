from flask import Flask, request, jsonify
from flask_cors import CORS
import json
import re
import os
from dotenv import load_dotenv
from openai import OpenAI
import urllib3
import httpx
import ssl
import instructor
from pydantic import BaseModel, Field
from typing import List

# Load environment variables from .env file
load_dotenv()

# Disable SSL verification warnings
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

app = Flask(__name__)
CORS(app)

class Exercise(BaseModel):
    name: str = Field(description="Exercise name")
    description: str = Field(description="Complete description of the exercise")
    duration: str = Field(description="Recommended duration (e.g., '15-20 min')")
    repetitions: str = Field(description="Number of sets and reps (e.g., '3 sets of 10')")
    videoUrl: str = Field(description="YouTube video URL for demonstration")

class TrainingSession(BaseModel):
    day: int = Field(ge=1, le=7, description="Day of the week (1-7, Monday-Sunday)")
    title: str = Field(description="Session title")
    duration: str = Field(description="Session duration (e.g., '45 min')")
    exercises: List[str] = Field(description="List of exercises for this session")
    notes: str | None = Field(default=None, description="Optional notes for the session")

class TrainingWeek(BaseModel):
    weekNumber: int = Field(ge=1, description="Week number in the plan")
    focus: str = Field(description="Main focus of this week (e.g., 'Building endurance')")
    sessions: List[TrainingSession] = Field(description="Training sessions for this week")
    milestone: str | None = Field(default=None, description="Optional milestone for this week")

class TrainingPlan(BaseModel):
    duration: str = Field(description="Total plan duration (e.g., '4 weeks', '8 weeks')")
    goal: str = Field(description="Main goal of the training plan")
    weeks: List[TrainingWeek] = Field(min_length=2, max_length=4, description="Weekly training schedule (2-4 weeks for beginners)")
    equipment: List[str] = Field(description="Required equipment")
    progressionTips: List[str] = Field(min_length=3, max_length=5, description="Tips for progressing in the plan")

class SportAlternative(BaseModel):
    sport: str = Field(description="Alternative sport name")
    score: int = Field(ge=0, le=100, description="Compatibility score")
    reason: str = Field(description="Why this is a good alternative")
    benefits: List[str] = Field(min_length=3, max_length=5, description="Main benefits (3-5)")
    precautions: List[str] = Field(min_length=2, max_length=4, description="Precautions (2-4)")

class SportRecommendation(BaseModel):
    sport: str = Field(description="Name of the recommended sport")
    score: int = Field(ge=0, le=100, description="Compatibility score between 0 and 100")
    reason: str = Field(description="Short and precise reason for recommendation")
    explanation: str = Field(description="2-3 sentences explaining why this sport suits the profile")
    benefits: List[str] = Field(min_length=5, max_length=5, description="List of exactly 5 benefits")
    precautions: List[str] = Field(min_length=4, max_length=4, description="List of exactly 4 precautions")
    exercises: List[Exercise] = Field(min_length=3, max_length=3, description="List of exactly 3 exercises")
    alternatives: List[SportAlternative] = Field(min_length=2, max_length=3, description="2-3 alternative sports")
    trainingPlan: TrainingPlan = Field(description="Detailed training plan for beginners")  

MODEL_NAME = "meta-llama/Llama-3.2-3B-Instruct"

HF_TOKEN = os.getenv("HUGGINGFACE_TOKEN", None)

http_client = httpx.Client(verify=False)

base_client = OpenAI(
    base_url="https://router.huggingface.co/v1",
    api_key=HF_TOKEN,
    http_client=http_client,
    timeout=120.0,
)

client = instructor.from_openai(base_client, mode=instructor.Mode.JSON)

def query_huggingface(prompt, model=MODEL_NAME, language="en"):
    """
    Send a request to Hugging Face using OpenAI-compatible API with Instructor
    Instructor ensures the response matches the Pydantic schema and retries if needed
    """
    try:
        if not HF_TOKEN:
            print("Error: No Hugging Face token found in .env file")
            return None
        
        response = client.chat.completions.create(
            model=model,
            response_model=SportRecommendation,
            messages=[
                {"role": "system", "content": f"You are an expert sports recommendation assistant. You MUST respond in {language} language for all text content (sport names, descriptions, benefits, etc). Only JSON keys stay in English. You MUST include alternatives and trainingPlan in your response."},
                {"role": "user", "content": prompt}
            ],
            max_tokens=8192,
            temperature=0.3,
            top_p=0.9,
            max_retries=3,
        )
        
        return response.model_dump() if response else None
            
    except Exception as e:
        print(f"Error during Hugging Face request: {e}")
        print(f"Make sure your token in .env is valid and has the correct permissions")
        return None

def build_prompt(profile):
    """
    Constructs a detailed prompt for AI based on the comprehensive user profile
    """
    # Physical metrics
    height_m = profile.get('height', 170) / 100
    bmi = profile.get('weight', 70) / (height_m ** 2)
    leg_length = profile.get('legLength', 'N/A')
    arm_length = profile.get('armLength', 'N/A')
    waist_size = profile.get('waistSize', 'N/A')
    
    # Health conditions
    health_issues = []
    if profile.get('jointProblems'):
        health_issues.append("joint problems")
    if profile.get('kneeProblems'):
        health_issues.append("knee problems")
    if profile.get('backProblems'):
        health_issues.append("back problems")
    if profile.get('heartProblems'):
        health_issues.append("heart problems")
    if profile.get('healthConditions'):
        health_issues.extend(profile['healthConditions'])
    if profile.get('otherHealthIssues'):
        health_issues.append(profile['otherHealthIssues'])
    if profile.get('injuries'):
        health_issues.append(f"injuries: {profile['injuries']}")
    if profile.get('allergies'):
        health_issues.append(f"allergies: {profile['allergies']}")
    
    health_text = ", ".join(health_issues) if health_issues else "no particular health issues"
    
    # Goals and motivations
    main_goal = profile.get('mainGoal', 'general fitness')
    specific_goals = ", ".join(profile.get('specificGoals', [])) if profile.get('specificGoals') else "none specified"
    motivations = ", ".join(profile.get('motivations', [])) if profile.get('motivations') else "none specified"
    fears = ", ".join(profile.get('fears', [])) if profile.get('fears') else "none"
    
    # Lifestyle and availability
    available_time = profile.get('availableTime', 'flexible')
    preferred_time = profile.get('preferredTime', 'any time')
    available_days = profile.get('availableDays', 'N/A')
    work_type = profile.get('workType', 'not specified')
    sleep_quality = profile.get('sleepQuality', 'normal')
    stress_level = profile.get('stressLevel', 'moderate')
    lifestyle = profile.get('lifestyle', 'not specified')
    
    # Preferences
    exercise_preferences = ", ".join(profile.get('exercisePreferences', [])) if profile.get('exercisePreferences') else "no preferences"
    exercise_aversions = ", ".join(profile.get('exerciseAversions', [])) if profile.get('exerciseAversions') else "none"
    equipment_available = ", ".join(profile.get('equipmentAvailable', [])) if profile.get('equipmentAvailable') else "none"
    music_preference = profile.get('musicPreference', 'any')
    social_preference = profile.get('socialPreference', 'flexible')
    
    # Experience
    practiced_sports = ", ".join(profile.get('practisedSports', [])) if profile.get('practisedSports') else "None"
    favorite_activity = profile.get('favoriteActivity', 'not specified')
    past_experience = profile.get('pastExperienceWithFitness', 'beginner')
    success_factors = ", ".join(profile.get('successFactors', [])) if profile.get('successFactors') else "not specified"
    
    # Challenges and support
    challenges = ", ".join(profile.get('primaryChallenges', [])) if profile.get('primaryChallenges') else "none"
    support_system = profile.get('supportSystem', 'not specified')
    
    prompt = f"""You are an expert in sports recommendation. Analyze this comprehensive profile and recommend ONE sport that perfectly matches this person.

PHYSICAL PROFILE:
Age: {profile.get('age', 'N/A')} years | Gender: {profile.get('gender', 'N/A')}
Height: {profile.get('height', 'N/A')}cm | Weight: {profile.get('weight', 'N/A')}kg | BMI: {bmi:.1f}
Leg Length: {leg_length}cm | Arm Length: {arm_length}cm | Waist: {waist_size}cm

FITNESS & HEALTH:
Fitness Level: {profile.get('fitnessLevel', 'beginner')} | Activity Level: {profile.get('activityLevel', 'sedentary')}
Exercise Frequency: {profile.get('exerciseFrequency', 'never')}
Health Issues: {health_text}

GOALS & MOTIVATION:
Main Goal: {main_goal}
Specific Goals: {specific_goals}
Motivations: {motivations}
Fears/Concerns: {fears}

AVAILABILITY & LIFESTYLE:
Available Time: {available_time} | Preferred Time: {preferred_time} | Days/Week: {available_days}
Work Type: {work_type} | Sleep Quality: {sleep_quality} | Stress Level: {stress_level}
Lifestyle: {lifestyle}

PREFERENCES:
Exercise Preferences: {exercise_preferences}
Exercise Aversions: {exercise_aversions}
Location: {profile.get('locationPreference', 'any')} | Team/Solo: {profile.get('teamPreference', 'flexible')}
Equipment Available: {equipment_available}
Music: {music_preference} | Social: {social_preference}

EXPERIENCE:
Practiced Sports: {practiced_sports}
Favorite Activity: {favorite_activity}
Past Experience: {past_experience}
Success Factors: {success_factors}

CHALLENGES:
Primary Challenges: {challenges}
Support System: {support_system}

CRITICAL INSTRUCTIONS:
1. Analyze ALL the information above to make the BEST recommendation
2. Consider physical metrics, health issues, goals, lifestyle, preferences, and experience
3. IMPORTANT: ALL text content MUST be written in this language: "{profile.get('language', 'en')}"
   - Sport name, reason, explanation, benefits, precautions, exercise names/descriptions
   - For example: if language is "pt" → "Natação" not "Swimming"
   - if "fr" → "Natation", if "es" → "Natación", if "de" → "Schwimmen"
   - Only JSON keys stay in English
4. Preferred Tone: {profile.get('preferredTone', 'encouraging')}
5. Learning Style: {profile.get('learningStyle', 'visual')}

MANDATORY - You MUST provide:
✅ Main sport recommendation with score, reason, explanation
✅ 5 benefits
✅ 4 precautions
✅ 3 exercises with name, description, duration, repetitions, videoUrl
✅ 2-3 ALTERNATIVE SPORTS (alternatives field) - REQUIRED:
   * Each with: sport name, score (0-100), reason, 3-5 benefits, 2-4 precautions
   * Example alternatives: if main sport is Swimming, alternatives could be Aqua Aerobics, Water Polo, Rowing
✅ COMPLETE TRAINING PLAN (trainingPlan field) - REQUIRED:
   * duration: "2 weeks" or "4 weeks"
   * goal: aligned with user objectives
   * weeks: Array of 2-4 weeks, each containing:
     - weekNumber: 1, 2, 3, 4
     - focus: "Building foundation", "Increasing intensity", etc.
     - sessions: 3-4 sessions per week with:
       · day: 1-7 (1=Monday, 7=Sunday)
       · title: "Beginner session", "Endurance training", etc.
       · duration: "30 min", "45 min", etc.
       · exercises: ["Exercise 1", "Exercise 2", "Exercise 3"]
       · notes: optional tips
     - milestone: optional achievement ("Complete first full session")
   * equipment: ["Item 1", "Item 2"]
   * progressionTips: 3-5 tips for improvement

IMPORTANT: The response MUST include both 'alternatives' and 'trainingPlan' fields or it will be rejected and retried.

START your response with {{ and END with }}"""
    
    return prompt

def parse_ai_response(ai_response):
    """
    Parse the AI response and extract the JSON
    Improve robustness by cleaning up poorly formatted responses
    """
    try:
        cleaned = ai_response.strip()
        
        if cleaned.startswith('```json'):
            cleaned = cleaned[7:]
        if cleaned.startswith('```'):
            cleaned = cleaned[3:]
        if cleaned.endswith('```'):
            cleaned = cleaned[:-3]
        
        cleaned = cleaned.strip()
        
        try:
            result = json.loads(cleaned)
            if 'sport' in result and 'score' in result:
                return result
        except json.JSONDecodeError:
            pass
        
        json_match = re.search(r'\{[\s\S]*\}', cleaned)
        if json_match:
            json_str = json_match.group(0)
            try:
                result = json.loads(json_str)
                if 'sport' in result and 'score' in result:
                    return result
            except json.JSONDecodeError as je:
                print(f"Detailed JSON error: {je}")
                print(f"Error position: {je.pos}")
                print(f"Error line: {je.lineno}, Column: {je.colno}")
        
        return None
    except Exception as e:
        print(f"Error parsing AI response: {e}")
        print(f"Full raw response: {ai_response}")
        return None

@app.route('/api/analyze', methods=['POST'])
def analyze_profile():
    """
    Main endpoint that receives the user profile and returns
    a sport recommendation generated by the AI (Hugging Face)
    """
    try:
        profile = request.get_json()
        
        if not profile:
            return jsonify({"error": "No profile provided"}), 400
        
        prompt = build_prompt(profile)
        language = profile.get('language', 'en')
        
        # Instructor guarantees a valid JSON response matching the schema
        recommendation = query_huggingface(prompt, language=language)
        
        if recommendation:
            print("✅ Valid structured response received from Hugging Face")
            return jsonify(recommendation), 200
        else:
            return jsonify({
                "error": "AI request failed",
                "message": "Could not get response from Hugging Face API after retries"
            }), 500
            
    except Exception as e:
        print(f"Error: {e}")
        return jsonify({
            "error": str(e),
            "message": "An unexpected error occurred"
        }), 500

@app.route('/api/health', methods=['GET'])
def health_check():
    """
    Endpoint to check that the API is working
    and that Hugging Face API is accessible
    """
    try:
        if not HF_TOKEN:
            return jsonify({
                "status": "OK",
                "huggingface_status": "No Token",
                "model_used": MODEL_NAME,
                "has_token": False,
                "message": "Add HUGGINGFACE_TOKEN to .env file"
            }), 200
        
        # Test connection with a simple request
        test_response = base_client.chat.completions.create(
            model=MODEL_NAME,
            messages=[{"role": "user", "content": "Hello"}],
            max_tokens=10,
        )
        
        hf_status = "OK" if test_response else "Error"
        
        return jsonify({
            "status": "OK",
            "huggingface_status": hf_status,
            "model_used": MODEL_NAME,
            "has_token": True
        }), 200
    except Exception as e:
        return jsonify({
            "status": "OK",
            "huggingface_status": "Error",
            "error": str(e),
            "message": "The API is working but Hugging Face API is not reachable."
        }), 200

if __name__ == '__main__':
    print(f" AI model used: {MODEL_NAME}")
    port = int(os.getenv('PORT', 5000))
    app.run(debug=False, host='0.0.0.0', port=port)
