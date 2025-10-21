using UnityEngine;

public class BrakeLights : MonoBehaviour
{
	[SerializeField] private GameObject[] backLights; // assign in inspector
	[SerializeField] private CarController controller;

	private void Awake()
	{
		if (controller != null)
			controller.BrakePress += OnBrake;
	}

	private void OnDestroy()
	{
		if (controller != null)
			controller.BrakePress -= OnBrake;
	}

	private void OnBrake(bool val)
	{
		if (backLights == null) return;
		foreach (var go in backLights)
			if (go) go.SetActive(val);
	}
}
