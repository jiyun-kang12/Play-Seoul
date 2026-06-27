using UnityEngine;

public class WaterFlowController : MonoBehaviour
{
    public Vector2 mainFlowDirection = new Vector2(0f, 1f);
    public Vector2 normalFlowDirection = new Vector2(0.2f, 1f);

    public float mainFlowSpeed = 0.12f;
    public float normalFlowSpeed = 0.2f;

    private Renderer waterRenderer;
    private Vector2 mainOffset;
    private Vector2 normalOffset;

    void Start()
    {
        waterRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (waterRenderer == null) return;

        mainOffset += mainFlowDirection.normalized * mainFlowSpeed * Time.deltaTime;
        normalOffset += normalFlowDirection.normalized * normalFlowSpeed * Time.deltaTime;

        waterRenderer.material.SetTextureOffset("_MainTex", mainOffset);
        waterRenderer.material.SetTextureOffset("_BumpMap", normalOffset);
    }
}