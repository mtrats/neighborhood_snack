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
    public Canvas dialogueCanvas;
    public AudioClip talkClip;
    //public AudioClip winClip;
    public AudioSource speaker;

    private INPCPersonality _personality;
    private string _activePersonalityName;
    private List<(string player, string npc)> _conversationHistory = new List<(string player, string npc)>();
    private bool IsTalking;

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
        _conversationHistory.Clear();

        if (voiceRecorder != null)
            voiceRecorder.OnRecordingComplete += HandleRecordingComplete;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(IsDialogueOpen);

        if (dialogueText != null)
            dialogueText.text = CurrentDialogue.ToString();

        if (trustScoreText != null)
            trustScoreText.text = $"Trust: {TrustScore} / {SimpleGeminiMic.WinThreshold}";

        OnDialogueOpenChanged();
        OnDialogueChanged();
        OnTrustScoreChanged();
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

private int CheckKeywords(string transcript)
{
    string lower = transcript.ToLower();
    int bonusScore = 0;

    Dictionary<string, int> keywords = _personality != null
        ? _personality.GetKeywords()
        : new Dictionary<string, int>();

    foreach (var keyword in keywords)
    {
        if (lower.Contains(keyword.Key))
        {
            Debug.Log($"[Keywords] Matched keyword: '{keyword.Key}' (+{keyword.Value})");
            bonusScore += keyword.Value;
        }
    }

    return bonusScore;
}

public void SetPersonality(INPCPersonality personality)
{
    string nextPersonalityName = personality?.NPCName;

    if (HasStateAuthority && !string.IsNullOrEmpty(_activePersonalityName) && _activePersonalityName != nextPersonalityName)
        ResetConversation();

    _personality = personality;
    _activePersonalityName = nextPersonalityName;
    Debug.Log($"[TownspersonInteraction] Personality set to: {_personality?.NPCName ?? "null"}");
}

    public string FormatDialogue(string dialogue)
    {
        if (string.IsNullOrWhiteSpace(dialogue))
            return dialogue;

        string npcName = _personality != null ? _personality.NPCName : "NPC";
        return $"{npcName}: {dialogue}";
    }

public void ResetConversation()
{
    if (!HasStateAuthority) return;

    TrustScore = 0;
    _conversationHistory.Clear();
    CurrentDialogue = string.Empty;
    IsDialogueOpen = false;
    IsMicBusy = false;
    _activePersonalityName = null;
    Debug.Log("[TownspersonInteraction] Conversation reset.");
}

private async void HandleRecordingComplete(AudioClip clip)
{
    if (!HasStateAuthority) return;

    if (_personality == null)
    {
        Debug.LogError("[TownspersonInteraction] No INPCPersonality found on " + gameObject.name);
        IsMicBusy = false;
        return;
    }

    string listeningNpcName = _personality.NPCName;
    CurrentDialogue = $"{listeningNpcName} is listening...";

    WhisperResult transcription = await whisperService.Transcribe(clip);

    if (!transcription.Success)
    {
        CurrentDialogue = transcription.ErrorMessage;
        IsMicBusy = false;
        return;
    }

    string npcName = _personality.NPCName;
    CurrentDialogue = $"{npcName} is thinking...";

    string fullPrompt = _personality.BuildPrompt(TrustScore, LastSpeakerName.ToString());

    GeminiResponse result = await geminiService.ProcessVoiceToAI(
        transcription.Transcript,
        fullPrompt,
        _conversationHistory
    );

    _conversationHistory.Add((transcription.Transcript, result.dialogue));

    int keywordBonus = CheckKeywords(transcription.Transcript);
    TrustScore = Mathf.Max(0, TrustScore + result.score + keywordBonus);
    IsTalking = true;
    CurrentDialogue = FormatDialogue(result.dialogue);
    //IsTalking = false;

    if (TrustScore >= SimpleGeminiMic.WinThreshold)
        RPC_ShowWinResponse(GetWinLine());

    IsMicBusy = false;
}
    private void OnDialogueChanged()
    {
        if (dialogueText != null) {
            dialogueText.text = CurrentDialogue.ToString();
            if (IsDialogueOpen && IsTalking)
            {
                speaker.PlayOneShot(talkClip);
                IsTalking = false;
            }
    }
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowWinResponse(string winLine)
    {
        CurrentDialogue = FormatDialogue(winLine);
        IsDialogueOpen = true;
        IsMicBusy = false;
        StartCoroutine(HideDialogueAfterWin());
    }

    private string GetWinLine()
    {
        return _personality != null
            ? _personality.GetWinLine()
            : "All right. Come in.";
    }

    private System.Collections.IEnumerator HideDialogueAfterWin()
    {
        yield return new WaitForSeconds(1.5f);

        if (!HasStateAuthority)
            yield break;

        if (HasUnwonNPCs())
        {
            IsDialogueOpen = false;
        }
    }

    private bool HasUnwonNPCs()
    {
        var triggers = FindObjectsOfType<ProximityConversationTrigger>();

        foreach (var trigger in triggers)
        {
            if (!trigger.HasWon)
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (voiceRecorder != null)
            voiceRecorder.OnRecordingComplete -= HandleRecordingComplete;
    }
}