using UnityEngine;
using System.Collections;

public class DogBarkController : MonoBehaviour
{
    [Header("Bark Settings")]
    public float barkDuration = 1.5f;
    [Header("References")]
    public NPCWaypointMovement movement;
    private bool isBarking = false;
    public void ActiveBark()
    {
        if (isBarking) return;

        NoiseSystem.Emit(new NoiseInfo(
            NoiseType.DogBark,
            transform.position,
            10f
        ));

        StartCoroutine(BarkRoutine());
    }

    private IEnumerator BarkRoutine()
    {
        isBarking = true;

        // 🔴 BERHENTI SAAT BARK
        movement.StopMovement(true);
        yield return new WaitForSeconds(barkDuration);
        // 🟢 LANJUT JALAN SETELAH BARK
        movement.StopMovement(false);

        isBarking = false;
    }
}
