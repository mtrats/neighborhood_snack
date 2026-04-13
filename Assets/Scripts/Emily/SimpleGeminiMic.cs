using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Google.GenAI;
using Google.GenAI.Types;

// Standard JSON utility for parsing
[Serializable]
public class GeminiResponse
{
    public string dialogue;
    public int score; // -1 for negative/rude, 0 for neutral, 1 for positive/kind
}

public class SimpleGeminiMic : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string apiKey = "AIzaSyCRam26QX4U8X4hBNf63N_GT0hRk0wJ3ts";
    [SerializeField] private int recordingLength = 5;
    
    [Header("UI References")]
    public TextMeshProUGUI statusText;

    [Header("Game Logic")]
    public int currentTrustScore = 0;
    public Animator doorAnimator;

    private Client _client;
    private string _micDevice;
    private AudioClip _recordingClip;
    private bool _isProcessing = false;

    private const string WorkingModel = "gemini-2.5-flash";

    void Awake()
    {
        _client = new Client(apiKey: apiKey);

        if (Microphone.devices.Length > 0)
        {
            _micDevice = Microphone.devices[0];
            statusText.text = "Mic Ready. Click to Record.";
        }
        else
        {
            statusText.text = "No Microphone Found!";
        }
    }

    public void ToggleRecording()
    {
        if (_isProcessing) return;

        if (!Microphone.IsRecording(_micDevice))
        {
            StartRecording();
        }
        else
        {
            StopAndSend();
        }
    }

    private void StartRecording()
    {
        _recordingClip = Microphone.Start(_micDevice, false, recordingLength, 16000);
        statusText.text = "Recording... (Speak now)";
        Invoke(nameof(StopAndSend), (float)recordingLength);
    }

    private async void StopAndSend()
    {
        if (!Microphone.IsRecording(_micDevice)) return;

        CancelInvoke(nameof(StopAndSend));
        Microphone.End(_micDevice);
        
        _isProcessing = true;
        statusText.text = "Townsperson is listening...";

        try
        {
            string rawJson = await SendToGemini(_recordingClip);
            
            // Parse the JSON result
            GeminiResponse result = JsonUtility.FromJson<GeminiResponse>(rawJson);
            
            // Update UI with ONLY the dialogue
            statusText.text = result.dialogue;

            // Apply game logic based on the hidden score
            HandleGameLogic(result.score);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("429") || e.Message.Contains("quota"))
                statusText.text = "API Busy. Wait 60s.";
            else
                statusText.text = "Error understanding audio.";
            
            Debug.LogError(e);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void HandleGameLogic(int score)
    {
        currentTrustScore += score;
        Debug.Log($"Current Trust Score: {currentTrustScore}");

        // If they are nice enough (Score reaches 2), open the door
        if (currentTrustScore >= 2 && doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
            Debug.Log("The townsperson let you in!");
        }
    }

    private async Task<string> SendToGemini(AudioClip clip)
    {
        byte[] wavData = ConvertToWav(clip);

        // Define the personality AND the output format.
        string systemPrompt = @"You are a grumpy townsperson named Barnaby. 
        Respond to the user's audio input. 
        Determine if they are being KIND (1), NEUTRAL (0), or RUDE (-1).
        
        Return your response ONLY as a JSON object with these fields:
        {
            ""dialogue"": ""your grumpy response text here"",
            ""score"": 1
        }
        
        Examples:
        Input: 'Hello sir, could I please come in?' -> Output: {""dialogue"": ""Fine, fine. Wipe your boots first."", ""score"": 1}
        Input: 'Move out of the way old man!' -> Output: {""dialogue"": ""Watch your tone, brat! Get lost!"", ""score"": -1}";

        var contents = new List<Content>
        {
            new Content
            {
                Role = "user",
                Parts = new List<Part>
                {
                    new Part { Text = systemPrompt },
                    // FIXED: Passing raw byte array instead of Base64 string to resolve conversion error
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
            return response.Candidates[0].Content.Parts[0].Text;
        }

        return "{}";
    }

    private byte[] ConvertToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
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
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return stream.ToArray();
        }
    }
}