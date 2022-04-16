using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent (typeof (SimpleCarController))]
public class AIVehicleController : MonoBehaviour {

    public enum AIBrake {
        NoBrake,
        WayPointDirection,
        WayPointDistance,
    }

    public float m_CautiousSpeedFactor = 0.05f; // percentage of max speed to use when being maximally cautious
    [SerializeField][Range (0, 180)] private float m_CautiousMaxAngle = 50f; // angle of approaching corner to treat as warranting maximum caution
    [SerializeField] private float m_CautiousMaxDistance = 100f; // distance at which distance-based cautiousness begins
    [SerializeField] private float m_CautiousAngularVelocityFactor = 30f; // how cautious the AI should be when considering its own current angular velocity (i.e. easing off acceleration if spinning!)
    [SerializeField] private float m_SteerSensitivity = 0.05f; // how sensitively the AI uses steering input to turn to the desired direction
    [SerializeField] private float m_AccelSensitivity = 0.04f; // How sensitively the AI uses the accelerator to reach the current desired speed
    [SerializeField] private float BrakeSens = 1f; // How sensitively the AI uses the brake to reach the current desired speed
    [SerializeField] private float m_LateralWanderDistance = 3f; // how far the car will wander laterally towards its target
    [SerializeField] private float m_LateralWanderSpeed = 0.1f; // how fast the lateral wandering will fluctuate
    [SerializeField][Range (0, 1)] private float m_AccelWanderAmount = 0.1f; // how much the cars acceleration will wander
    [SerializeField] private float m_AccelWanderSpeed = 0.1f; // how fast the cars acceleration wandering will fluctuate
    [SerializeField] private AIBrake BrakeState = AIBrake.WayPointDistance; // what should the AI consider when accelerating/braking?
    public bool IsDriving; // whether the AI is currently actively driving or stopped.
    [SerializeField] private bool ShouldStopWhenTargetReached; // should we stop driving when we reach the target?
    [SerializeField] private float m_ReachTargetThreshold = 2; // proximity to target to consider we 'reached' it, and stop driving.
    [SerializeField] private readonly float maxDistToBrake = 10f;
    private float RandomPerlin; // A random value for the car to base its wander on (so that AI cars don't all wander in the same pattern)
    private SimpleCarController SimpleCarController; // Reference to actual car controller we are controlling
    private float stopTimeFromOtherCar; // time until which to avoid the car we recently collided with
    private float slowDownFactor; // how much to slow down due to colliding with another car, whilst avoiding
    private float PathOffSet; // direction (-1 or 1) in which to offset path to avoid other car, whilst avoiding
    private Rigidbody RigidBody;

    public List<SimpleCarController> Vehicles = new List<SimpleCarController> ();
    private List<GameObject> vehiclegameObject;
    public bool distancetoStops;
    public string FrontVehicleName;

    int Limit = 1000;

    public float brakeValue = 0;
    public float accelValue = 0;

    [Space (20)]
    [SerializeField] private float DistanceToOtherVehicle;
    [SerializeField] private bool hasDetectedObstacle;
    [SerializeField] private bool IsinFront;
    [SerializeField] private float GetOtherCarSpeed = 0;

    private void Start () {
        SimpleCarController = GetComponent<SimpleCarController> ();
        RigidBody = GetComponent<Rigidbody> ();

        brakeValue = .1f;
        RandomPerlin = Random.value * 100;

        foreach (var vehicle in Vehicles) {
            Debug.Log (vehicle.transform.localPosition);
        }

    }
    private void Update () {
        foreach (var v in Vehicles) {

            Vector3 fwd = transform.TransformDirection (Vector3.forward);
            var dist = Vector3.Distance (transform.localPosition, v.transform.localPosition);
            brakeValue = 1f / dist;
            accelValue = dist / .03f;

            hasDetectedObstacle = dist < maxDistToBrake && v.CurrentSpeed <=
                GetComponent<SimpleCarController> ().CurrentSpeed;

            Vector3 otherVehicle = transform.localPosition - v.transform.localPosition;

            if (Vector3.Dot (fwd, otherVehicle) > 0 && dist != 0 && dist < 5 && v.GetComponent<Rigidbody> ().velocity.magnitude < RigidBody.velocity.magnitude) {
                // if vehicle is forward then  apply brakes
                IsinFront = true;
                FrontVehicleName = v.transform.name;

            } else {
                FrontVehicleName = "";
                IsinFront = false;
                brakeValue = 0;
                accelValue = dist * .03f;
            }

        }
    }

