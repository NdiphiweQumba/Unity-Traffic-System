using UnityEditor;
using UnityEngine;


public class WayPointManager : EditorWindow
 {
    #region Public Fields 
	 public Transform WayPointRoot;
    
    
	 public Transform SidesParent;
    [MenuItem("Tools/Waypoint Editor")]
	    public static void Open()
	{
        GetWindow<WayPointManager>();
    }
    #endregion

    #region Private Fields
    #endregion

    #region MonoBehaviour Callback 
    #endregion
    #region Editor Callbacks
    private void OnGUI()
    {
        SerializedObject obj = new SerializedObject(this);

	    EditorGUILayout.PropertyField(obj.FindProperty("WayPointRoot"));
	    EditorGUILayout.PropertyField(obj.FindProperty("SidesParent"));

        if (WayPointRoot == null)
        {
            EditorGUILayout.HelpBox("Root Transform must be selected please assign in the root tranform", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            DrawButtons();
            EditorGUILayout.EndVertical();
        }
        obj.ApplyModifiedProperties();
    }
    #endregion

    #region Public Methods 
    public void DrawButtons()
    {
        if (GUILayout.Button("Create WayPoint"))
        {
	        CreatePoint();
        }
    }
    private void CreateWayPoint()
    {
        GameObject waypointobject = new GameObject($"Waypoint  {WayPointRoot.childCount}", typeof(WayPoint));
	    waypointobject.transform.SetParent(WayPointRoot, false); // World Position reset

        WayPoint wayPoint = waypointobject.GetComponent<WayPoint>();

        if (WayPointRoot.childCount > 1)
        {
            wayPoint.PreviousWayPoint = WayPointRoot.GetChild(WayPointRoot.childCount - 2).GetComponent<WayPoint>();
            wayPoint.PreviousWayPoint.NextWayPoint = wayPoint;
            // Place waypoint at last position //
            wayPoint.transform.position = wayPoint.PreviousWayPoint.transform.position;
            waypointobject.transform.forward = wayPoint.PreviousWayPoint.transform.forward; 	
        }

        Selection.activeGameObject = wayPoint.gameObject;
    }
	 
	 private void CreatePoint()
	 {
	 	GameObject obj = new GameObject($"Side {SidesParent.childCount}", typeof(WayPoint));
	 
	 	obj.transform.SetParent(SidesParent, false);
	 	
	 	WayPoint wp = obj.GetComponent<WayPoint>();
	 	
	 	wp.PreviousWayPoint = SidesParent.childCount > 1 ? SidesParent.GetChild(SidesParent.childCount - 2).GetComponent<WayPoint>() : null;
	 	wp.PreviousWayPoint.NextWayPoint = wp; // when next gets created assign previous
	 	
	 }
	#endregion

	#region Private 
	private void OtherWayPoints () 
    {

	}
	#endregion

}