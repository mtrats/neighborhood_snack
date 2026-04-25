using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    public static void AssignToPlayer(Transform playerTransform)
    {
        CinemachineFreeLook cam = GameObject.FindObjectOfType<CinemachineFreeLook>();
        if (cam != null && playerTransform != null)
        {
            Transform camTarget = playerTransform.Find("CameraTarget");
            cam.Follow = playerTransform;
            cam.LookAt = camTarget != null ? camTarget : playerTransform;
        }
    }
}
