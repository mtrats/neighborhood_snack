using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Google.GenAI;
using Google.GenAI.Types;

[Serializable]
public class GeminiResponse
{
    public string dialogue;
    public int score;
}

public class SimpleGeminiMic : MonoBehaviour
{
    private Client _client;
    private const string WorkingModel = "gemini-2.5-flash";

    // Thresholds that define Barnaby's personality phases
    private const int MidTrustThreshold = 3;
    private const int TestPhaseThreshold = 6;
    public const int WinThreshold = 9;

    void Awake()
    {
        _client = new Client(apiKey: LoadApiKey());
    }

    public async Task<GeminiResponse> ProcessVoiceToAI(string transcript, int currentTrustScore, string speakerName)
    {
        string systemPrompt = BuildPrompt(currentTrustScore, speakerName);

        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part>
                {
                    new Part { Text = systemPrompt },
                    new Part { Text = $"The player said: {transcript}" } // replaces InlineData
                }
            }
        };

        var response = await _client.Models.GenerateContentAsync(
            model: WorkingModel,
            contents: contents,
            config: new GenerateContentConfig { ResponseMimeType = "application/json" }
        );

        if (response?.Candidates != null && response.Candidates.Count > 0)
        {
            string rawJson = response.Candidates[0].Content.Parts[0].Text;
            return JsonUtility.FromJson<GeminiResponse>(rawJson);
        }

        return new GeminiResponse { dialogue = "...", score = 0 };
    }

    private string BuildPrompt(int trustScore, string speakerName)
    {
        string personalityBlock;

        if (trustScore < MidTrustThreshold)
        {
            personalityBlock =
                @"You are Barnaby, a deeply suspicious and grumpy old townsperson. 
                A stranger is at your door at night. You do NOT trust them at all.
                You are hostile and dismissive. Short, sharp responses.";
        }
        else if (trustScore < TestPhaseThreshold)
        {
            personalityBlock =
                @"You are Barnaby, a grumpy but slightly curious old townsperson.
                The stranger has been polite enough to earn a little of your attention —
                you'd never admit it, but something about them is getting through.
                Your tone should feel NOTICEABLY warmer than before. You are still 
                guarded but you are engaging more. Ask them real questions.";
        }
        else
        {
            personalityBlock =
                @"You are Barnaby, a grumpy old townsperson who has been genuinely 
                worn down by this stranger. You are almost ready to let them in.
                Your tone should feel CLEARLY different from when you started — 
                tired, almost reluctant kindness. Give them one final question 
                or test before you make your decision.";
        }
        
        string phaseLabel = trustScore < MidTrustThreshold ? "PHASE 1 (hostile)" :
                        trustScore < TestPhaseThreshold ? "PHASE 2 (softening)" :
                        "PHASE 3 (almost convinced)";

    return $@"{personalityBlock}

    CURRENT PHASE: {phaseLabel} — your dialogue MUST reflect this phase clearly.
    Trust so far: {trustScore}/{WinThreshold}.
    The stranger's name is: {speakerName}. Address them by name occasionally.
    There may be multiple strangers taking turns speaking to you.
    The stranger is a vampire, though you don't know this yet.

    IMPORTANT: Keep your dialogue under 70 words. Be concise.

    Score their approach: KIND and genuine = 1, NEUTRAL or evasive = 0, RUDE or pushy = -1.

    Return ONLY a JSON object:
    {{
        ""dialogue"": ""Barnaby's response here"",
        ""score"": 0
    }}";
    }

    private string LoadApiKey()
    {
        string envPath = System.IO.Path.Combine(Application.dataPath, "..", ".env");

        if (!System.IO.File.Exists(envPath))
            throw new Exception(".env file not found at: " + envPath);

        foreach (var line in System.IO.File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var parts = line.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == "GEMINI_API_KEY")
                return parts[1].Trim();
        }

        throw new Exception("GEMINI_API_KEY not found in .env file");
    }
}