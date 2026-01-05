using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public enum State { Patrol, Waiting, Investigate, Alert, Catch }
    public State state = State.Patrol;

    /* ================= PLAYER ================= */
    [Header("Player")]
    public PlayerMovement player;

    /* ================= ANIMATION ================= */
    [Header("Animation")]
    public Animator animator;
    public string animHorizontalParam = "Horizontal";
    public string animVerticalParam = "Vertical";
    public string animIsMovingParam = "IsMoving";
    private Vector2 lastMoveDirection = Vector2.down;

    /* ================= ROOM ================= */
    [Header("Room System")]
    public string roomID = "Room1";

    /* ================= FOV ================= */
    [Header("FOV Visual")]
    [SerializeField] private GameObject fovPrefab;
    private FieldOfView2D fovVisual;

    /* ================= MOVEMENT ================= */
    [Header("Movement")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public float waitTime = 1.5f;
    public float rotationSpeed = 90f;

    private int patrolIndex;
    private float stateTimer;
    private float rotationDirection = 1f;

    /* ================= VISION ================= */
    [Header("Vision")]
    public float viewDistance = 6f;
    [Range(0f, 360f)] public float fov = 90f;
    public LayerMask obstacleMask;

    /* ================= HEARING ================= */
    [Header("Hearing")]
    public float barkHearingDistance = 15f;
    public float puzzleHearingDistance = 999f;

    /* ================= INVESTIGATE ================= */
    [Header("Investigate")]
    public float investigateStopDistance = 0.5f;
    public float investigateWaitTime = 2f;
    public float barkBlockWindow = 0.4f;
    public float investigateOffset = 1.5f;

    private Vector2 investigateTarget;
    private List<Vector2> currentPath;
    private int currentPathIndex;
    private float lastBarkTime = -999f;
    private Vector3Int lastPatrolCell;

    /* ================= CHASE ================= */
    [Header("Catch")]
    public float chasePathUpdateInterval = 0.5f;
    public float catchDistance = 0.5f;
    public float maxChaseDistance = 20f;
    public float losePlayerTimeout = 2f;

    private float lastChasePathUpdate;
    private float losePlayerTimer;

    /* ================= ALERT ================= */
    [Header("Alert")]
    public float alertDuration = 2f;
    public float alertRotationSpeed = 45f;
    private Vector2 alertDirection;

    /* ================= INTERNAL ================= */
    private Rigidbody2D rb;
    private float checkTimer;
    private const float CHECK_INTERVAL = 0.1f;
    private const float WAYPOINT_REACH_DISTANCE = 0.3f;

    /* ================= UNITY ================= */

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (fovPrefab != null)
        {
            fovVisual = Instantiate(fovPrefab, transform)
                .GetComponent<FieldOfView2D>();
        }
    }

    private void OnEnable() => NoiseSystem.OnNoise += HandleNoise;
    private void OnDisable() => NoiseSystem.OnNoise -= HandleNoise;

    private void Start()
    {
        if (fovVisual != null)
        {
            fovVisual.SetFoV(fov);
            fovVisual.SetViewDistance(viewDistance);
        }

        // PENTING: Validate dan fix semua patrol points
        if (GridPathfinding.Instance != null && patrolPoints.Length > 0)
        {
            ValidateAndFixPatrolPoints();
        }

        // Generate path ke patrol point pertama saat start
        if (patrolPoints.Length > 0)
        {
            GeneratePath(patrolPoints[patrolIndex].position);
        }
    }

    /// <summary>
    /// Validate semua patrol points dan snap ke grid jika perlu
    /// </summary>
    private void ValidateAndFixPatrolPoints()
    {
        if (GridPathfinding.Instance == null || GridPathfinding.Instance.obstacleTilemap == null)
        {
            Debug.LogWarning($"[Enemy {gameObject.name}] Cannot validate patrol points - GridPathfinding not ready!");
            return;
        }

        Tilemap tilemap = GridPathfinding.Instance.obstacleTilemap;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            Vector3 originalPos = patrolPoints[i].position;
            Vector3Int cell = tilemap.WorldToCell(originalPos);
            
            // Check jika patrol point ada di obstacle
            if (tilemap.HasTile(cell))
            {
                Debug.LogWarning($"[Enemy {gameObject.name}] Patrol point {i} is on obstacle at {cell}! Finding nearest free cell...");
                
                // Cari cell terdekat yang kosong
                Vector3Int freeCell = FindNearestFreeCellForPatrol(cell);
                Vector3 newPos = tilemap.GetCellCenterWorld(freeCell);
                
                patrolPoints[i].position = newPos;
                Debug.Log($"[Enemy {gameObject.name}] Moved patrol point {i} from {originalPos} to {newPos}");
            }
            else
            {
                // Snap ke center cell untuk consistency
                Vector3 snappedPos = tilemap.GetCellCenterWorld(cell);
                if (Vector3.Distance(originalPos, snappedPos) > 0.1f)
                {
                    patrolPoints[i].position = snappedPos;
                    Debug.Log($"[Enemy {gameObject.name}] Snapped patrol point {i} to grid center: {snappedPos}");
                }
            }
        }
    }

    /// <summary>
    /// Cari cell terdekat yang tidak ada obstacle
    /// </summary>
    private Vector3Int FindNearestFreeCellForPatrol(Vector3Int blockedCell)
    {
        Tilemap tilemap = GridPathfinding.Instance.obstacleTilemap;
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        
        queue.Enqueue(blockedCell);
        visited.Add(blockedCell);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            
            if (!tilemap.HasTile(current))
            {
                return current;
            }

            // Check 4 directions
            Vector3Int[] neighbors = new Vector3Int[]
            {
                current + Vector3Int.up,
                current + Vector3Int.down,
                current + Vector3Int.left,
                current + Vector3Int.right
            };

            foreach (Vector3Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return blockedCell; // Fallback
    }

    private void Update()
    {
        UpdateFOV();

        checkTimer += Time.deltaTime;
        if (checkTimer >= CHECK_INTERVAL)
        {
            checkTimer = 0f;
            if (state != State.Catch && CanSeePlayer())
                ChangeState(State.Catch);
        }

        switch (state)
        {
            case State.Patrol: HandlePatrol(); break;
            case State.Waiting: HandleWaiting(); break;
            case State.Investigate: HandleInvestigate(); break;
            case State.Alert: HandleAlert(); break;
            case State.Catch: HandleCatch(); break;
        }
    }

    /* ================= NOISE ================= */

    private void HandleNoise(NoiseInfo noise)
    {
        if (state == State.Catch) return;
        if (!string.IsNullOrEmpty(noise.roomID) && noise.roomID != roomID) return;

        float distance = Vector2.Distance(transform.position, noise.position);
        float maxDist = noise.type == NoiseType.DogBark ? barkHearingDistance : puzzleHearingDistance;
        if (distance > maxDist) return;

        if (noise.type == NoiseType.DogBark)
        {
            lastBarkTime = Time.time;
            alertDirection = (noise.position - (Vector2)transform.position).normalized;
            ChangeState(State.Alert);
            return;
        }

        if (noise.type == NoiseType.Puzzle)
        {
            if (Time.time - lastBarkTime <= barkBlockWindow) return;
            if (state == State.Alert) return;

            investigateTarget = noise.position;
            GeneratePath(investigateTarget);
            ChangeState(State.Investigate);
        }
    }

    /* ================= STATES ================= */

    private void ChangeState(State newState)
    {
        if (state == newState) return;

        // Simpan posisi patrol saat keluar dari patrol
        if (state == State.Patrol && GridPathfinding.Instance != null)
        {
            lastPatrolCell = GridPathfinding.Instance.obstacleTilemap
                .WorldToCell(transform.position);
        }

        state = newState;
        stateTimer = 0f;

        if (newState == State.Catch)
        {
            losePlayerTimer = 0f;
            lastChasePathUpdate = chasePathUpdateInterval;
        }
    }

    private void HandlePatrol()
    {
        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning($"[Enemy {gameObject.name}] No patrol points assigned!");
            return;
        }

        // Pastikan ada path yang valid
        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.Log($"[Enemy {gameObject.name}] No path, generating new path to patrol point {patrolIndex}");
            GeneratePath(patrolPoints[patrolIndex].position);
            
            // Jika masih gagal generate path, skip ke patrol point berikutnya
            if (currentPath == null || currentPath.Count == 0)
            {
                Debug.LogError($"[Enemy {gameObject.name}] Failed to generate path! Skipping to next patrol point.");
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                stateTimer = 0f;
            }
            return;
        }

        // Follow path menggunakan pathfinding
        if (currentPathIndex < currentPath.Count)
        {
            Vector2 targetWaypoint = currentPath[currentPathIndex];
            MoveTowards(targetWaypoint);

            // Check apakah sudah sampai waypoint
            if (Vector2.Distance(transform.position, targetWaypoint) <= WAYPOINT_REACH_DISTANCE)
            {
                currentPathIndex++;
                
                // Jika sudah sampai akhir path (patrol point)
                if (currentPathIndex >= currentPath.Count)
                {
                    Debug.Log($"[Enemy {gameObject.name}] Reached patrol point {patrolIndex}");
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    ChangeState(State.Waiting);
                }
            }
        }
        else
        {
            // Path habis tapi belum sampai, regenerate path
            Debug.LogWarning($"[Enemy {gameObject.name}] Path exhausted, regenerating...");
            GeneratePath(patrolPoints[patrolIndex].position);
        }
    }

    private void HandleWaiting()
    {
        stateTimer += Time.deltaTime;
        animator?.SetBool(animIsMovingParam, false);

        if (fovVisual != null)
            fovVisual.transform.Rotate(0, 0, rotationSpeed * rotationDirection * Time.deltaTime);

        if (stateTimer >= waitTime)
        {
            // Generate path ke patrol point berikutnya
            GeneratePath(patrolPoints[patrolIndex].position);
            ChangeState(State.Patrol);
        }
    }

    private void HandleInvestigate()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            // Tidak ada path, tunggu sebentar lalu return
            stateTimer += Time.deltaTime;
            animator?.SetBool(animIsMovingParam, false);
            
            if (stateTimer >= investigateWaitTime)
                ReturnToPatrol();
            return;
        }

        // Follow path
        if (currentPathIndex < currentPath.Count)
        {
            Vector2 targetWaypoint = currentPath[currentPathIndex];
            MoveTowards(targetWaypoint);

            if (Vector2.Distance(transform.position, targetWaypoint) <= WAYPOINT_REACH_DISTANCE)
            {
                currentPathIndex++;
            }
        }
        else
        {
            // Sampai di tujuan investigate, tunggu sebentar
            stateTimer += Time.deltaTime;
            animator?.SetBool(animIsMovingParam, false);
            
            if (stateTimer >= investigateWaitTime)
                ReturnToPatrol();
        }
    }

    private void HandleAlert()
    {
        stateTimer += Time.deltaTime;
        animator?.SetBool(animIsMovingParam, false);
        fovVisual?.transform.Rotate(0, 0, alertRotationSpeed * Time.deltaTime);

        if (stateTimer >= alertDuration)
            ReturnToPatrol();
    }

    private void HandleCatch()
    {
        if (player == null || !player.isAlive) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        
        // Tangkap player jika sudah dekat
        if (dist <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        // Terlalu jauh, return to patrol
        if (dist > maxChaseDistance)
        {
            ReturnToPatrol();
            return;
        }

        // Update lose player timer
        losePlayerTimer = CanSeePlayer() ? 0f : losePlayerTimer + Time.deltaTime;
        if (losePlayerTimer >= losePlayerTimeout)
        {
            ReturnToPatrol();
            return;
        }

        // Update path secara berkala
        lastChasePathUpdate += Time.deltaTime;
        if (lastChasePathUpdate >= chasePathUpdateInterval)
        {
            GeneratePath(player.transform.position);
            lastChasePathUpdate = 0f;
        }

        // Follow path ke player
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector2 targetWaypoint = currentPath[currentPathIndex];
            MoveTowards(targetWaypoint);

            if (Vector2.Distance(transform.position, targetWaypoint) <= WAYPOINT_REACH_DISTANCE)
            {
                currentPathIndex++;
            }
        }
    }

    /* ================= MOVEMENT ================= */

    private void MoveTowards(Vector2 target)
    {
        Vector2 pos = rb.position;
        Vector2 dir = (target - pos).normalized;
        
        // Move using rigidbody
        rb.MovePosition(Vector2.MoveTowards(pos, target, moveSpeed * Time.deltaTime));
        
        // Update animation dan FOV
        UpdateAnimation(dir, true);
        UpdateFOVDirection(dir);
    }

    /* ================= HELPERS ================= */

    private void GeneratePath(Vector2 target)
    {
        if (GridPathfinding.Instance == null)
        {
            Debug.LogError("[Enemy] GridPathfinding instance not found!");
            currentPath = null;
            return;
        }

        // PENTING: Snap target ke grid center untuk menghindari posisi di antara cell
        Vector3Int targetCell = GridPathfinding.Instance.obstacleTilemap.WorldToCell(target);
        Vector2 snappedTarget = GridPathfinding.Instance.obstacleTilemap.GetCellCenterWorld(targetCell);
        
        Debug.Log($"[Enemy] Original target: {target}, Snapped to: {snappedTarget} (cell: {targetCell})");

        currentPath = GridPathfinding.Instance.FindPath(transform.position, snappedTarget);
        currentPathIndex = 0;

        // Debug visualisasi (opsional)
        if (currentPath != null && currentPath.Count > 0)
        {
            GridPathfinding.Instance.SetDebugPath(currentPath);
            Debug.Log($"[Enemy] Generated path with {currentPath.Count} waypoints");
        }
        else
        {
            Debug.LogWarning($"[Enemy] Failed to generate path to {snappedTarget}");
        }
    }

    private void ReturnToPatrol()
    {
        if (GridPathfinding.Instance != null)
        {
            // Kembali ke posisi patrol terakhir
            Vector2 pos = GridPathfinding.Instance.obstacleTilemap
                .GetCellCenterWorld(lastPatrolCell);
            GeneratePath(pos);
            ChangeState(State.Investigate);
        }
        else
        {
            // Fallback jika tidak ada pathfinding
            ChangeState(State.Patrol);
            if (patrolPoints.Length > 0)
                GeneratePath(patrolPoints[patrolIndex].position);
        }
    }

    private void UpdateAnimation(Vector2 dir, bool moving)
    {
        if (animator == null) return;

        animator.SetBool(animIsMovingParam, moving);
        
        if (dir.magnitude > 0.01f)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                animator.SetFloat(animHorizontalParam, Mathf.Sign(dir.x));
                animator.SetFloat(animVerticalParam, 0);
            }
            else
            {
                animator.SetFloat(animVerticalParam, Mathf.Sign(dir.y));
                animator.SetFloat(animHorizontalParam, 0);
            }
        }
    }

    private void UpdateFOVDirection(Vector2 dir)
    {
        if (fovVisual != null && dir.magnitude > 0.01f)
            fovVisual.SetAimDirection(dir);
    }

    private void UpdateFOV()
    {
        if (fovVisual != null)
            fovVisual.SetOrigin(transform.position);
    }

    private bool CanSeePlayer()
    {
        if (player == null || player.isHidden) return false;

        Vector2 dir = player.transform.position - transform.position;
        if (dir.magnitude > viewDistance) return false;

        // Check FOV angle
        Vector2 facingDir = fovVisual != null ? 
            (Vector2)fovVisual.transform.right : Vector2.right;
        
        if (Vector2.Angle(facingDir, dir) > fov * 0.5f) return false;

        // Raycast check untuk obstacle
        return !Physics2D.Raycast(transform.position, dir.normalized, dir.magnitude, obstacleMask);
    }

    private void CatchPlayer()
    {
        player.isAlive = false;
        Time.timeScale = 0f;
        Debug.Log("[Enemy] Player caught!");
    }

    // Gizmos untuk debug
    private void OnDrawGizmos()
    {
        // Draw patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                    
                    // Draw line ke next point
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, 
                                       patrolPoints[nextIndex].position);
                    }
                }
            }
        }

        // Draw current path
        if (Application.isPlaying && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            
            // Draw current waypoint target
            if (currentPathIndex < currentPath.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(currentPath[currentPathIndex], 0.2f);
            }
        }
    }
}