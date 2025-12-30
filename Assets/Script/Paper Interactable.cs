using UnityEngine;
using UnityEngine.InputSystem;
public class PaperInteractable : MonoBehaviour, IInteractable
{
    public InteractionKey InteractionKey => InteractionKey.E;
    [Header("UI Elements")]
    public GameObject paperUI;
    public GameObject panel;

    private bool isReading = false;
    private GameObject currentPlayer;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        paperUI.SetActive(false);
        panel.SetActive(false);
    }

    public void OnEnter(GameObject player)
    {
        this.currentPlayer = player;
        this.playerMovement = player.GetComponent<PlayerMovement>();
        if (!isReading)
        {
            paperUI.SetActive(true);
        }
    }
    public void OnExit(GameObject player)
    {
        if (!isReading)
        {
            paperUI.SetActive(false);
            this.currentPlayer = null;
            this.playerMovement = null;
        }
    }
    public void Interact(GameObject player)
    {
        if (isReading)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }
    private void OpenPanel()
    {
        panel.SetActive(true);
        isReading = true;
        paperUI.SetActive(false);
        playerMovement.enabled = false;
        Time.timeScale = 0;
        Debug.Log("Membuka panel kertas");
    }
    private void ClosePanel()
    {
        panel.SetActive(false);
        isReading = false;
        paperUI.SetActive(true);
        playerMovement.enabled = true;
        Time.timeScale = 1;
        Debug.Log("Menutup panel kertas");
    }
    private void Update()
    {
        if (!isReading) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }
}