using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private IInteractable current;
    private void Update()
    {
        if(current == null) return;

        if(IsPressed(current.InteractionKey))
        {
            current.Interact(gameObject);
        }
    }
    private bool IsPressed(InteractionKey key)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        switch (key)
        {
            case InteractionKey.E:
                return keyboard.eKey.wasPressedThisFrame;

            case InteractionKey.F:
                return keyboard.fKey.wasPressedThisFrame;

            case InteractionKey.Space:
                return keyboard.spaceKey.wasPressedThisFrame;

            case InteractionKey.Q:
                return keyboard.qKey.wasPressedThisFrame;
        }
        return false;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            current = interactable;
            current.OnEnter(gameObject);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            current.OnExit(gameObject);
            current = null;
        }
    }
}