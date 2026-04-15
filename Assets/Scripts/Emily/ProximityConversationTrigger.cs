using UnityEngine;

public class ProximityConversationTrigger : MonoBehaviour
{
    private Transform player;

    public Canvas dialogueCanvas;
    public TownspersonInteraction interaction;
    public float triggerDistance = 1f;

    private bool hasTriggered = false;

    void Start()
    {
        Debug.Log("[Proximity] Script started on: " + gameObject.name);

        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
            Debug.Log("[Proximity] Canvas hidden at start");
        }
        else
        {
            Debug.LogError("[Proximity] Canvas is NOT assigned!");
        }
    }

    void Update()
    {
        if (hasTriggered) return;

        // Try to find player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("[Proximity] Player found: " + player.name);
            }
            else
            {
                Debug.Log("[Proximity] Player not found yet...");
            }

            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);

        Debug.Log($"[Proximity] Distance to player: {distance}");

        if (distance <= triggerDistance)
        {
            Debug.Log("[Proximity] Within trigger distance!");

            TriggerConversation();
        }
    }

    void TriggerConversation()
    {
        hasTriggered = true;

        Debug.Log("[Proximity] Triggering conversation!");

        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
            Debug.Log("[Proximity] Canvas ENABLED");
        }

        if (interaction != null)
        {
            Debug.Log("[Proximity] Interaction found");

            if (interaction.HasStateAuthority)
            {
                interaction.CurrentDialogue = "…Who’s there? What do you want?";
                Debug.Log("[Proximity] Dialogue set");
            }
            else
            {
                Debug.LogWarning("[Proximity] No StateAuthority - dialogue not set");
            }
        }
        else
        {
            Debug.LogError("[Proximity] Interaction is NULL!");
        }
    }
}