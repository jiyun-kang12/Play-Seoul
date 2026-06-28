using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1f;
    public float runSpeed = 3f;
    public float rotationSmoothTime = 0.12f;

    public float acceleration = 4f;
    public float deceleration = 8f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 1.4f;
    public float gravity = -20f;
    public float groundedStickForce = -2f;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;

    private CharacterController controller;
    private float turnSmoothVelocity;
    private float currentMoveSpeed;
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("PlayerMovement: CharacterController가 없습니다. " + name + "에 CharacterController를 추가하세요.", this);
            enabled = false;
            return;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // CharacterController가 런타임 중 제거/파괴되었을 경우 안전하게 중단
        if (controller == null)
        {
            return;
        }

        bool isGrounded = controller.isGrounded;

        // 바닥에 붙어 있을 때 살짝 아래로 눌러서 grounded 판정 안정화
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;
        bool hasInput = input.magnitude > 0.1f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float targetMoveSpeed = 0f;

        if (hasInput)
        {
            targetMoveSpeed = isRunning ? runSpeed : walkSpeed;
        }

        float speedChangeRate = targetMoveSpeed > currentMoveSpeed
            ? acceleration
            : deceleration;

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            targetMoveSpeed,
            speedChangeRate * Time.deltaTime
        );

        Vector3 moveDirection = Vector3.zero;

        if (hasInput && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection = camForward * input.z + camRight * input.x;
            moveDirection.Normalize();

            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                rotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        // 점프 입력
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
            }
        }

        // 중력 누적
        verticalVelocity += gravity * Time.deltaTime;

        // 최종 이동 = 수평 이동 + 수직 이동
        Vector3 horizontalMove = moveDirection * currentMoveSpeed;
        Vector3 verticalMove = Vector3.up * verticalVelocity;
        Vector3 finalMove = horizontalMove + verticalMove;

        controller.Move(finalMove * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("Speed", currentMoveSpeed);
        }
    }
}