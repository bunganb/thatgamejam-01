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

    private Mesh mesh;
    private Vector3 originWorld;
    private float startingAngle;

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "FOV Mesh";
        GetComponent<MeshFilter>().mesh = mesh;

        // Biar aman: kalau belum ada material, kasih default sederhana
        var mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            // shader ini biasanya ada di Unity built-in
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            mr.sharedMaterial.color = new Color(1f, 1f, 1f, 0.25f); // putih transparan
        }
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
}
