using UnityEngine;
using System.Collections;
public class DogBarkController : MonoBehaviour
{
    [Header("Bark Settings")]
    public float barkDuration = 1.5f;

    [Header("References")]
    public NPCWaypointMovement movement;
    private Animator animator;

    private bool isBarking = false;

    public void ActiveBark()
    {
        if (isBarking) return;
        StartCoroutine(BarkRoutine());
    }

    private IEnumerator BarkRoutine()
    {
        isBarking = true;
        if(animator != null)
            animator.SetBool("Bark", true);
        movement.StopMovement(false);
        yield return new WaitForSeconds(barkDuration);
        movement.StopMovement(true);
        isBarking = false;
    }
}
