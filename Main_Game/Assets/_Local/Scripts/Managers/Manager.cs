using System.Collections;
using UnityEngine;
using UnityTemplateProjects;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    public delegate void OnPassingRoad();
    public static event OnPassingRoad pass;
    public Button ButtonStartControls;
    public Button ButtonExit;

    public EventMade Event;
    [SerializeField] private float enableGameStart;
    private void Start()
    {
        pass += CountingVehicle;
        pass += CountUp;

        StartCoroutine(GameStart(enableGameStart));
    }

    public void CountingVehicle()
    {
        Debug.Log("Counting Vehicles");
    }
    public void CountUp()
    {
        Debug.Log("Counting Up");
    }
    public void ExitGame(string scenename)
    {
        SceneManager.LoadScene(scenename);
    }
    public void StartControls()
    {
        ButtonStartControls.enabled = true;
    }
    public void ShowButton()
    {
        ButtonStartControls.gameObject.SetActive(false);
        ButtonExit.gameObject.SetActive(true);
        FindObjectOfType<SimpleCameraController>().enabled = ButtonStartControls.enabled;
    }
    private IEnumerator GameStart(float time)
    {
        yield return new WaitForSeconds(time);
        StartControls();
    }

}

[System.Serializable]
public class EventMade :  UnityEngine.Events.UnityEvent<float, string>
{
    public static string MyString;     
}