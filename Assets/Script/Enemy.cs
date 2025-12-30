using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Moving, Waiting, Alert, Catch }
    public State state = State.Moving;

    [Header("Player")]
    public PlayerMovement player;

    [Header("FOV Visual")]
    [SerializeField] private GameObject fovPrefab;
    private FieldOfView2D fovVisual;

    [Header("Movement")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public float waitTime = 1.5f;

    private int patrolIndex;
    private float waitTimer;

    [Header("Vision")]
    public float viewDistance = 6f;
    [Range(0f, 360f)] public float fov = 90f;
    public LayerMask obstacleMask;

    [Header("Catch")]
    public float catchDistance = 1.2f;

    private void Awake()
    {
        if (fovPrefab != null)
        {
            var go = Instantiate(fovPrefab, transform);
            fovVisual = go.GetComponent<FieldOfView2D>();
        }
    }

    private void Start()
    {
        if (fovVisual != null)
        {
            fovVisual.SetFoV(fov);
            fovVisual.SetViewDistance(viewDistance);
        }
    }

    private void Update()
    {
        UpdateFOV();

        // ATURAN UTAMA
        if (CanSeePlayer() && !player.isHidden)
        {
            state = State.Catch;
        }

        switch (state)
        {
            case State.Moving:
                PatrolMove();
                break;

            case State.Waiting:
                HandleWaiting();
                break;

            case State.Alert:
                // nanti dipakai untuk suara
                break;

            case State.Catch:
                CatchPlayer();
                break;
        }
    }

    private void UpdateFOV()
    {
        if (fovVisual == null) return;

        fovVisual.SetOrigin(transform.position);
        fovVisual.SetAimDirection(transform.right);
    }

    private void PatrolMove()
    {
        if (patrolPoints.Length == 0) return;

        Vector3 target = patrolPoints[patrolIndex].position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        Vector3 dir = (target - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
            transform.right = dir;

        if (Vector2.Distance(transform.position, target) < 0.05f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            state = State.Waiting;
            waitTimer = 0f;
        }
    }

    private void HandleWaiting()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTime)
        {
            state = State.Moving;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null || player.isHidden) return false;

        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)(player.transform.position - transform.position);

        if (toPlayer.magnitude > viewDistance) return false;

        float angle = Vector2.Angle(transform.right, toPlayer);
        if (angle > fov * 0.5f) return false;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            toPlayer.normalized,
            toPlayer.magnitude,
            obstacleMask
        );

        return hit.collider == null;
    }

    private void CatchPlayer()
    {
        if (!player.isAlive) return;

        player.isAlive = false;
        Debug.Log("GAME OVER");
        Time.timeScale = 0f;
    }
}
