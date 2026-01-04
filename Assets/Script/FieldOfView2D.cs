using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FieldOfView2D : MonoBehaviour
{
    [Header("View Settings")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Range(1, 360)] private float fov = 90f;
    [SerializeField] private float viewDistance = 8f;
    [SerializeField, Range(3, 300)] private int rayCount = 100;

    [Header("Origin (local offset)")]
    [SerializeField] private Vector3 originOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 10; // Tinggi agar di atas tilemap
    [SerializeField] private Color fovColor = new Color(1f, 1f, 0f, 0.2f); // Kuning transparan

    private Mesh mesh;
    private Vector3 originWorld;
    private float startingAngle;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "FOV Mesh";
        GetComponent<MeshFilter>().mesh = mesh;

        // Setup MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Buat material baru dengan shader yang support transparency
        Material fovMaterial = new Material(Shader.Find("Sprites/Default"));
        fovMaterial.color = fovColor;
        meshRenderer.sharedMaterial = fovMaterial;
        
        // PENTING: Set sorting layer dan order
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
        
        Debug.Log($"[FOV] Sorting Layer: {sortingLayerName}, Order: {sortingOrder}");
    }

    private void LateUpdate()
    {
        originWorld = transform.position + originOffset;

        float angle = startingAngle;
        float angleIncrease = fov / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2]; // 0 = origin, sisanya tepi
        int[] triangles = new int[rayCount * 3];

        // vertices pakai LOCAL SPACE mesh
        vertices[0] = transform.InverseTransformPoint(originWorld);

        int vertexIndex = 1;
        int triangleIndex = 0;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 dir = GetVectorFromAngle(angle);
            RaycastHit2D hit = Physics2D.Raycast(originWorld, dir, viewDistance, obstacleMask);

            Vector3 pointWorld = (hit.collider == null)
                ? originWorld + dir * viewDistance
                : (Vector3)hit.point;

            vertices[vertexIndex] = transform.InverseTransformPoint(pointWorld);

            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
                triangleIndex += 3;
            }

            vertexIndex++;
            angle -= angleIncrease;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    public void SetOrigin(Vector3 worldOrigin) => originWorld = worldOrigin;

    public void SetAimDirection(Vector3 aimDir)
    {
        // startingAngle itu sudut paling "kiri" cone
        startingAngle = GetAngleFromVector(aimDir) + fov / 2f;
    }

    public void SetFoV(float newFov) => fov = newFov;
    public void SetViewDistance(float dist) => viewDistance = dist;

    // Method untuk update color dari luar (opsional)
    public void SetColor(Color color)
    {
        fovColor = color;
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            meshRenderer.sharedMaterial.color = color;
        }
    }

    // Method untuk update sorting order dari luar (opsional)
    public void SetSortingOrder(int order)
    {
        sortingOrder = order;
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = order;
        }
    }

    // ===== Helpers =====
    private static Vector3 GetVectorFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private static float GetAngleFromVector(Vector3 dir)
    {
        dir.Normalize();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        return angle;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw FOV direction
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + originOffset;
        Vector3 aimDir = GetVectorFromAngle(startingAngle - fov / 2f);
        Gizmos.DrawRay(origin, aimDir * viewDistance);
    }
}