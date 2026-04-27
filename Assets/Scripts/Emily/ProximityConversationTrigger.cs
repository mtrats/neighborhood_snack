using UnityEngine;

public class ProximityConversationTrigger : MonoBehaviour
{
    private Transform player;

    public Canvas dialogueCanvas;
    public TownspersonInteraction interaction;
    public float triggerDistance = 1f;
    public float exitDistance = 3f;

    private INPCPersonality _personality;
    private bool hasTriggered = false;

    public bool HasWon { get; private set; } = false;

    void Start()
    {
        _personality = GetComponent<INPCPersonality>();

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        // Once won, this NPC is done — no more re-triggering
        if (HasWon) return;

        // Try to find the local player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);

        if (!hasTriggered && distance <= triggerDistance)
        {
            TriggerConversation();
        }
        else if (hasTriggered && distance > exitDistance)
        {
            ExitConversation();
        }
        else if (hasTriggered && interaction != null
                 && interaction.TrustScore >= SimpleGeminiMic.WinThreshold)
        {
            MarkAsWon();
        }
    }

    void TriggerConversation()
    {
        hasTriggered = true;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);

        if (interaction != null)
        {
            Debug.Log("[Proximity] Interaction found");

            // Push this NPC's personality to the shared interaction object
            interaction.SetPersonality(_personality);

            if (interaction.HasStateAuthority)
            {
                string openingLine = _personality != null
                    ? _personality.GetOpeningLine()
                    : "\u2026Who's there? What do you want?";

                interaction.CurrentDialogue = interaction.FormatDialogue(openingLine);
            }
            else
            {
                Debug.LogWarning("[Proximity] No StateAuthority - dialogue not set");
            }
        }
    }

    void ExitConversation()
    {
        hasTriggered = false;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        // Reset trust and history so every fresh approach starts from zero
        if (interaction != null)
            interaction.ResetConversation();

        Debug.Log($"[Proximity] Player left {_personality?.NPCName ?? gameObject.name}'s range — trust reset.");
    }

    void MarkAsWon()
    {
        HasWon = true;
        Debug.Log($"[Proximity] {_personality?.NPCName ?? gameObject.name} has been convinced!");

        // Check whether every NPC in the scene has now been won
        var allTriggers = FindObjectsOfType<ProximityConversationTrigger>();
        foreach (var trigger in allTriggers)
        {
            if (!trigger.HasWon)
            {
                Debug.Log($"[Proximity] Still waiting on: {trigger._personality?.NPCName ?? trigger.gameObject.name}");
                return;
            }
        }

        Debug.Log("[Proximity] ALL NPCs convinced — LEVEL COMPLETE!");
        // TODO: trigger your level-complete flow here (scene transition, UI, etc.)
    }
}