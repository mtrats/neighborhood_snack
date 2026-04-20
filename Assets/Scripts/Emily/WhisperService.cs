using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperService : MonoBehaviour
{
    private const string WhisperEndpoint = "https://api.openai.com/v1/audio/transcriptions";
    private string _apiKey;

    void Awake()
    {
        _apiKey = LoadApiKey();
    }

    public async Task<WhisperResult> Transcribe(AudioClip clip)
    {
        byte[] wavData = ConvertToWav(clip);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("response_format", "text");

        using (UnityWebRequest request = UnityWebRequest.Post(WhisperEndpoint, form))
        {
            request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");

            var operation = request.SendWebRequest();

            // Await the web request without blocking the main thread
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Whisper error: {request.responseCode} - {request.downloadHandler.text}");
                return new WhisperResult
                {
                    Success = false,
                    ErrorMessage = ParseErrorMessage(request.responseCode, request.downloadHandler.text)
                };
            }

            string transcript = request.downloadHandler.text.Trim();

            if (string.IsNullOrEmpty(transcript))
            {
                return new WhisperResult
                {
                    Success = false,
                    ErrorMessage = "Nothing was heard. Please try again."
                };
            }

            Debug.Log($"Whisper transcript: {transcript}");

            return new WhisperResult
            {
                Success = true,
                Transcript = transcript
            };
        }
    }

    private string ParseErrorMessage(long responseCode, string responseBody)
    {
        switch (responseCode)
        {
            case 401:
                return "Invalid OpenAI API key. Check your .env file.";
            case 429:
                return "Too many requests. Please wait a moment.";
            case 413:
                return "Recording was too large. Try a shorter recording.";
            default:
                return $"Transcription failed ({responseCode}). Please try again.";
        }
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
            if (parts.Length == 2 && parts[0].Trim() == "OPENAI_API_KEY")
                return parts[1].Trim();
        }

        throw new Exception("OPENAI_API_KEY not found in .env file");
    }

    private byte[] ConvertToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (var stream = new System.IO.MemoryStream())
        {
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((ushort)(clip.channels * 2));
                writer.Write((ushort)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                    writer.Write((short)(sample * short.MaxValue));
            }
            return stream.ToArray();
        }
    }
}

public class WhisperResult
{
    public bool Success;
    public string Transcript;
    public string ErrorMessage;
}