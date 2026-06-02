using UnityEngine;
using UnityEditor;

public class CenterChildrenToParentPivot
{
    [MenuItem("Tools/Center Children To Parent Pivot")]
    static void CenterChildren()
    {
        GameObject parent = Selection.activeGameObject;

        if (parent == null)
        {
            Debug.LogWarning("부모 오브젝트를 선택해야 함");
            return;
        }

        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("자식 Renderer가 없음");
            return;
        }

        Bounds worldBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 worldCenter = worldBounds.center;
        Vector3 parentWorldPos = parent.transform.position;

        Vector3 offsetWorld = parentWorldPos - worldCenter;

        Undo.RecordObjects(GetDirectChildrenTransforms(parent), "Center Children To Parent Pivot");

        foreach (Transform child in parent.transform)
        {
            child.position += offsetWorld;
        }

        Debug.Log($"자식들을 부모 pivot 기준으로 중앙 정렬함. Offset: {offsetWorld}");
    }

    static Object[] GetDirectChildrenTransforms(GameObject parent)
    {
        Object[] objects = new Object[parent.transform.childCount];

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            objects[i] = parent.transform.GetChild(i);
        }

        return objects;
    }
}