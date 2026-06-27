using UnityEngine;
using System.Collections;

public class PlayerSitController : MonoBehaviour
{
    [Header("현재 앉은 상태")]
    public bool isSitting = false;

    private bool canStandUp = false;

    private CharacterController characterController;
    private Rigidbody rb;
    private Animator animator;
    private PlayerMovement playerMovement;

    private ChairSitInteract currentChair;

    private Vector3 savedStandPosition;
    private Quaternion savedStandRotation;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        // Animator가 Player 본체가 아니라 자식 캐릭터 모델에 있을 수 있으므로 InChildren 사용
        animator = GetComponentInChildren<Animator>();

        // 기존 이동 스크립트
        playerMovement = GetComponent<PlayerMovement>();

        if (animator == null)
        {
            Debug.LogWarning("Animator를 찾지 못했습니다. Player 또는 자식 오브젝트에 Animator가 있는지 확인하세요.");
        }
        else
        {
            Debug.Log("Animator 찾음: " + animator.gameObject.name);
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("PlayerMovement를 찾지 못했습니다. 이동 스크립트 이름이 PlayerMovement인지 확인하세요.");
        }
    }

    void Update()
    {
        // 앉아 있는 상태에서 E를 다시 누르면 일어나기
        if (isSitting && canStandUp && Input.GetKeyDown(KeyCode.E))
        {
            StandUp();
        }
    }

    // 새 방식: SitPoint / StandPoint 없이 위치와 회전을 직접 받아서 앉기
    public void SitOnChairByPosition(
        Vector3 seatPosition,
        Quaternion seatRotation,
        Vector3 standPosition,
        Quaternion standRotation,
        ChairSitInteract chair
    )
    {
        if (isSitting)
        {
            return;
        }

        isSitting = true;
        canStandUp = false;

        currentChair = chair;

        savedStandPosition = standPosition;
        savedStandRotation = standRotation;

        // 이동 스크립트 끄기
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // CharacterController 끄기
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Rigidbody 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Player를 앉는 위치로 이동
        transform.position = seatPosition;
        transform.rotation = seatRotation;

        // 앉기 애니메이션 실행
        if (animator != null)
        {
            animator.SetBool("IsSitting", true);

            // 이전 Trigger가 남아 있을 수 있으니 한번 초기화 후 실행
            animator.ResetTrigger("SitTrigger");
            animator.SetTrigger("SitTrigger");

            Debug.Log("Animator SitTrigger 실행됨");
        }
        else
        {
            Debug.LogWarning("Animator가 없어서 앉기 애니메이션을 실행하지 못했습니다.");
        }

        Debug.Log("앉기 실행됨");

        StartCoroutine(EnableStandUpAfterDelay());
    }

    // 기존 SitPoint 방식도 혹시 필요할 수 있으니 남겨둠
    public void SitOnChair(Transform sitPoint, Transform standPoint, ChairSitInteract chair)
    {
        if (sitPoint == null)
        {
            Debug.LogWarning("SitPoint가 연결되지 않았습니다.");
            return;
        }

        Vector3 seatPosition = sitPoint.position;
        Quaternion seatRotation = sitPoint.rotation;

        Vector3 standPosition;
        Quaternion standRotation;

        if (standPoint != null)
        {
            standPosition = standPoint.position;
            standRotation = standPoint.rotation;
        }
        else
        {
            standPosition = transform.position + transform.forward * 0.8f;
            standRotation = transform.rotation;
        }

        SitOnChairByPosition(
            seatPosition,
            seatRotation,
            standPosition,
            standRotation,
            chair
        );
    }

    IEnumerator EnableStandUpAfterDelay()
    {
        // E키 한 번 눌렀을 때 앉자마자 바로 일어나는 것 방지
        yield return new WaitForSeconds(0.3f);
        canStandUp = true;
    }

    void StandUp()
    {
        if (!isSitting)
        {
            return;
        }

        isSitting = false;
        canStandUp = false;

        // 일어나기 애니메이션이 따로 없다면 IsSitting false로 Idle 복귀
        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
        }

        // Player를 일어나는 위치로 이동
        transform.position = savedStandPosition;
        transform.rotation = savedStandRotation;

        // CharacterController 다시 켜기
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 이동 스크립트 다시 켜기
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Rigidbody 다시 활성화
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (currentChair != null)
        {
            currentChair.SetOccupied(false);
        }

        currentChair = null;

        Debug.Log("일어났습니다.");
    }
}