using UnityEngine;
using UnityEngine.InputSystem;

public class PaperInteractable : MonoBehaviour, IInteractable
{
    public InteractionKey InteractionKey => InteractionKey.E;

    [Header("Paper Content")]
    public Sprite paperSprite;

    [Header("UI")]
    public GameObject icon;

    private bool isReading = false;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (icon != null)
            icon.SetActive(false);
    }

    public void OnEnter(GameObject player)
    {
        playerMovement = player.GetComponent<PlayerMovement>();

        if (!isReading && icon != null)
            icon.SetActive(true);
    }

    public void OnExit(GameObject player)
    {
        if (isReading) return;

        if (icon != null)
            icon.SetActive(false);

        playerMovement = null;
    }

    public void Interact(GameObject player)
    {
        if (isReading)
            Close();
        else
            Open();
    }

    private void Open()
    {
        isReading = true;

        if (icon != null)
            icon.SetActive(false);

        playerMovement.enabled = false;
        PaperPanelUI.Instance.Open(paperSprite);

        Debug.Log($"Membuka kertas: {paperSprite.name}");
    }

    private void Close()
    {
        isReading = false;

        playerMovement.enabled = true;
        PaperPanelUI.Instance.Close();

        if (icon != null)
            icon.SetActive(true);

        Debug.Log("Menutup kertas");
    }

    private void Update()
    {
        if (!isReading) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame ||
            kb.backspaceKey.wasPressedThisFrame ||
            kb.eKey.wasPressedThisFrame)
        {
            Close();
        }
    }
}
