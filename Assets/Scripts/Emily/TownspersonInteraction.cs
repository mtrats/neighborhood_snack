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
    public Canvas dialogueCanvas; 
    

    [Networked] public NetworkBool IsMicBusy { get; set; }
    [Networked] public NetworkString<_64> LastSpeakerName { get; set; }

    [Networked, OnChangedRender(nameof(OnDialogueChanged))]
    public NetworkString<_512> CurrentDialogue { get; set; }

    [Networked, OnChangedRender(nameof(OnDialogueOpenChanged))]
    public NetworkBool IsDialogueOpen { get; set; }

    [Networked, OnChangedRender(nameof(OnTrustScoreChanged))]
public int TrustScore { get; set; }
    public override void Spawned()
    {
        if (voiceRecorder != null)
            voiceRecorder.OnRecordingComplete += HandleRecordingComplete;
    }

// Replace OnClickRecord with this:
public void OnClickRecord()
{
    Debug.Log("Button Clicked!");

    if (IsMicBusy) return;

    // Store who is speaking before requesting authority
    string localPlayerName = Runner.LocalPlayer.ToString(); 

    Object.RequestStateAuthority();
    StartCoroutine(WaitForAuthorityThenRecord(localPlayerName));
}

private System.Collections.IEnumerator WaitForAuthorityThenRecord(string speakerName)
{
    // Wait until authority is confirmed
    float timeout = 2f;
    while (!HasStateAuthority && timeout > 0f)
    {
        timeout -= Time.deltaTime;
        yield return null;
    }

    if (!HasStateAuthority)
    {
        Debug.LogWarning("Could not get State Authority in time.");
        yield break;
    }

    IsMicBusy = true;
    IsDialogueOpen = true;
    LastSpeakerName = speakerName;
    CurrentDialogue = "Recording...";
    voiceRecorder.StartRecording(5);
}

// Add this near your other variables at the top of the class
private readonly Dictionary<string, int> stageKeywords = new Dictionary<string, int>
{
    // Phase 1 -> 2 Keywords (Breaking the ice)
    { "friend", 2 },
    { "trade", 2 },
    { "buy", 2 },
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

    WhisperResult transcription = await whisperService.Transcribe(clip);

    if (!transcription.Success)
    {
        CurrentDialogue = transcription.ErrorMessage;
        IsMicBusy = false;
        return;
    }

    CurrentDialogue = "Barnaby is thinking...";

    int keywordBonus = CheckKeywords(transcription.Transcript);
    TrustScore = Mathf.Max(0, TrustScore + keywordBonus);

    // Pass speaker name to Gemini
    GeminiResponse result = await geminiService.ProcessVoiceToAI(
        transcription.Transcript,
        TrustScore,
        LastSpeakerName.ToString()  // ← new parameter
    );

    TrustScore = Mathf.Max(0, TrustScore + result.score);
    CurrentDialogue = result.dialogue;

    if (TrustScore >= SimpleGeminiMic.WinThreshold)
        RPC_OpenDoor();

    IsMicBusy = false;
}
    private void OnDialogueChanged()
{
    if (dialogueText != null)
        dialogueText.text = CurrentDialogue.ToString();
}

private void OnDialogueOpenChanged()
{
    if (dialogueCanvas != null)
        dialogueCanvas.gameObject.SetActive(IsDialogueOpen);
}

private void OnTrustScoreChanged()
{
    if (trustScoreText != null)
        trustScoreText.text = $"Trust: {TrustScore} / {SimpleGeminiMic.WinThreshold}";
}

// You can now delete the Render() override entirely

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