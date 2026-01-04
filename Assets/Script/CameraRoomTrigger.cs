using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraRoomTrigger : MonoBehaviour
{
    public CinemachineCamera roomCamera;
    public GameObject door;
    [Header("Room Patrol Settings")]
    public NPCWaypointMovement dog;
    public List<Transform> roomWaypoints;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        roomCamera.Priority = 20;
        StartCoroutine(ActivateDoorWithDelay(0.2f));
        if (dog == null)
        {
            Debug.LogError("Dog reference missing");
            return;
        }

        dog.SetWaypoints(roomWaypoints);
        Debug.Log("Dog patrol changed for new room");
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        roomCamera.Priority = 0;
        if (door != null)
        {
            door.SetActive(false);
        }
    }
    private System.Collections.IEnumerator ActivateDoorWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (door != null)
        {
            door.SetActive(true);
        }
    }
}
