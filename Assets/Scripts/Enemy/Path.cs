using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();
    [SerializeField]
    private bool alwaysDrawPath = true;
    [SerializeField]
    private Color pathColor = Color.green;

    void OnDrawGizmos()
    {
        if (alwaysDrawPath)
        {
            DrawPathGizmos();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!alwaysDrawPath)
        {
            DrawPathGizmos();
        }
    }

    private void DrawPathGizmos()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        Gizmos.color = pathColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            // Draw sphere at waypoint
            Gizmos.DrawSphere(waypoints[i].position, 0.4f);

            // Draw line to next waypoint (connecting loop)
            int nextIndex = (i + 1) % waypoints.Count;
            if (waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
            }
        }
    }
}
