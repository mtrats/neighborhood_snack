using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelWin : MonoBehaviour
{
    public GameObject winText;
    public GameObject winButton;
    public TownspersonInteraction interaction;
    
    // Update is called once per frame
    void Update()
    {
        if (interaction == null)
            return;
        if (interaction.Object == null || !interaction.Object.IsValid)
            return;
        if (interaction.TrustScore >= SimpleGeminiMic.WinThreshold)
        {
            winText.SetActive(true);
            winButton.SetActive(true);
        }
    }
}
