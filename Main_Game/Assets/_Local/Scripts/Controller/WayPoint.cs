using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Waypoint node used by AI and editor tools.
/// PreviousWayPoint / NextWayPoint are WayPoint references for easy chaining and transform access.
/// WayPointsAround contains alternative branches from this waypoint.
/// </summary>
[DisallowMultipleComponent]
public class WayPoint : MonoBehaviour
{
	[Header("Links")]
	public WayPoint PreviousWayPoint;
	public WayPoint NextWayPoint;

	[Tooltip("Alternative branch waypoints (used when NextWayPoint is null).")]
	public WayPoint[] WayPointsAround;

	[Header("Gizmo / lane width")]
	[Tooltip("Lateral width for gizmos and path offset.")]
	public float Width = 3f;

	/// <summary>
	/// Convenient way to get NextWayPoint as a Transform (for compatibility with code that expects a Transform).
	/// Returns null if there is no NextWayPoint.
	/// </summary>
	public Transform NextWayPointTransform => NextWayPoint != null ? NextWayPoint.transform : null;

	/// <summary>
	/// Convenience helper: list of transforms for branching code that expects Transform[] or List&lt;Transform&gt;.
	/// </summary>
	public List<Transform> Next_Points
	{
		get
		{
			var list = new List<Transform>();
			if (WayPointsAround != null)
			{
				for (int i = 0; i < WayPointsAround.Length; i++)
					if (WayPointsAround[i] != null)
						list.Add(WayPointsAround[i].transform);
			}
			return list;
		}
	}

#if UNITY_EDITOR
	// draw a small label in the editor (optional)
	private void OnDrawGizmosSelected()
	{
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.25f, name);
	}
#endif
}
