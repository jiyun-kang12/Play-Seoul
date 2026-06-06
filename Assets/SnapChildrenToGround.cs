using UnityEngine;

public class SnapChildrenToParentY : MonoBehaviour
{
    [ContextMenu("Snap Children To Parent Y")]
    void SnapAll()
    {
        float targetY = transform.position.y;

        foreach (Transform child in transform)
        {
            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0) continue;

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            float bottomY = bounds.min.y;
            float offsetY = targetY - bottomY;

            child.position += new Vector3(0, offsetY, 0);
        }

        Debug.Log("자식 오브젝트들의 바닥을 부모 Y 위치에 맞췄습니다.");
    }
}