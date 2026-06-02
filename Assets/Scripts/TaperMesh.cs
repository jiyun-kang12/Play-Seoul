using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class TaperMesh : MonoBehaviour
{
    public enum TaperPivot
    {
        Center,
        Left,
        Right
    }

    [Header("Taper Settings")]
    [Range(0.1f, 2f)]
    public float endWidthRatio = 0.5f;

    public TaperPivot pivot = TaperPivot.Center;
    public bool reverseDirection = false;

    [Header("Actions")]
    public bool apply = false;
    public bool saveMeshAsset = false;

    void Update()
    {
        if (apply)
        {
            apply = false;
            ApplyTaper(false);
        }

        if (saveMeshAsset)
        {
            saveMeshAsset = false;
            ApplyTaper(true);
        }
    }

    void ApplyTaper(bool saveAsset)
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        if (mf == null)
        {
            Debug.LogWarning("MeshFilter가 없습니다.", this);
            return;
        }

        if (mf.sharedMesh == null)
        {
            Debug.LogWarning("Mesh가 없습니다.", this);
            return;
        }

        Mesh sourceMesh = mf.sharedMesh;
        Mesh mesh = Instantiate(sourceMesh);
        mesh.name = sourceMesh.name + "_Tapered";

        Vector3[] vertices = mesh.vertices;

        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        foreach (Vector3 v in vertices)
        {
            minZ = Mathf.Min(minZ, v.z);
            maxZ = Mathf.Max(maxZ, v.z);
            minX = Mathf.Min(minX, v.x);
            maxX = Mathf.Max(maxX, v.x);
        }

        float pivotX;

        switch (pivot)
        {
            case TaperPivot.Left:
                pivotX = minX;
                break;

            case TaperPivot.Right:
                pivotX = maxX;
                break;

            default:
                pivotX = (minX + maxX) * 0.5f;
                break;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            float t = reverseDirection
                ? Mathf.InverseLerp(maxZ, minZ, vertices[i].z)
                : Mathf.InverseLerp(minZ, maxZ, vertices[i].z);

            float scale = Mathf.Lerp(1f, endWidthRatio, t);

            vertices[i].x = pivotX + (vertices[i].x - pivotX) * scale;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

#if UNITY_EDITOR
        if (saveAsset)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/" + mesh.name + ".asset"
            );

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            Undo.RecordObject(mf, "Assign Tapered Mesh");

            mf.sharedMesh = savedMesh;

            EditorUtility.SetDirty(mf);
            EditorUtility.SetDirty(gameObject);

            PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);

            Debug.Log("Saved Tapered Mesh Asset: " + path, this);

            return;
        }
#endif

        mf.sharedMesh = mesh;

#if UNITY_EDITOR
        EditorUtility.SetDirty(mf);
        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}