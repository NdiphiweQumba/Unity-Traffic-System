using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SimpleCarController : MonoBehaviour
{
    public WheelCollider WheelFL;//the wheel colliders
    public WheelCollider WheelFR;
    public WheelCollider WheelBL;
    public WheelCollider WheelBR;

    public GameObject FL;//the wheel gameobjects
    public GameObject FR;
    public GameObject BL;
    public GameObject BR;

    public float topSpeed = 250f;//the top speed
    public float maxTorque = 200f;//the maximum torque to apply to wheels
    public float maxSteerAngle = 30f;
    public float currentSpeed;
    public float maxBrakeTorque = 2200;


    private float Forward;//forward axis
    private float Turn;//turn axis

    private float TurnAngle;
    private float Brake;//brake axis


	public bool allWheelDrive;

	public Rigidbody rb;//rigid body of car

    private Transform NextWayPoint => CurrentWayPoint.GetComponent<WayPoint>().NextWayPoint.transform;
   public Transform Center;
    public Transform CurrentWayPoint;

    public Vector3 RelativeVector;
    public float DistanceToWayPoint;
    void Start ()  =>  rb = GetComponent<Rigidbody>();
	void FixedUpdate ()   //fixed update is more physics realistic
	{
        DistanceToWayPoint = Vector3.Distance(CurrentWayPoint.transform.position, Center.transform.position);

        RelativeVector = Center.transform.InverseTransformPoint(CurrentWayPoint.transform.position);

        Debug.Log("Current WayPoint: " + CurrentWayPoint.transform.name);

        Forward = .2f;
        Turn    = RelativeVector.x / RelativeVector.magnitude;
        Brake   = 0;

        WheelFL.steerAngle = maxSteerAngle * Turn;
        WheelFR.steerAngle = maxSteerAngle * Turn;

        Debug.Log($"Relative vector X: {RelativeVector.x} Y: {RelativeVector.y} Z: {RelativeVector.z}");

        Debug.Log("Wheel FL Steer Angle " + WheelFL.steerAngle);
        Debug.Log("Wheel FR Steer Angle " + WheelFR.steerAngle);


        currentSpeed = 2 * 22 / 7 * WheelBL.radius * WheelBL.rpm * 60 / 1000; //formula for calculating speed in kmph

        if(currentSpeed < topSpeed)
        {
            WheelBL.motorTorque = maxTorque * Forward;//run the wheels on back left and back right
            WheelBR.motorTorque = maxTorque * Forward;
        }
        else
        {
            WheelBL.motorTorque = 0;//run the wheels on back left and back right
            WheelBR.motorTorque = 0;

        }
        //the top speed will not be accurate but will try to slow the car before top speed

        WheelBL.brakeTorque = maxBrakeTorque * Brake;
        WheelBR.brakeTorque = maxBrakeTorque * Brake;
       // WheelFL.brakeTorque = maxBrakeTorque * Brake;
       // WheelFR.brakeTorque = maxBrakeTorque * Brake;

        if(DistanceToWayPoint < 1)
           CurrentWayPoint = NextWayPoint;
    }
    void Update()//update is called once per frame
    {
        Quaternion flq;//rotation of wheel collider
        Vector3 flv;//position of wheel collider
        WheelFL.GetWorldPose(out flv,out flq);//get wheel collider position and rotation
        FL.transform.position = flv;
        FL.transform.rotation = flq;

        Quaternion Blq;//rotation of wheel collider
        Vector3 Blv;//position of wheel collider
        WheelBL.GetWorldPose(out Blv, out Blq);//get wheel collider position and rotation
        BL.transform.position = Blv;
        BL.transform.rotation = Blq;

        Quaternion fRq;//rotation of wheel collider
        Vector3 fRv;//position of wheel collider
        WheelFR.GetWorldPose(out fRv, out fRq);//get wheel collider position and rotation
        FR.transform.position = fRv;
        FR.transform.rotation = fRq;

        Quaternion BRq;//rotation of wheel collider
        Vector3 BRv;//position of wheel collider
        WheelBR.GetWorldPose(out BRv, out BRq);//get wheel collider position and rotation
        BR.transform.position = BRv;
        BR.transform.rotation = BRq;


		// All Wheel Drive //
		AllWheelDrive ();
    }

	// All Wheel Drive // 
	void AllWheelDrive()
	{
		if (allWheelDrive) 
		{
			WheelBL.motorTorque = maxTorque * Forward;//run the wheels on back left and back right
			WheelBR.motorTorque = maxTorque * Forward;
			WheelFL.motorTorque = maxTorque * Forward;
			WheelFR.motorTorque = maxTorque * Forward;
		}
	}

}

[System.Serializable]
public class VeicleInfo: System.Object
{
    public WheelCollider WheelColliderLeft;
    public WheelCollider WheelColliderRight;

    public GameObject    WheelVisualLefet;
    public GameObject WheelVisualRight;

    public float Motor;
    public float Steer;
    public float ReverseTurn;

}