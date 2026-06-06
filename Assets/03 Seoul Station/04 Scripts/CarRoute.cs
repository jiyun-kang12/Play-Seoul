using System.Collections.Generic;
using UnityEngine;

public class CarRoute : MonoBehaviour
{
    [Header("Route Settings")]
    public bool loop = true;

    [Tooltip("비워두면 자식 Transform들을 자동으로 Waypoint로 사용합니다.")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Gizmo Settings")]
    public bool drawGizmos = true;
    public Color routeColor = Color.yellow;
    public float waypointRadius = 0.4f;

    public int Count => waypoints == null ? 0 : waypoints.Count;

    private void Reset()
    {
        RefreshWaypointsFromChildren();
    }

    private void OnValidate()
    {
        if (waypoints == null)
            waypoints = new List<Transform>();
    }

    [ContextMenu("Refresh Waypoints From Children")]
    public void RefreshWaypointsFromChildren()
    {
        waypoints.Clear();

        foreach (Transform child in transform)
        {
            waypoints.Add(child);
        }
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0)
            return null;

        if (loop)
        {
            int safeIndex = Mod(index, waypoints.Count);
            return waypoints[safeIndex];
        }

        if (index < 0 || index >= waypoints.Count)
            return null;

        return waypoints[index];
    }

    public int GetNextIndex(int currentIndex)
    {
        if (waypoints == null || waypoints.Count == 0)
            return -1;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= waypoints.Count)
        {
            return loop ? 0 : -1;
        }

        return nextIndex;
    }

    public Vector3 GetWaypointPosition(int index)
    {
        Transform wp = GetWaypoint(index);
        return wp == null ? transform.position : wp.position;
    }

    private int Mod(int value, int count)
    {
        return (value % count + count) % count;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        if (waypoints == null || waypoints.Count == 0)
            return;

        Gizmos.color = routeColor;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            if (current == null)
                continue;

            Gizmos.DrawSphere(current.position, waypointRadius);

            int nextIndex = i + 1;

            if (nextIndex < waypoints.Count)
            {
                Transform next = waypoints[nextIndex];
                if (next != null)
                    Gizmos.DrawLine(current.position, next.position);
            }
            else if (loop && waypoints.Count > 1)
            {
                Transform first = waypoints[0];
                if (first != null)
                    Gizmos.DrawLine(current.position, first.position);
            }
        }
    }
}