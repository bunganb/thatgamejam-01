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
    private void Update()
    {
        if (waypoints.Count == 0 || waypoints == null || !isMoving) return;
        MoveTowardsWaypoint();
    }
    private void LateUpdate()
    {
        if (waypoints.Count == 0) return;

        float dir = waypoints[currentWaypointIndex].position.x - transform.position.x;
        if (dir != 0)
            transform.localScale = new Vector3(Mathf.Sign(dir), 1, 1);
    }
    private void MoveTowardsWaypoint()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        transform.position = Vector2.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);
        if(Vector2.Distance(transform.position, targetWaypoint.position) < 0.05f)
        {
            AdvanceIndex();
        }
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
    public void StopMovement(bool enable)
    {
        isMoving = enable;
    }
}
