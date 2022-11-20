using UnityEngine;

// [ExecuteAlways]
public class CollisionHandler : MonoBehaviour
{
    #region Private Fields 
    
    [SerializeField]
    private Transform               Parent;
    private GameObject              GameObjectHit;
    private AIVehicleController     Controller;
    private Vector3                 Origin;
    private Vector3                 Direection;
    private Color                   raycastColor;

    [SerializeField] private float maxHitDistance;
    [SerializeField] private float maxSafeDistance = 10f;
    [SerializeField] private float SphereRadius = 1;

    #endregion End Private Fields

    #region Public Fields
    public  float  CurrentHitDistance;
    public LayerMask Mask;
    #endregion End Public Fields

    private void Awake()
    {
       // Controller.OnCaution += RedColor; //  Delegate what Happens when hit // 
    }
    private void Update()
    {
        Origin = this.transform.position;

        Direection = Parent.forward;

        RaycastHit hit;

        if (Physics.SphereCast(Origin, SphereRadius,
                               Direection, out hit,
                               maxHitDistance, Mask,
                               QueryTriggerInteraction.UseGlobal))
        {
            GameObjectHit = hit.transform.gameObject;
            CurrentHitDistance = hit.distance;
        }
        else
        {
            CurrentHitDistance = maxHitDistance;
            GameObjectHit = null;
        }

        raycastColor = CurrentHitDistance > maxSafeDistance ? Color.blue : Color.red;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = raycastColor;
        Debug.DrawLine(Origin, Origin + Direection * CurrentHitDistance);
        Gizmos.DrawWireSphere(Origin + Direection * CurrentHitDistance, SphereRadius);
    }
    public void RedColor(bool val)
    {
        /// Do something when Red Sinal //
    }
}
