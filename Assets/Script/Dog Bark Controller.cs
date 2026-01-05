using UnityEngine;
using System.Collections;

public class DogBarkController : MonoBehaviour
{
    [Header("Bark Settings")]
    public float barkDuration = 1.5f;

    [Header("Room/Level System")]
    [Tooltip("ID room/level tempat dog berada (harus sama dengan enemy di room ini)")]
    public string roomID = "Room1";

    [Header("References")]
    public NPCWaypointMovement movement;
    private bool isBarking = false;
    public void ActiveBark()
    {
        if (isBarking) return;

        // Emit suara anjing dengan room ID
        NoiseSystem.Emit(new NoiseInfo(NoiseType.DogBark, transform.position, 10f, roomID));
        
        Debug.Log($"[Dog] Bark activated in room: {roomID}");

        StartCoroutine(BarkRoutine());
    }

    private IEnumerator BarkRoutine()
    {
        isBarking = true;
        movement.StopMovement(true);
        yield return new WaitForSeconds(barkDuration);
        if (movement != null)
            movement.StopMovement(true);
        movement.StopMovement(false);
        isBarking = false;
    }
}