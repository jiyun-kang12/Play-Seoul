using UnityEngine;

/// <summary>
/// Scrolls the water material's UVs to fake flowing water.
/// Works with URP/Lit (uses _BaseMap_ST). All secondary maps in URP/Lit share the
/// base UV, so scrolling _BaseMap makes the whole surface flow coherently.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class WaterFlowController : MonoBehaviour
{
    [Header("Flow Direction (UV space)")]
    [Tooltip("Direction the water flows in UV space. Will be normalized.")]
    public Vector2 flowDirection = new Vector2(0f, 1f);

    [Header("Flow Speed (UV units per second)")]
    public float flowSpeed = 0.12f;

    [Tooltip("Texture property to scroll. URP/Lit uses _BaseMap; falls back to _MainTex if not present.")]
    public string textureProperty = "_BaseMap";

    private Renderer waterRenderer;
    private Material waterMaterial;
    private int propertyID;
    private Vector2 baseScale = Vector2.one;
    private Vector2 offset;

    void Start()
    {
        waterRenderer = GetComponent<Renderer>();

        // Use the instanced material so we don't modify the shared asset at runtime.
        waterMaterial = waterRenderer.material;

        // Resolve the texture property: prefer the configured one, fall back to legacy aliases.
        if (!waterMaterial.HasProperty(textureProperty))
        {
            if (waterMaterial.HasProperty("_BaseMap")) textureProperty = "_BaseMap";
            else if (waterMaterial.HasProperty("_MainTex")) textureProperty = "_MainTex";
        }

        propertyID = Shader.PropertyToID(textureProperty);

        // Preserve the authored tiling (scale) so only the offset animates.
        baseScale = waterMaterial.GetTextureScale(propertyID);

        enabled = waterMaterial.HasProperty(propertyID);
    }

    void Update()
    {
        offset += flowDirection.normalized * (flowSpeed * Time.deltaTime);

        // Keep offset bounded to avoid float precision loss over long sessions.
        offset.x %= 1f;
        offset.y %= 1f;

        waterMaterial.SetTextureScale(propertyID, baseScale);
        waterMaterial.SetTextureOffset(propertyID, offset);
    }
}