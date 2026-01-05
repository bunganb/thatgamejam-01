using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public enum State { Patrol, Waiting, Investigate, Alert, Catch }
    public State state = State.Patrol;

    [Header("Player")]
    public PlayerMovement player;

    [Header("Room/Level System")]
    [Tooltip("ID room/level tempat enemy berada (harus sama dengan puzzle/noise di room ini)")]
    public string roomID = "Room1";

    [Header("FOV Visual")]
    [SerializeField] private GameObject fovPrefab;
    private FieldOfView2D fovVisual;

    [Header("Movement")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public float waitTime = 1.5f;
    public float rotationSpeed = 90f; // Rotation speed untuk Waiting state
    [Tooltip("Gunakan pathfinding untuk patrol movement?")]
    public bool usePathfindingForPatrol = true;

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
    public float investigateStopDistance = 0.5f;
    public float investigateWaitTime = 2f;
    public float barkBlockWindow = 0.4f;
    [Tooltip("Gunakan pathfinding untuk navigate ke target?")]
    public bool usePathfinding = true;
    [Tooltip("Offset jarak dari target noise (agar tidak menumpuk di puzzle)")]
    public float investigateOffset = 1.5f;

    private Vector2 investigateTarget;
    private List<Vector2> currentPath;
    private int currentPathIndex;
    private float lastBarkTime = -999f;
    private Vector3Int lastPatrolCell; // Simpan posisi terakhir saat patrol
    private bool isReturningToPatrol; // Flag untuk cek apakah sedang kembali

    [Header("Catch (Chase Player)")]
    [Tooltip("Interval untuk recalculate path saat chase (detik)")]
    public float chasePathUpdateInterval = 0.5f;
    [Tooltip("Jarak untuk consider player tertangkap")]
    public float catchDistance = 0.5f;
    [Tooltip("Jarak maksimal chase (jika player lebih jauh, enemy return to patrol)")]
    public float maxChaseDistance = 20f;
    [Tooltip("Waktu tunggu setelah player hilang dari view sebelum return (detik)")]
    public float losePlayerTimeout = 2f;
    
    private float lastChasePathUpdate;
    private float losePlayerTimer; // Timer saat player hilang dari pandangan
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

        // CRITICAL: Check room ID - hanya dengar noise dari room yang sama
        if (!string.IsNullOrEmpty(noise.roomID) && !string.IsNullOrEmpty(roomID))
        {
            if (noise.roomID != roomID)
            {
                Debug.Log($"[Enemy] Ignored noise from different room: {noise.roomID} (my room: {roomID})");
                return;
            }
        }

        float distance = Vector2.Distance(transform.position, noise.position);
        
        // Tentukan hearing distance berdasarkan tipe noise
        float maxHearingDistance = noise.type == NoiseType.DogBark ? barkHearingDistance : puzzleHearingDistance;
        
        // Check hearing distance
        if (distance > maxHearingDistance)
        {
            Debug.Log($"[Enemy] Noise too far: {distance:F2} > {maxHearingDistance:F2}");
            return;
        }

        Debug.Log($"[Enemy] Heard {noise.type} at distance {distance:F2} in room {roomID}");

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

            // Cari posisi investigate terbaik di sekitar puzzle
            investigateTarget = FindBestInvestigatePosition(noise.position);
            
            Debug.Log($"[Enemy] Investigate target: {investigateTarget} (puzzle at {noise.position})");
            
            // Generate path jika pathfinding enabled
            if (usePathfinding && GridPathfinding.Instance != null)
            {
                Debug.Log("[Enemy] Using pathfinding to investigate");
                currentPath = GridPathfinding.Instance.FindPath(transform.position, investigateTarget);
                currentPathIndex = 0;
                
                if (currentPath != null && currentPath.Count > 0)
                {
                    Debug.Log($"[Enemy] Path generated with {currentPath.Count} waypoints");
                    
                    // Debug visualization
                    GridPathfinding.Instance.SetDebugPath(currentPath);
                }
                else
                {
                    Debug.LogWarning("[Enemy] Path generation failed! Falling back to direct movement");
                    currentPath = null;
                }
            }
            else
            {
                if (!usePathfinding)
                {
                    Debug.Log("[Enemy] Pathfinding disabled, using direct movement");
                }
                else if (GridPathfinding.Instance == null)
                {
                    Debug.LogError("[Enemy] GridPathfinding.Instance is NULL! Make sure PathfindingManager exists in scene");
                }
                currentPath = null;
            }
            
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
            case State.Patrol:
                // Simpan posisi terakhir saat masih patrol
                if (GridPathfinding.Instance != null && GridPathfinding.Instance.obstacleTilemap != null)
                {
                    lastPatrolCell = GridPathfinding.Instance.obstacleTilemap.WorldToCell(transform.position);
                }
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

            case State.Catch:
                Debug.Log($"[Enemy] Chasing player!");
                isReturningToPatrol = false;
                lastChasePathUpdate = 0f; // Reset timer untuk immediate path generation
                losePlayerTimer = 0f; // Reset lose player timer
                break;
        }
    }

    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Vector2 targetPos = patrolPoints[patrolIndex].position;
        Vector2 currentPos = transform.position;

        // Gunakan pathfinding untuk patrol jika enabled
        if (usePathfindingForPatrol && usePathfinding && GridPathfinding.Instance != null)
        {
            // Generate path ke patrol point jika belum ada
            if (currentPath == null || currentPath.Count == 0 || !isReturningToPatrol)
            {
                // Check jika ini first time atau baru ganti target patrol point
                bool needNewPath = currentPath == null || currentPath.Count == 0;
                
                // Atau jika path yang ada bukan menuju patrol point saat ini
                if (currentPath != null && currentPath.Count > 0)
                {
                    Vector2 pathEnd = currentPath[currentPath.Count - 1];
                    float distToTarget = Vector2.Distance(pathEnd, targetPos);
                    if (distToTarget > 0.5f)
                    {
                        needNewPath = true;
                    }
                }

                if (needNewPath)
                {
                    currentPath = GridPathfinding.Instance.FindPath(currentPos, targetPos);
                    currentPathIndex = 0;
                    isReturningToPatrol = false;

                    if (currentPath != null && currentPath.Count > 0)
                    {
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"[Enemy] Patrol path to point {patrolIndex}: {currentPath.Count} waypoints");
                        }
                        GridPathfinding.Instance.SetDebugPath(currentPath);
                    }
                    else
                    {
                        Debug.LogWarning($"[Enemy] Failed to generate patrol path to point {patrolIndex}, using direct movement");
                        currentPath = null;
                    }
                }
            }

            // Follow path jika ada
            if (currentPath != null && currentPath.Count > 0)
            {
                if (currentPathIndex < currentPath.Count)
                {
                    Vector2 targetWaypoint = currentPath[currentPathIndex];
                    float distanceToWaypoint = Vector2.Distance(currentPos, targetWaypoint);

                    if (distanceToWaypoint <= 0.3f)
                    {
                        currentPathIndex++;
                    }
                    else
                    {
                        Vector2 direction = (targetWaypoint - currentPos).normalized;
                        if (direction.sqrMagnitude > 0.01f)
                        {
                            FaceDirection(direction);
                        }
                        MoveTowards(targetWaypoint);
                    }
                }
                
                // Check apakah sudah sampai di patrol point
                float distToPatrolPoint = Vector2.Distance(currentPos, targetPos);
                if (distToPatrolPoint < 0.2f)
                {
                    Debug.Log($"[Enemy] Reached patrol point {patrolIndex}");
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    currentPath = null; // Clear path untuk generate baru ke next point
                    ChangeState(State.Waiting);
                }
            }
            else
            {
                // Fallback ke direct movement jika path gagal
                DirectPatrolMovement(targetPos, currentPos);
            }
        }
        else
        {
            // Direct patrol movement (tanpa pathfinding)
            DirectPatrolMovement(targetPos, currentPos);
        }
    }

    private void DirectPatrolMovement(Vector2 targetPos, Vector2 currentPos)
    {
        // Hadapkan ke target sebelum bergerak
        Vector2 direction = (targetPos - currentPos).normalized;
        if (direction.sqrMagnitude > 0.01f)
        {
            FaceDirection(direction);
        }
        
        MoveTowards(targetPos);

        if (Vector2.Distance(currentPos, targetPos) < 0.1f)
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
        
        // Jika pakai pathfinding dan ada path
        if (usePathfinding && currentPath != null && currentPath.Count > 0)
        {
            // Cek apakah sudah sampai di akhir path
            if (currentPathIndex >= currentPath.Count)
            {
                // Sudah sampai di akhir path
                if (isReturningToPatrol)
                {
                    // Sudah kembali ke posisi patrol, lanjutkan patrol
                    Debug.Log("[Enemy] Returned to patrol position, resuming patrol");
                    currentPath = null;
                    isReturningToPatrol = false;
                    ChangeState(State.Patrol);
                    return;
                }
                else
                {
                    // Sampai di lokasi investigate, tunggu sebentar
                    stateTimer += Time.deltaTime;
                    
                    if (Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[Enemy] At investigate location, waiting... {stateTimer:F1}/{investigateWaitTime}");
                    }

                    // Rotasi pelan saat waiting
                    transform.Rotate(0, 0, 30f * Time.deltaTime);

                    if (stateTimer >= investigateWaitTime)
                    {
                        Debug.Log("[Enemy] Investigation complete, returning to last patrol position");
                        ReturnToPatrol();
                    }
                    return;
                }
            }

            // Masih ada waypoint yang harus dituju
            Vector2 targetWaypoint = currentPath[currentPathIndex];
            float distanceToWaypoint = Vector2.Distance(currentPos, targetWaypoint);

            // Debug setiap beberapa frame
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Enemy] Following path: waypoint {currentPathIndex}/{currentPath.Count}, distance: {distanceToWaypoint:F2}, returning: {isReturningToPatrol}");
            }

            if (distanceToWaypoint <= 0.3f)
            {
                // Pindah ke waypoint berikutnya
                currentPathIndex++;
                Debug.Log($"[Enemy] Reached waypoint {currentPathIndex - 1}, moving to next (now {currentPathIndex}/{currentPath.Count})");
            }
            else
            {
                // Bergerak ke waypoint saat ini
                Vector2 direction = (targetWaypoint - currentPos).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    FaceDirection(direction);
                }
                MoveTowards(targetWaypoint);
            }
        }
        else
        {
            // Fallback ke direct movement (tanpa pathfinding)
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
    }

    private void ReturnToPatrol()
    {
        if (usePathfinding && GridPathfinding.Instance != null && lastPatrolCell != Vector3Int.zero)
        {
            // Generate path kembali ke posisi patrol terakhir
            Vector2 lastPatrolWorld = GridPathfinding.Instance.obstacleTilemap.GetCellCenterWorld(lastPatrolCell);
            
            Debug.Log($"[Enemy] Generating return path to last patrol position: {lastPatrolWorld}");
            
            currentPath = GridPathfinding.Instance.FindPath(transform.position, lastPatrolWorld);
            currentPathIndex = 0;
            isReturningToPatrol = true; // Set flag
            
            if (currentPath != null && currentPath.Count > 0)
            {
                Debug.Log($"[Enemy] Return path generated with {currentPath.Count} waypoints");
                GridPathfinding.Instance.SetDebugPath(currentPath);
                
                // Reset timer untuk proses return
                stateTimer = 0f;
            }
            else
            {
                Debug.LogWarning("[Enemy] Failed to generate return path, switching to Patrol");
                currentPath = null;
                isReturningToPatrol = false;
                ChangeState(State.Patrol);
            }
        }
        else
        {
            // Fallback: langsung kembali ke state Patrol
            Debug.Log("[Enemy] No pathfinding or last position, switching to Patrol");
            currentPath = null;
            isReturningToPatrol = false;
            ChangeState(State.Patrol);
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

        Vector2 playerPos = player.transform.position;
        Vector2 currentPos = transform.position;
        float distanceToPlayer = Vector2.Distance(currentPos, playerPos);

        // Check apakah player tertangkap
        if (distanceToPlayer < catchDistance)
        {
            CatchPlayer();
            return;
        }

        // Check apakah player terlalu jauh (melebihi max chase distance)
        if (distanceToPlayer > maxChaseDistance)
        {
            Debug.Log($"[Enemy] Player too far ({distanceToPlayer:F1}m > {maxChaseDistance}m), returning to patrol");
            currentPath = null;
            ReturnToPatrolFromChase();
            return;
        }

        // Check apakah player masih terlihat
        bool canSeePlayerNow = CanSeePlayer();
        
        if (!canSeePlayerNow || player.isHidden)
        {
            // Player tidak terlihat, mulai hitung timer
            losePlayerTimer += Time.deltaTime;
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Enemy] Lost sight of player... {losePlayerTimer:F1}/{losePlayerTimeout}s");
            }

            if (losePlayerTimer >= losePlayerTimeout)
            {
                Debug.Log("[Enemy] Player lost for too long, returning to patrol");
                currentPath = null;
                ReturnToPatrolFromChase();
                return;
            }
            
            // Masih dalam timeout, terus chase ke posisi terakhir
        }
        else
        {
            // Player masih terlihat, reset timer
            losePlayerTimer = 0f;
        }

        // Pathfinding chase
        if (usePathfinding && GridPathfinding.Instance != null)
        {
            // Update path secara periodik (player bergerak terus)
            lastChasePathUpdate += Time.deltaTime;
            
            if (currentPath == null || lastChasePathUpdate >= chasePathUpdateInterval)
            {
                lastChasePathUpdate = 0f;
                
                // Generate path ke posisi player
                currentPath = GridPathfinding.Instance.FindPath(currentPos, playerPos);
                currentPathIndex = 0;
                
                if (currentPath != null && currentPath.Count > 0)
                {
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[Enemy] Chase path updated: {currentPath.Count} waypoints");
                    }
                    GridPathfinding.Instance.SetDebugPath(currentPath);
                }
                else
                {
                    Debug.LogWarning("[Enemy] Failed to generate chase path, using direct chase");
                    currentPath = null;
                }
            }

            // Follow path jika ada
            if (currentPath != null && currentPath.Count > 0)
            {
                if (currentPathIndex < currentPath.Count)
                {
                    Vector2 targetWaypoint = currentPath[currentPathIndex];
                    float distanceToWaypoint = Vector2.Distance(currentPos, targetWaypoint);

                    if (distanceToWaypoint <= 0.3f)
                    {
                        currentPathIndex++;
                    }
                    else
                    {
                        Vector2 direction = (targetWaypoint - currentPos).normalized;
                        if (direction.sqrMagnitude > 0.01f)
                        {
                            FaceDirection(direction);
                        }
                        MoveTowards(targetWaypoint);
                    }
                }
                else
                {
                    // Sampai akhir path tapi belum tangkap player
                    // Force update path di frame berikutnya
                    lastChasePathUpdate = chasePathUpdateInterval;
                }
            }
            else
            {
                // Fallback: direct chase jika path gagal
                DirectChasePlayer(playerPos, currentPos);
            }
        }
        else
        {
            // Tanpa pathfinding: direct chase
            DirectChasePlayer(playerPos, currentPos);
        }
    }

    private void ReturnToPatrolFromChase()
    {
        // Sama seperti return dari investigate, tapi dari chase
        if (usePathfinding && GridPathfinding.Instance != null && lastPatrolCell != Vector3Int.zero)
        {
            Vector2 lastPatrolWorld = GridPathfinding.Instance.obstacleTilemap.GetCellCenterWorld(lastPatrolCell);
            
            Debug.Log($"[Enemy] Returning to patrol from chase: {lastPatrolWorld}");
            
            currentPath = GridPathfinding.Instance.FindPath(transform.position, lastPatrolWorld);
            currentPathIndex = 0;
            isReturningToPatrol = true;
            
            if (currentPath != null && currentPath.Count > 0)
            {
                Debug.Log($"[Enemy] Return path generated with {currentPath.Count} waypoints");
                GridPathfinding.Instance.SetDebugPath(currentPath);
                
                // Ganti ke Investigate state untuk menggunakan return logic yang sudah ada
                ChangeState(State.Investigate);
            }
            else
            {
                Debug.LogWarning("[Enemy] Failed to generate return path from chase");
                currentPath = null;
                isReturningToPatrol = false;
                ChangeState(State.Patrol);
            }
        }
        else
        {
            Debug.Log("[Enemy] No pathfinding or last position, switching to Patrol");
            currentPath = null;
            isReturningToPatrol = false;
            ChangeState(State.Patrol);
        }
    }

    private void DirectChasePlayer(Vector2 playerPos, Vector2 currentPos)
    {
        Vector2 direction = (playerPos - currentPos).normalized;
        if (direction.sqrMagnitude > 0.01f)
        {
            FaceDirection(direction);
        }
        MoveTowards(playerPos);
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

    private Vector2 FindBestInvestigatePosition(Vector2 puzzlePos)
    {
        Vector2 enemyPos = transform.position;
        
        // Jika tidak ada pathfinding, return posisi puzzle langsung
        if (!usePathfinding || GridPathfinding.Instance == null || GridPathfinding.Instance.obstacleTilemap == null)
        {
            return puzzlePos;
        }

        Tilemap tilemap = GridPathfinding.Instance.obstacleTilemap;
        
        // Kandidat posisi: kiri, kanan, atas, bawah dari puzzle
        Vector2[] candidates = new Vector2[]
        {
            new Vector2(puzzlePos.x - investigateOffset, puzzlePos.y), // Kiri
            new Vector2(puzzlePos.x + investigateOffset, puzzlePos.y), // Kanan
            new Vector2(puzzlePos.x, puzzlePos.y + investigateOffset), // Atas
            new Vector2(puzzlePos.x, puzzlePos.y - investigateOffset), // Bawah
        };

        Vector2 bestPosition = puzzlePos;
        float bestScore = float.MaxValue;

        foreach (Vector2 candidate in candidates)
        {
            Vector3Int cell = tilemap.WorldToCell(candidate);
            
            // Skip jika posisi ada obstacle
            if (tilemap.HasTile(cell))
            {
                continue;
            }

            // Score = jarak dari enemy ke kandidat (lebih dekat = lebih baik)
            float distanceFromEnemy = Vector2.Distance(enemyPos, candidate);
            
            if (distanceFromEnemy < bestScore)
            {
                bestScore = distanceFromEnemy;
                bestPosition = candidate;
            }
        }

        // Jika semua kandidat terblokir, cari nearest free cell
        if (bestScore == float.MaxValue)
        {
            Debug.LogWarning("[Enemy] All investigate positions blocked, finding nearest free cell");
            Vector3Int puzzleCell = tilemap.WorldToCell(puzzlePos);
            Vector3Int freeCell = FindNearestFreeCell(puzzleCell, tilemap);
            bestPosition = tilemap.GetCellCenterWorld(freeCell);
        }

        return bestPosition;
    }

    private Vector3Int FindNearestFreeCell(Vector3Int startCell, Tilemap tilemap)
    {
        // BFS untuk cari cell kosong terdekat
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        
        queue.Enqueue(startCell);
        visited.Add(startCell);

        // Directions: kiri, kanan, atas, bawah
        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.up,
            Vector3Int.down
        };

        int maxIterations = 50; // Prevent infinite loop
        int iterations = 0;

        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector3Int current = queue.Dequeue();
            
            // Jika cell ini kosong, return
            if (!tilemap.HasTile(current))
            {
                return current;
            }

            // Check neighbors
            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = current + dir;
                
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Fallback: return start cell
        return startCell;
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

        // Draw current patrol path (pathfinding)
        if (state == State.Patrol)
        {
            if (currentPath != null && currentPath.Count > 0)
            {
                // Cyan path untuk patrol dengan pathfinding
                Gizmos.color = Color.cyan;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
                
                // Current waypoint
                if (currentPathIndex < currentPath.Count)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(currentPath[currentPathIndex], 0.15f);
                }
            }
            
            // Draw target patrol point
            if (patrolPoints != null && patrolIndex < patrolPoints.Length)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(patrolPoints[patrolIndex].position, 0.25f);
            }
        }

        // Draw investigate target
        if (state == State.Investigate)
        {
            // Draw path jika ada
            if (currentPath != null && currentPath.Count > 0)
            {
                // Warna berbeda untuk return path vs investigate path
                Gizmos.color = isReturningToPatrol ? Color.green : Color.magenta;
                
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
                
                // Highlight current waypoint
                if (currentPathIndex < currentPath.Count)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(currentPath[currentPathIndex], 0.2f);
                }
            }
            
            // Draw investigate target (hanya jika belum return)
            if (!isReturningToPatrol)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(investigateTarget, investigateStopDistance);
            }
            
            // Draw last patrol position
            if (lastPatrolCell != Vector3Int.zero && GridPathfinding.Instance != null)
            {
                Vector2 lastPatrolWorld = GridPathfinding.Instance.obstacleTilemap.GetCellCenterWorld(lastPatrolCell);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(lastPatrolWorld, 0.3f);
            }
        }

        // Draw chase path
        if (state == State.Catch)
        {
            if (currentPath != null && currentPath.Count > 0)
            {
                // Red path untuk chase
                Gizmos.color = Color.red;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
                
                // Current waypoint
                if (currentPathIndex < currentPath.Count)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                    Gizmos.DrawWireSphere(currentPath[currentPathIndex], 0.2f);
                }
            }
            
            // Draw line to player
            if (player != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, player.transform.position);
                
                // Warna berubah jika player hampir keluar dari max chase distance
                if (distToPlayer > maxChaseDistance * 0.8f)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange warning
                }
                else
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Transparent red
                }
                
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
            
            // Draw max chase distance circle
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxChaseDistance);
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