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

    // Thresholds that define NPC personality phases
    public const int WinThreshold = 7;

    void Awake()
    {
        _client = new Client(apiKey: LoadApiKey());
    }

    public async Task<GeminiResponse> ProcessVoiceToAI(string transcript, string fullPrompt, List<(string player, string npc)> history)
    {
        // 1. System prompt as first user turn
        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = fullPrompt } }
            },
            // 2. Model acknowledges the prompt
            new Content
            {
                Role = "model",
                Parts = new List<Part> { new Part { Text = "Understood." } }
            }
        };

        // 3. Replay prior conversation turns
        foreach (var (playerText, npcText) in history)
        {
            contents.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = playerText } }
            });
            contents.Add(new Content
            {
                Role = "model",
                Parts = new List<Part> { new Part { Text = npcText } }
            });
        }

        // 4. Current player message
        contents.Add(new Content
        {
            Role = "user",
            Parts = new List<Part> { new Part { Text = $"The player said: {transcript}" } }
        });

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