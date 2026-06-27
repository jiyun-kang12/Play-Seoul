using UnityEngine;

public class ChairSitInteract : MonoBehaviour
{
    [Header("UI 안내 문구")]
    public GameObject interactText;

    [Header("의자 기준 앉는 위치")]
    public Vector3 seatOffset = new Vector3(0f, 0.4f, 0f);

    [Header("의자 기준 일어나는 위치")]
    public Vector3 standOffset = new Vector3(0f, 0f, -1.0f);

    [Header("앉았을 때 바라볼 방향 보정")]
    public float sitRotationY = 0f;

    private PlayerSitController playerSitController;
    private bool playerInRange = false;
    private bool isOccupied = false;

    void Start()
    {
        if (interactText != null)
        {
            interactText.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInRange && !isOccupied && Input.GetKeyDown(KeyCode.E))
        {
            Sit();
        }
    }

    void Sit()
    {
        Debug.Log("Sit 함수 실행됨");

        if (playerSitController == null)
        {
            Debug.LogWarning("PlayerSitController가 없습니다.");
            return;
        }

        isOccupied = true;

        if (interactText != null)
        {
            interactText.SetActive(false);
        }

        Vector3 seatPosition = transform.TransformPoint(seatOffset);
        Quaternion seatRotation = transform.rotation * Quaternion.Euler(0f, sitRotationY, 0f);

        Vector3 standPosition = transform.TransformPoint(standOffset);
        Quaternion standRotation = transform.rotation;

        Debug.Log("SitOnChair 실행");

        playerSitController.SitOnChairByPosition(
            seatPosition,
            seatRotation,
            standPosition,
            standRotation,
            this
        );
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerSitController controller = other.GetComponentInParent<PlayerSitController>();

        if (controller != null && !isOccupied)
        {
            playerSitController = controller;
            playerInRange = true;

            if (interactText != null)
            {
                interactText.SetActive(true);
            }

            Debug.Log("E키를 누르면 앉을 수 있습니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerSitController controller = other.GetComponentInParent<PlayerSitController>();

        if (controller != null && controller == playerSitController)
        {
            playerInRange = false;
            playerSitController = null;

            if (interactText != null)
            {
                interactText.SetActive(false);
            }

            Debug.Log("의자 범위에서 나갔습니다.");
        }
    }
}