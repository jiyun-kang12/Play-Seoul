using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 1f;
    public float runSpeed = 3f;
    public float rotationSmoothTime = 0.12f;

    public float acceleration = 4f;
    public float deceleration = 8f;

    public Animator animator;
    public Transform cameraTransform;

    private CharacterController controller;
    private float turnSmoothVelocity;
    private float currentMoveSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;
        bool hasInput = input.magnitude > 0.1f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float targetMoveSpeed = 0f;
        float targetAnimSpeed = 0f;

        if (hasInput)
        {
            targetMoveSpeed = isRunning ? runSpeed : walkSpeed;
            targetAnimSpeed = isRunning ? runSpeed : walkSpeed;
        }

        float speedChangeRate;

        if (targetMoveSpeed > currentMoveSpeed)
        {
            speedChangeRate = acceleration;   // 빨라질 때
        }
        else
        {
            speedChangeRate = deceleration;   // 느려질 때
        }

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            targetMoveSpeed,
            speedChangeRate * Time.deltaTime
        );

        if (hasInput)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = camForward * input.z + camRight * input.x;
            moveDirection.Normalize();

            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                rotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            controller.Move(moveDirection * currentMoveSpeed * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", targetAnimSpeed);
        }
    }
}