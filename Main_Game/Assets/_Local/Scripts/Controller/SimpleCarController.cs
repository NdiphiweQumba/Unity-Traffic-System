using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
	public Action<bool> BrakePress;
	[Header("Wheels")]
	public VehicleInfo[] VehicleInfo;

	[Header("Car tuning")]
	public float TopSpeed = 30f;             // meters per second
	public float maxTorque = 1500f;
	public float maxSteerAngle = 30f;        // degrees
	public float MaxBrakeTorque = 3000f;

	[Header("Navigation")]
	public Transform CurrentWayPoint;
	public Transform NextWayPoint;

	[Header("Debug / runtime")]
	public float CurrentSpeed { get; private set; } // m/s
	public float DistanceToWayPoint { get; private set; }

	[Header("Input")]
	[Tooltip("Enable only for a player-driven car. AI cars should leave this off.")]
	public bool allowDirectPlayerInput = false;

	[Tooltip("Optional debug label for player input state.")]
	public UnityEngine.UI.Text debugStatusText;

	private Rigidbody rb;
	private Vector3 relativeVectorToWaypoint;
	private float randomPerlin;

	private float steerInputSmooth;
	private float torqueInputSmooth;
	private float brakeInputSmooth;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		randomPerlin = UnityEngine.Random.value * 100f;

		if (rb != null)
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		}
	}

	private void FixedUpdate()
	{
		// update current speed (m/s) and call BrakePress so lights or other listeners can respond
		CurrentSpeed = rb.linearVelocity.magnitude;
		BrakePress?.Invoke(brakeInputSmooth > 0.1f || (CurrentSpeed < 0.5f && Mathf.Abs(torqueInputSmooth) < 0.05f));
	}

	/// <summary>
	/// Primary interface used by player or AI to drive the car.
	/// steer: -1..1, accel: -1..1 (negative = reverse), brake: 0..1
	/// </summary>
	[Space(30)]
	public float LeftKey;
	public float RightKey;

	public float DownKey;
	public float UpKey;


	public UnityEngine.UI.Text text;

	public void Drive(float steer, float accel, float brake)
	{
		if (allowDirectPlayerInput)
		{
			steer = Input.GetAxis("Horizontal");
			accel = Mathf.Max(0f, Input.GetAxis("Vertical"));
			brake = Mathf.Clamp01(-Input.GetAxis("Vertical"));

			LeftKey = Mathf.Max(0f, -steer);
			RightKey = Mathf.Max(0f, steer);
			DownKey = brake;
			UpKey = accel;

			if (debugStatusText != null)
			{
				debugStatusText.text = Input.GetKey(KeyCode.Space) ? "Brake key pressed" : string.Empty;
			}
		}
		else
		{
			LeftKey = steer < 0f ? -steer : 0f;
			RightKey = steer > 0f ? steer : 0f;
			DownKey = brake;
			UpKey = accel > 0f ? accel : 0f;
		}

		// smooth the inputs to avoid jitter
		steerInputSmooth = Mathf.Lerp(steerInputSmooth, steer, Time.fixedDeltaTime * 8f);
		torqueInputSmooth = Mathf.Lerp(torqueInputSmooth, accel, Time.fixedDeltaTime * 4f);
		brakeInputSmooth = Mathf.Lerp(brakeInputSmooth, brake, Time.fixedDeltaTime * 10f);

		// limit forward torque if over top speed (only reduce when same direction)
		bool tryingToGoForward = torqueInputSmooth > 0 && rb.linearVelocity.magnitude > 0.1f && Vector3.Dot(transform.forward, rb.linearVelocity.normalized) > 0.5f;
		float speedFactor = 1f;
		if (tryingToGoForward && rb.linearVelocity.magnitude > TopSpeed)
			speedFactor = Mathf.Clamp01(1f - ((rb.linearVelocity.magnitude - TopSpeed) / TopSpeed));

		foreach (var info in VehicleInfo)
		{
			if (info == null) continue;

			// update wheel visuals
			if (info.WheelColliderLeft && info.WheelVisualLeft)
			{
				UpdateWheelPose(info.WheelColliderLeft, info.WheelVisualLeft);
			}
			if (info.WheelColliderRight && info.WheelVisualRight)
			{
				UpdateWheelPose(info.WheelColliderRight, info.WheelVisualRight);
			}

			// steering
			if (info.Steer)
			{
				float targetAngle = maxSteerAngle * steerInputSmooth;
				if (info.WheelColliderLeft != null)
					info.WheelColliderLeft.steerAngle = Mathf.Lerp(info.WheelColliderLeft.steerAngle, targetAngle, Time.fixedDeltaTime * 8f);
				if (info.WheelColliderRight != null)
					info.WheelColliderRight.steerAngle = Mathf.Lerp(info.WheelColliderRight.steerAngle, targetAngle, Time.fixedDeltaTime * 8f);
			}

			// motor torque (applied only to motor wheels)
			if (info.Motor)
			{
				float appliedTorque = maxTorque * torqueInputSmooth * speedFactor;
				// reverse torque control
				if (torqueInputSmooth < 0)
					appliedTorque *= 0.5f; // reduce reverse torque
				if (brakeInputSmooth > 0.05f)
					appliedTorque = 0f;

				if (info.WheelColliderLeft != null)
					info.WheelColliderLeft.motorTorque = Mathf.Lerp(info.WheelColliderLeft.motorTorque, appliedTorque, Time.fixedDeltaTime * 4f);
				if (info.WheelColliderRight != null)
					info.WheelColliderRight.motorTorque = Mathf.Lerp(info.WheelColliderRight.motorTorque, appliedTorque, Time.fixedDeltaTime * 4f);
			}

			// brakes
			if (info.Brakes)
			{
				float appliedBrake = MaxBrakeTorque * brakeInputSmooth;
				// if braking while above top speed, add a bit more braking force
				if (rb.linearVelocity.magnitude > TopSpeed) appliedBrake += (rb.linearVelocity.magnitude - TopSpeed) * 200f;
				if (torqueInputSmooth > 0.1f && brakeInputSmooth < 0.05f)
					appliedBrake = 0f;

				if (info.WheelColliderLeft != null)
					info.WheelColliderLeft.brakeTorque = Mathf.Lerp(info.WheelColliderLeft.brakeTorque, appliedBrake, Time.fixedDeltaTime * 10f);
				if (info.WheelColliderRight != null)
					info.WheelColliderRight.brakeTorque = Mathf.Lerp(info.WheelColliderRight.brakeTorque, appliedBrake, Time.fixedDeltaTime * 10f);
			}
		}

		// update waypoint distances and relative vector (safe guard if CurrentWayPoint is null)
		if (CurrentWayPoint != null)
		{
			Vector3 wheelCenter = GetWheelCenterPosition();
			DistanceToWayPoint = Vector3.Distance(CurrentWayPoint.position, wheelCenter);
			relativeVectorToWaypoint = transform.InverseTransformPoint(CurrentWayPoint.position);

			if (DistanceToWayPoint < 7f)
			{
				var wp = CurrentWayPoint.GetComponent<WayPoint>();
				if (wp != null)
				{
					if (wp.NextWayPoint != null)
						NextWayPoint = wp.NextWayPoint.transform; // <-- use .transform
					else if (wp.WayPointsAround != null && wp.WayPointsAround.Length > 0)
						NextWayPoint = wp.WayPointsAround[UnityEngine.Random.Range(0, wp.WayPointsAround.Length)].transform; // <-- .transform
				}

				if (NextWayPoint != null)
					CurrentWayPoint = NextWayPoint;
			}
		}
	}

	private Vector3 GetWheelCenterPosition()
	{
		if (VehicleInfo != null && VehicleInfo.Length > 0 && VehicleInfo[0] != null)
		{
			var left = VehicleInfo[0].WheelColliderLeft;
			var right = VehicleInfo[0].WheelColliderRight;
			if (left && right)
				return (left.transform.position + right.transform.position) / 2f;
		}
		return transform.position;
	}

	private void UpdateWheelPose(WheelCollider wc, GameObject vis)
	{
		if (wc == null || vis == null) return;
		Quaternion quat;
		Vector3 pos;
		wc.GetWorldPose(out pos, out quat);
		vis.transform.position = pos;
		vis.transform.rotation = quat;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
		if (CurrentWayPoint)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(transform.position, CurrentWayPoint.position);
		}
	}

	private void OnDestroy()
	{
		BrakePress = null;
	}
}
