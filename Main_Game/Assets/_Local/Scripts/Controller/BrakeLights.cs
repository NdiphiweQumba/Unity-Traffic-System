using UnityEngine;
public class BrakeLights : MonoBehaviour
{
    [SerializeField]
    private GameObject BackLights;
    [SerializeField]
    private SimpleCarController Controller;
    private void Awake() => Controller.BrakePress += OnBrake;

    public void OnBrake(bool val)
    {
        if (BackLights)
            BackLights.SetActive(val);
    }
}
