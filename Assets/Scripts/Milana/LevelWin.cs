using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelWin : MonoBehaviour
{
    public GameObject winText;
    public GameObject winButton;
    private bool hasShown = false;

    // Update is called once per frame
    void Update()
    {
        if (hasShown) return;

        if (AllNPCsWon())
        {
            if (winText != null)
                winText.SetActive(true);

            if (winButton != null)
                winButton.SetActive(true);

            hasShown = true;
        }
    }

    private bool AllNPCsWon()
    {
        var triggers = FindObjectsOfType<ProximityConversationTrigger>();

        // Need at least one NPC in the scene
        if (triggers.Length == 0) return false;

        foreach (var trigger in triggers)
        {
            if (!trigger.HasWon) return false;
        }

        return true;
    }
}
