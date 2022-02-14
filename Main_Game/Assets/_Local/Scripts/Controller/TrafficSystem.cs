using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSystem : MonoBehaviour {
    public int count = 0;
    Queue<int> vehicle = new Queue<int> ();
    List<int> arrivedVehicles = new List<int> ();

    public float SecondsToWait;
    [SerializeField] private bool IsStop;

    public void ArriveAtStop () {
        vehicle.Enqueue (count++);

    }
    public void VehicleCountList () {
        for (int i = 0; i <= vehicle.Count; i++) {
            arrivedVehicles.Add (i);
        }
        Debug.Log (arrivedVehicles.Count);
    }

    public void LeaveStop () {
        vehicle.Dequeue ();
    }
    void Update () {
        if (Input.GetButtonUp ("Jump")) {
            ArriveAtStop ();
            VehicleCountList ();
        }
    }
    private void OnTriggerEnter (Collider col) {
        if (col.tag == "Vehicle") {
            var vehicleController = col.transform.GetComponent<SimpleCarController> ();
            vehicleController.ShouldStop = true;
            StartCoroutine (vehicleController.VehicleWaitSeconds (vehicleController.WaitTimeAtStop));
            ArriveAtStop ();
        }
    }
    private void OnTriggerStay (Collider col) {
        if (col.tag == "Vehicle") {

            var vehicleController = col.transform.GetComponent<SimpleCarController> ();

            if (vehicle.Contains (vehicleController.countNumber) || vehicleController.Wait == 0) {
                vehicleController.ShouldStop = true;
            } else {
                vehicleController.ShouldStop = false;
            }
        }
    }
    private void OnTriggerExit (Collider col) {
        if (col.tag == "Vehicle") {
            col.transform.GetComponent<SimpleCarController> ().ShouldStop = true;
        }
    }

}