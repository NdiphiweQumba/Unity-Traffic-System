using System;
using System.ComponentModel;
using System.Drawing;
using UnityEditor;
using UnityEngine;

public class WaypointEditor {
    [DrawGizmo (GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]

    public static void OnDrawSceneGizmo (WayPoint waypoint, GizmoType gizmotype) {
        if ((gizmotype & GizmoType.Selected) != 0) {
            Gizmos.color = Color.yellow;
        } else {
            Gizmos.color = Color.yellow * .5f;
        }

        Gizmos.DrawSphere (waypoint.transform.position, 1f);

        Gizmos.color = Color.white;

        Gizmos.DrawLine (waypoint.transform.position + (waypoint.transform.right * waypoint.Width / 2),
            waypoint.transform.position - (waypoint.transform.right * waypoint.Width / 2));

        if (waypoint.PreviousWayPoint != null) {
            Gizmos.color = Color.red;
            Vector3 offset = waypoint.transform.right * waypoint.Width / 2f;
            Vector3 offsetto = waypoint.PreviousWayPoint.transform.right * waypoint.PreviousWayPoint.Width / 2f;

            Gizmos.DrawLine (waypoint.transform.position + offset, waypoint.PreviousWayPoint.transform.position + offsetto);
        }

        if (waypoint.NextWayPoint != null) {
            Gizmos.color = Color.green;
            Vector3 offset = waypoint.transform.right * -waypoint.Width / 2f;
            Vector3 offsetto = waypoint.NextWayPoint.transform.right * -waypoint.NextWayPoint.Width / 2f;

            Gizmos.DrawLine (waypoint.transform.position + offset, waypoint.NextWayPoint.transform.position + offsetto);
        }

    }

}