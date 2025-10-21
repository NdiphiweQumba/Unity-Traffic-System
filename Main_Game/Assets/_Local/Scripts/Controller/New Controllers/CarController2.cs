using UnityEngine;

public class CarController2 : MonoBehaviour /// ai
{
    [SerializeField] private Transform[] frontWheels;
    [SerializeField] private Transform[] rearWheels;
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private float maxTorque = 1000f;
    [SerializeField] private float brakeTorque = 2000f;
    [SerializeField] private float maxSpeed = 100f;

	[SerializeField]private Transform target;
	[SerializeField]private float currentSpeed;
    private Rigidbody rb;

	[SerializeField]private Transform WheelscColsParent;
	[SerializeField]private Transform WheelsMeshes;
	
	private void Awake () => rb = GetComponent<Rigidbody>();

    private void FixedUpdate ()
    {
        if (target != null)
        {
            // Calculate torque based on current speed and target distance
            float currentTorque = maxTorque * Mathf.Clamp01(1f - currentSpeed / maxSpeed);
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            float motorTorque = currentTorque * Mathf.Clamp01(1f - distanceToTarget / 10f);

            // Calculate steering angle based on target position
            Vector3 localTarget = transform.InverseTransformPoint(target.position);
	        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            float steeringAngle = Mathf.Clamp(targetAngle, -maxSteeringAngle, maxSteeringAngle);

	        // Apply torque and steering to all wheels
	        for (int i = 0; i < 3; i++) 
	        {
		        ApplyWheelTorque(WheelscColsParent.GetChild(i), motorTorque);
		        ApplyWheelSteering(WheelscColsParent.GetChild(i), steeringAngle);
	        }
	       
            foreach (Transform wheel in rearWheels)
            {
	            for (int i = 0; i > 2; i++) 
	            {
		            ApplyWheelTorque(WheelscColsParent.GetChild(i), motorTorque);
	            }
            }

            // Apply braking force if the vehicle is close to the target
            if (distanceToTarget < 2f)
            {
	            for (int i = 0; i < WheelscColsParent.childCount; i++) 
	            {
		            ApplyWheelTorque(WheelscColsParent.GetChild(i), motorTorque);
		            ApplyWheelSteering(WheelscColsParent.GetChild(i), steeringAngle);
	            }
            }
            // Update current speed
            currentSpeed = rb.linearVelocity.magnitude;
        }
    }

    public void SetTarget (Transform target)
    {
        this.target = target;
    }

    private void ApplyWheelTorque (Transform wheel, float torque)
	{
		var wheelmesheparent = WheelsMeshes;
		var wheelCollider    = WheelscColsParent;

        WheelCollider collider = wheel.GetComponent<WheelCollider>();
        collider.motorTorque = torque;
    }

    private void ApplyWheelSteering (Transform wheel, float angle)
    {
        WheelCollider collider = wheel.GetComponent<WheelCollider>();
        collider.steerAngle = angle;
    }

    private void ApplyWheelBrake (Transform wheel, float brakeTorque)
    {
        WheelCollider collider = wheel.GetComponent<WheelCollider>();
        collider.brakeTorque = brakeTorque;
    }
}
