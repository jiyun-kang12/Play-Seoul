using UnityEditor;
using UnityEngine;

public class CityPrefabScatterTool : EditorWindow
{
    private GameObject prefab;
    private Transform parent;
    private LayerMask placementMask = ~0;

    private float rayStartHeight = 1000f;
    private float rayDistance = 3000f;

    private bool randomYaw = true;

    private bool useRandomScale = false;
    private float minScale = 0.85f;
    private float maxScale = 1.15f;

    [MenuItem("Tools/City/Prefab Scatter Tool")]
    public static void Open()
    {
        GetWindow<CityPrefabScatterTool>("City Scatter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab",
            prefab,
            typeof(GameObject),
            false
        );

        parent = (Transform)EditorGUILayout.ObjectField(
            "Parent",
            parent,
            typeof(Transform),
            true
        );

        placementMask = LayerMaskField("Placement Mask", placementMask);

        EditorGUILayout.Space();

        rayStartHeight = EditorGUILayout.FloatField("Ray Start Height", rayStartHeight);
        rayDistance = EditorGUILayout.FloatField("Ray Distance", rayDistance);

        EditorGUILayout.Space();

        randomYaw = EditorGUILayout.Toggle("Random Y Rotation", randomYaw);

        EditorGUILayout.Space();

        useRandomScale = EditorGUILayout.Toggle("Use Random Scale", useRandomScale);

        if (useRandomScale)
        {
            EditorGUI.indentLevel++;

            minScale = EditorGUILayout.FloatField("Min Scale", minScale);
            maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

            if (minScale <= 0f)
                minScale = 0.01f;

            if (maxScale <= 0f)
                maxScale = 0.01f;

            if (minScale > maxScale)
            {
                float temp = minScale;
                minScale = maxScale;
                maxScale = temp;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Scene View에서 Shift + 좌클릭하면 해당 위치의 가장 높은 Collider 표면에 prefab이 배치됩니다.\n\n" +
            "순서:\n" +
            "1. Prefab 생성\n" +
            "2. Parent 지정\n" +
            "3. 위치/회전 지정\n" +
            "4. 상위 Prefab Transform에 Uniform Scale 적용\n" +
            "5. Renderer bounds 기준으로 바닥에 자동 정렬\n\n" +
            "주의: Parent의 Scale은 가능하면 1,1,1로 두는 것을 권장합니다.",
            MessageType.Info
        );
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (prefab == null)
            return;

        if (e.type != EventType.MouseDown)
            return;

        if (e.button != 0 || !e.shift)
            return;

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (!Physics.Raycast(mouseRay, out RaycastHit firstHit, Mathf.Infinity, placementMask))
            return;

        Vector3 xzPos = firstHit.point;

        Vector3 topOrigin = new Vector3(
            xzPos.x,
            rayStartHeight,
            xzPos.z
        );

        Ray downRay = new Ray(topOrigin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(
            downRay,
            rayDistance,
            placementMask
        );

        if (hits.Length == 0)
            return;

        RaycastHit highestHit = hits[0];

        foreach (RaycastHit hit in hits)
        {
            if (hit.point.y > highestHit.point.y)
                highestHit = hit;
        }

        PlacePrefab(highestHit.point);

        e.Use();
    }

    private void PlacePrefab(Vector3 surfacePoint)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        if (obj == null)
            return;

        Undo.RegisterCreatedObjectUndo(obj, "Place City Prefab");

        if (parent != null)
            obj.transform.SetParent(parent, true);

        obj.transform.position = surfacePoint;

        if (randomYaw)
        {
            obj.transform.rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );
        }
        else
        {
            obj.transform.rotation = prefab.transform.rotation;
        }

        ApplyRandomScale(obj);

        AlignBottomToSurface(obj, surfacePoint.y);

        Selection.activeGameObject = obj;
    }

    private void ApplyRandomScale(GameObject obj)
    {
        if (!useRandomScale)
            return;

        float randomScale = Random.Range(minScale, maxScale);

        obj.transform.localScale = prefab.transform.localScale * randomScale;
    }

    private void AlignBottomToSurface(GameObject obj, float surfaceY)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float bottomY = bounds.min.y;
        float offsetY = surfaceY - bottomY;

        obj.transform.position += new Vector3(0f, offsetY, 0f);
    }

    private static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        string[] layers = new string[32];
        int[] layerNumbers = new int[32];

        int count = 0;

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);

            if (!string.IsNullOrEmpty(layerName))
            {
                layers[count] = layerName;
                layerNumbers[count] = i;
                count++;
            }
        }

        string[] finalLayers = new string[count];
        int[] finalLayerNumbers = new int[count];

        for (int i = 0; i < count; i++)
        {
            finalLayers[i] = layers[i];
            finalLayerNumbers[i] = layerNumbers[i];
        }

        int maskWithoutEmpty = 0;

        for (int i = 0; i < finalLayerNumbers.Length; i++)
        {
            if (((1 << finalLayerNumbers[i]) & selected.value) > 0)
                maskWithoutEmpty |= 1 << i;
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(
            label,
            maskWithoutEmpty,
            finalLayers
        );

        int mask = 0;

        for (int i = 0; i < finalLayerNumbers.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) > 0)
                mask |= 1 << finalLayerNumbers[i];
        }

        selected.value = mask;
        return selected;
    }
}