using UnityEngine;
using UnityEditor;

public class MapPhysicsSetupTool : EditorWindow
{
    private enum ColliderMode
    {
        FloorThinBox,
        NormalBox,
        MeshCollider
    }

    private ColliderMode colliderMode = ColliderMode.FloorThinBox;

    private bool includeInactive = true;
    private bool skipExistingCollider = true;
    private bool setStaticTogether = true;

    private float floorThickness = 0.2f;
    private float floorYOffset = -0.05f;

    private bool meshColliderConvex = false;

    [MenuItem("Tools/Map Tools/Map Physics Setup Tool")]
    public static void Open()
    {
        GetWindow<MapPhysicsSetupTool>("Map Physics Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Physics Setup Tool", EditorStyles.boldLabel);

        colliderMode = (ColliderMode)EditorGUILayout.EnumPopup("Collider Mode", colliderMode);

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        skipExistingCollider = EditorGUILayout.Toggle("Skip Existing Collider", skipExistingCollider);
        setStaticTogether = EditorGUILayout.Toggle("Set Static Together", setStaticTogether);

        GUILayout.Space(8);

        if (colliderMode == ColliderMode.FloorThinBox)
        {
            GUILayout.Label("Floor Thin Box Settings", EditorStyles.boldLabel);
            floorThickness = EditorGUILayout.FloatField("Floor Thickness", floorThickness);
            floorYOffset = EditorGUILayout.FloatField("Floor Y Offset", floorYOffset);
        }

        if (colliderMode == ColliderMode.MeshCollider)
        {
            GUILayout.Label("Mesh Collider Settings", EditorStyles.boldLabel);
            meshColliderConvex = EditorGUILayout.Toggle("Convex", meshColliderConvex);
        }

        GUILayout.Space(12);

        if (GUILayout.Button("Apply To Selected Hierarchy"))
            ApplyToSelected();

        if (GUILayout.Button("Remove Colliders From Selected Hierarchy"))
            RemoveColliders();

        if (GUILayout.Button("Set Selected Hierarchy Static Only"))
            SetStaticOnly(true);

        if (GUILayout.Button("Unset Selected Hierarchy Static Only"))
            SetStaticOnly(false);
    }

    private void ApplyToSelected()
    {
        int colliderAdded = 0;
        int staticSet = 0;
        int skipped = 0;

        foreach (GameObject root in Selection.gameObjects)
        {
            Transform[] targets = root.GetComponentsInChildren<Transform>(includeInactive);

            foreach (Transform t in targets)
            {
                GameObject source = t.gameObject;

                if (ShouldSkip(source))
                {
                    skipped++;
                    continue;
                }

                GameObject colliderTarget = GetColliderTarget(source);

                if (colliderTarget == null)
                {
                    skipped++;
                    continue;
                }

                bool hasMesh = source.TryGetComponent(out MeshFilter meshFilter) &&
                               meshFilter.sharedMesh != null;

                bool hasRenderer = source.TryGetComponent<Renderer>(out _);

                if (!hasMesh && !hasRenderer)
                {
                    skipped++;
                    continue;
                }

                if (setStaticTogether)
                {
                    if (!source.isStatic)
                    {
                        Undo.RecordObject(source, "Set Static");
                        source.isStatic = true;
                        EditorUtility.SetDirty(source);
                        staticSet++;
                    }

                    if (colliderTarget != source && !colliderTarget.isStatic)
                    {
                        Undo.RecordObject(colliderTarget, "Set Static");
                        colliderTarget.isStatic = true;
                        EditorUtility.SetDirty(colliderTarget);
                        staticSet++;
                    }
                }

                if (skipExistingCollider && colliderTarget.GetComponent<Collider>() != null)
                {
                    skipped++;
                    continue;
                }

                bool added = false;

                switch (colliderMode)
                {
                    case ColliderMode.FloorThinBox:
                        added = AddFloorThinBoxColliderFromSource(source, colliderTarget);
                        break;

                    case ColliderMode.NormalBox:
                        added = AddNormalBoxColliderFromSource(source, colliderTarget);
                        break;

                    case ColliderMode.MeshCollider:
                        added = AddMeshColliderFromSource(source, colliderTarget);
                        break;
                }

                if (added) colliderAdded++;
                else skipped++;
            }
        }

        Debug.Log(
            $"Map Physics Setup Complete | Colliders Added: {colliderAdded}, Static Set: {staticSet}, Skipped: {skipped}"
        );
    }

    private GameObject GetColliderTarget(GameObject source)
    {
        if (source == null)
            return null;

        if (IsDefaultChild(source) && source.transform.parent != null)
            return source.transform.parent.gameObject;

        return source;
    }

