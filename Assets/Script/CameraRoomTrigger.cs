using Unity.Cinemachine;
using UnityEngine;

public class CameraRoomTrigger : MonoBehaviour
{
    public CinemachineCamera roomCamera;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        roomCamera.Priority = 20;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        roomCamera.Priority = 0;
    }
}
