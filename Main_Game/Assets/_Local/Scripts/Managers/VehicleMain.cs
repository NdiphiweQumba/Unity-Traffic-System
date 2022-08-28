using System.Runtime.CompilerServices;
using UnityEngine;
public class VehicleMain 
    : CarMain<IDriveTrain, VehicleStats, EngineSpecs, CarWheels>
{
    [SerializeField] private CarWheels carWheels;
    [SerializeField] private CarMechanics mechanics;
    [SerializeField] private VehicleStats runTimeInfo;
    [SerializeField] private EngineSpecs engineSpecs;
}

public class CarMain<T, T1, T2, T3> : MonoBehaviour
    where T  : IDriveTrain
    where T1 : VehicleStats
    where T2 : EngineSpecs
    where T3 : CarWheels
{
    public T wheels;
    public T2 EngineInfo;

}
