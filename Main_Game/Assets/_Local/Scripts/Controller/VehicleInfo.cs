using UnityEngine;

[System.Serializable]
public class VehicleInfo
{
	public WheelCollider WheelColliderLeft;
	public WheelCollider WheelColliderRight;
	public GameObject WheelVisualLeft;
	public GameObject WheelVisualRight;
	public bool Motor = true;
	public bool Steer = true;
	public bool Brakes = true;
	public float ReverseTurn = 1f;
}
