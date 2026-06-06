using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Effects/Vertical Gradient")]
public class VerticalGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = new Color(0.56f, 0.52f, 0.47f, 0.55f);
    [SerializeField] private Color bottomColor = new Color(0.56f, 0.52f, 0.47f, 0.0f);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        if (verts.Count == 0) return;

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var v in verts)
        {
            minY = Mathf.Min(minY, v.position.y);
            maxY = Mathf.Max(maxY, v.position.y);
        }

        float height = maxY - minY;
        if (height <= 0f) return;

        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];

            float t = Mathf.InverseLerp(minY, maxY, v.position.y);

            // 위쪽은 topColor, 아래쪽은 bottomColor
            v.color = Color.Lerp(bottomColor, topColor, t);

            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}