using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PolyShapeMeshFixer : MonoBehaviour
{
    [Header("Thickness")]
    public bool normalizeThickness = false;
    public float thickness = 0.05f;
    public bool bottomAtZero = true;

    [Header("UV")]
    public bool recalculateUV = true;

    [Tooltip("텍스처 1회 반복이 몇 Unity unit/meter인지")]
    public float textureWorldSize = 1f;

    [Tooltip("월드 좌표 기준 UV 사용")]
    public bool useWorldUV = true;

    [Header("Apply")]
    public bool apply = false;

    [Header("Bake")]
    [Tooltip("현재 오브젝트를 일반 Mesh로 굳히고 ProBuilder/PolyShape 컴포넌트 제거")]
    public bool bakeToRegularMesh = false;

    void Update()
    {
        if (apply)
        {
            apply = false;
            ApplyFix();
        }

#if UNITY_EDITOR
        if (bakeToRegularMesh)
        {
            bakeToRegularMesh = false;
            BakeToRegularMesh();
        }
#endif
    }

    void ApplyFix()
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("MeshFilter 또는 Mesh가 없습니다.");
            return;
        }

        Mesh mesh = Instantiate(mf.sharedMesh);
        mesh.name = gameObject.name + "_FixedMesh";

        Vector3[] vertices = mesh.vertices;

        if (normalizeThickness)
        {
            NormalizeYThickness(vertices);
            mesh.vertices = vertices;
        }

        if (recalculateUV)
        {
            RecalculateDirectionalUV(mesh, mesh.vertices);
        }

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        mf.sharedMesh = mesh;

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        EditorUtility.SetDirty(mf);
#endif

        Debug.Log("Apply 완료: 임시 Fixed Mesh 적용됨");
    }

#if UNITY_EDITOR
    void BakeToRegularMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("MeshFilter 또는 Mesh가 없습니다.");
            return;
        }

        Mesh bakedMesh = Instantiate(mf.sharedMesh);
        bakedMesh.name = gameObject.name + "_BakedMesh";

        Vector3[] vertices = bakedMesh.vertices;

        if (normalizeThickness)
        {
            NormalizeYThickness(vertices);
            bakedMesh.vertices = vertices;
        }

        if (recalculateUV)
        {
            RecalculateDirectionalUV(bakedMesh, bakedMesh.vertices);
        }

        bakedMesh.RecalculateBounds();
        bakedMesh.RecalculateNormals();

        mf.sharedMesh = bakedMesh;

        RemoveComponentIfExists("UnityEngine.ProBuilder.PolyShape");
        RemoveComponentIfExists("UnityEngine.ProBuilder.ProBuilderMesh");
        RemoveComponentIfExists("UnityEngine.ProBuilder.Entity");

        EditorUtility.SetDirty(gameObject);
        EditorUtility.SetDirty(mf);

        Debug.Log("Bake 완료: 현재 오브젝트가 일반 Mesh로 확정됨");
    }
#endif

    void NormalizeYThickness(Vector3[] vertices)
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            minY = Mathf.Min(minY, vertices[i].y);
            maxY = Mathf.Max(maxY, vertices[i].y);
        }

        float oldHeight = maxY - minY;

        if (oldHeight <= 0.0001f)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].y = bottomAtZero ? 0f : thickness;
            }

            return;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            float t = Mathf.InverseLerp(minY, maxY, vertices[i].y);

            if (bottomAtZero)
                vertices[i].y = Mathf.Lerp(0f, thickness, t);
            else
                vertices[i].y = Mathf.Lerp(-thickness, 0f, t);
        }
    }

    void RecalculateDirectionalUV(Mesh mesh, Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        float size = Mathf.Max(0.0001f, textureWorldSize);

        mesh.RecalculateNormals();
        Vector3[] normals = mesh.normals;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = useWorldUV
                ? transform.TransformPoint(vertices[i])
                : vertices[i];

            Vector3 n = useWorldUV
                ? transform.TransformDirection(normals[i]).normalized
                : normals[i].normalized;

            float ax = Mathf.Abs(n.x);
            float ay = Mathf.Abs(n.y);
            float az = Mathf.Abs(n.z);

            // 윗면 / 아랫면: XZ 기준
            if (ay >= ax && ay >= az)
            {
                uvs[i] = new Vector2(
                    p.x / size,
                    p.z / size
                );
            }
            // 앞면 / 뒷면: XY 기준
            else if (az >= ax && az >= ay)
            {
                uvs[i] = new Vector2(
                    p.x / size,
                    p.y / size
                );
            }
            // 좌면 / 우면: ZY 기준
            else
            {
                uvs[i] = new Vector2(
                    p.z / size,
                    p.y / size
                );
            }
        }

        mesh.uv = uvs;
    }

#if UNITY_EDITOR
    void RemoveComponentIfExists(string typeName)
    {
        Component[] components = GetComponents<Component>();

        foreach (Component c in components)
        {
            if (c == null) continue;

            if (c.GetType().FullName == typeName)
            {
                DestroyImmediate(c);
            }
        }
    }

    [ContextMenu("Print Mesh Info")]
    void PrintMeshInfo()
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.Log("Mesh 없음");
            return;
        }

        string path = AssetDatabase.GetAssetPath(mf.sharedMesh);

        Debug.Log("Mesh Name: " + mf.sharedMesh.name);

        if (string.IsNullOrEmpty(path))
            Debug.Log("저장된 Asset 아님. 씬 내부 Mesh임.");
        else
            Debug.Log("Mesh Asset Path: " + path);
    }
#endif
}