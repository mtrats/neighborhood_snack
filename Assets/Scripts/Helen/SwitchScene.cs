using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    public string SceneToLoad;

    public void LoadTheScene()
    {
        SceneManager.LoadScene(SceneToLoad);
    }
}
