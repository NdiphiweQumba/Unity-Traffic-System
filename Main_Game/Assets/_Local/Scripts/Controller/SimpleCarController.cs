using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCarController : MonoBehaviour {

    public TrafficSystem TrafficSystem;

    #region Private Fields
    [SerializeField] private VehicleInfo[] VehicleInfo;

    public float TopSpeed; //the top speed
    [SerializeField] private float maxTorque; //the maximum torque to apply to wheels
    [SerializeField] private float maxSteerAngle;

    [SerializeField] private float forward; //forward axis
    private float Turn; //turn axis

    [SerializeField] private float turnAngle;
    [SerializeField] private float brake; //brake axis
    [SerializeField] private float accellerate = 0;

    public  Rigidbody RigidBody;


    [SerializeField] public Transform CurrentWayPoint;
    public Transform NextWayPoint;

    private Vector3 RelativeVector;
    #endregion

    #region  Public Fields
    public float CurrentSpeed;
    public float MaxBrakeTorque;
    public float DistanceToWayPoint;

    public bool ShouldStop;
    public bool ObjectInFront;
    public int StopArrivalNumber =0;
    public int WaitTimeAtStop;
    public int Wait;
    public float m_RandomPerlin;
    public int countNumber;
    public float dist;
    #endregion

    void Awake () => m_RandomPerlin = Random.value * 100;
    void Start () => RigidBody = GetComponent<Rigidbody> ();
    
    public void Drive (float turn, float accel, float brake) {

        foreach (var info in VehicleInfo) {
            WheelCollider leftCollider = info.WheelColliderLeft;
            WheelCollider rightCollider = info.WheelColliderRight;

            GameObject leftVisualWheel = info.WheelVisualLeft;
            GameObject rightVisualWheel = info.WheelVisualRight;

            Quaternion quat; //rotation of wheel collider
            Vector3 pos; //position of wheel collider
            leftCollider.GetWorldPose (out pos, out quat); //get wheel collider position and rotation
            leftVisualWheel.transform.position = pos;
            leftVisualWheel.transform.rotation = quat;

            rightCollider.GetWorldPose (out pos, out quat); //get wheel collider position and rotation
            rightVisualWheel.transform.position = pos;
            rightVisualWheel.transform.rotation = quat;

            turn = RelativeVector.x / RelativeVector.magnitude;

            // Wheel Colliders left and right // 
            var left = info.WheelColliderLeft;
            var right = info.WheelColliderRight;

            if (info.Steer) {
                info.WheelColliderLeft.steerAngle = maxSteerAngle * turn;
                info.WheelColliderRight.steerAngle = maxSteerAngle * turn;
            }

            ////CurrentSpeed = 2 * 22 / 7 * left.radius * right.rpm * 60 / 1000; // Calculating speed in kmph

            CurrentSpeed = RigidBody.velocity.magnitude * 2.23693629f;
            left.motorTorque = maxTorque * accel;
            right.motorTorque = maxTorque * accel;

            //the top speed will not be accurate but will try to slow the car before top speed
            left.brakeTorque = MaxBrakeTorque * brake;
            right.brakeTorque = MaxBrakeTorque * brake;
        }

        DistanceToWayPoint = Vector3.Distance (CurrentWayPoint.transform.position, (VehicleInfo[0].WheelColliderRight.transform.position +
            VehicleInfo[0].WheelColliderLeft.transform.position) / 2);
        RelativeVector = transform.InverseTransformPoint (CurrentWayPoint.transform.position);

        if (DistanceToWayPoint < 7) {
            if (CurrentWayPoint.GetComponent<WayPoint> ().NextWayPoint != null) {
                NextWayPoint = CurrentWayPoint.GetComponent<WayPoint> ().NextWayPoint.transform;
            } else {
                int randomPoint = Random.Range (0, CurrentWayPoint.GetComponent<WayPoint> ().WayPointsAround.Length);
                NextWayPoint = CurrentWayPoint.GetComponent<WayPoint> ().WayPointsAround[randomPoint].transform;
            }
            CurrentWayPoint = NextWayPoint;
        }
    }

    public IEnumerator VehicleWaitSeconds (float sec) {
        yield return new WaitForSeconds (sec);
        Wait = (int) sec;
        ShouldStop = false;
    }

}

[System.Serializable]
public class VehicleInfo : System.Object {
    public WheelCollider WheelColliderLeft;
    public WheelCollider WheelColliderRight;
    public GameObject WheelVisualLeft;
    public GameObject WheelVisualRight;

    public bool Motor;
    public bool Steer;
    public bool Brakes;
    public float ReverseTurn;
}