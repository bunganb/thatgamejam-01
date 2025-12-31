using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Patrol, Waiting, Investigate, Alert, Catch }
    public State state = State.Patrol;

    [Header("Player")]
    public PlayerMovement player;

    [Header("FOV Visual")]
    [SerializeField] private GameObject fovPrefab;
    private FieldOfView2D fovVisual;

    [Header("Movement")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public float waitTime = 1.5f;
    public float rotationSpeed = 90f; // Rotation speed untuk Waiting state

    private int patrolIndex;
    private float stateTimer;
    private float rotationDirection = 1f; // 1 atau -1 untuk rotasi kanan/kiri

    [Header("Vision")]
    public float viewDistance = 6f;
    [Range(0f, 360f)] public float fov = 90f;
    public LayerMask obstacleMask;

    [Header("Hearing (Noise Detection)")]
    [Tooltip("Jarak maksimal enemy bisa dengar Dog Bark")]
    public float barkHearingDistance = 15f;
    [Tooltip("Jarak maksimal enemy bisa dengar Puzzle (set sangat besar untuk unlimited)")]
    public float puzzleHearingDistance = 999f;
    
    [Header("Investigate (Noise)")]
    public float investigateStopDistance = 0.2f;
    public float investigateWaitTime = 2f;
    public float barkBlockWindow = 0.4f;

    private Vector2 investigateTarget;
    private float lastBarkTime = -999f;

    [Header("Alert Settings")]
    public float alertDuration = 2f;
    public float alertRotationSpeed = 45f;
    private Vector2 alertDirection;

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private float checkInterval = 0.1f;
    private float checkTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (fovPrefab != null)
        {
            GameObject go = Instantiate(fovPrefab, transform);
            fovVisual = go.GetComponent<FieldOfView2D>();
        }
    }

    private void OnEnable()
    {
        NoiseSystem.OnNoise += HandleNoise;
    }

    private void OnDisable()
    {
        NoiseSystem.OnNoise -= HandleNoise;
    }

    private void Start()
    {
        if (fovVisual != null)
        {
            fovVisual.SetFoV(fov);
            fovVisual.SetViewDistance(viewDistance);
        }

        lastPosition = transform.position;
        
        // Validasi patrol points
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("[Enemy] No patrol points assigned!");
        }
    }

    private void Update()
    {
        UpdateFOV();

        // Check player visibility dengan interval untuk optimasi
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            
            if (state != State.Catch && CanSeePlayer())
            {
                ChangeState(State.Catch);
            }
        }

        // State machine
        switch (state)
        {
            case State.Patrol:
                HandlePatrol();
                break;

            case State.Waiting:
                HandleWaiting();
                break;

            case State.Investigate:
                HandleInvestigate();
                break;

            case State.Alert:
                HandleAlert();
                break;

            case State.Catch:
                HandleCatch();
                break;
        }
    }

    private void HandleNoise(NoiseInfo noise)
    {
        // Ignore noise saat sedang mengejar
        if (state == State.Catch) return;

        float distance = Vector2.Distance(transform.position, noise.position);
        
        // Tentukan hearing distance berdasarkan tipe noise
        float maxHearingDistance = noise.type == NoiseType.DogBark ? barkHearingDistance : puzzleHearingDistance;
        
        // Check hearing distance
        if (distance > maxHearingDistance)
        {
            Debug.Log($"[Enemy] Noise too far: {distance:F2} > {maxHearingDistance:F2}");
            return;
        }

        Debug.Log($"[Enemy] Heard {noise.type} at distance {distance:F2}");

        if (noise.type == NoiseType.DogBark)
        {
            lastBarkTime = Time.time;
            alertDirection = (noise.position - (Vector2)transform.position).normalized;
            ChangeState(State.Alert);
            return;
        }

        if (noise.type == NoiseType.Puzzle)
        {
            // Skip puzzle noise jika baru saja ada bark
            bool barkRecently = (Time.time - lastBarkTime) <= barkBlockWindow;
            
            if (barkRecently)
            {
                Debug.Log("[Enemy] Puzzle ignored - bark happened recently");
                return;
            }
            
            if (state == State.Alert)
            {
                Debug.Log("[Enemy] Puzzle ignored - currently in Alert state");
                return;
            }

            investigateTarget = noise.position;
            ChangeState(State.Investigate);
        }
    }

    private void ChangeState(State newState)
    {
        if (state == newState) return;

        // Exit current state
        switch (state)
        {
            case State.Waiting:
                rotationDirection = 1f; // Reset rotation
                break;
        }

        state = newState;
        stateTimer = 0f;

        // Enter new state
        switch (newState)
        {
            case State.Alert:
                if (alertDirection != Vector2.zero)
                {
                    FaceDirection(alertDirection);
                }
                break;

            case State.Investigate:
                Debug.Log($"[Enemy] Investigating position: {investigateTarget}");
                break;
        }
    }

    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Vector2 targetPos = patrolPoints[patrolIndex].position;
        
        // Hadapkan ke target sebelum bergerak
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude > 0.01f)
        {
            FaceDirection(direction);
        }
        
        MoveTowards(targetPos);

        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            ChangeState(State.Waiting);
        }
    }

    private void HandleWaiting()
    {
        stateTimer += Time.deltaTime;

        // Rotasi vision cone kiri-kanan
        float rotationAmount = rotationSpeed * rotationDirection * Time.deltaTime;
        transform.Rotate(0, 0, rotationAmount);

        // Ganti arah rotasi setiap 0.5 detik untuk efek bolak-balik
        if (stateTimer % 1f < Time.deltaTime)
        {
            rotationDirection *= -1f;
        }

        if (stateTimer >= waitTime)
        {
            ChangeState(State.Patrol);
        }
    }

    private void HandleInvestigate()
    {
        Vector2 currentPos = transform.position;
        float distance = Vector2.Distance(currentPos, investigateTarget);

        if (distance <= investigateStopDistance)
        {
            stateTimer += Time.deltaTime;

            // Rotasi pelan saat menunggu di lokasi investigate
            transform.Rotate(0, 0, 30f * Time.deltaTime);

            if (stateTimer >= investigateWaitTime)
            {
                Debug.Log("[Enemy] Investigation complete, returning to patrol");
                ChangeState(State.Patrol);
            }
        }
        else
        {
            // Face direction saat bergerak
            Vector2 direction = (investigateTarget - currentPos).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                FaceDirection(direction);
            }
            
            MoveTowards(investigateTarget);
        }
    }

    private void HandleAlert()
    {
        stateTimer += Time.deltaTime;

        // Rotasi pelan untuk "scanning" area
        transform.Rotate(0, 0, alertRotationSpeed * Time.deltaTime);

        if (stateTimer >= alertDuration)
        {
            Debug.Log("[Enemy] Alert ended, returning to patrol");
            ChangeState(State.Patrol);
        }
    }

    private void HandleCatch()
    {
        if (player == null || !player.isAlive) return;

        // Bergerak menuju player
        Vector2 targetPos = player.transform.position;
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        
        // Hadapkan ke player
        if (direction.sqrMagnitude > 0.01f)
        {
            FaceDirection(direction);
        }
        
        MoveTowards(targetPos);

        // Check collision/tangkap
        float distanceToPlayer = Vector2.Distance(transform.position, targetPos);
        if (distanceToPlayer < 0.5f)
        {
            CatchPlayer();
        }

        // Jika player sembunyi, kembali ke patrol
        if (player.isHidden && !CanSeePlayer())
        {
            Debug.Log("[Enemy] Player hidden, returning to patrol");
            ChangeState(State.Patrol);
        }
    }

    private void MoveTowards(Vector2 target)
    {
        Vector2 currentPos = transform.position;
        Vector2 direction = (target - currentPos).normalized;
        Vector2 newPos = Vector2.MoveTowards(currentPos, target, moveSpeed * Time.deltaTime);

        if (rb != null)
        {
            rb.MovePosition(newPos);
        }
        else
        {
            transform.position = newPos;
        }

        // Face direction saat bergerak
        if (direction.sqrMagnitude > 0.01f)
        {
            FaceDirection(direction);
        }

        lastPosition = newPos;
    }

    private void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        
        direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void UpdateFOV()
    {
        if (fovVisual == null) return;
        
        fovVisual.SetOrigin(transform.position);
        fovVisual.SetAimDirection(transform.right);
    }

    private bool CanSeePlayer()
    {
        if (player == null || player.isHidden) return false;

        Vector2 origin = transform.position;
        Vector2 playerPos = player.transform.position;
        Vector2 toPlayer = playerPos - origin;
        float distance = toPlayer.magnitude;

        // Check distance
        if (distance > viewDistance) return false;

        // Check FOV angle
        float angle = Vector2.Angle(transform.right, toPlayer);
        if (angle > fov * 0.5f) return false;

        // Check obstacles dengan raycast
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            toPlayer.normalized,
            distance,
            obstacleMask
        );

        return hit.collider == null;
    }

    private void CatchPlayer()
    {
        if (player == null || !player.isAlive) return;

        player.isAlive = false;
        Debug.Log("[Enemy] GAME OVER - Player Caught!");
        
        // Opsional: Tambahkan event atau method game over disini
        Time.timeScale = 0f;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw patrol path
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                int next = (i + 1) % patrolPoints.Length;
                if (patrolPoints[i] != null && patrolPoints[next] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
                }
            }
        }

        // Draw investigate target
        if (state == State.Investigate)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(investigateTarget, investigateStopDistance);
            Gizmos.DrawLine(transform.position, investigateTarget);
        }

        // Draw vision range (FOV - hijau)
        Gizmos.color = state == State.Catch ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        // Draw bark hearing range (kuning transparan) - hanya jika wajar
        if (barkHearingDistance < 50f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, barkHearingDistance);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw bark hearing range
        if (barkHearingDistance < 50f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, barkHearingDistance);
            
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * barkHearingDistance, 
                $"Bark Hearing: {barkHearingDistance}m"
            );
        }
        
        // Draw vision range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * viewDistance, 
            $"Vision: {viewDistance}m"
        );
        
        // Info puzzle hearing
        UnityEditor.Handles.Label(
            transform.position + Vector3.down * 0.5f, 
            $"Puzzle: {(puzzleHearingDistance > 100f ? "Unlimited" : puzzleHearingDistance + "m")}"
        );
    }
}