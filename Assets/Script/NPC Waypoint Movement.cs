using UnityEngine;
using System.Collections.Generic;

public class NPCWaypointMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public bool loop = true;
    public bool reserseAtEnd = false;

    [Header("Waypoints")]
    public List<Transform> waypoints = new List<Transform>();

    private int currentWaypointIndex = 0;
    private int direction = 1;
    private bool isMoving = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sprite;

    private Vector2 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Count == 0 || !isMoving)
        {
            velocity = Vector2.zero;
            UpdateAnimation();
            return;
        }

        MoveTowardsWaypoint();
        UpdateAnimation();
    }


    private void MoveTowardsWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];

        Vector2 dir = (target.position - transform.position).normalized;
        velocity = dir * speed;

        rb.MovePosition(rb.position + velocity * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            AdvanceIndex();
        }

        // Flip sprite
        if (dir.x != 0)
            sprite.flipX = dir.x < 0;
    }

    private void UpdateAnimation()
    {
        float speedValue = velocity.magnitude;

        animator.SetFloat("Speed", speedValue);     
        animator.SetFloat("MoveX", velocity.x);
    }

    private void AdvanceIndex()
    {
        currentWaypointIndex += direction;

        if (currentWaypointIndex >= waypoints.Count || currentWaypointIndex < 0)
        {
            if (reserseAtEnd)
            {
                direction *= -1;
                currentWaypointIndex += direction * 2;
            }
            else if (loop)
            {
                currentWaypointIndex = 0;
            }
            else
            {
                currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Count - 1);
            }
        }
    }

    public void StopMovement(bool stop)
    {
        isMoving = !stop;
        velocity = Vector2.zero;
    }

    public void SetWaypoints(List<Transform> newWaypoints)
    {
        if (newWaypoints == null || newWaypoints.Count == 0)
            return;

        waypoints = newWaypoints;
        currentWaypointIndex = 0;
        direction = 1;
    }
}
