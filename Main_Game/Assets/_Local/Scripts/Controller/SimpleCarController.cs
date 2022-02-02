using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCarController : MonoBehaviour {

    public TrafficSystem TrafficSystem;

    #region Private Fields
    [SerializeField] private VehicleInfo[] VehicleInfo;
    [SerializeField] private float TopSpeed; //the top speed
    [SerializeField] private float maxTorque; //the maximum torque to apply to wheels
    [SerializeField] private float maxSteerAngle;

    private float forward; //forward axis
    private float Turn; //turn axis

    [SerializeField] private float turnAngle;
    [SerializeField] private float brake; //brake axis
    private Rigidbody RigidBody;

    public Transform Center;
    public Transform ForwardDirection;

    private Transform NextWayPoint => CurrentWayPoint.GetComponent<WayPoint> ().NextWayPoint.transform;
    [SerializeField] private Transform CurrentWayPoint;
    private Vector3 RelativeVector;
    #endregion

    #region  Public Fields
    public float CurrentSpeed;
    public float MaxBrakeTorque;
    public float DistanceToWayPoint;

    public bool ShouldStop;
    public int StopArrivalNumber => TrafficSystem.count - 1;
    public int WaitTimeAtStop;
    public int Wait;

    public int countNumber;
    #endregion

    void Start () => RigidBody = GetComponent<Rigidbody> ();
    private void Update () {
        //  Debug.Log("Wheel Center: " + (VehicleInfo[0].WheelColliderLeft.transform.position + VehicleInfo[0].WheelColliderRight.transform.position) / 2);

        SpeedModifier ();

        countNumber = StopArrivalNumber;
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
        }
    }
    private void FixedUpdate () //fixed update is more physics realistic
    {
        DistanceToWayPoint = Vector3.Distance (CurrentWayPoint.transform.position, (VehicleInfo[0].WheelColliderRight.transform.position +
            VehicleInfo[0].WheelColliderLeft.transform.position) / 2);
        RelativeVector = Center.InverseTransformPoint (CurrentWayPoint.transform.position);

        Turn = RelativeVector.x / RelativeVector.magnitude;

        foreach (var info in VehicleInfo) {
            // Wheel Colliders left and right // 
            var left = info.WheelColliderLeft;
            var right = info.WheelColliderRight;

            if (info.Steer) {
                info.WheelColliderLeft.steerAngle = maxSteerAngle * Turn;
                info.WheelColliderRight.steerAngle = maxSteerAngle * Turn;
            }

            CurrentSpeed = 2 * 22 / 7 * left.radius * right.rpm * 60 / 1000; // Calculating speed in kmph

            left.motorTorque = CurrentSpeed < TopSpeed && info.Motor ? maxTorque * forward : 0;
            right.motorTorque = CurrentSpeed < TopSpeed && info.Motor ? maxTorque * forward : 0;

            //the top speed will not be accurate but will try to slow the car before top speed
            left.brakeTorque = info.Brakes || ShouldStop || Input.GetAxis ("Jump") > 0 ? MaxBrakeTorque * brake : 0;
            right.brakeTorque = info.Brakes || ShouldStop || Input.GetAxis ("Jump") > 0 ? MaxBrakeTorque * brake : 0;
        }

        CurrentWayPoint = DistanceToWayPoint < 1 ? NextWayPoint : CurrentWayPoint;
    }
    private void SpeedModifier () {

        RaycastHit hit;
        if (Physics.Raycast (Center.transform.position, Center.transform.TransformDirection (Vector3.forward), out hit)) {
            Debug.DrawRay (Center.transform.position, Center.transform.TransformDirection (Vector3.forward) * 10, Color.cyan);
            if (hit.collider.tag == "Vehicle") {
                forward = 0.00001f; // hit.collider.GetComponent<RigidBody> ().velocity.magnitude / 10;
                Debug.Log ("Hit Vehicle");
            } else {
                forward = Random.Range (0.001f, 1f);
            }

        }

    }

    public IEnumerator VehicleWaitSeconds (float sec) {
        yield return new WaitForSeconds (sec);
        Wait = (int) sec;
        ShouldStop = false;
        TrafficSystem.LeaveStop ();
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