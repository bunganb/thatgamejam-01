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

        return key switch
        {
            InteractionKey.E => keyboard.eKey.wasPressedThisFrame,
            InteractionKey.F => keyboard.fKey.wasPressedThisFrame,
            InteractionKey.Space => keyboard.spaceKey.wasPressedThisFrame,
            _ => false,
        };
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<IInteractable>(out var interactable))
        {
            current = interactable;
            current.OnEnter(gameObject);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent<IInteractable>(out var interactable))
        {
            current.OnExit(gameObject);
            current = null;
        }
    }
}