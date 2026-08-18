using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CarController), typeof(Rigidbody))]
public class AIVehicleController : MonoBehaviour
{
	[Header("Sensors")]
	public float sensorLength = 10f;
	public float sideSensorAngle = 30f;
	public float sensorSpacing = 0.5f;
	public LayerMask obstacleMask;

	[Header("Driving")]
	public float stopDistance = 6f;    // distance to start strong braking for obstacle
	public float slowDistance = 12f;   // distance to start slowing
	[Range(0f, 1f)] public float accelDamping = 0.9f;

	[Header("Behavior")]
	public bool IsDriving = true;
	[Tooltip("Used to nudge cars laterally so they don't take identical path")]
	public float wanderAmount = 1.5f;
	[Tooltip("How long to hold the brake after a hard collision.")]
	public float collisionBrakeDuration = 0.8f;

	private CarController car;
	private Rigidbody rb;
	private Collider[] ownColliders;
	private float randomPerlin;
	private float blockedUntilTime = 0f;
	private float collisionSteerBias = 0f;
	private IntersectionZone currentIntersectionZone;

	private void Awake()
	{
		car = GetComponent<CarController>();
		rb = GetComponent<Rigidbody>();
		ownColliders = GetComponentsInChildren<Collider>();
		randomPerlin = Random.value * 100f;
		if (obstacleMask.value == 0)
			obstacleMask = Physics.DefaultRaycastLayers;

		if (rb != null)
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		}
	}

	private void FixedUpdate()
	{
		if (!car) return;

		if (!IsDriving)
		{
			car.Drive(0f, 0f, 1f);
			return;
		}

		// Navigation target is car.CurrentWayPoint
		if (car.CurrentWayPoint == null)
		{
			car.Drive(0f, 0f, 0f);
			return;
		}

		// compute target offset so cars don't run exact center line
		Vector3 targetPos = car.CurrentWayPoint.position;
		float lateralNoise = Mathf.PerlinNoise(Time.time * 0.1f, randomPerlin) * 2f - 1f;
		targetPos += car.CurrentWayPoint.right * (lateralNoise * wanderAmount);

		// basic steering to waypoint
		Vector3 localTarget = transform.InverseTransformPoint(targetPos);
		float steerAngle = Mathf.Clamp(Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg * 0.02f, -1f, 1f);

		// base desired speed by waypoint distance/angle
		float desiredSpeed = car.TopSpeed;
		float approachingAngle = Vector3.Angle(car.CurrentWayPoint.forward, rb.linearVelocity.sqrMagnitude > 0.1f ? rb.linearVelocity.normalized : transform.forward);
		float angleFactor = Mathf.InverseLerp(0f, 90f, approachingAngle);
		desiredSpeed = Mathf.Lerp(car.TopSpeed, car.TopSpeed * 0.5f, angleFactor);

		float distToWP = Vector3.Distance(transform.position, car.CurrentWayPoint.position);
		float distFactor = Mathf.InverseLerp(0f, 100f, distToWP);
		desiredSpeed = Mathf.Lerp(car.TopSpeed * 0.5f, car.TopSpeed, distFactor);

		// sensors - forward, slight left/right and side rays
		bool obstacleInFront = false;
		float obstacleDistance = Mathf.Infinity;
		RaycastHit hit;

		Vector3 sensorOrigin = transform.position + transform.up * 0.8f + transform.forward * 1.2f;

		// center forward sensor
		if (TryGetObstacle(sensorOrigin, transform.forward, sensorLength, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
		}

		// angled sensors
		Vector3 rightDir = Quaternion.AngleAxis(sideSensorAngle, transform.up) * transform.forward;
		Vector3 leftDir = Quaternion.AngleAxis(-sideSensorAngle, transform.up) * transform.forward;

		if (TryGetObstacle(sensorOrigin, rightDir, sensorLength * 0.9f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			// steer away from obstacle
			steerAngle -= 0.6f;
		}
		if (TryGetObstacle(sensorOrigin, leftDir, sensorLength * 0.9f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			steerAngle += 0.6f;
		}

		// lateral clearance sensors (helps avoid scraping at intersections)
		Vector3 rightSensorOrigin = sensorOrigin + transform.right * sensorSpacing;
		Vector3 leftSensorOrigin = sensorOrigin - transform.right * sensorSpacing;
		if (TryGetObstacle(rightSensorOrigin, transform.forward, sensorLength * 0.8f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			steerAngle -= 0.3f;
		}
		if (TryGetObstacle(leftSensorOrigin, transform.forward, sensorLength * 0.8f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			steerAngle += 0.3f;
		}

		// Intersection handling (if we're waiting for permission to enter intersection, we should stop)
		if (currentIntersectionZone != null && !currentIntersectionZone.IsVehicleAllowedThrough(this.transform))
		{
			// slow to stop before intersection
			float distToIntersection = currentIntersectionZone.DistanceToZone(transform.position);
			if (distToIntersection < 10f)
			{
				// come to a full stop if close
				car.Drive(0f, 0f, 1f);
				return;
			}
			else
			{
				desiredSpeed = Mathf.Min(desiredSpeed, car.TopSpeed * 0.25f);
			}
		}

		// If obstacle in front -> brake or reduce speed according to distance
		float accelInput = 0f;
		float brakeInput = 0f;

		if (Time.time < blockedUntilTime)
		{
			accelInput = 0f;
			brakeInput = 1f;
			steerAngle = Mathf.Clamp(steerAngle + collisionSteerBias, -1f, 1f);
		}
		else if (obstacleInFront)
		{
			// strong braking if close
			if (obstacleDistance < stopDistance)
			{
				brakeInput = 1f;
				accelInput = 0f;
			}
			else if (obstacleDistance < slowDistance)
			{
				float slowFactor = Mathf.InverseLerp(slowDistance, stopDistance, obstacleDistance);
				float targetSpeed = Mathf.Lerp(0f, desiredSpeed, slowFactor);
				float speedDelta = targetSpeed - car.CurrentSpeed;
				accelInput = Mathf.Clamp(speedDelta * 0.1f, -1f, 1f) * accelDamping;
				brakeInput = speedDelta < -0.5f ? Mathf.Clamp(-speedDelta * 0.5f, 0f, 1f) : 0f;
			}
		}
		else
		{
			// no obstacle -> go to desired speed smoothly
			float speedDelta = desiredSpeed - car.CurrentSpeed;
			accelInput = Mathf.Clamp(speedDelta * 0.05f, -1f, 1f);
			brakeInput = 0f;
		}

		// small safety: if two vehicles approach from opposite directions (both trying to stop) prefer to yield if we are slower
		// (handled by intersection zone and sensors)

		// Apply drive (steer normalized -1..1)
		steerAngle = Mathf.Clamp(steerAngle, -1f, 1f);
		car.Drive(steerAngle, accelInput, brakeInput);
	}

	private void OnTriggerEnter(Collider other)
	{
		// detect intersection zone (tag "IntersectionZone")
		if (other.TryGetComponent<IntersectionZone>(out var zone))
		{
			currentIntersectionZone = zone;
			currentIntersectionZone.VehicleEntered(this.transform);
		}

		if (other.CompareTag("StopPoint"))
		{
			// come to a dead stop
			IsDriving = false;
			StartCoroutine(ResumeAfterStop(1.5f));
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent<IntersectionZone>(out var zone))
		{
			if (currentIntersectionZone == zone)
			{
				zone.VehicleExited(this.transform);
				currentIntersectionZone = null;
			}
		}
	}

	private IEnumerator ResumeAfterStop(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		IsDriving = true;
	}

	private void OnCollisionEnter(Collision collision)
	{
		HandleCollision(collision);
	}

	private void OnCollisionStay(Collision collision)
	{
		HandleCollision(collision);
	}

	private bool TryGetObstacle(Vector3 origin, Vector3 direction, float length, out RaycastHit closestHit)
	{
		var hits = Physics.RaycastAll(origin, direction.normalized, length, obstacleMask, QueryTriggerInteraction.Ignore);
		float bestDistance = float.PositiveInfinity;
		closestHit = default;

		for (int i = 0; i < hits.Length; i++)
		{
			var candidate = hits[i];
			if (candidate.collider == null || IsOwnCollider(candidate.collider))
				continue;

			if (candidate.distance < bestDistance)
			{
				bestDistance = candidate.distance;
				closestHit = candidate;
			}
		}

		return bestDistance < float.PositiveInfinity;
	}

	private bool IsOwnCollider(Collider collider)
	{
		if (collider == null) return false;

		for (int i = 0; i < ownColliders.Length; i++)
		{
			if (ownColliders[i] == collider)
				return true;
		}

		return false;
	}

	private void HandleCollision(Collision collision)
	{
		if (collision == null || collision.contactCount == 0)
			return;

		blockedUntilTime = Time.time + collisionBrakeDuration;

		Vector3 localContact = transform.InverseTransformPoint(collision.GetContact(0).point);
		if (Mathf.Abs(localContact.x) > 0.05f)
		{
			collisionSteerBias = localContact.x > 0f ? -0.75f : 0.75f;
		}
		else
		{
			collisionSteerBias = Random.value > 0.5f ? 0.5f : -0.5f;
		}
	}
}
