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

# Load environment variables from .env file
load_dotenv()

# Disable SSL verification warnings
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

app = Flask(__name__)
CORS(app)  

# Hugging Face configuration using OpenAI-compatible API
MODEL_NAME = "meta-llama/Llama-3.2-3B-Instruct"

# Load Hugging Face token from .env file
HF_TOKEN = os.getenv("HUGGINGFACE_TOKEN", None)

# Create custom httpx client with SSL verification disabled
http_client = httpx.Client(verify=False)

# Initialize OpenAI client with Hugging Face router and custom http client
client = OpenAI(
    base_url="https://router.huggingface.co/v1",
    api_key=HF_TOKEN,
    http_client=http_client,
    timeout=120.0,
)

def query_huggingface(prompt, model=MODEL_NAME):
    """
    Send a request to Hugging Face using OpenAI-compatible API
    """
    try:
        if not HF_TOKEN:
            print("Error: No Hugging Face token found in .env file")
            return None
        
        # Use chat completion endpoint
        response = client.chat.completions.create(
            model=model,
            messages=[
                {"role": "system", "content": "You are a helpful assistant that provides JSON responses only."},
                {"role": "user", "content": prompt}
            ],
            max_tokens=4096,
            temperature=0.3,
            top_p=0.9,
        )
        
        if response.choices and len(response.choices) > 0:
            return response.choices[0].message.content
        
        return None
            
    except Exception as e:
        print(f"Error during Hugging Face request: {e}")
        print(f"Make sure your token in .env is valid and has the correct permissions")
        return None

def build_prompt(profile):
    """
    Constructs a detailed prompt for AI based on the user profile
    """
    height_m = profile.get('height', 170) / 100
    bmi = profile.get('weight', 70) / (height_m ** 2)
    
    health_issues = []
    if profile.get('jointProblems'):
        health_issues.append("joint problems")
    if profile.get('kneeProblems'):
        health_issues.append("knee problems")
    if profile.get('backProblems'):
        health_issues.append("back problems")
    if profile.get('heartProblems'):
        health_issues.append("heart problems")
    if profile.get('otherHealthIssues'):
        health_issues.append(profile['otherHealthIssues'])
    
    health_text = ", ".join(health_issues) if health_issues else "no particular health issues"
    
    prompt = f"""You are an expert in sports recommendation. Analyze this profile and recommend ONE sport.

PROFILE:
Age: {profile['age']} years | Gender: {profile['gender']} | Height: {profile['height']}cm | Weight: {profile['weight']}kg | BMI: {bmi:.1f}
Activity Level: {profile['activityLevel']} | Exercise Frequency: {profile['exerciseFrequency']} | Practiced Sports: {profile.get('practisedSports', 'None')}
Health: {health_text}
Goal: {profile['mainGoal']} | Location: {profile['locationPreference']} | Type: {profile['teamPreference']}

CRITICAL INSTRUCTIONS:
1. Respond ONLY with a valid JSON object
2. Do NOT add ANY text before or after the JSON
3. Use double quotes for ALL strings
4. Do NOT include an imageUrl field in your response (it will be added automatically)
5. Ensure the JSON is COMPLETE up to the last brace
6. IMPORTANT: ALL text content MUST be written in this language: "{profile["language"]}" - This means the sport name, reason, explanation, ALL benefits, ALL precautions, and ALL exercise names and descriptions MUST be in {profile["language"]}. For example, if language is "pt" write "Natação" not "Swimming", if "fr" write "Natation" not "Swimming", if "es" write "Natación" not "Swimming". Only the JSON keys stay in English.

FORMAT EXACT (copy this structure):
{{
  "sport": "Sport name",
  "score": 88,
  "reason": "Short and precise reason",
  "explanation": "2-3 sentences explaining why this sport suits this profile.",
  "benefits": ["Benefit 1", "Benefit 2", "Benefit 3", "Benefit 4", "Benefit 5"],
  "precautions": ["Precaution 1", "Precaution 2", "Precaution 3", "Precaution 4"],
  "exercises": [
    {{"name": "Exercise 1", "description": "Complete description", "duration": "15-20 min", "repetitions": "3 sets of 10", "videoUrl": "https://www.youtube.com/watch?v=example1"}},
    {{"name": "Exercise 2", "description": "Complete description", "duration": "10-15 min", "repetitions": "4 sets of 12", "videoUrl": "https://www.youtube.com/watch?v=example2"}},
    {{"name": "Exercise 3", "description": "Complete description", "duration": "20-30 min", "repetitions": "2 sets of 15", "videoUrl": "https://www.youtube.com/watch?v=example3"}}
  ]
}}

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
        
        
        ai_response = query_huggingface(prompt)
        
        if ai_response:
            print("Response received from Hugging Face")
            print(f"Raw response (first 200 chars): {ai_response[:200]}")
            
            recommendation = parse_ai_response(ai_response)
            
            if recommendation:               
                return jsonify(recommendation), 200
            else:
                return jsonify({
                    "error": "Failed to parse AI response",
                    "message": "The AI response could not be parsed as valid JSON"
                }), 500
        else:
            return jsonify({
                "error": "AI request failed",
                "message": "Could not get response from Hugging Face API"
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
        test_response = client.chat.completions.create(
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
