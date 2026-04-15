using UnityEngine;
using Fusion;
using TMPro;
using System.Threading.Tasks;

public class TownspersonInteraction : NetworkBehaviour
{
    [Header("References")]
    public VoiceRecorder voiceRecorder;
    public SimpleGeminiMic geminiService;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI trustScoreText; // optional, useful for debugging
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
        if (!HasInputAuthority || IsMicBusy) return;

        Object.RequestStateAuthority();
        IsMicBusy = true;
        dialogueText.text = "Recording...";
        voiceRecorder.StartRecording(5);
    }

    private async void HandleRecordingComplete(AudioClip clip)
    {
        if (!HasStateAuthority) return;

        CurrentDialogue = "Barnaby is thinking...";

        GeminiResponse result = await geminiService.ProcessVoiceToAI(clip, TrustScore);

        // Update trust score, clamped so it can't go below 0
        TrustScore = Mathf.Max(0, TrustScore + result.score);

        CurrentDialogue = result.dialogue;

        if (TrustScore >= SimpleGeminiMic.WinThreshold)
            RPC_OpenDoor();

        IsMicBusy = false;
    }

    public override void Render()
    {
        dialogueText.text = CurrentDialogue.ToString();

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