using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    private readonly List<IInteractable> interactables = new();
    private IInteractable current;

    void Update()
    {
        if (current == null) return;

        if (IsPressed(current.InteractionKey))
        {
            current.Interact(gameObject);
        }
    }

    private bool IsPressed(InteractionKey key)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        return key switch
        {
            InteractionKey.E => kb.eKey.wasPressedThisFrame,
            InteractionKey.F => kb.fKey.wasPressedThisFrame,
            InteractionKey.Space => kb.spaceKey.wasPressedThisFrame,
            InteractionKey.Q => kb.qKey.wasPressedThisFrame,
            _ => false
        };
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            if (!interactables.Contains(interactable))
                interactables.Add(interactable);

            UpdateCurrent();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            if (interactables.Contains(interactable))
            {
                interactable.OnExit(gameObject);
                interactables.Remove(interactable);
                UpdateCurrent();
            }
        }
    }

    private void UpdateCurrent()
    {
        // Matikan UI semua
        foreach (var i in interactables)
            i.OnEnter(gameObject); // aman karena tiap objek handle sendiri

        // Pilih prioritas tertinggi (sementara: yang terakhir masuk)
        current = interactables.Count > 0
            ? interactables[^1]
            : null;
    }
}
