using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;
    //public GameObject spawner;

    void IPlayerJoined.PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        GameObject spawner = GameObject.FindWithTag("Spawner");

        Vector3 spawnPos = spawner != null ? spawner.transform.position : Vector3.zero;
        Quaternion spawnRot = spawner != null ? spawner.transform.rotation : Quaternion.identity;

        NetworkObject spawnedPlayer = Runner.Spawn(PlayerPrefab, spawnPos, spawnRot, Runner.LocalPlayer);
        PlayerCamera.AssignToPlayer(spawnedPlayer.transform);
    }
}
