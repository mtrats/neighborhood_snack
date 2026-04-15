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
    private const string WorkingModel = "gemini-2.5-flash-lite";

    // Thresholds that define Barnaby's personality phases
    private const int MidTrustThreshold = 3;
    private const int TestPhaseThreshold = 6;
    public const int WinThreshold = 9;

    void Awake()
    {
        _client = new Client(apiKey: LoadApiKey());
    }

    public async Task<GeminiResponse> ProcessVoiceToAI(AudioClip clip, int currentTrustScore)
    {
        byte[] wavData = ConvertToWav(clip);
        string systemPrompt = BuildPrompt(currentTrustScore);

        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part>
                {
                    new Part { Text = systemPrompt },
                    new Part { InlineData = new Blob { MimeType = "audio/wav", Data = wavData } }
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

    private string BuildPrompt(int trustScore)
    {
        string personalityBlock;

        if (trustScore < MidTrustThreshold)
        {
            // Phase 1: Hostile. Short, dismissive, suspicious of strangers.
            personalityBlock =
                @"You are Barnaby, a deeply suspicious and grumpy old townsperson. 
                A stranger is at your door at night. You do NOT trust them at all.
                You are hostile and dismissive. You ask them what they want and why 
                they should be trusted. Short, sharp responses. You are not warming up yet.
                Rude or pushy behaviour makes you angrier. Polite behaviour makes you 
                slightly less hostile but you are still very suspicious.";
        }
        else if (trustScore < TestPhaseThreshold)
        {
            // Phase 2: Softening. You're starting to consider it but won't show it easily.
            personalityBlock =
                @"You are Barnaby, a grumpy but slightly curious old townsperson.
                The stranger at your door has been polite enough to get your attention,
                though you'd never admit it. You are still guarded but you're starting 
                to engage a little more. Ask them questions about themselves or why they 
                need shelter. You're testing whether they're genuine. Rudeness now would 
                really disappoint you and make you pull back. Kindness is slowly working.";
        }
        else
        {
            // Phase 3: Testing. You're almost convinced but you need one good reason.
            personalityBlock =
                @"You are Barnaby, a grumpy old townsperson who has been slowly worn down 
                by this stranger's persistence and manners. You are very close to letting 
                them in but you need to be sure. Give them one final test or question — 
                something personal, something that proves they mean no harm. A genuinely 
                kind or earnest answer should push you over the edge. A rude or dismissive 
                answer would slam the door shut on all that progress. Make this feel like 
                a real moment of decision.";
        }

        return $@"{personalityBlock}

        The stranger is a vampire, though you don't know this yet. You just see someone 
        who needs to be invited in.

        Respond to the stranger's audio message. Score their approach:
        KIND and genuine = 1, NEUTRAL or evasive = 0, RUDE or pushy = -1.

        Scoring notes:
        - Flattery without substance should only score 0, not 1.
        - Repetitive polite phrases that feel hollow score 0 after the first time.
        - A truly heartfelt or creative appeal scores 1.
        - Aggression or impatience always scores -1 regardless of phase.

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

    private byte[] ConvertToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (var stream = new System.IO.MemoryStream())
        {
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((ushort)(clip.channels * 2));
                writer.Write((ushort)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                    writer.Write((short)(sample * short.MaxValue));
            }
            return stream.ToArray();
        }
    }
}