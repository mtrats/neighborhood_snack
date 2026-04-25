using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner2 : MonoBehaviour
{
    // so both players can spawn in level 2
    // attached to spawner (where transform comes from)
    public GameObject PlayerPrefab;

    private IEnumerator Start()
    {
        NetworkRunner runner = null;
        while (runner == null || !runner.IsRunning)
        {
            runner = FindObjectOfType<NetworkRunner>();
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        if (!runner.TryGetPlayerObject(runner.LocalPlayer, out _))
        {
            runner.Spawn(
                PlayerPrefab,
                transform.position,
                transform.rotation,
                runner.LocalPlayer
            );
        }
    }
}
