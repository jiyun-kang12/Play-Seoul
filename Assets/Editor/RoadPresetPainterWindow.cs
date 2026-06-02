using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoadPresetPainterWindow : EditorWindow
{
    enum PrefabForwardAxis
    {
        Z_Axis,
        X_Axis
    }

    GameObject roadPrefab;
    RoadPresetSet presetSet;

    Transform presetPrevPiece;
    Transform presetNextPiece;

    PrefabForwardAxis forwardAxis = PrefabForwardAxis.X_Axis;

    float segmentLength = 10f;
    float yOffset = 0f;
    float rotationOffset = 0f;

    float mouseSampleDistance = 1f;
    float angleThreshold = 10f;

    bool paintMode = false;
    bool isDrawing = false;

    readonly List<Vector3> rawPoints = new List<Vector3>();

    Transform generatedParent;
    Transform lastRoad;

    Vector3 currentDirection;
    float straightDistanceBuffer = 0f;

    [MenuItem("Tools/Road Preset Painter")]
    public static void ShowWindow()
    {
        GetWindow<RoadPresetPainterWindow>("Road Preset Painter");
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnGUI()
    {
        roadPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Road Prefab",
            roadPrefab,
            typeof(GameObject),
            false
        );

        presetSet = (RoadPresetSet)EditorGUILayout.ObjectField(
            "Preset Set",
            presetSet,
            typeof(RoadPresetSet),
            false
        );

        forwardAxis = (PrefabForwardAxis)EditorGUILayout.EnumPopup(
            "Prefab Forward Axis",
            forwardAxis
        );

        EditorGUILayout.Space();

        segmentLength = EditorGUILayout.FloatField("Segment Length", segmentLength);
        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        rotationOffset = EditorGUILayout.FloatField("Rotation Offset", rotationOffset);

        EditorGUILayout.Space();

        mouseSampleDistance = EditorGUILayout.FloatField("Mouse Sample Distance", mouseSampleDistance);
        angleThreshold = EditorGUILayout.FloatField("Angle Threshold", angleThreshold);

        EditorGUILayout.Space();

        paintMode = EditorGUILayout.Toggle("Paint Mode", paintMode);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Capture Targets", EditorStyles.boldLabel);

        presetPrevPiece = (Transform)EditorGUILayout.ObjectField(
            "Prev Piece",
            presetPrevPiece,
            typeof(Transform),
            true
        );

        presetNextPiece = (Transform)EditorGUILayout.ObjectField(
            "Next Piece",
            presetNextPiece,
            typeof(Transform),
            true
        );

        if (GUILayout.Button("Capture Straight From Prev / Next"))
        {
            CaptureStraight();
        }

        if (GUILayout.Button("Capture Turn From Prev / Next"))
        {
            CaptureTurn();
        }

        if (GUILayout.Button("Clear Drawing"))
        {
            rawPoints.Clear();
            isDrawing = false;
        }

        EditorGUILayout.HelpBox(
            "Paint Mode ON + Shift + 좌클릭 드래그 = 도로 그리기\n" +
            "Paint Mode OFF = Scene 클릭/선택 정상 작동\n\n" +
            "Prev Piece = 이전 도로 조각\n" +
            "Next Piece = 다음 도로 조각\n\n" +
            "Capture는 선택 순서 안 씀. 슬롯에 직접 넣고 저장.",
            MessageType.Info
        );
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!paintMode)
            return;

        Event e = Event.current;

        if (e.alt)
            return;

        // Shift를 누를 때만 도로 그리기 시작.
        // 단, 이미 그리고 있는 중이면 MouseUp 처리를 위해 계속 받음.
        if (!e.shift && !isDrawing)
            return;

        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            Vector3? point = GetMouseWorldPoint(e.mousePosition);

            if (point.HasValue)
            {
                isDrawing = true;
                rawPoints.Clear();
                rawPoints.Add(point.Value);
                e.Use();
            }
        }

        if (isDrawing && e.type == EventType.MouseDrag && e.button == 0)
        {
            Vector3? point = GetMouseWorldPoint(e.mousePosition);

            if (point.HasValue)
            {
                Vector3 p = point.Value;

                if (rawPoints.Count == 0 ||
                    Vector3.Distance(rawPoints[rawPoints.Count - 1], p) >= mouseSampleDistance)
                {
                    rawPoints.Add(p);
                    SceneView.RepaintAll();
                }

                e.Use();
            }
        }

        if (isDrawing && e.type == EventType.MouseUp && e.button == 0)
        {
            isDrawing = false;

            if (rawPoints.Count >= 2)
            {
                BuildRoad(rawPoints);
            }

            e.Use();
        }

        DrawPreview();
    }

    Vector3? GetMouseWorldPoint(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
        {
            Vector3 p = hit.point;
            p.y += yOffset;
            return p;
        }

        return null;
    }

    void BuildRoad(List<Vector3> points)
    {
        if (roadPrefab == null || presetSet == null)
        {
            Debug.LogWarning("Road Prefab과 Preset Set이 필요함");
            return;
        }

        if (points.Count < 2)
            return;

        GameObject parentObj = new GameObject("Generated Road");
        Undo.RegisterCreatedObjectUndo(parentObj, "Create Generated Road");
        generatedParent = parentObj.transform;

        lastRoad = null;
        straightDistanceBuffer = 0f;

        Vector3 firstDir = points[1] - points[0];
        firstDir.y = 0f;

        if (firstDir.sqrMagnitude < 0.001f)
            return;

        firstDir.Normalize();

        currentDirection = firstDir;

        Quaternion firstRot = GetRotationFromForward(currentDirection);
        GameObject firstRoad = CreateRoadPiece(points[0], firstRot);
        lastRoad = firstRoad.transform;

        currentDirection = GetForwardFromRoad(lastRoad);

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 move = points[i] - points[i - 1];
            move.y = 0f;

            float distance = move.magnitude;

            if (distance < 0.001f)
                continue;

            Vector3 newDir = move.normalized;

            float angle = Vector3.Angle(currentDirection, newDir);
            float crossY = Vector3.Cross(currentDirection, newDir).y;

            if (angle < angleThreshold)
            {
                straightDistanceBuffer += distance;

                while (straightDistanceBuffer >= segmentLength)
                {
                    PlaceStraight();
                    straightDistanceBuffer -= segmentLength;
                }
            }
            else
            {
                TurnSide side = crossY > 0f ? TurnSide.Left : TurnSide.Right;
                TurnPreset preset = FindNearestTurnPreset(angle, side);

                if (preset == null)
                {
                    Debug.LogWarning($"Preset 없음 / angle: {angle}, side: {side}");
                    continue;
                }

                PlaceTurn(preset);

                // 중요:
                // 여기서 preset.angle로 방향 추측하지 않음.
                // PlaceTurn 내부에서 실제 생성된 lastRoad 방향을 읽음.
                straightDistanceBuffer = 0f;
            }
        }

        Debug.Log("Preset Road 생성 완료");
    }

    void PlaceStraight()
    {
        if (lastRoad == null)
            return;

        Vector3 pos = lastRoad.TransformPoint(presetSet.straightLocalPositionOffset);
        Quaternion rot =
            lastRoad.rotation *
            Quaternion.Euler(0f, presetSet.straightLocalYawOffset, 0f);

        GameObject road = CreateRoadPiece(pos, rot);
        lastRoad = road.transform;

        currentDirection = GetForwardFromRoad(lastRoad);
    }

    void PlaceTurn(TurnPreset preset)
    {
        if (lastRoad == null)
            return;

        Vector3 pos = lastRoad.TransformPoint(preset.localPositionOffset);
        Quaternion rot =
            lastRoad.rotation *
            Quaternion.Euler(0f, preset.localYawOffset, 0f);

        GameObject road = CreateRoadPiece(pos, rot);
        lastRoad = road.transform;

        currentDirection = GetForwardFromRoad(lastRoad);
    }

    Vector3 GetForwardFromRoad(Transform road)
    {
        Vector3 dir;

        if (forwardAxis == PrefabForwardAxis.Z_Axis)
        {
            dir = road.forward;
        }
        else
        {
            dir = road.right;
        }

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return currentDirection;

        dir.Normalize();
        return dir;
    }

    TurnPreset FindNearestTurnPreset(float angle, TurnSide side)
    {
        if (presetSet == null)
            return null;

        TurnPreset best = null;
        float bestDiff = float.MaxValue;

        foreach (TurnPreset preset in presetSet.turnPresets)
        {
            if (preset.side != side)
                continue;

            float diff = Mathf.Abs(preset.angle - angle);

            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = preset;
            }
        }

        return best;
    }

    GameObject CreateRoadPiece(Vector3 position, Quaternion rotation)
    {
        GameObject road = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);

        if (road == null)
        {
            Debug.LogWarning("Prefab 생성 실패");
            return null;
        }

        Undo.RegisterCreatedObjectUndo(road, "Create Road Piece");

        road.transform.position = position;
        road.transform.rotation = rotation;
        road.transform.SetParent(generatedParent);

        return road;
    }

    Quaternion GetRotationFromForward(Vector3 forward)
    {
        if (forwardAxis == PrefabForwardAxis.Z_Axis)
        {
            return Quaternion.LookRotation(forward) *
                   Quaternion.Euler(0f, rotationOffset, 0f);
        }

        return Quaternion.LookRotation(forward) *
               Quaternion.Euler(0f, -90f + rotationOffset, 0f);
    }

    void CaptureStraight()
    {
        if (presetSet == null)
        {
            Debug.LogWarning("Preset Set 필요");
            return;
        }

        if (!TryGetPresetPair(out Transform prev, out Transform next))
            return;

        presetSet.straightLocalPositionOffset = prev.InverseTransformPoint(next.position);
        presetSet.straightLocalYawOffset = GetLocalYaw(prev, next);

        EditorUtility.SetDirty(presetSet);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Straight 저장 완료 / Offset: {presetSet.straightLocalPositionOffset}, " +
            $"Yaw: {presetSet.straightLocalYawOffset}"
        );
    }

    void CaptureTurn()
    {
        if (presetSet == null)
        {
            Debug.LogWarning("Preset Set 필요");
            return;
        }

        if (!TryGetPresetPair(out Transform prev, out Transform next))
            return;

        float yaw = GetLocalYaw(prev, next);
        float angle = Mathf.Abs(yaw);

        // 네 기준에 맞게 반전한 상태.
        // 만약 또 좌/우가 반대로 저장되면 이 줄만 다시 반대로 바꾸면 됨.
        TurnSide side = yaw < 0f ? TurnSide.Right : TurnSide.Left;

        TurnPreset preset = new TurnPreset();
        preset.angle = angle;
        preset.side = side;
        preset.localPositionOffset = prev.InverseTransformPoint(next.position);
        preset.localYawOffset = yaw;

        presetSet.turnPresets.Add(preset);

        EditorUtility.SetDirty(presetSet);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Turn 저장 완료 / angle: {angle}, side: {side}, " +
            $"offset: {preset.localPositionOffset}, yaw: {yaw}"
        );
    }

    bool TryGetPresetPair(out Transform prev, out Transform next)
    {
        prev = presetPrevPiece;
        next = presetNextPiece;

        if (prev == null || next == null)
        {
            Debug.LogWarning("Prev Piece와 Next Piece 슬롯에 직접 넣어야 함");
            return false;
        }

        if (prev == next)
        {
            Debug.LogWarning("Prev Piece와 Next Piece가 같음");
            return false;
        }

        return true;
    }

    float GetLocalYaw(Transform prev, Transform next)
    {
        Quaternion localRot = Quaternion.Inverse(prev.rotation) * next.rotation;
        return NormalizeAngle(localRot.eulerAngles.y);
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }

    void DrawPreview()
    {
        if (!isDrawing || rawPoints.Count < 2)
            return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        for (int i = 0; i < rawPoints.Count - 1; i++)
        {
            Handles.DrawLine(rawPoints[i], rawPoints[i + 1]);
        }
    }
}