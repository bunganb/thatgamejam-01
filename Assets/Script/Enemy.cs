using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Moving, Waiting, Alert, Catch }
    public State state = State.Moving;

    [Header("Refs")]
    public Transform player;
    public FieldOfView2D fovVisual; // optional (mesh)
    
    [Header("Movement")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public float waitTime = 1.5f;
    private int patrolIndex = 0;
    private float waitTimer = 0f;

    [Header("Vision (2D)")]
    public float viewDistance = 6f;
    [Range(0f, 360f)] public float fov = 90f;
    public LayerMask obstacleMask; // tembok, dll
    public LayerMask playerMask;   // layer player (opsional)
    
    [Header("Alert")]
    public float catchDistance = 1.2f;

    private void Start()
    {
        // sync visual kalau ada
        if (fovVisual != null)
        {
            fovVisual.SetFoV(fov);
            fovVisual.SetViewDistance(viewDistance);
        }
    }

    private void Update()
    {
        // update visual cone
        if (fovVisual != null)
        {
            fovVisual.SetOrigin(transform.position);
            fovVisual.SetAimDirection(transform.right); // asumsi musuh menghadap kanan sebagai forward
        }

        bool canSeePlayer = CanSeePlayer();

        switch (state)
        {
            case State.Moving:
                if (canSeePlayer) { state = State.Alert; break; }
                PatrolMove();
                break;

            case State.Waiting:
                if (canSeePlayer) { state = State.Alert; break; }
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTime)
                {
                    waitTimer = 0f;
                    state = State.Moving;
                }
                break;

            case State.Alert:
                // contoh: kejar / atau hanya “menghadap”
                FacePlayer();

                // kalau kehilangan player, balik patrol
                if (!canSeePlayer)
                {
                    state = State.Moving;
                    break;
                }

                // kalau sudah dekat → catch
                if (Vector2.Distance(transform.position, player.position) <= catchDistance)
                {
                    state = State.Catch;
                }
                break;

            case State.Catch:
                // TODO: logic game over / tangkap
                // misalnya disable movement dsb
                break;
        }
    }

    private void PatrolMove()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Vector3 target = patrolPoints[patrolIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        // arahkan “forward” (transform.right) ke target
        Vector3 dir = (target - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
            transform.right = dir; // 2D: right sebagai forward

        if (Vector2.Distance(transform.position, target) <= 0.05f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            state = State.Waiting;
            waitTimer = 0f;
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
            transform.right = dir;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)(player.position - transform.position);

        // 1) jarak
        if (toPlayer.magnitude > viewDistance) return false;

        // 2) sudut (gunakan transform.right sebagai forward)
        Vector2 forward = transform.right;
        float angleToPlayer = Vector2.Angle(forward, toPlayer);
        if (angleToPlayer > fov * 0.5f) return false;

        // 3) line of sight (ketutup tembok?)
        RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, toPlayer.magnitude, obstacleMask);
        if (hit.collider != null) return false;

        return true;
    }
}
