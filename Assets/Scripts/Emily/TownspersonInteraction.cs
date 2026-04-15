using UnityEngine;
using Fusion;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
public class TownspersonInteraction : NetworkBehaviour
{
    [Header("References")]
    public VoiceRecorder voiceRecorder;
    public SimpleGeminiMic geminiService;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI trustScoreText; 
    public Animator houseDoorAnimator;

    [Networked] public NetworkBool IsMicBusy { get; set; }
    [Networked] public NetworkString<_128> CurrentDialogue { get; set; }
    [Networked] public int TrustScore { get; set; }

    public override void Spawned()
    {
        if (voiceRecorder != null)
            voiceRecorder.OnRecordingComplete += HandleRecordingComplete;
    }

public void OnClickRecord()
{
    Debug.Log("Button Clicked!");

    // 1. Check if mic is already in use
    if (IsMicBusy) 
    {
        Debug.LogWarning("Click Failed: Mic is already busy.");
        return;
    }

    // 2. Request State Authority so we can change 'CurrentDialogue' and 'IsMicBusy'
    // This allows the client to "take control" of the NPC long enough to talk to it.
    Object.RequestStateAuthority();
    
    // 3. Set the Networked properties
    // Note: These might take a tiny fraction of a second to sync
    IsMicBusy = true;
    CurrentDialogue = "Recording..."; 
    
    // 4. Start the local recording logic
    Debug.Log("Starting voice recorder...");
    voiceRecorder.StartRecording(5);
}

// Add this near your other variables at the top of the class
private readonly Dictionary<string, int> stageKeywords = new Dictionary<string, int>
{
    // Phase 1 -> 2 Keywords (Breaking the ice)
    { "friend", 2 },
    { "trade", 2 },
    { "peace", 2 },
    { "lost", 2 },
    { "doctor", 2 },

    // Phase 2 -> 3 Keywords (Building a connection)
    { "family", 3 },
    { "danger", 3 },
    { "news", 3 },

    // Phase 3 -> Win Keywords (The final push)
    { "honest", 4 },
    { "promise", 4 },
    { "help", 4 }
};

private async void HandleRecordingComplete(AudioClip clip)
{
    if (!HasStateAuthority) return;

    CurrentDialogue = "Barnaby is thinking...";

    try 
    {
        // 1. Get the AI response
        GeminiResponse result = await geminiService.ProcessVoiceToAI(clip, TrustScore);
        
        // 2. Calculate Keyword Bonus
        int bonus = 0;
        string lowerDialogue = result.dialogue.ToLower();

        foreach (var entry in stageKeywords)
        {
            if (lowerDialogue.Contains(entry.Key))
            {
                Debug.Log($"<color=green>Keyword Bonus Triggered: {entry.Key} (+{entry.Value})</color>");
                bonus += entry.Value;
                // We break after one match per turn to prevent score exploitation
                break; 
            }
        }

        // 3. Apply the combined score (AI Sentiment + Manual Bonus)
        // This ensures even a '0' from Gemini can progress if keywords are used
        TrustScore = Mathf.Max(0, TrustScore + result.score + bonus);
        CurrentDialogue = result.dialogue;

        // 4. Check for Win State (WinThreshold is 9 per your script)
        if (TrustScore >= SimpleGeminiMic.WinThreshold)
        {
            Debug.Log("Barnaby is convinced! Opening door.");
            RPC_OpenDoor();
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Gemini API Error: {e.Message}");
        CurrentDialogue = "I'm a bit busy right now, partner. Try again in a minute.";
    }
    finally 
    {
        IsMicBusy = false;
    }
}
    public override void Render()
    {
        // Only update UI if the string has actually changed to save performance
        string currentText = CurrentDialogue.ToString();
        if (dialogueText != null && dialogueText.text != currentText)
        {
            dialogueText.text = currentText;
        }

        if (trustScoreText != null)
            trustScoreText.text = $"Trust: {TrustScore} / {SimpleGeminiMic.WinThreshold}";
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OpenDoor()
    {
        if (houseDoorAnimator != null)
            houseDoorAnimator.SetTrigger("Open");

        dialogueText.text = "...Fine. Come in then. Wipe your boots.";
    }

    private void OnDestroy()
    {
        if (voiceRecorder != null)
            voiceRecorder.OnRecordingComplete -= HandleRecordingComplete;
    }
}