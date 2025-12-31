using UnityEngine;

public class SequencePuzzleStep : MonoBehaviour, IInteractable
{
    public InteractionKey interactionKey = InteractionKey.Space;
    public InteractionKey InteractionKey => interactionKey;
    [Header("References")]
    public SequencePuzzleController controller;
    [Header("UI Settings")]
    public GameObject ui;

    private bool isActive = false;

    private void Awake()
    {
        if (ui != null)
            ui.SetActive(false);
    }
    public void OnEnter(GameObject player)
    {
        if (!isActive && ui != null)
        {   
            ui.SetActive(true);    
        }
    }
    public void OnExit(GameObject player)
    {
        if (!isActive && ui != null)
        {
            ui.SetActive(false);
        }
    }
    private void Active()
    {
        isActive = true;
        if (ui != null) ui.SetActive(false);

        controller.NotifyActived(this);
        Debug.Log("Step Activated");

        // suara puzzle (tanpa anjing)
        NoiseSystem.Emit(new NoiseInfo(NoiseType.Puzzle, transform.position, 8f));
    }

    public void ResetStep()
    {
        isActive = false;
        if (ui != null)
            ui.SetActive(false);
    }
    public void Interact(GameObject player)
    {
        if (!isActive)
        {
            Active();
        }
    }
}
