using UnityEngine;
using System.Collections;

public class PlayerSkillController : MonoBehaviour
{
    public float cooldown = 5f;

    [Header("References")]
    public DogBarkController dog;

    public bool IsOnCooldown { get; private set; }
    public float CooldownProgress { get; private set; } // 0–1

    public bool TryActivateSkill()
    {
        if (IsOnCooldown || dog == null)
            return false;

        dog.ActiveBark();
        StartCoroutine(CooldownRoutine());
        return true;
    }

    private IEnumerator CooldownRoutine()
    {
        IsOnCooldown = true;
        CooldownProgress = 1f;

        float timer = cooldown;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            CooldownProgress = timer / cooldown;
            yield return null;
        }

        CooldownProgress = 0f;
        IsOnCooldown = false;
    }
}