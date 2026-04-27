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
        
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
            
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
            }
            

            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= triggerDistance)
        {
            TriggerConversation();
        }
    }

    void TriggerConversation()
    {
        hasTriggered = true;

        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
            
        }

        if (interaction != null)
        {
            Debug.Log("[Proximity] Interaction found");

            if (interaction.HasStateAuthority)
            {
                interaction.CurrentDialogue = "…Who’s there? What do you want?";

            }
            else
            {
                Debug.LogWarning("[Proximity] No StateAuthority - dialogue not set");
            }
        }
    }
}