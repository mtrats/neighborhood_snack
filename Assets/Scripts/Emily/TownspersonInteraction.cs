using UnityEngine;
using Fusion;
using TMPro;
using System.Threading.Tasks;

public class TownspersonInteraction : NetworkBehaviour
{
    [Header("References")]
    public VoiceRecorder voiceRecorder;
    public GeminiService geminiService;
    public TextMeshProUGUI dialogueText;
    public Animator houseDoorAnimator;

    // This boolean syncs across the network so only one person can talk
    [Networked] public NetworkBool IsMicBusy { get; set; }
    
    // This string syncs the AI's response so both players see the dialogue
    [Networked] public NetworkString<_128> CurrentDialogue { get; set; }

    public override void Spawned()
    {
        // Subscribe to the recorder's event
        if (voiceRecorder != null)
        {
            voiceRecorder.OnRecordingComplete += HandleRecordingComplete;
        }
    }

    // Called by your UI Button
    public void OnClickRecord()
    {
        // Only start if nobody else is talking
        if (!IsMicBusy)
        {
            // Requesting authority to change the networked boolean
            Object.RequestStateAuthority();
            IsMicBusy = true;
            
            dialogueText.text = "Recording...";
            voiceRecorder.StartRecording(5); // Record for 5 seconds
        }
    }

    private async void HandleRecordingComplete(AudioClip clip)
    {
        dialogueText.text = "Townsperson is thinking...";

        // Send to Gemini
        string aiResult = await geminiService.ProcessVoiceToAI(clip);

        // Update the networked string so both players see it
        // We use an RPC or authority check to update networked data
        if (Object.HasStateAuthority)
        {
            CurrentDialogue = aiResult;
            
            // Logic to unlock door based on AI response keywords
            if (aiResult.ToLower().Contains("enter") || aiResult.ToLower().Contains("welcome"))
            {
                TriggerDoorOpen();
            }

            // Release the mic lock
            IsMicBusy = false;
        }
    }

    // This runs on every client when CurrentDialogue changes
    public override void Render()
    {
        dialogueText.text = CurrentDialogue.ToString();
    }

    private void TriggerDoorOpen()
    {
        if (houseDoorAnimator != null)
        {
            houseDoorAnimator.SetTrigger("Open");
        }
    }

    private void OnDestroy()
    {
        if (voiceRecorder != null)
        {
            voiceRecorder.OnRecordingComplete -= HandleRecordingComplete;
        }
    }
}