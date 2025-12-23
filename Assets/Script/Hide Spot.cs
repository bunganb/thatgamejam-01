using UnityEngine;
public class HideSpot : MonoBehaviour, IInteractable
{
    public InteractionKey InteractionKey => InteractionKey.E;
    public GameObject icon;
    private bool isHiding = false;
    private GameObject currentPlayer;
    private Rigidbody2D rb;
    private void Awake()
    {
        icon.SetActive(false);
    }
    public void OnEnter(GameObject player)
    {
        icon.SetActive(true);
        currentPlayer = player;
        rb = player.GetComponent<Rigidbody2D>();
    }
    public void OnExit(GameObject player)
    {
        if (!isHiding)
        {
            currentPlayer = null;
            
        }
        icon.SetActive(false);
    }
    public void Interact(GameObject player)
    {
        if (isHiding)
        {
            unhide(player);
        }
        else
        {
            hide(player);
        }
    }
    private void hide(GameObject player)
    {
        
        player.GetComponent<SpriteRenderer>().enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        player.GetComponent<PlayerMovement>().enabled = false;
        icon.SetActive(false);
        isHiding = true;
        
    }
    private void unhide(GameObject player)
    {
       
        player.GetComponent<SpriteRenderer>().enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        player.GetComponent<PlayerMovement>().enabled = true;
        isHiding = false;
        icon.SetActive(true);
        
    }
} 