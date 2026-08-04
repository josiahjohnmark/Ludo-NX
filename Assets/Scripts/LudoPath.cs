using System.Collections.Generic;
using UnityEngine;

public class LudoPath : MonoBehaviour
{
    public List<TileNode> waypoints = new List<TileNode>();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].transform.position, 0.15f);
                if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
                }
            }
        }
    }
}