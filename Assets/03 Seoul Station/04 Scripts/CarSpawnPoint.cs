using System.Collections.Generic;
using UnityEngine;

public class CarSpawnPoint : MonoBehaviour
{
    public enum VehicleGroup
    {
        Normal,
        Taxi,
        Bus,
        Truck,
        Scooter,
        Special
    }

    [System.Serializable]
    public class VehicleSpawnEntry
    {
        public GameObject prefab;
        public VehicleGroup group = VehicleGroup.Normal;

        [Min(0f)]
        public float weight = 1f;

        [Header("Optional Speed Override")]
        public bool overrideSpeed = false;
        public float moveSpeed = 5f;
    }

    [Header("Route")]
    public CarRoute route;
    public int startWaypointIndex = 0;

    [Header("Spawn Settings")]
    public Transform runtimeParent;
    public List<VehicleSpawnEntry> vehicles = new List<VehicleSpawnEntry>();

    public bool spawnOnStart = true;
    public int initialSpawnCount = 1;

    public bool keepSpawning = true;
    public int maxAliveFromThisSpawner = 2;

    public float minSpawnInterval = 6f;
    public float maxSpawnInterval = 12f;

    [Header("Vehicle Agent Defaults")]
    public float defaultMoveSpeed = 5f;
    public float defaultRotationSpeed = 8f;
    public float defaultArriveDistance = 0.7f;
    public float defaultGroundOffset = 0.05f;

    [Header("Debug")]
    public bool drawGizmo = true;
    public Color gizmoColor = Color.green;
    public float gizmoRadius = 0.8f;

    private readonly List<CarTrafficAgent> aliveCars = new List<CarTrafficAgent>();
    private float nextSpawnTime;

    private void Start()
    {
        ScheduleNextSpawn();

        if (spawnOnStart)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                TrySpawn();
            }
        }
    }

    private void Update()
    {
        CleanupDeadCars();

        if (!keepSpawning)
            return;

        if (Time.time < nextSpawnTime)
            return;

        TrySpawn();
        ScheduleNextSpawn();
    }

    private void TrySpawn()
    {
        if (route == null || route.Count == 0)
        {
            Debug.LogWarning($"{name}: Route가 없거나 Waypoint가 비어있습니다.");
            return;
        }

        CleanupDeadCars();

        if (aliveCars.Count >= maxAliveFromThisSpawner)
            return;

        VehicleSpawnEntry entry = PickVehicle();

        if (entry == null || entry.prefab == null)
            return;

        Vector3 spawnPos = route.GetWaypointPosition(startWaypointIndex);
        Quaternion spawnRot = GetSpawnRotation(spawnPos);

        Transform parent = runtimeParent != null ? runtimeParent : null;

        GameObject carObj = Instantiate(entry.prefab, spawnPos, spawnRot, parent);
        carObj.name = entry.prefab.name + "_Runtime";

        CarTrafficAgent agent = carObj.GetComponent<CarTrafficAgent>();

        if (agent == null)
        {
            Debug.LogWarning($"{carObj.name}: CarTrafficAgent가 없습니다.");
            Destroy(carObj);
            return;
        }

        agent.route = route;
        agent.startWaypointIndex = startWaypointIndex;
        agent.snapToStartOnPlay = true;

        agent.moveSpeed = entry.overrideSpeed ? entry.moveSpeed : defaultMoveSpeed;
        agent.rotationSpeed = defaultRotationSpeed;
        agent.arriveDistance = defaultArriveDistance;
        agent.groundOffset = defaultGroundOffset;

        aliveCars.Add(agent);
    }

    private VehicleSpawnEntry PickVehicle()
    {
        if (vehicles == null || vehicles.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (VehicleSpawnEntry entry in vehicles)
        {
            if (entry == null || entry.prefab == null)
                continue;

            totalWeight += Mathf.Max(0f, entry.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (VehicleSpawnEntry entry in vehicles)
        {
            if (entry == null || entry.prefab == null)
                continue;

            current += Mathf.Max(0f, entry.weight);

            if (randomValue <= current)
                return entry;
        }

        return vehicles[vehicles.Count - 1];
    }

    private Quaternion GetSpawnRotation(Vector3 spawnPos)
    {
        int nextIndex = route.GetNextIndex(startWaypointIndex);

        if (nextIndex == -1)
            return transform.rotation;

        Vector3 nextPos = route.GetWaypointPosition(nextIndex);
        Vector3 dir = nextPos - spawnPos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private void ScheduleNextSpawn()
    {
        float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        nextSpawnTime = Time.time + interval;
    }

    private void CleanupDeadCars()
    {
        for (int i = aliveCars.Count - 1; i >= 0; i--)
        {
            if (aliveCars[i] == null)
                aliveCars.RemoveAt(i);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

        if (route != null && route.Count > 0)
        {
            Vector3 routeStart = route.GetWaypointPosition(startWaypointIndex);
            Gizmos.DrawLine(transform.position, routeStart);
        }
    }
}