    private bool IsDefaultChild(GameObject obj)
    {
        return string.Equals(obj.name, "default", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool AddFloorThinBoxColliderFromSource(GameObject source, GameObject target)
    {
        if (source == null || target == null)
            return false;

        if (!TryGetBoundsInTargetLocal(source, target, out Bounds localBounds))
            return false;

        Undo.RecordObject(target, "Add Floor Thin Box Collider");

        BoxCollider box = Undo.AddComponent<BoxCollider>(target);

        Vector3 center = localBounds.center;
        Vector3 size = localBounds.size;

        center.y += floorYOffset;
        size.y = Mathf.Max(0.01f, floorThickness);

        box.center = center;
        box.size = size;

        EditorUtility.SetDirty(target);
        return true;
    }

    private bool AddNormalBoxColliderFromSource(GameObject source, GameObject target)
    {
        if (source == null || target == null)
            return false;

        if (!TryGetBoundsInTargetLocal(source, target, out Bounds localBounds))
            return false;

        Undo.RecordObject(target, "Add Normal Box Collider");

        BoxCollider box = Undo.AddComponent<BoxCollider>(target);
        box.center = localBounds.center;
        box.size = localBounds.size;

        EditorUtility.SetDirty(target);
        return true;
    }

    private bool AddMeshColliderFromSource(GameObject source, GameObject target)
    {
        if (source == null || target == null)
            return false;

        if (!source.TryGetComponent(out MeshFilter meshFilter))
            return false;

        if (meshFilter.sharedMesh == null)
            return false;

        Undo.RecordObject(target, "Add Mesh Collider");

        MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(target);
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = meshColliderConvex;

        EditorUtility.SetDirty(target);
        return true;
    }

    private bool TryGetBoundsInTargetLocal(GameObject source, GameObject target, out Bounds localBounds)
    {
        localBounds = new Bounds();

        if (source == null || target == null)
            return false;

        if (!source.TryGetComponent(out MeshFilter meshFilter))
            return false;

        Mesh mesh = meshFilter.sharedMesh;

        if (mesh == null)
            return false;

        Bounds meshBounds = mesh.bounds;

        Vector3 min = meshBounds.min;
        Vector3 max = meshBounds.max;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        bool initialized = false;

        foreach (Vector3 corner in corners)
        {
            Vector3 worldPoint = source.transform.TransformPoint(corner);
            Vector3 targetLocalPoint = target.transform.InverseTransformPoint(worldPoint);

            if (!initialized)
            {
                localBounds = new Bounds(targetLocalPoint, Vector3.zero);
                initialized = true;
            }
            else
            {
                localBounds.Encapsulate(targetLocalPoint);
            }
        }

        return initialized;
    }

    private void RemoveColliders()
    {
        int removed = 0;

        foreach (GameObject root in Selection.gameObjects)
        {
            Transform[] targets = root.GetComponentsInChildren<Transform>(includeInactive);

            foreach (Transform t in targets)
            {
                Collider[] colliders = t.GetComponents<Collider>();

                foreach (Collider col in colliders)
                {
                    Undo.DestroyObjectImmediate(col);
                    removed++;
                }
            }
        }

        Debug.Log($"Removed Colliders: {removed}");
    }

    private void SetStaticOnly(bool value)
    {
        int changed = 0;
        int skipped = 0;

        foreach (GameObject root in Selection.gameObjects)
        {
            Transform[] targets = root.GetComponentsInChildren<Transform>(includeInactive);

            foreach (Transform t in targets)
            {
                GameObject obj = t.gameObject;

                if (ShouldSkip(obj))
                {
                    skipped++;
                    continue;
                }

                Undo.RecordObject(obj, value ? "Set Static" : "Unset Static");
                obj.isStatic = value;
                EditorUtility.SetDirty(obj);

                changed++;
            }
        }

        Debug.Log(
            value
                ? $"Set Static Complete | Changed: {changed}, Skipped: {skipped}"
                : $"Unset Static Complete | Changed: {changed}, Skipped: {skipped}"
        );
    }

    private bool ShouldSkip(GameObject obj)
    {
        if (obj == null)
            return true;

        string n = obj.name.ToLower();

        if (n.Contains("player")) return true;
        if (n.Contains("npc")) return true;
        if (n.Contains("character")) return true;
        if (n.Contains("human")) return true;
        if (n.Contains("person")) return true;

        if (n.Contains("door")) return true;
        if (n.Contains("anim")) return true;

        // default는 skip하지 않음.
        // default의 Mesh bounds를 읽어서 부모에 Collider를 붙이기 위함.

        return false;
    }
}