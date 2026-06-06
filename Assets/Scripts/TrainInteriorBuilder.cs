using UnityEngine;

public class TrainInteriorBuilder : MonoBehaviour
{
    [Header("Train Interior Size")]
    public float trainLength = 18f;
    public float trainWidth = 4f;
    public float trainHeight = 3.2f;

    [Header("Wall / Floor Thickness")]
    public float wallThickness = 0.15f;
    public float floorThickness = 0.15f;
    public float ceilingThickness = 0.15f;

    [Header("Materials")]
    public Material floorMaterial;
    public Material ceilingMaterial;
    public Material wallMaterial;

    [Header("Optional Prefabs")]
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;
    public GameObject leftWallPrefab;
    public GameObject rightWallPrefab;

    [Header("Build Option")]
    public bool buildOnStart = true;
    public bool clearBeforeBuild = true;

    private void Start()
    {
        if (buildOnStart)
        {
            BuildTrainInterior();
        }
    }

    [ContextMenu("Build Train Interior")]
    public void BuildTrainInterior()
    {
        if (clearBeforeBuild)
        {
            ClearChildren();
        }

        CreateFloor();
        CreateCeiling();
        CreateLeftWall();
        CreateRightWall();
    }

    private void CreateFloor()
    {
        Vector3 position = new Vector3(0f, 0f, 0f);
        Vector3 scale = new Vector3(trainWidth, floorThickness, trainLength);

        GameObject floor = CreatePart(
            "Floor",
            floorPrefab,
            position,
            scale,
            floorMaterial
        );

        floor.transform.SetParent(transform);
    }

    private void CreateCeiling()
    {
        Vector3 position = new Vector3(0f, trainHeight, 0f);
        Vector3 scale = new Vector3(trainWidth, ceilingThickness, trainLength);

        GameObject ceiling = CreatePart(
            "Ceiling",
            ceilingPrefab,
            position,
            scale,
            ceilingMaterial
        );

        ceiling.transform.SetParent(transform);
    }

    private void CreateLeftWall()
    {
        Vector3 position = new Vector3(-trainWidth / 2f, trainHeight / 2f, 0f);
        Vector3 scale = new Vector3(wallThickness, trainHeight, trainLength);

        GameObject leftWall = CreatePart(
            "LeftWall",
            leftWallPrefab,
            position,
            scale,
            wallMaterial
        );

        leftWall.transform.SetParent(transform);
    }

    private void CreateRightWall()
    {
        Vector3 position = new Vector3(trainWidth / 2f, trainHeight / 2f, 0f);
        Vector3 scale = new Vector3(wallThickness, trainHeight, trainLength);

        GameObject rightWall = CreatePart(
            "RightWall",
            rightWallPrefab,
            position,
            scale,
            wallMaterial
        );

        rightWall.transform.SetParent(transform);
    }

    private GameObject CreatePart(
        string objectName,
        GameObject prefab,
        Vector3 localPosition,
        Vector3 targetScale,
        Material material
    )
    {
        GameObject part;

        if (prefab != null)
        {
            part = Instantiate(prefab, transform);
            part.name = objectName;
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;

            // 프리팹 기준 크기가 다를 수 있으므로 일단 스케일로 맞춤
            part.transform.localScale = targetScale;
        }
        else
        {
            part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = objectName;
            part.transform.SetParent(transform);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = targetScale;
        }

        if (material != null)
        {
            Renderer renderer = part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material = material;
            }
        }

        return part;
    }

    private void ClearChildren()
    {
        int childCount = transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}