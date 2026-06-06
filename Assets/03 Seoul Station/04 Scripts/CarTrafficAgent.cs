using UnityEngine;

public class CarTrafficAgent : MonoBehaviour
{
    [Header("Route")]
    public CarRoute route;
    public int startWaypointIndex = 0;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;
    public float arriveDistance = 0.5f;

    [Header("Placement")]
    public bool snapToStartOnPlay = true;
    public float groundOffset = 0f;

    [Header("Debug")]
    public bool drawForwardRay = true;

    private int currentWaypointIndex;
    private bool initialized;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!initialized)
            return;

        MoveAlongRoute();
    }

    private void Initialize()
    {
        if (route == null || route.Count == 0)
        {
            Debug.LogWarning($"{name}: Route가 없거나 Waypoint가 비어있습니다.");
            enabled = false;
            return;
        }

        currentWaypointIndex = startWaypointIndex;

        if (snapToStartOnPlay)
        {
            Vector3 startPos = route.GetWaypointPosition(currentWaypointIndex);
            startPos.y += groundOffset;
            transform.position = startPos;

            int nextIndex = route.GetNextIndex(currentWaypointIndex);
            if (nextIndex != -1)
            {
                Vector3 nextPos = route.GetWaypointPosition(nextIndex);
                Vector3 dir = nextPos - transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        initialized = true;
    }

    private void MoveAlongRoute()
    {
        Transform targetWp = route.GetWaypoint(currentWaypointIndex);

        if (targetWp == null)
        {
            enabled = false;
            return;
        }

        Vector3 targetPos = targetWp.position;
        targetPos.y = transform.position.y;

        Vector3 toTarget = targetPos - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= arriveDistance)
        {
            int nextIndex = route.GetNextIndex(currentWaypointIndex);

            if (nextIndex == -1)
            {
                enabled = false;
                return;
            }

            currentWaypointIndex = nextIndex;
            return;
        }

        Vector3 moveDir = toTarget.normalized;

        transform.position += moveDir * moveSpeed * Time.deltaTime;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawForwardRay)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
    }
}