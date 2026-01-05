using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script untuk snap patrol points ke grid tilemap
/// Attach ke GameObject yang sama dengan Enemy
/// </summary>
public class PatrolPointSnapper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Tilemap yang digunakan sebagai referensi grid")]
    public Tilemap referenceTilemap;
    
    [Header("Patrol Points to Snap")]
    public Transform[] patrolPoints;
    
    [Header("Settings")]
    [Tooltip("Snap ke center cell atau ke corner?")]
    public bool snapToCenter = true;

#if UNITY_EDITOR
    [ContextMenu("Snap All Patrol Points to Grid")]
    public void SnapAllPatrolPoints()
    {
        if (referenceTilemap == null)
        {
            Debug.LogError("[PatrolPointSnapper] Reference tilemap is not assigned!");
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("[PatrolPointSnapper] No patrol points assigned!");
            return;
        }

        int snappedCount = 0;
        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;

            Vector3 originalPos = point.position;
            Vector3Int cell = referenceTilemap.WorldToCell(originalPos);
            
            Vector3 snappedPos;
            if (snapToCenter)
            {
                snappedPos = referenceTilemap.GetCellCenterWorld(cell);
            }
            else
            {
                snappedPos = referenceTilemap.CellToWorld(cell);
            }

            // Pertahankan Z position
            snappedPos.z = originalPos.z;
            
            Undo.RecordObject(point, "Snap Patrol Point");
            point.position = snappedPos;
            
            Debug.Log($"[PatrolPointSnapper] Snapped '{point.name}' from {originalPos} to {snappedPos} (cell: {cell})");
            snappedCount++;
        }

        Debug.Log($"[PatrolPointSnapper] Successfully snapped {snappedCount} patrol points!");
        EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("Auto-Find Patrol Points from Children")]
    public void AutoFindPatrolPoints()
    {
        // Cari semua child dengan nama mengandung "patrol" atau "waypoint"
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        System.Collections.Generic.List<Transform> foundPoints = new System.Collections.Generic.List<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child == transform) continue; // Skip self
            
            string nameLower = child.name.ToLower();
            if (nameLower.Contains("patrol") || nameLower.Contains("waypoint") || nameLower.Contains("point"))
            {
                foundPoints.Add(child);
            }
        }

        if (foundPoints.Count > 0)
        {
            patrolPoints = foundPoints.ToArray();
            Debug.Log($"[PatrolPointSnapper] Found {patrolPoints.Length} patrol points!");
        }
        else
        {
            Debug.LogWarning("[PatrolPointSnapper] No patrol points found in children!");
        }
    }

    [ContextMenu("Validate All Points (Check for Obstacles)")]
    public void ValidateAllPoints()
    {
        if (referenceTilemap == null)
        {
            Debug.LogError("[PatrolPointSnapper] Reference tilemap is not assigned!");
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("[PatrolPointSnapper] No patrol points assigned!");
            return;
        }

        int invalidCount = 0;
        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;

            Vector3Int cell = referenceTilemap.WorldToCell(point.position);
            bool hasObstacle = referenceTilemap.HasTile(cell);

            if (hasObstacle)
            {
                Debug.LogError($"[PatrolPointSnapper] ⚠️ '{point.name}' is on OBSTACLE at cell {cell}!", point);
                invalidCount++;
            }
            else
            {
                Debug.Log($"[PatrolPointSnapper] ✓ '{point.name}' is valid at cell {cell}", point);
            }
        }

        if (invalidCount > 0)
        {
            Debug.LogWarning($"[PatrolPointSnapper] Found {invalidCount} patrol points on obstacles!");
        }
        else
        {
            Debug.Log($"[PatrolPointSnapper] All {patrolPoints.Length} patrol points are valid!");
        }
    }
#endif

    // Visualisasi di Scene view
    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // Draw patrol points
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            Vector3 pos = patrolPoints[i].position;
            
            // Check if on obstacle
            bool isOnObstacle = false;
            if (referenceTilemap != null)
            {
                Vector3Int cell = referenceTilemap.WorldToCell(pos);
                isOnObstacle = referenceTilemap.HasTile(cell);
            }

            // Red if on obstacle, green if valid
            Gizmos.color = isOnObstacle ? Color.red : Color.green;
            Gizmos.DrawWireSphere(pos, 0.3f);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.2f);

            // Draw index number
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"P{i}", 
                new GUIStyle() { 
                    normal = new GUIStyleState() { 
                        textColor = isOnObstacle ? Color.red : Color.white 
                    },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                });
            #endif

            // Draw path line
            if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, patrolPoints[i + 1].position);
            }
        }

        // Draw return line to first point
        if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw grid overlay when selected
        if (referenceTilemap == null || patrolPoints == null) return;

        Gizmos.color = new Color(0, 1, 1, 0.3f);
        
        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;

            Vector3Int cell = referenceTilemap.WorldToCell(point.position);
            Vector3 cellCenter = referenceTilemap.GetCellCenterWorld(cell);
            Vector3 cellSize = referenceTilemap.cellSize;

            // Draw cell boundary
            Gizmos.DrawWireCube(cellCenter, cellSize);
        }
    }
}