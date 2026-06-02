using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AutoSnapChildEdgesTool : EditorWindow
{
    private bool autoRotate = true;
    private bool keepY = true;
    private float gap = 0f;

    private enum SideMode
    {
        ForwardBackOnly,
        AllSides
    }

    private SideMode sideMode = SideMode.ForwardBackOnly;

    [MenuItem("Tools/Auto Snap Child Edges")]
    public static void Open()
    {
        GetWindow<AutoSnapChildEdgesTool>("Auto Snap Child Edges");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "두 오브젝트를 선택하세요.\n마지막으로 클릭한 오브젝트가 이동 대상입니다.\n부모 오브젝트 선택 가능.",
            MessageType.Info
        );

        sideMode = (SideMode)EditorGUILayout.EnumPopup("후보 면", sideMode);
        autoRotate = EditorGUILayout.Toggle("회전 자동 보정", autoRotate);
        keepY = EditorGUILayout.Toggle("Y 유지", keepY);
        gap = EditorGUILayout.FloatField("간격", gap);

        if (GUILayout.Button("Auto Snap"))
            AutoSnap();
    }

    private void AutoSnap()
    {
        if (Selection.gameObjects.Length != 2)
        {
            Debug.LogWarning("오브젝트 2개만 선택해야 합니다.");
            return;
        }

        GameObject mover = Selection.activeGameObject;
        GameObject target = Selection.gameObjects[0] == mover
            ? Selection.gameObjects[1]
            : Selection.gameObjects[0];

        EdgePair pair = FindBestPair(target, mover);

        if (pair == null)
        {
            Debug.LogWarning("붙일 끝선을 찾지 못했습니다.");
            return;
        }

        Undo.RecordObject(mover.transform, "Auto Snap Child Edges");

        if (autoRotate)
        {
            float angle = Vector3.SignedAngle(
                pair.mover.normal,
                -pair.target.normal,
                Vector3.up
            );

            mover.transform.Rotate(Vector3.up, angle, Space.World);
        }

        // 회전 후 다시 계산
        pair = FindBestPair(target, mover);

        Vector3 offset = pair.target.center - pair.mover.center;
        offset += pair.target.normal * gap;

        if (keepY)
            offset.y = 0f;

        mover.transform.position += offset;

        Debug.Log(
            $"기준: {target.name} / 이동: {mover.name}\n" +
            $"붙인 끝선: {pair.target.ownerName}.{pair.target.sideName} ↔ {pair.mover.ownerName}.{pair.mover.sideName}"
        );
    }

    private EdgePair FindBestPair(GameObject target, GameObject mover)
    {
        List<Edge> targetEdges = GetChildEdges(target);
        List<Edge> moverEdges = GetChildEdges(mover);

        EdgePair best = null;
        float bestScore = float.MaxValue;

        foreach (Edge t in targetEdges)
        {
            foreach (Edge m in moverEdges)
            {
                float dist = Vector3.Distance(Flat(t.center), Flat(m.center));
                float facingPenalty = 1f - Vector3.Dot(t.normal, -m.normal);

                // 가까운 끝선 우선 + 서로 마주보는 면 선호
                float score = dist + facingPenalty * 3f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = new EdgePair(t, m);
                }
            }
        }

        return best;
    }

    private List<Edge> GetChildEdges(GameObject root)
    {
        List<Edge> edges = new List<Edge>();
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null)
                continue;

            Transform tr = mf.transform;
            Vector3[] vertices = mf.sharedMesh.vertices;

            Vector3 forward = FlatDir(tr.forward);
            Vector3 right = FlatDir(tr.right);

            if (sideMode == SideMode.ForwardBackOnly)
            {
                edges.Add(MakeEdge(mf, vertices, "Forward", forward, right));
                edges.Add(MakeEdge(mf, vertices, "Back", -forward, right));
            }
            else
            {
                edges.Add(MakeEdge(mf, vertices, "Forward", forward, right));
                edges.Add(MakeEdge(mf, vertices, "Back", -forward, right));
                edges.Add(MakeEdge(mf, vertices, "Right", right, forward));
                edges.Add(MakeEdge(mf, vertices, "Left", -right, forward));
            }
        }

        return edges;
    }

    private Edge MakeEdge(MeshFilter mf, Vector3[] vertices, string sideName, Vector3 normal, Vector3 tangent)
    {
        Transform tr = mf.transform;

        float maxN = float.MinValue;
        float minT = float.MaxValue;
        float maxT = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 local in vertices)
        {
            Vector3 world = tr.TransformPoint(local);

            float n = Vector3.Dot(world, normal);
            float t = Vector3.Dot(world, tangent);

            if (n > maxN) maxN = n;
            if (t < minT) minT = t;
            if (t > maxT) maxT = t;
            if (world.y < minY) minY = world.y;
            if (world.y > maxY) maxY = world.y;
        }

        float midT = (minT + maxT) * 0.5f;
        float midY = (minY + maxY) * 0.5f;

        Vector3 center = normal * maxN + tangent * midT;
        center.y = midY;

        return new Edge
        {
            ownerName = mf.gameObject.name,
            sideName = sideName,
            center = center,
            normal = normal
        };
    }

    private Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    private Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.normalized;
    }

    private class Edge
    {
        public string ownerName;
        public string sideName;
        public Vector3 center;
        public Vector3 normal;
    }

    private class EdgePair
    {
        public Edge target;
        public Edge mover;

        public EdgePair(Edge target, Edge mover)
        {
            this.target = target;
            this.mover = mover;
        }
    }
}