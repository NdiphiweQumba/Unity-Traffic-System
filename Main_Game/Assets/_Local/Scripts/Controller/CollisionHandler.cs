using UnityEngine;

// [ExecuteAlways]
public class CollisionHandler : MonoBehaviour
{
	#region Private Fields 

	[SerializeField]
	private Transform Parent;
	private GameObject hitObject;
	private Vector3 Origin;
	private Vector3 Direection;
	private Color raycastColor;

	[SerializeField] private float maxHitDistance;
	[SerializeField] private float maxSafeDistance = 10f;
	[SerializeField] private float SphereRadius = 1;

	#endregion End Private Fields

	#region Public Fields
	public float CurrentHitDistance;
	public LayerMask Mask;
	public Collider CurrentHitCollider { get; private set; }
	public bool HasHit => CurrentHitCollider != null;
	public bool IsUnsafeHit => HasHit && CurrentHitDistance <= maxSafeDistance;
	#endregion End Public Fields

	#region  Monobehaviour Callbacks
	private void Awake()
	{
		if (Parent == null && transform.parent != null)
			Parent = transform.parent;

		RefreshMask();
	}
	private void Update()
	{
		if (Parent == null)
			return;

		RefreshMask();

		Origin = this.transform.position;

		Direection = Parent.forward;

		RaycastHit hit;

		if (Physics.SphereCast(Origin, SphereRadius,
							   Direection, out hit,
							   maxHitDistance, Mask,
							   QueryTriggerInteraction.Ignore))
		{
			hitObject = hit.transform.gameObject;
			CurrentHitDistance = hit.distance;
			CurrentHitCollider = hit.collider;
		}
		else
		{
			CurrentHitDistance = maxHitDistance;
			hitObject = null;
			CurrentHitCollider = null;
		}

		raycastColor = CurrentHitDistance > maxSafeDistance ? Color.blue : Color.red;

	}
	private void OnDrawGizmos()
	{
		Gizmos.color = raycastColor;
		Debug.DrawLine(Origin, Origin + Direection * CurrentHitDistance);
		Gizmos.DrawWireSphere(Origin + Direection * CurrentHitDistance, SphereRadius);
	}
	#endregion End Monobehaviour callbacks

	private void RefreshMask()
	{
		var ai = Parent != null ? Parent.GetComponent<AIVehicleController>() : null;
		Mask = ai != null ? ai.obstacleMask : Physics.DefaultRaycastLayers;
	}

	public void RedColor(bool val)
	{
		val = CurrentHitDistance > maxSafeDistance;
	}
}
