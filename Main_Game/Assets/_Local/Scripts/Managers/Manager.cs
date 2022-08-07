using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public delegate void OnPassingRoad();
    public static event OnPassingRoad pass;

    public EventMade Event;

    private void Start()
    {
        pass += CountingVehicle;
        pass += CountUp;
    }

    public void CountingVehicle()
    {
        Debug.Log("Counting Vehicles");
    }
    public void CountUp()
    {
        Debug.Log("Counting Up");
    }
}

[System.Serializable]
public class EventMade :  UnityEngine.Events.UnityEvent<float, string>
{
    public static string MyString;     
}