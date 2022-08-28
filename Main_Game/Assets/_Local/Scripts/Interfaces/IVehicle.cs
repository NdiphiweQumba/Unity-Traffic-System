using System;
using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;
public interface Ivehicle<T>: INotifyPropertyChanged, IObservable<T>
    where T : class 
{
    
}
public interface VehicleStats: IDriveTrain
{
    public void VehicleStates();
}


public interface IDriveTrain
{
    #region Fields
    float[] GearRatios { get; set; }
    #endregion

    #region Enums
    enum DriveWheels { ForwardDrive, RearDrive, AllDrive }
    enum Transmission { Manual, Automatic, Electric, Hybrid}
    #endregion End Fields And Enums

    #region Methods
    void SetDriveType(DriveWheels dt);
    void TransmissionType(Transmission tt);
    #endregion End Methods
}
partial class CarMechanics : IDriveTrain
{
    float[] IDriveTrain.GearRatios
    {
        get => GearRatios;
        set => throw new NotImplementedException();
    }

    public float[] GearRatios; 

    public IDriveTrain.Transmission Transmission = IDriveTrain.Transmission.Manual;
    public IDriveTrain.DriveWheels DriveType     = IDriveTrain.DriveWheels.RearDrive;

    public virtual void SetDriveType(IDriveTrain.DriveWheels dt)
    {
        this.DriveType = dt;
        switch (dt)
        {
            case IDriveTrain.DriveWheels.ForwardDrive:
                break;
            case IDriveTrain.DriveWheels.RearDrive:
                break;
            case IDriveTrain.DriveWheels.AllDrive:
                break;
            default:
                break;
        }

    }
    public virtual void TransmissionType(IDriveTrain.Transmission tt)
    {
        this.Transmission = tt;

        switch (tt)
        {
            case IDriveTrain.Transmission.Manual:
                break;
            case IDriveTrain.Transmission.Automatic:
                break;
            case IDriveTrain.Transmission.Electric:
                break;
            case IDriveTrain.Transmission.Hybrid:
                break;
            default:
                break;
        }
    }
    
}

[System.Serializable]
public class CarWheels
{
    public WheelCollider[] WheelColliders;
    public Transform[] WheelTransforms;

    protected internal void UpdateWheelGeometry(WheelCollider[] wheelCollider = null, Transform[] wheelMesh = null)
    {
       
        this.WheelColliders = wheelCollider;
        this.WheelTransforms = wheelMesh;
        Vector3 pos;
        Quaternion quat;

        for (int i = 0; i < wheelCollider.Length; i++)
        {
            wheelCollider[i].GetWorldPose(out pos, out quat); 
           
            wheelMesh[i].position = pos;
            wheelMesh[i].rotation = quat;
        }
     }


}

public interface IVehicleSpecifics
{

}

[System.Serializable]
public class EngineSpecs 
{
    public float[] gearRatios;

    public float finalDriveRatio = 3.23f;
    public float minRPM = 800;

    public float maxRPM = 6400;

    public float maxTorque = 664;
    public float torqueRPM = 4000;

    public float maxPower = 317000;
    public float powerRPM = 5000;

    public float engineInertia = 0.3f;


    public float engineBaseFriction = 25f;

    public float engineRPMFriction = 0.02f;

    public Vector3 engineOrientation = Vector3.forward;

    public float differentialLockCoefficient = 0;

}
