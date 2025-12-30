using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Gameplay State")]
    public bool isHidden;
    public bool isAlive = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!isAlive) return;

        ReadInput();
        UpdateAnimation();
        FlipSprite();
    }

    private void FixedUpdate()
    {
        if (!isAlive || isHidden)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float x =
            (Keyboard.current.dKey.isPressed ? 1 : 0) -
            (Keyboard.current.aKey.isPressed ? 1 : 0);

        float y =
            (Keyboard.current.wKey.isPressed ? 1 : 0) -
            (Keyboard.current.sKey.isPressed ? 1 : 0);

        moveInput = new Vector2(x, y).normalized;
    }
    private void UpdateAnimation()
    {
        float animX = Mathf.Abs(moveInput.x);
        float animY = moveInput.y;

        animator.SetFloat("MoveX", animX);
        animator.SetFloat("MoveY", animY);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }
    private void FlipSprite()
    {
        if (moveInput.x < 0)
            spriteRenderer.flipX = true;
        else if (moveInput.x > 0)
            spriteRenderer.flipX = false;
    }
}