    private void FixedUpdate () {
        var TopSpeed = SimpleCarController.TopSpeed;
        if (!IsDriving || SimpleCarController.ShouldStop) {
            SimpleCarController.Drive (0, 0, 1);
        } else {
            Vector3 forward = transform.forward;
            if (RigidBody.velocity.magnitude > TopSpeed * 0.1f)
                forward = RigidBody.velocity;

            float desiredSpeed = TopSpeed;

            switch (BrakeState) {
                case AIBrake.WayPointDirection:
                    {
                        float approachingAngle = Vector3.Angle (SimpleCarController.CurrentWayPoint.forward, forward);

                        float spinningAngle = RigidBody.angularVelocity.magnitude * 30f;

                        float slowDown = Mathf.InverseLerp (0, 50f, Mathf.Max (spinningAngle, approachingAngle));
                        desiredSpeed = Mathf.Lerp (TopSpeed, TopSpeed * .5f, slowDown);
                        break;
                    }
                case AIBrake.WayPointDistance:
                    {
                        Vector3 delta = SimpleCarController.CurrentWayPoint.transform.position - transform.position; // Check

                        float slowDownDistance = Mathf.InverseLerp (100f, 0, delta.magnitude);

                        float spinningAngle = RigidBody.angularVelocity.magnitude * 30f;

                        float slowDown = Mathf.Max (Mathf.InverseLerp (0, 50f, spinningAngle), slowDownDistance);
                        desiredSpeed = Mathf.Lerp (TopSpeed, TopSpeed * .5f, slowDown);
                        break;
                    }
                case AIBrake.NoBrake:

                    break;
            }
            Vector3 targetPositionOffset = SimpleCarController.CurrentWayPoint.transform.position;

            if (Time.time < stopTimeFromOtherCar) {
                desiredSpeed *= slowDownFactor;
                targetPositionOffset += SimpleCarController.CurrentWayPoint.transform.right * PathOffSet;
            } else {
                targetPositionOffset += SimpleCarController.CurrentWayPoint.transform.right *
                    (Mathf.PerlinNoise (Time.time * .1f, RandomPerlin) * 2 - 1) *
                    2;

            }
            float InputSens = (desiredSpeed < SimpleCarController.CurrentSpeed) ? 1 : 0.04f;

            float accellerate = Mathf.Clamp ((desiredSpeed - SimpleCarController.CurrentSpeed) * InputSens, -1, 1);

            accellerate *= (1 - .1f) + (Mathf.PerlinNoise (Time.time * .1f, RandomPerlin) * .1f);

            Vector3 localTarget = transform.InverseTransformPoint (targetPositionOffset);

            float steertorwards = Mathf.Atan2 (localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            float steer = Mathf.Clamp (steertorwards * .5f, -1, 1) * Mathf.Sign (SimpleCarController.CurrentSpeed);

            if (IsinFront)
                SimpleCarController.Drive (steer, .4f, brakeValue);
            else
                SimpleCarController.Drive (steer, accellerate * accelValue, 0);

            if (ShouldStopWhenTargetReached && localTarget.magnitude < 2)
                IsDriving = false;
        }

    }
    private void OnCollisionStay (Collision col) {
        // detect collision against other cars, so that we can take evasive action
        if (col.rigidbody != null) {
            var otherAI = col.rigidbody.GetComponent<SimpleCarController> ();
            if (otherAI != null) {
                // we'll take evasive action for 1 second
                stopTimeFromOtherCar = Time.time + 1;

                // but who's in front?...
                if (Vector3.Angle (transform.forward, otherAI.transform.position - transform.position) < 90) {
                    // the other ai is in front, so it is only good manners that we ought to brake...
                    slowDownFactor = 0.5f;
                } else {
                    // we're in front! ain't slowing down for anybody...
                    slowDownFactor = 1;
                }

                // both cars should take evasive action by driving along an offset from the path centre,
                // away from the other car
                var otherCarLocalDelta = transform.InverseTransformPoint (otherAI.transform.position);
                float otherCarAngle = Mathf.Atan2 (otherCarLocalDelta.x, otherCarLocalDelta.z);
                PathOffSet = 3 * -Mathf.Sign (otherCarAngle);
            }
        }
    }
    #region  Private Methods
    // To Fix this method  /// 
    private void ApplyBrakes (float distance) {

        // Vector3 fwd = transform.TransformDirection (Vector3.forward);
        // Vector3 otherv = col.GetComponent<Transform> ().position - transform.position;

        // Check if the other vehicle is in front then apply brakes 
        // if (Vector3.Dot (fwd, otherv) < 0) {
        //     Debug.Log ("Vehicle Detected Behind this transform position");
        // } else {
        //     ApplyBrakes (dist);
        // }

        // if (distance > 30) {
        //     brakeValue = 0;
        // } else if (distance > 20)
        //     brakeValue = 0.001f;
        // else if (distance >= 8)
        //     brakeValue = .05f; ////
        // else if (distance >= 5)
        //     brakeValue = .09f; ///
        // else if (distance >= 3)
        //     brakeValue = .2f; ///
        // else if (distance < 3)
        //     brakeValue = 20F;

        // switch (distance) {
        //     // case distance >= 5:
        //     //     break;

        //     // default:
        // }

    }
    #endregion
}