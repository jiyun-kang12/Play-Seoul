using UnityEngine;
using UnityEditor;

public class DropToSurfaceTool
{
    [MenuItem("Tools/Drop Selected To Surface")]
    static void DropSelectedToSurface()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            DropObject(obj);
        }
    }

    static void DropObject(GameObject obj)
    {
        Renderer renderer = obj.GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            Debug.LogWarning($"{obj.name}: Renderer 없음");
            return;
        }

        Bounds bounds = renderer.bounds;

        Vector3 rayOrigin = new Vector3(
            obj.transform.position.x,
            bounds.max.y + 100f,
            obj.transform.position.z
        );

        Ray ray = new Ray(rayOrigin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);

        if (hits.Length == 0)
        {
            Debug.LogWarning($"{obj.name}: 아래에 충돌 대상 없음");
            return;
        }

        System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == obj.transform || hit.transform.IsChildOf(obj.transform))
                continue;

            float objectBottomY = bounds.min.y;
            float offsetFromPivotToBottom = obj.transform.position.y - objectBottomY;

            Undo.RecordObject(obj.transform, "Drop To Surface");

            obj.transform.position = new Vector3(
                obj.transform.position.x,
                hit.point.y + offsetFromPivotToBottom,
                obj.transform.position.z
            );

            Debug.Log($"{obj.name} dropped to {hit.collider.name}");
            return;
        }

        Debug.LogWarning($"{obj.name}: 자기 자신 제외 후 닿는 대상 없음");
    }
}