using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private Transform[] frontWheels;
    [SerializeField] private Transform[] rearWheels;
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private float maxTorque = 1000f;
    [SerializeField] private float brakeTorque = 2000f;
    [SerializeField] private float reverseTorque = 500f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float downforce = 100f;
    [SerializeField] private float airResistance = 1f;

    private float currentSpeed;
    private Rigidbody rb;

    private void Awake ()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
    }

    private void FixedUpdate ()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isBraking = Input.GetKey(KeyCode.Space);

        // Calculate torque based on current speed and player input
        float currentTorque = maxTorque * Mathf.Clamp01(1f - currentSpeed / maxSpeed);
        float motorTorque = currentTorque * verticalInput;

        // Apply torque to all wheels
        foreach (Transform wheel in frontWheels)
        {
            ApplyWheelTorque(wheel, motorTorque);
            ApplyWheelSteering(wheel, horizontalInput);
        }
        foreach (Transform wheel in rearWheels)
        {
            ApplyWheelTorque(wheel, motorTorque);
        }

        // Apply braking force to all wheels
        if (isBraking)
        {
            foreach (Transform wheel in frontWheels)
            {
                ApplyWheelBrake(wheel, brakeTorque);
            }
            foreach (Transform wheel in rearWheels)
            {
                ApplyWheelBrake(wheel, brakeTorque);
            }
        }

        // Apply reverse torque if player is moving backward
        if (verticalInput < 0f)
        {
            foreach (Transform wheel in frontWheels)
            {
                ApplyWheelTorque(wheel, -reverseTorque);
            }
            foreach (Transform wheel in rearWheels)
            {
                ApplyWheelTorque(wheel, -reverseTorque);
            }
        }

        // Update current speed and apply air resistance
        currentSpeed = rb.linearVelocity.magnitude;
        rb.AddForce(-rb.linearVelocity.normalized * currentSpeed * airResistance);

        // Apply downforce to keep the vehicle on the ground
        rb.AddForce(-transform.up * downforce);
    }

    private void ApplyWheelTorque (Transform wheel, float torque)
    {
        WheelCollider collider = wheel.GetComponent<WheelCollider>();
        collider.motorTorque = torque;
    }

    private void ApplyWheelSteering (Transform wheel, float angle)
    {
        WheelCollider collider = wheel.GetComponent<WheelCollider>();
        collider.steerAngle = Mathf.Clamp(angle, -maxSteeringAngle, maxSteeringAngle);
    }

    private void ApplyWheelBrake (Transform wheel, float brakeTorque)
    {
        WheelCollider collider = wheel.GetComponent<WheelCollider>();
        collider.brakeTorque = brakeTorque;
    }
}