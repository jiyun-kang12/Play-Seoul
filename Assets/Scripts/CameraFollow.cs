using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Transform lookTarget;

    public float distance = 5f;
    public float height = 2.2f;

    public float positionSmooth = 8f;
    public float yawSmooth = 3f;
    public float lookSmooth = 15f;

    private float currentYaw;

    void Start()
    {
        if (player != null)
        {
            currentYaw = player.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (player == null || lookTarget == null) return;

        // 카메라가 플레이어 등 뒤 방향으로 천천히 회전
        currentYaw = Mathf.LerpAngle(
            currentYaw,
            player.eulerAngles.y,
            yawSmooth * Time.deltaTime
        );

        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);

        // 위치 기준을 player.position이 아니라 lookTarget.position으로 잡음
        Vector3 desiredPosition =
            lookTarget.position
            - yawRotation * Vector3.forward * distance
            + Vector3.up * height;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            positionSmooth * Time.deltaTime
        );

        // 시선은 CameraTarget을 빠르게 바라봄 → 캐릭터 중앙 유지
        Vector3 direction = lookTarget.position - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                lookSmooth * Time.deltaTime
            );
        }
    }
}