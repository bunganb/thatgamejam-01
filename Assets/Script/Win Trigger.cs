using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WinTrigger : MonoBehaviour
{
    private bool triggered = false;

    private void Awake()
    {
        // Pastikan collider adalah trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Win Trigger Entered by: " + other.name);
        if (triggered) return;
        
        if (!other.CompareTag("Player"))
            return;

        triggered = true;
        TriggerWin();
    }

    private void TriggerWin()
    {
        Debug.Log("WIN TRIGGERED");

        // Matikan kontrol player (opsional tapi direkomendasikan)
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

        if (player != null)
            player.isAlive = false;

        // Tampilkan UI win
        UIManager.Instance.WinPanelUI();
    }
}
