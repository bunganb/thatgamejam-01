using UnityEngine;
public interface IInteractable
{
    InteractionKey InteractionKey { get; }
    void OnEnter(GameObject player);
    void OnExit(GameObject player);
    void Interact(GameObject player);
}
