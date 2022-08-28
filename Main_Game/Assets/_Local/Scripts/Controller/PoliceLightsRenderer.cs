using System.Collections;
using UnityEngine;

public class PoliceLightsRenderer : MonoBehaviour
{
    public Material MaterialCustom;
    [SerializeField]private Color color1; 
    [SerializeField]private Color color2; 
    [SerializeField]private Color color3; 
    [SerializeField]private Color color4; 
    public bool isInDanger = false;
    float startTime;
    public float speed = 1.0f;

    void Start()
    {
        startTime = Time.time; 
        var Mat = GetComponent<Renderer>().sharedMaterial; 
        Mat.EnableKeyword("_EMISSION"); 
    }
    
    private void Update()
    {
        if (!isInDanger)
        {
            float frac = (Mathf.Sin(Time.time - startTime) * speed);
            GetComponent<Renderer>().sharedMaterial.SetColor("_EmissiveColor", 
                                                    Color.Lerp(color1, color2, frac));
        }
        else if (isInDanger)
        {
            float frac = (Mathf.Sin(Time.time - startTime) * speed);

            GetComponent<Renderer>().sharedMaterial.SetColor("_EmissiveColor", Color.Lerp(color3, color4, frac));
        }
    }
}


/*
 *      Lerp Material Color Runtime ///
 *      public Color StartColor;
 *      public Color EndColor;
 *      public float time;
 *      bool goingForward;
 *      bool isCycling;
 *      private Material myMaterial;

 *      private void Awake()
 *      {
 *          goingForward = true;
 *          isCycling = false;
 *          myMaterial = GetComponent<Renderer>().material;
 *      }
 *      
 *      private void Update()
 *      {
 *          if (!isCycling)
 *          {
 *              if (goingForward)
 *                  StartCoroutine(CycleMaterial(StartColor, EndColor, time, myMaterial));
 *              else
 *                  StartCoroutine(CycleMaterial(EndColor, StartColor, time, myMaterial));
 *          }
 *      }
 *      
 *      private IEnumerator CycleMaterial(Color startColor, Color endColor, float cycleTime, Material mat)
 *      {
 *          isCycling = true;
 *          float currentTime = 0;
 *          while (currentTime < cycleTime)
 *          {
 *              currentTime += Time.deltaTime;
 *              float t = currentTime / cycleTime;
 *              Color currentColor = Color.Lerp(startColor, endColor, t);
 *              mat.color = currentColor;
 *              yield return null;
 *          }
 *          isCycling = false;
 *          goingForward = !goingForward;
 *      
 *      }
//// End Lerp Material Color
////*/

