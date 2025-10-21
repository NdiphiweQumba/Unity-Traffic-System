using UnityEngine;

public class AIController : MonoBehaviour
{
	public float maxSteerAngle => GetComponent<CarController>().maxSteerAngle;
	public float maxMotorTorque => GetComponent<CarController>().maxTorque;
	public float maxBrakeTorque => GetComponent<CarController>().MaxBrakeTorque;
	public float sensorLength = 10f;
	public float avoidAngle = 30f;

	private void FixedUpdate()
	{
		float steerAngle = CalculateSteeringAngle();
		float motorTorque = CalculateMotorTorque();
		float brakeTorque = CalculateBrakeTorque();

		GetComponent<CarController>().Drive(steerAngle, motorTorque, brakeTorque);
	}

	private float CalculateSteeringAngle()
	{
		RaycastHit hit;
		Vector3 sensorStartPos = transform.position + transform.forward * 2f;
		float avoidMultiplier = 0;
		Vector3 sensorRight = Quaternion.AngleAxis(avoidAngle, transform.up) * transform.forward;
		Vector3 sensorLeft = Quaternion.AngleAxis(-avoidAngle, transform.up) * transform.forward;

		// Check for obstacles on the right side
		if (Physics.Raycast(sensorStartPos, sensorRight, out hit, sensorLength))
		{
			if (hit.transform.CompareTag("Vehicle"))
				avoidMultiplier -= 1f;
		}

		// Check for obstacles on the left side
		if (Physics.Raycast(sensorStartPos, sensorLeft, out hit, sensorLength))
		{
			if (hit.transform.CompareTag("Vehicle"))
				avoidMultiplier += 1f;
		}

		float steerAngle = avoidMultiplier * maxSteerAngle;
		return steerAngle;
	}

	private float CalculateMotorTorque()
	{
		return maxMotorTorque;
	}

	private float CalculateBrakeTorque()
	{
		RaycastHit hit;
		Vector3 sensorStartPos = transform.position + transform.forward * 2f;

		// Check if there is an obstacle in front
		if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
		{
			if (hit.transform.CompareTag("Vehicle"))
				return maxBrakeTorque;
		}

		return 0f;
	}

	private void ApplySteering(float steerAngle)
	{
		foreach (VehicleInfo info in GetComponent<CarController>().VehicleInfo)
		{
			info.WheelColliderLeft.steerAngle = steerAngle;
			info.WheelColliderRight.steerAngle = steerAngle;
		}
	}

	private void ApplyTorque(float torque)
	{
		foreach (VehicleInfo info in GetComponent<CarController>().VehicleInfo)
		{
			info.WheelColliderRight.motorTorque = torque;
			info.WheelColliderLeft.motorTorque = torque;
		}
	}

	private void ApplyBrakes(float brakeTorque)
	{
		foreach (VehicleInfo info in GetComponent<CarController>().VehicleInfo)
		{
			info.WheelColliderLeft.brakeTorque = brakeTorque;
			info.WheelColliderRight.brakeTorque = brakeTorque;
		}
	}
}
