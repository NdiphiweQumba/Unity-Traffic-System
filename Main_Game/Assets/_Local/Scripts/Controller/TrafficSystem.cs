using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSystem 
    : MonoBehaviour 
{
    public List<Car> car = new List<Car>();

    public int NumberOfVehicles;
    private void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Vehicle")
        {
            NumberOfVehicles++;
            Debug.Log($"Vehicles Entered {NumberOfVehicles}");
        }
    }
    private void OnTriggerStay(Collider col)
    {

    }
    private void OnTriggerExit(Collider col)
    {

    }
}


[System.Serializable]
public class Car
{
    public String CarName;
    public SimpleCarController CarController; 
    public int  CarId;


    public Car(string n, int id)
    {
        
    }

    private IEnumerator StopSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    } 
    
}