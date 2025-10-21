using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls access to an intersection trigger zone.
/// Modes:
///  - Exclusive: only one vehicle inside at a time (simple lock)
///  - TrafficLight: a TrafficLight component controls which direction(s) can go
///  - Priority: right-hand-rule style yielding + first-come-first-served for non-conflicting directions
/// 
/// Vehicles should call RequestEntry(thisTransform, approachForward) when approaching. If false is returned
/// they should stop before entering the zone. On successful entry, call VehicleExited on leaving.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IntersectionController : MonoBehaviour
{
	public enum Mode { Exclusive, TrafficLight, Priority }
	public Mode mode = Mode.Priority;

	[Header("Traffic Light (if using TrafficLight mode)")]
	public TrafficLight trafficLight; // optional; if null and mode==TrafficLight, default cycle is used

	[Header("Priority settings")]
	[Tooltip("How many degrees difference between vectors is considered 'same direction' (non-conflicting).")]
	public float sameDirectionAngle = 45f;

	[Tooltip("If another vehicle is on your right within this angle threshold, you will yield in Priority mode.")]
	public float rightHandThreshold = 60f;

	// vehicles currently allowed in zone (may contain multiple vehicles if non-conflicting)
	private readonly HashSet<Transform> allowedVehicles = new HashSet<Transform>();
	// vehicles waiting (queue)
	private readonly Queue<Transform> waitingVehicles = new Queue<Transform>();

	private Collider zoneCollider;

	private void Awake()
	{
		zoneCollider = GetComponent<Collider>();
		zoneCollider.isTrigger = true;
		if (mode == Mode.TrafficLight && trafficLight == null)
		{
			// create a default trafficlight if none supplied
			trafficLight = gameObject.AddComponent<TrafficLight>();
		}
	}

	private void Update()
	{
		if (mode == Mode.TrafficLight && trafficLight != null)
		{
			// periodically allow vehicles when light says go
			if (trafficLight.IsGreen)
			{
				// flush queue for vehicles heading in allowed direction(s)
				TryAllowWaitingVehicles();
			}
		}
		else if (mode == Mode.Exclusive)
		{
			// exclusive: allow next waiting if empty
			if (allowedVehicles.Count == 0 && waitingVehicles.Count > 0)
			{
				var next = waitingVehicles.Dequeue();
				allowedVehicles.Add(next);
			}
		}
		else if (mode == Mode.Priority)
		{
			TryAllowWaitingVehicles();
		}
	}

	/// <summary>
	/// Called by vehicle when it is starting to approach the intersection. 
	/// approachForward should be the vehicle's forward vector (world space).
	/// Returns true if the vehicle is allowed to enter immediately; false if it must wait/stop.
	/// If returns true the vehicle should proceed; when it leaves the zone it should call VehicleExited().
	/// </summary>
	public bool RequestEntry(Transform vehicle, Vector3 approachForward)
	{
		if (vehicle == null) return false;

		// if already allowed, trivially allow
		if (allowedVehicles.Contains(vehicle)) return true;

		switch (mode)
		{
			case Mode.Exclusive:
				if (allowedVehicles.Count == 0)
				{
					allowedVehicles.Add(vehicle);
					return true;
				}
				waitingVehicles.Enqueue(vehicle);
				return false;

			case Mode.TrafficLight:
				// only allow entry if green
				if (trafficLight != null && trafficLight.IsGreen)
				{
					allowedVehicles.Add(vehicle);
					return true;
				}
				waitingVehicles.Enqueue(vehicle);
				return false;

			case Mode.Priority:
				// check conflicts with currently inside vehicles
				bool conflictWithInside = false;
				foreach (var inside in allowedVehicles)
				{
					if (IsConflict(vehicle.position, approachForward, inside.position, inside.forward))
					{
						conflictWithInside = true;
						// if inside vehicle is on our right -> we yield
						if (IsOnRight(vehicle.position, approachForward, inside.position))
						{
							waitingVehicles.Enqueue(vehicle);
							return false;
						}
					}
				}

				// if we found conflicts but none were on our right, we allow (first-come-first-served)
				if (!conflictWithInside)
				{
					allowedVehicles.Add(vehicle);
					return true;
				}
				else
				{
					// there are conflicts but none on our right — to avoid deadlock, prefer first come (if not already queued)
					if (!waitingVehicles.Contains(vehicle))
						waitingVehicles.Enqueue(vehicle);
					TryAllowWaitingVehicles(); // try to let queue progress
					return false;
				}

			default:
				waitingVehicles.Enqueue(vehicle);
				return false;
		}
	}

	/// <summary>
	/// Called by vehicle when it leaves the intersection; removes from allowed set and tries next queued vehicles.
	/// </summary>
	public void VehicleExited(Transform vehicle)
	{
		if (vehicle == null) return;
		if (allowedVehicles.Contains(vehicle)) allowedVehicles.Remove(vehicle);

		// allow next queued vehicles as possible
		TryAllowWaitingVehicles();
	}

	/// <summary>
	/// Tries to promote queued vehicles into the allowed set depending on mode and conflicts.
	/// </summary>
	private void TryAllowWaitingVehicles()
	{
		if (waitingVehicles.Count == 0) return;

		int queueSize = waitingVehicles.Count;
		// iterate through up to queueSize elements (avoid infinite loops)
		for (int i = 0; i < queueSize; i++)
		{
			Transform v = waitingVehicles.Dequeue();
			if (v == null) continue;

			// if already inside skip
			if (allowedVehicles.Contains(v)) continue;

			var forward = v.forward;

			if (mode == Mode.TrafficLight)
			{
				if (trafficLight != null && trafficLight.IsGreen)
				{
					allowedVehicles.Add(v);
				}
				else
				{
					// put back into queue
					waitingVehicles.Enqueue(v);
				}
			}
			else if (mode == Mode.Exclusive)
			{
				if (allowedVehicles.Count == 0)
					allowedVehicles.Add(v);
				else
					waitingVehicles.Enqueue(v);
			}
			else // Priority
			{
				bool blockedByRight = false;
				foreach (var inside in allowedVehicles)
				{
					if (IsOnRight(v.position, v.forward, inside.position))
					{
						blockedByRight = true;
						break;
					}
				}

				if (!blockedByRight)
				{
					// also ensure no strong conflicts with allowed vehicles (overly conservative)
					bool conflict = false;
					foreach (var inside in allowedVehicles)
					{
						if (IsConflict(v.position, v.forward, inside.position, inside.forward))
						{
							conflict = true;
							break;
						}
					}

					if (!conflict)
						allowedVehicles.Add(v);
					else
						waitingVehicles.Enqueue(v);
				}
				else
				{
					waitingVehicles.Enqueue(v);
				}
			}
		}
	}

	/// <summary>
	/// Approximates whether two approach vectors/positions will conflict in the intersection.
	/// We use angle heuristics:
	/// - If directions are similar (small angle) then not conflicting
	/// - If directions are roughly opposite, not conflicting (they pass through without crossing)
	/// - Otherwise treat as conflicting.
	/// </summary>
	private bool IsConflict(Vector3 posA, Vector3 forwardA, Vector3 posB, Vector3 forwardB)
	{
		// angle between forwards
		float angle = Vector3.Angle(forwardA, forwardB);

		// if they head in similar direction, not conflict
		if (angle < sameDirectionAngle) return false;

		// if roughly opposite (straight oncoming), treat as non-conflict (they pass)
		if (Mathf.Abs(angle - 180f) < sameDirectionAngle) return false;

		// otherwise we treat as potentially conflicting
		return true;
	}

	/// <summary>
	/// Returns true if "other" is on the right of "me" (approx).
	/// We compute signed angle from my forward to vector to other; if negative and within threshold -> other is right.
	/// </summary>
	private bool IsOnRight(Vector3 myPos, Vector3 myForward, Vector3 otherPos)
	{
		Vector3 dirToOther = (otherPos - myPos).normalized;
		float signed = SignedAngleBetween(myForward, dirToOther, Vector3.up);
		// other is to our right if signed < 0 and absolute angle is within threshold
		return (signed < 0f && Mathf.Abs(signed) < rightHandThreshold);
	}

	private float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 up)
	{
		float ang = Vector3.Angle(a, b);
		float sign = Mathf.Sign(Vector3.Dot(up, Vector3.Cross(a, b)));
		return ang * sign;
	}

	#region Editor visualization
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		var c = Color.cyan;
		c.a = 0.25f;
		Gizmos.color = c;
		Gizmos.DrawCube(transform.position, transform.localScale);
		// draw allowed vehicles
		Gizmos.color = Color.green;
		foreach (var v in allowedVehicles)
			if (v != null) Gizmos.DrawSphere(v.position + Vector3.up * 0.1f, 0.2f);
		Gizmos.color = Color.yellow;
		foreach (var v in waitingVehicles)
			if (v != null) Gizmos.DrawWireSphere(v.position + Vector3.up * 0.1f, 0.25f);
	}
#endif
	#endregion
}
