using UnityEngine;

public class SequencePuzzleStep : MonoBehaviour, IInteractable
{
    public InteractionKey interactionKey = InteractionKey.Space;
    public InteractionKey InteractionKey => interactionKey;

    [Header("Step ID")]
    public int stepID;

    [Header("References")]
    public SequencePuzzleController controller;

    [Header("Room/Level System")]
    [Tooltip("ID room/level tempat puzzle berada (harus sama dengan enemy di room ini)")]
    public string roomID = "Room1";

    [Header("UI Settings")]
    public GameObject ui;

    private void Awake()
    {
        if (ui != null)
            ui.SetActive(false);
    }

    public void OnEnter(GameObject player)
    {
        if (controller != null && controller.IsSolved)
            return;

        if (ui != null)
            ui.SetActive(true);
    }

    public void OnExit(GameObject player)
    {
        if (ui != null)
            ui.SetActive(false);
    }

    public void Interact(GameObject player)
    {
        if (controller == null || controller.IsSolved)
            return;

        // Send input ke controller
        controller.ReceiveInput(stepID);

        // Emit noise dengan room ID
        NoiseSystem.Emit(
            new NoiseInfo(NoiseType.Puzzle, transform.position, 8f, roomID)
        );
        
        Debug.Log($"[Puzzle] Step {stepID} activated in room: {roomID}");
    }
}