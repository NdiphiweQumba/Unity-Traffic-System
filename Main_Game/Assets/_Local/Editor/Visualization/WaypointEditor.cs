// Place this file in Assets/Editor/WaypointEditor.cs (recommended).
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene gizmos for WayPoint objects.
/// Expects the WayPoint class to expose: float Width; WayPoint PreviousWayPoint; WayPoint NextWayPoint;
/// Adjust member names if your WayPoint class uses different names/types.
/// </summary>
[InitializeOnLoad]
public static class WaypointEditor
{
	// DrawGizmo attribute requires a static method with (WayPoint, GizmoType)
	[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
	public static void OnDrawSceneGizmo(WayPoint waypoint, GizmoType gizmoType)
	{
		if (waypoint == null) return;

		// Color when selected vs not selected
		if ((gizmoType & GizmoType.Selected) != 0)
			Gizmos.color = Color.yellow;
		else
			Gizmos.color = Color.yellow * 0.5f;

		// Sphere at waypoint
		float sphereSize = Mathf.Max(0.25f, waypoint.Width * 0.1f); // fallback if Width small or zero
		Gizmos.DrawSphere(waypoint.transform.position, sphereSize);

		// draw lateral width line
		Gizmos.color = Color.white;
		float halfWidth = Mathf.Max(0.01f, waypoint.Width * 0.5f);
		Gizmos.DrawLine(waypoint.transform.position + (waypoint.transform.right * halfWidth),
						waypoint.transform.position - (waypoint.transform.right * halfWidth));

		// Draw connection to previous waypoint (if present)
		if (waypoint.PreviousWayPoint != null)
		{
			Gizmos.color = Color.red;
			var prev = waypoint.PreviousWayPoint;
			if (prev != null)
			{
				float prevHalf = Mathf.Max(0.01f, prev.Width * 0.5f);
				Vector3 offset = waypoint.transform.right * halfWidth;
				Vector3 offsetTo = prev.transform.right * prevHalf;
				Gizmos.DrawLine(waypoint.transform.position + offset, prev.transform.position + offsetTo);
			}
		}

		// Draw connection to next waypoint (if present)
		if (waypoint.NextWayPoint != null)
		{
			Gizmos.color = Color.green;
			var next = waypoint.NextWayPoint;
			if (next != null)
			{
				float nextHalf = Mathf.Max(0.01f, next.Width * 0.5f);
				Vector3 offset = waypoint.transform.right * -halfWidth;
				Vector3 offsetTo = next.transform.right * -nextHalf;
				Gizmos.DrawLine(waypoint.transform.position + offset, next.transform.position + offsetTo);
			}
		}
	}
}
#endif
