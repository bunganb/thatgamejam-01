using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridPathfinding : MonoBehaviour
{
    [Header("Tilemap References")]
    [Tooltip("Tilemap yang berisi obstacle/wall")]
    public Tilemap obstacleTilemap;
    
    [Header("Pathfinding Settings")]
    [Tooltip("Maksimal node yang dicek (prevent infinite loop)")]
    public int maxIterations = 1000;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private static GridPathfinding instance;
    public static GridPathfinding Instance => instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Cari path dari start ke target menggunakan A* algorithm
    /// </summary>
    public List<Vector2> FindPath(Vector2 startWorld, Vector2 targetWorld)
    {
        if (obstacleTilemap == null)
        {
            Debug.LogError("[Pathfinding] No obstacle tilemap assigned!");
            return new List<Vector2> { targetWorld };
        }

        if (showDebugLogs)
            Debug.Log($"[Pathfinding] Finding path from {startWorld} to {targetWorld}");

        // Convert world position ke grid cell
        Vector3Int startCell = obstacleTilemap.WorldToCell(startWorld);
        Vector3Int targetCell = obstacleTilemap.WorldToCell(targetWorld);

        if (showDebugLogs)
            Debug.Log($"[Pathfinding] Start cell: {startCell}, Target cell: {targetCell}");

        // Check jika start adalah obstacle (shouldn't happen, tapi safety check)
        if (IsObstacle(startCell))
        {
            Debug.LogWarning("[Pathfinding] Start position is inside obstacle! Finding nearest free cell...");
            startCell = FindNearestFreeCell(startCell, 10);
            if (showDebugLogs)
                Debug.Log($"[Pathfinding] New start cell: {startCell}");
        }

        // Check jika target adalah obstacle
        if (IsObstacle(targetCell))
        {
            Debug.LogWarning("[Pathfinding] Target is inside obstacle! Finding nearest free cell...");
            targetCell = FindNearestFreeCell(targetCell, 10);
            if (showDebugLogs)
                Debug.Log($"[Pathfinding] New target cell: {targetCell}");
        }

        // Double check: jika masih obstacle, return fallback
        if (IsObstacle(startCell) || IsObstacle(targetCell))
        {
            Debug.LogError("[Pathfinding] Cannot find valid start or target cell!");
            return new List<Vector2> { targetWorld };
        }

        // A* algorithm
        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, PathNode> allNodes = new Dictionary<Vector3Int, PathNode>();

        PathNode startNode = new PathNode(startCell, null, 0, GetDistance(startCell, targetCell));
        openList.Add(startNode);
        allNodes[startCell] = startNode;

        int iterations = 0;

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Ambil node dengan F cost terendah
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost ||
                    (openList[i].FCost == currentNode.FCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);

            // Sampai di target
            if (currentNode.position == targetCell)
            {
                if (showDebugLogs)
                    Debug.Log($"[Pathfinding] ✓ Path found! {iterations} iterations");
                return RetracePath(startNode, currentNode);
            }

            // Check semua neighbor (4 directions: up, down, left, right)
            foreach (Vector3Int neighbor in GetNeighbors(currentNode.position))
            {
                // Skip jika sudah di closed set atau ada obstacle
                if (closedSet.Contains(neighbor) || IsObstacle(neighbor))
                    continue;

                float newGCost = currentNode.gCost + GetDistance(currentNode.position, neighbor);

                PathNode neighborNode;
                if (!allNodes.TryGetValue(neighbor, out neighborNode))
                {
                    neighborNode = new PathNode(neighbor, currentNode, newGCost, GetDistance(neighbor, targetCell));
                    allNodes[neighbor] = neighborNode;
                    openList.Add(neighborNode);
                }
                else if (newGCost < neighborNode.gCost)
                {
                    neighborNode.gCost = newGCost;
                    neighborNode.parent = currentNode;

                    if (!openList.Contains(neighborNode))
                        openList.Add(neighborNode);
                }
            }
        }

        // Tidak ada path ditemukan
        Debug.LogError($"[Pathfinding] ✗ No path found from {startWorld} to {targetWorld} after {iterations} iterations");
        
        // Return direct path sebagai fallback
        List<Vector2> fallbackPath = new List<Vector2>();
        fallbackPath.Add(obstacleTilemap.GetCellCenterWorld(targetCell));
        return fallbackPath;
    }

    private List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            cell + Vector3Int.up,
            cell + Vector3Int.down,
            cell + Vector3Int.left,
            cell + Vector3Int.right,
            
            // Uncomment untuk diagonal movement
            // cell + new Vector3Int(1, 1, 0),
            // cell + new Vector3Int(-1, 1, 0),
            // cell + new Vector3Int(1, -1, 0),
            // cell + new Vector3Int(-1, -1, 0)
        };

        return neighbors;
    }

    private bool IsObstacle(Vector3Int cell)
    {
        return obstacleTilemap.HasTile(cell);
    }

    private Vector3Int FindNearestFreeCell(Vector3Int blockedCell, int maxRadius = 10)
    {
        // BFS untuk cari cell kosong terdekat
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        
        queue.Enqueue(blockedCell);
        visited.Add(blockedCell);

        int searchRadius = 0;

        while (queue.Count > 0 && searchRadius < maxRadius)
        {
            int levelSize = queue.Count;
            searchRadius++;

            for (int i = 0; i < levelSize; i++)
            {
                Vector3Int current = queue.Dequeue();
                
                // Cek apakah cell ini kosong
                if (!IsObstacle(current))
                {
                    Debug.Log($"[Pathfinding] Found free cell at {current} (distance: {searchRadius})");
                    return current;
                }

                // Add neighbors
                foreach (Vector3Int neighbor in GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Fallback: return original cell
        Debug.LogWarning($"[Pathfinding] No free cell found within radius {maxRadius}!");
        return blockedCell;
    }

    private float GetDistance(Vector3Int a, Vector3Int b)
    {
        // Manhattan distance (untuk 4-directional movement)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        
        // Euclidean distance (untuk 8-directional/diagonal)
        // return Vector3Int.Distance(a, b);
    }

    private List<Vector2> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector2> path = new List<Vector2>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            Vector2 worldPos = obstacleTilemap.GetCellCenterWorld(currentNode.position);
            path.Add(worldPos);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        if (showDebugLogs)
            Debug.Log($"[Pathfinding] Raw path has {path.Count} waypoints");

        // Simplify path (remove unnecessary waypoints)
        path = SimplifyPath(path);
        
        if (showDebugLogs)
            Debug.Log($"[Pathfinding] Simplified path has {path.Count} waypoints");

        return path;
    }

    private List<Vector2> SimplifyPath(List<Vector2> path)
    {
        if (path.Count <= 2) return path;

        List<Vector2> simplified = new List<Vector2>();
        simplified.Add(path[0]);

        Vector2 previousDirection = Vector2.zero;

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 direction = (path[i + 1] - path[i]).normalized;

            // Hanya add jika direction berubah
            if (Vector2.Dot(direction, previousDirection) < 0.99f) // Toleransi kecil
            {
                simplified.Add(path[i]);
                previousDirection = direction;
            }
        }

        simplified.Add(path[path.Count - 1]);
        return simplified;
    }

    // Visualisasi path di editor
    private List<Vector2> debugPath;

    public void SetDebugPath(List<Vector2> path)
    {
        debugPath = path;
    }

    private void OnDrawGizmos()
    {
        if (debugPath == null || debugPath.Count == 0) return;

        // Draw path lines
        Gizmos.color = Color.cyan;
        for (int i = 0; i < debugPath.Count - 1; i++)
        {
            Gizmos.DrawLine(debugPath[i], debugPath[i + 1]);
        }

        // Draw waypoints
        for (int i = 0; i < debugPath.Count; i++)
        {
            Gizmos.color = (i == 0) ? Color.green : (i == debugPath.Count - 1) ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(debugPath[i], 0.15f);
            
            #if UNITY_EDITOR
            Vector3 labelPos = new Vector3(debugPath[i].x, debugPath[i].y + 0.3f, 0);
            UnityEditor.Handles.Label(labelPos, $"{i}", 
                new GUIStyle() { 
                    normal = new GUIStyleState() { textColor = Color.white },
                    fontSize = 10
                });
            #endif
        }
    }
}

/// <summary>
/// Node class untuk A* pathfinding
/// </summary>
public class PathNode
{
    public Vector3Int position;
    public PathNode parent;
    public float gCost; // Distance from start
    public float hCost; // Distance to target (heuristic)
    public float FCost => gCost + hCost; // Total cost

    public PathNode(Vector3Int pos, PathNode parent, float gCost, float hCost)
    {
        this.position = pos;
        this.parent = parent;
        this.gCost = gCost;
        this.hCost = hCost;
    }
}