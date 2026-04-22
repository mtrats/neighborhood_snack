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
    public WhisperService whisperService;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI trustScoreText; 
    public Animator houseDoorAnimator;
    

    [Networked] public NetworkBool IsMicBusy { get; set; }
    [Networked] public NetworkString<_512> CurrentDialogue { get; set; }
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

private int CheckKeywords(string transcript)
{
    string lower = transcript.ToLower();
    int bonusScore = 0;

    foreach (var keyword in stageKeywords)
    {
        if (lower.Contains(keyword.Key))
        {
            Debug.Log($"[Keywords] Matched keyword: '{keyword.Key}' (+{keyword.Value})");
            bonusScore += keyword.Value;
        }
    }

    return bonusScore;
}

private async void HandleRecordingComplete(AudioClip clip)
{
    if (!HasStateAuthority) return;

    CurrentDialogue = "Barnaby is listening...";

    // Step 1: Transcribe
    WhisperResult transcription = await whisperService.Transcribe(clip);

    if (!transcription.Success)
    {
        CurrentDialogue = transcription.ErrorMessage;
        IsMicBusy = false;
        return;
    }

    CurrentDialogue = "Barnaby is thinking...";

    // Step 2: Check transcript for keywords
    int keywordBonus = CheckKeywords(transcription.Transcript);

    // Step 3: Send transcript to Gemini
    GeminiResponse result = await geminiService.ProcessVoiceToAI(
        transcription.Transcript,
        TrustScore
    );

    // Step 4: Apply both scores
    int totalScore = result.score + keywordBonus;
    Debug.Log($"[TrustScore] Gemini score: {result.score}, Keyword bonus: {keywordBonus}, Total: {totalScore}");

    TrustScore = Mathf.Max(0, TrustScore + totalScore);
    CurrentDialogue = result.dialogue;

    if (TrustScore >= SimpleGeminiMic.WinThreshold)
        RPC_OpenDoor();

    IsMicBusy = false;
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