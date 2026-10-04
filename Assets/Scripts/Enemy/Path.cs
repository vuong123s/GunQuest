using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();
    [SerializeField]
    private bool alwaysDrawPath = true;
    [SerializeField]
    private Color pathColor = Color.green;

    private void OnDrawGizmos()
    {
        if (alwaysDrawPath)
        {
            DrawPathGizmos();
        }
    }

    private void OnDrawGizmosSelected()
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
            Gizmos.DrawSphere(waypoints[i].position, 0.4f);
            int nextIndex = (i + 1) % waypoints.Count;
            if (waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
            }
        }
    }
}
