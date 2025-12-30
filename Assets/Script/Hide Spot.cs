using UnityEngine;

public class HideSpot : MonoBehaviour, IInteractable
{
    public InteractionKey InteractionKey => InteractionKey.E;

    [Header("UI")]
    public GameObject icon;

    private bool isHiding;
    private PlayerMovement player;

    private void Awake()
    {
        if (icon != null)
            icon.SetActive(false);
    }

    public void OnEnter(GameObject go)
    {
        icon?.SetActive(true);
        player = go.GetComponent<PlayerMovement>();
    }

    public void OnExit(GameObject go)
    {
        if (!isHiding)
        {
            player = null;
            icon?.SetActive(false);
        }
    }

    public void Interact(GameObject go)
    {
        if (player == null) return;

        if (isHiding) Unhide();
        else Hide();
    }

    private void Hide()
    {
        isHiding = true;
        icon?.SetActive(false);

        player.isHidden = true;

        // efek fisik (opsional)
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }

    private void Unhide()
    {
        isHiding = false;
        icon?.SetActive(true);

        player.isHidden = false;
    }
}