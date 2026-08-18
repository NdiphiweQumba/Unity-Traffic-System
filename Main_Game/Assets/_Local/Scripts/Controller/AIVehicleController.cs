using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CarController), typeof(Rigidbody))]
public class AIVehicleController : MonoBehaviour
{
	[Header("Sensors")]
	public float sensorLength = 10f;
	public float sensorRadius = 0.75f;
	public float sideSensorAngle = 30f;
	public float sensorSpacing = 0.5f;
	public LayerMask obstacleMask;

	[Header("Driving")]
	public float stopDistance = 6f;    // distance to start strong braking for obstacle
	public float slowDistance = 12f;   // distance to start slowing
	[Range(0f, 1f)] public float accelDamping = 0.9f;
	public float waypointTurnInDistance = 4f;

	[Header("Behavior")]
	public bool IsDriving = true;
	[Tooltip("Used to nudge cars laterally so they don't take identical path")]
	public float wanderAmount = 0.1f;
	[Tooltip("How long to hold the brake after a hard collision.")]
	public float collisionBrakeDuration = 0.8f;

	private CarController car;
	private Rigidbody rb;
	private Collider[] ownColliders;
	private CollisionHandler[] collisionHandlers;
	private float randomPerlin;
	private float blockedUntilTime = 0f;
	private float collisionSteerBias = 0f;
	private IntersectionZone currentIntersectionZone;

	private const float RuntimeSensorLength = 12f;
	private const float RuntimeSensorRadius = 1f;
	private const float RuntimeSideSensorAngle = 22f;
	private const float RuntimeStopDistance = 8f;
	private const float RuntimeSlowDistance = 18f;
	private const float RuntimeTurnInDistance = 2.5f;
	private const float RuntimeWanderAmount = 0.02f;
	private const float TurnBrakeLookaheadDistance = 16f;
	private const int RuntimeObstacleMask = Physics.DefaultRaycastLayers;

	private void Awake()
	{
		car = GetComponent<CarController>();
		rb = GetComponent<Rigidbody>();
		ownColliders = GetComponentsInChildren<Collider>();
		collisionHandlers = GetComponentsInChildren<CollisionHandler>(true);
		randomPerlin = Random.value * 100f;
		ApplyRuntimeTuning();

		if (rb != null)
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		}

