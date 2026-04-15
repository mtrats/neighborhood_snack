using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Google.GenAI;
using Google.GenAI.Types;
// using dotenv.net;

public class GeminiService : MonoBehaviour
{
    private string apiKey;
    private Client _client;

    void Awake()
    {
        DotNetEnv.Env.Load();
        DotNetEnv.Env.TraversePath().Load();
        
        // Load API key from environment variable
        apiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("GEMINI_API_KEY not found in environment variables. Make sure .env file is loaded.");
            return;
        }

        _client = new Client(apiKey: apiKey);
    }

    /// <summary>
    /// Takes a Unity AudioClip, converts it to WAV, and sends it to Gemini 2.0 Flash.
    /// </summary>
    public async Task<string> ProcessVoiceToAI(AudioClip clip)
    {
        if (clip == null) return "No audio detected...";

        // 2. Data Conversion: Convert clip to bytes then to Base64
        byte[] wavData = ConvertAudioClipToWav(clip);

        // 3. Prepare the Content parts
        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part>
                {
                    new Part { Text = "Listen to this audio. Transcribe it exactly, then respond as a grumpy townsperson. Format: [Transcript] | [Reply]" },
                    new Part { InlineData = new Blob { MimeType = "audio/wav", Data = wavData } }
                }
            }
        };

        try
        {
            // 4. API Call using the stable direct-parameter method signature.
            // This avoids issues where 'GenerateContentRequest' might be ambiguous or internal in some SDK builds.
            var response = await _client.Models.GenerateContentAsync(
                model: "gemini-2.0-flash", 
                contents: contents
            );
            
            // 5. Robust safety checks to prevent NullReferenceExceptions
            if (response?.Candidates != null && response.Candidates.Count > 0 && 
                response.Candidates[0].Content?.Parts != null && response.Candidates[0].Content.Parts.Count > 0)
            {
                return response.Candidates[0].Content.Parts[0].Text;
            }

            return "The townsperson is staring at you blankly...";
        }
        catch (Exception e)
        {
            Debug.LogError($"Gemini API Error: {e.Message}");
            return "The townsperson is ignoring you...";
        }
    }

    /// <summary>
    /// Helper to add a WAV header to Unity's raw PCM data.
    /// Gemini requires a valid audio container (WAV/MP3/AAC) to process sound.
    /// </summary>
    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                // WAV Header logic
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2); // File size
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Sub-chunk size
                writer.Write((ushort)1); // Audio format (PCM)
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2); // Byte rate
                writer.Write((ushort)(clip.channels * 2)); // Block align
                writer.Write((ushort)16); // Bits per sample
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2); // Data size

                // Convert float samples (-1.0 to 1.0) to 16-bit PCM shorts
                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return stream.ToArray();
        }
    }
}