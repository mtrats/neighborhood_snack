using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchLevelScene : MonoBehaviour
{
    public string SceneToLoad;
    public GameObject text;

    public void LoadTheScene()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        
        if (runner != null)
        {
            if (runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObject))
            {
                runner.Despawn(playerObject);
            }

            runner.LoadScene(SceneRef.FromIndex(
                SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{SceneToLoad}.unity")
            ));
        }
    }
}
