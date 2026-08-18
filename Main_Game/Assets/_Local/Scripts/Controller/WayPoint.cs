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

	[Tooltip("Optional branch choices. If populated, these are used as routing options and NextWayPoint acts as the straight/default choice.")]
	public WayPoint[] WayPointsAround;

	[Header("Gizmo / lane width")]
	[Tooltip("Lateral width for gizmos and path offset.")]
	public float Width = 3f;

	/// <summary>
	/// Resolves the next waypoint using this node's authored routes.
	/// Branch entries in WayPointsAround are treated as primary choices and
	/// NextWayPoint is included as the straight/default option when it is not already present.
	/// When no route is configured, advance to the next lane group's first waypoint so cars keep circulating.
	/// </summary>
	public WayPoint ResolveNextWaypoint(WayPoint previousWaypoint = null, Vector3 preferredForward = default)
	{
		if (IsTerminalWaypoint())
			return FindLoopFallback();

		var candidates = GetRouteCandidates();

		if (previousWaypoint != null && candidates.Count > 1)
			candidates.RemoveAll(candidate => candidate == previousWaypoint);

		if (candidates.Count == 1)
			return candidates[0];

		if (candidates.Count > 1)
		{
			Vector3 heading = preferredForward.sqrMagnitude > 0.001f
				? preferredForward.normalized
				: GetFallbackForward(previousWaypoint);

			return ChooseBestCandidate(candidates, heading);
		}

		return FindLoopFallback();
	}

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

			if (NextWayPoint != null && !list.Contains(NextWayPoint.transform))
				list.Add(NextWayPoint.transform);

			return list;
		}
	}

	public List<WayPoint> GetRouteCandidates()
	{
		var candidates = new List<WayPoint>();

		if (WayPointsAround != null)
		{
			for (int i = 0; i < WayPointsAround.Length; i++)
				AddCandidate(candidates, WayPointsAround[i]);
		}

		AddCandidate(candidates, NextWayPoint);
		return candidates;
	}

	private void AddCandidate(List<WayPoint> candidates, WayPoint candidate)
	{
		if (candidate == null || candidate == this || candidates.Contains(candidate))
			return;

		candidates.Add(candidate);
	}

	private WayPoint ChooseBestCandidate(List<WayPoint> candidates, Vector3 heading)
	{
		WayPoint bestCandidate = null;
		float bestScore = float.PositiveInfinity;

		for (int i = 0; i < candidates.Count; i++)
		{
			var candidate = candidates[i];
			if (candidate == null)
				continue;

			Vector3 toCandidate = candidate.transform.position - transform.position;
			if (toCandidate.sqrMagnitude < 0.001f)
				continue;

			Vector3 candidateDirection = toCandidate.normalized;
			float turnAngle = Vector3.Angle(heading, candidateDirection);
			float distancePenalty = toCandidate.magnitude * 0.08f;
			float score = turnAngle + distancePenalty;

			if (score < bestScore)
			{
				bestScore = score;
				bestCandidate = candidate;
			}
		}

		return bestCandidate;
	}

	private Vector3 GetFallbackForward(WayPoint previousWaypoint)
	{
		if (previousWaypoint != null)
		{
			Vector3 incoming = transform.position - previousWaypoint.transform.position;
			if (incoming.sqrMagnitude > 0.001f)
				return incoming.normalized;
		}

		if (PreviousWayPoint != null)
		{
			Vector3 incoming = transform.position - PreviousWayPoint.transform.position;
			if (incoming.sqrMagnitude > 0.001f)
				return incoming.normalized;
		}

		return transform.forward;
	}

	private WayPoint FindLoopFallback()
	{
		var parent = transform.parent;
		if (parent == null)
			return null;

		var root = parent.parent;
		if (root != null)
		{
			int currentParentIndex = parent.GetSiblingIndex();
			for (int offset = 1; offset <= root.childCount; offset++)
			{
				int siblingIndex = (currentParentIndex + offset) % root.childCount;
				var laneParent = root.GetChild(siblingIndex);
				var firstWaypoint = FindFirstWaypointInParent(laneParent);
				if (firstWaypoint != null)
					return firstWaypoint;
			}
		}

		for (int i = 0; i < parent.childCount; i++)
		{
			var sibling = parent.GetChild(i).GetComponent<WayPoint>();
			if (sibling != null && sibling != this)
				return sibling;
		}

		return null;
	}

	private bool IsTerminalWaypoint()
	{
		return name.StartsWith("Last");
	}

	private WayPoint FindFirstWaypointInParent(Transform laneParent)
	{
		if (laneParent == null)
			return null;

		for (int i = 0; i < laneParent.childCount; i++)
		{
			var waypoint = laneParent.GetChild(i).GetComponent<WayPoint>();
			if (waypoint != null)
				return waypoint;
		}

		return null;
	}

#if UNITY_EDITOR
	// draw a small label in the editor (optional)
	private void OnDrawGizmosSelected()
	{
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.25f, name);
	}
#endif
}
