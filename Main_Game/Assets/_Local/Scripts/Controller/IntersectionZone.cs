using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple exclusive intersection reservation. Only one vehicle allowed inside zone at a time.
/// Vehicles call VehicleEntered/VehicleExited (via trigger events in AIVehicleController).
/// </summary>
[RequireComponent(typeof(Collider))]
public class IntersectionZone : MonoBehaviour
{
	private Transform currentVehicle;
	private Queue<Transform> waitingQueue = new Queue<Transform>();

	private void Awake()
	{
		var col = GetComponent<Collider>();
		col.isTrigger = true;
	}

	/// <summary>
	/// Called by vehicle when it enters the trigger. We queue them and only allow one at a time.
	/// </summary>
	public void VehicleEntered(Transform vehicle)
	{
		if (currentVehicle == null)
		{
			currentVehicle = vehicle;
		}
		else
		{
			if (!waitingQueue.Contains(vehicle))
				waitingQueue.Enqueue(vehicle);
		}
	}

	/// <summary>
	/// Called by vehicle when it exits. Releases next in queue.
	/// </summary>
	public void VehicleExited(Transform vehicle)
	{
		if (currentVehicle == vehicle)
		{
			currentVehicle = null;
			if (waitingQueue.Count > 0)
			{
				currentVehicle = waitingQueue.Dequeue();
			}
		}
		else
		{
			// maybe it was queued but left early
			var newQ = new Queue<Transform>();
			while (waitingQueue.Count > 0)
			{
				var t = waitingQueue.Dequeue();
				if (t != vehicle) newQ.Enqueue(t);
			}
			waitingQueue = newQ;
		}
	}

	/// <summary>
	/// Used by AI to check if they are allowed to pass (true if currentVehicle is null or it's them).
	/// </summary>
	public bool IsVehicleAllowedThrough(Transform vehicle)
	{
		return currentVehicle == null || currentVehicle == vehicle;
	}

	/// <summary>
	/// Simple helper: approximate distance from point to center of zone.
	/// </summary>
	public float DistanceToZone(Vector3 point)
	{
		return Vector3.Distance(transform.position, point);
	}
}
