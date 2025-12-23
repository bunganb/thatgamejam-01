using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillController : MonoBehaviour
{
    public float cooldown = 5f;

    [Header("References")]
    public DogBarkController dog; // drag di Inspector

    private bool onCooldown = false;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ActivateSkill();
        }
    }

    private void ActivateSkill()
    {
        if (onCooldown)
        {
            Debug.Log("Skill on cooldown");
            return;
        }

        if (dog == null)
        {
            Debug.LogError("Dog reference not set!");
            return;
        }

        Debug.Log("Activating dog bark skill");
        dog.ActiveBark();
        StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