		if (car != null)
		{
			car.TopSpeed = Mathf.Clamp(car.TopSpeed <= 0f ? 20f : car.TopSpeed, 12f, 22f);
			car.maxTorque = Mathf.Clamp(car.maxTorque <= 0f ? 300f : car.maxTorque, 200f, 350f);
			car.MaxBrakeTorque = Mathf.Max(car.MaxBrakeTorque, 2500f);
		}
	}

	private void OnValidate()
	{
		ApplyRuntimeTuning();
	}

	private void ApplyRuntimeTuning()
	{
		if (sensorLength <= 0f || sensorLength > 20f)
			sensorLength = RuntimeSensorLength;

		if (sensorRadius <= 0f || sensorRadius > 2f)
			sensorRadius = RuntimeSensorRadius;

		sensorLength = Mathf.Clamp(sensorLength, 8f, 16f);
		sensorRadius = Mathf.Clamp(sensorRadius, 0.75f, 1.2f);
		sideSensorAngle = Mathf.Clamp(sideSensorAngle <= 0f ? RuntimeSideSensorAngle : sideSensorAngle, 10f, 25f);
		stopDistance = Mathf.Clamp(stopDistance <= 0f ? RuntimeStopDistance : stopDistance, 6f, 10f);
		slowDistance = Mathf.Clamp(slowDistance <= 0f ? RuntimeSlowDistance : slowDistance, stopDistance + 4f, 22f);
		waypointTurnInDistance = Mathf.Clamp(waypointTurnInDistance <= 0f ? RuntimeTurnInDistance : waypointTurnInDistance, 1.5f, 3f);
		wanderAmount = Mathf.Clamp(wanderAmount, 0f, RuntimeWanderAmount);

		obstacleMask = RuntimeObstacleMask;
	}

	private void FixedUpdate()
	{
		if (!car) return;
		car.AdvanceWaypointIfNeeded();

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

		Vector3 currentPos = car.CurrentWayPoint.position;
		Vector3 approachDirection = car.GetApproachDirection();
		Vector3 pathDirection = car.GetPathDirection();
		float distToWP = Vector3.Distance(transform.position, currentPos);
		Vector3 nextPos = car.NextWayPoint != null ? car.NextWayPoint.position : currentPos + pathDirection * 10f;
		float approachPreviewDistance = Mathf.Clamp(distToWP * 0.45f, 1.25f, 6f);
		Vector3 targetPos = currentPos - approachDirection * approachPreviewDistance;

		if (car.NextWayPoint != null)
		{
			float turnInBlend = 1f - Mathf.Clamp01((distToWP - 0.75f) / Mathf.Max(0.01f, waypointTurnInDistance - 0.75f));
			Vector3 exitDirection = (nextPos - currentPos).normalized;
			Vector3 turnPreview = currentPos + exitDirection * Mathf.Clamp(distToWP * 0.18f, 0.2f, 1f);
			targetPos = Vector3.Lerp(targetPos, turnPreview, turnInBlend * 0.22f);
		}

		float lateralNoise = Mathf.PerlinNoise(Time.time * 0.08f, randomPerlin) * 2f - 1f;
		float laneOffset = Mathf.Clamp(wanderAmount, 0f, 0.04f);
		targetPos += car.CurrentWayPoint.right * (lateralNoise * laneOffset);

		// basic steering to waypoint
		Vector3 localTarget = transform.InverseTransformPoint(targetPos);
		float steerAngle = Mathf.Clamp(Mathf.Atan2(localTarget.x, Mathf.Max(0.75f, localTarget.z)) * 1.5f, -1f, 1f);

		// base desired speed by waypoint distance/angle
		float desiredSpeed = car.TopSpeed;
		float approachingAngle = Vector3.Angle(transform.forward, (targetPos - transform.position).normalized);
		float angleFactor = Mathf.InverseLerp(0f, 75f, approachingAngle);
		desiredSpeed = Mathf.Lerp(car.TopSpeed, car.TopSpeed * 0.25f, angleFactor);
		float turnDemand = 0f;

		Vector3 currentToNext = nextPos - currentPos;
		if (currentToNext.sqrMagnitude > 0.01f)
		{
			float turnAngle = Vector3.Angle(approachDirection, currentToNext.normalized);
			float turnSlowFactor = Mathf.InverseLerp(10f, 75f, turnAngle);
			float intersectionApproach = 1f - Mathf.Clamp01((distToWP - 2f) / TurnBrakeLookaheadDistance);
			turnDemand = turnSlowFactor * intersectionApproach;
			desiredSpeed = Mathf.Lerp(desiredSpeed, car.TopSpeed * 0.22f, turnDemand);
		}

		float distFactor = Mathf.InverseLerp(0f, 25f, distToWP);
		desiredSpeed = Mathf.Lerp(car.TopSpeed * 0.2f, desiredSpeed, distFactor);

		// sensors - forward, slight left/right and side rays
		bool obstacleInFront = false;
		float obstacleDistance = Mathf.Infinity;
		float closingSpeed = 0f;
		RaycastHit hit;

		Vector3 sensorOrigin = transform.position + transform.up * 0.8f + transform.forward * 1.4f;

		// center forward sensor
		if (TryGetObstacle(sensorOrigin, transform.forward, sensorLength, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			closingSpeed = Mathf.Max(closingSpeed, GetClosingSpeed(hit.collider));
		}

		// angled sensors
		Vector3 rightDir = Quaternion.AngleAxis(sideSensorAngle, transform.up) * transform.forward;
		Vector3 leftDir = Quaternion.AngleAxis(-sideSensorAngle, transform.up) * transform.forward;

		if (TryGetObstacle(sensorOrigin, rightDir, sensorLength * 0.9f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			closingSpeed = Mathf.Max(closingSpeed, GetClosingSpeed(hit.collider));
			steerAngle -= 0.18f;
		}
		if (TryGetObstacle(sensorOrigin, leftDir, sensorLength * 0.9f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			closingSpeed = Mathf.Max(closingSpeed, GetClosingSpeed(hit.collider));
			steerAngle += 0.18f;
		}

		// lateral clearance sensors (helps avoid scraping at intersections)
		Vector3 rightSensorOrigin = sensorOrigin + transform.right * sensorSpacing;
		Vector3 leftSensorOrigin = sensorOrigin - transform.right * sensorSpacing;
		if (TryGetObstacle(rightSensorOrigin, transform.forward, sensorLength * 0.8f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			closingSpeed = Mathf.Max(closingSpeed, GetClosingSpeed(hit.collider));
			steerAngle -= 0.1f;
		}
		if (TryGetObstacle(leftSensorOrigin, transform.forward, sensorLength * 0.8f, out hit))
		{
			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, hit.distance);
			closingSpeed = Mathf.Max(closingSpeed, GetClosingSpeed(hit.collider));
			steerAngle += 0.1f;
		}

		MergeCollisionProbeHits(ref obstacleInFront, ref obstacleDistance, ref closingSpeed);

		// Intersection handling (if we're waiting for permission to enter intersection, we should stop)
		if (currentIntersectionZone != null && !currentIntersectionZone.IsVehicleAllowedThrough(this.transform))
		{
			// slow to stop before intersection
			float distToIntersection = currentIntersectionZone.DistanceToZone(transform.position);
			if (distToIntersection < 14f)
			{
				// come to a full stop if close
				car.Drive(0f, 0f, 1f);
				return;
			}
			else
			{
				desiredSpeed = Mathf.Min(desiredSpeed, car.TopSpeed * 0.18f);
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
			float dynamicStopDistance = stopDistance + Mathf.Clamp(closingSpeed * 1.1f, 0f, 14f);
			float dynamicSlowDistance = Mathf.Max(slowDistance + 10f, dynamicStopDistance + 8f);

			if (obstacleDistance < dynamicStopDistance)
			{
				brakeInput = 1f;
				accelInput = 0f;
				steerAngle *= 0.35f;
			}
			else if (obstacleDistance < dynamicSlowDistance)
			{
				float slowFactor = Mathf.InverseLerp(dynamicSlowDistance, dynamicStopDistance, obstacleDistance);
				float targetSpeed = Mathf.Lerp(0f, desiredSpeed, slowFactor);
				float speedDelta = targetSpeed - car.CurrentSpeed;
				accelInput = speedDelta > 0f ? Mathf.Clamp(speedDelta * 0.06f, 0f, 1f) * accelDamping : 0f;
				brakeInput = speedDelta < -0.1f ? Mathf.Clamp(-speedDelta * 1.15f, 0f, 1f) : 0f;
				if (brakeInput > 0.2f)
					steerAngle *= 0.6f;
			}
		}
		else
		{
			// no obstacle -> accelerate when under target speed, brake when entering a tight turn too fast
			float speedDelta = desiredSpeed - car.CurrentSpeed;
			if (speedDelta >= 0f)
			{
				accelInput = Mathf.Clamp(speedDelta * 0.05f, 0f, 1f);
				brakeInput = 0f;
			}
			else
			{
				accelInput = 0f;
				float overspeed = -speedDelta;
				float turnBrakeFactor = Mathf.InverseLerp(0.75f, car.TopSpeed * 0.65f, overspeed);
				brakeInput = Mathf.Clamp01(Mathf.Max(turnBrakeFactor, turnDemand * 0.85f));
				if (brakeInput > 0.2f)
					steerAngle *= 0.7f;
			}
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
		var hits = Physics.SphereCastAll(origin, sensorRadius, direction.normalized, length, obstacleMask, QueryTriggerInteraction.Ignore);
		float bestDistance = float.PositiveInfinity;
		closestHit = default;

		for (int i = 0; i < hits.Length; i++)
		{
			var candidate = hits[i];
			if (candidate.collider == null || IsOwnCollider(candidate.collider))
				continue;

			if (!ShouldTreatAsObstacle(candidate.collider))
				continue;

			if (candidate.distance < bestDistance)
			{
				bestDistance = candidate.distance;
				closestHit = candidate;
			}
		}

		return bestDistance < float.PositiveInfinity;
	}

	private void MergeCollisionProbeHits(ref bool obstacleInFront, ref float obstacleDistance, ref float closingSpeed)
	{
		if (collisionHandlers == null || collisionHandlers.Length == 0)
			return;

		for (int i = 0; i < collisionHandlers.Length; i++)
		{
			var probe = collisionHandlers[i];
			if (probe == null || !probe.enabled || !probe.gameObject.activeInHierarchy || !probe.HasHit)
				continue;

			obstacleInFront = true;
			obstacleDistance = Mathf.Min(obstacleDistance, probe.CurrentHitDistance);
			closingSpeed = Mathf.Max(closingSpeed, GetClosingSpeed(probe.CurrentHitCollider));
		}
	}

	private bool ShouldTreatAsObstacle(Collider collider)
	{
		if (collider == null)
			return false;

		if (collider.isTrigger)
			return false;

		if (IsOwnCollider(collider))
			return false;

		if (collider.TryGetComponent<IntersectionZone>(out _))
			return false;

		if (collider.attachedRigidbody == rb)
			return false;

		if (collider.attachedRigidbody != null)
			return true;

		return true;
	}

	private float GetClosingSpeed(Collider obstacle)
	{
		if (obstacle == null || rb == null)
			return 0f;

		var otherBody = obstacle.attachedRigidbody;
		var otherVelocity = otherBody != null ? otherBody.linearVelocity : Vector3.zero;
		var relativeVelocity = rb.linearVelocity - otherVelocity;
		return Mathf.Max(0f, Vector3.Dot(relativeVelocity, transform.forward));
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
