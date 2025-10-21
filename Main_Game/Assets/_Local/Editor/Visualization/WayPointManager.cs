// Place this file in an "Editor" folder: Assets/Editor/WayPointManager.cs
using UnityEditor;
using UnityEngine;

public class WayPointManager : EditorWindow
{
	#region Public Fields
	public Transform WayPointRoot;
	public Transform SidesParent;
	#endregion

	[MenuItem("Tools/Waypoint Editor")]
	public static void Open()
	{
		GetWindow<WayPointManager>("Waypoint Editor");
	}

	#region Editor Callbacks
	private void OnGUI()
	{
		// Use a SerializedObject so the fields show and can be saved properly in the EditorWindow
		SerializedObject so = new SerializedObject(this);
		so.Update();

		EditorGUILayout.PropertyField(so.FindProperty("WayPointRoot"));
		EditorGUILayout.PropertyField(so.FindProperty("SidesParent"));

		so.ApplyModifiedProperties();

		EditorGUILayout.Space();

		if (WayPointRoot == null)
		{
			EditorGUILayout.HelpBox("Root Transform must be assigned. Please assign a root transform.", MessageType.Warning);
			return;
		}

		EditorGUILayout.BeginVertical("box");
		DrawButtons();
		EditorGUILayout.EndVertical();
	}
	#endregion

	#region Buttons / Actions
	public void DrawButtons()
	{
		if (GUILayout.Button("Create WayPoint (root)"))
		{
			CreateWaypointUnderRoot();
		}

		GUILayout.Space(6);

		if (SidesParent == null)
		{
			EditorGUILayout.HelpBox("SidesParent not assigned - cannot create side waypoint.", MessageType.Info);
		}
		else
		{
			if (GUILayout.Button("Create Side WayPoint (sides parent)"))
			{
				CreateSideWaypoint();
			}
		}
	}
	private void CreateWaypointUnderRoot()
	{
		if (WayPointRoot == null)
		{
			Debug.LogWarning("WayPointRoot is null. Assign a root transform first.");
			return;
		}

		GameObject waypointObject = new GameObject($"Waypoint {WayPointRoot.childCount}");
		Undo.RegisterCreatedObjectUndo(waypointObject, "Create Waypoint");

		WayPoint wp = waypointObject.AddComponent<WayPoint>();
		waypointObject.transform.SetParent(WayPointRoot, false);
		waypointObject.transform.localPosition = Vector3.zero;
		waypointObject.transform.localRotation = Quaternion.identity;

		if (WayPointRoot.childCount > 1)
		{
			Transform prevT = WayPointRoot.GetChild(WayPointRoot.childCount - 2);
			WayPoint prevWp = prevT.GetComponent<WayPoint>();
			if (prevWp != null)
			{
				wp.PreviousWayPoint = prevWp;
				// assign the WayPoint instance (not a Transform)
				prevWp.NextWayPoint = wp;
				waypointObject.transform.position = prevWp.transform.position;
				waypointObject.transform.forward = prevWp.transform.forward;
			}
		}

		Selection.activeGameObject = waypointObject;
		EditorUtility.SetDirty(WayPointRoot);
	}

	private void CreateSideWaypoint()
	{
		if (SidesParent == null)
		{
			Debug.LogWarning("SidesParent is null. Assign SidesParent first.");
			return;
		}

		GameObject sideObj = new GameObject($"Side {SidesParent.childCount}");
		Undo.RegisterCreatedObjectUndo(sideObj, "Create Side Waypoint");

		WayPoint wp = sideObj.AddComponent<WayPoint>();
		sideObj.transform.SetParent(SidesParent, false);
		sideObj.transform.localPosition = Vector3.zero;
		sideObj.transform.localRotation = Quaternion.identity;

		if (SidesParent.childCount > 1)
		{
			Transform prevT = SidesParent.GetChild(SidesParent.childCount - 2);
			WayPoint prev = prevT.GetComponent<WayPoint>();
			if (prev != null)
			{
				wp.PreviousWayPoint = prev;
				// assign as WayPoint (not Transform)
				prev.NextWayPoint = wp;
			}
		}

		Selection.activeGameObject = sideObj;
		EditorUtility.SetDirty(SidesParent);
	}
	#endregion

	#region Other (placeholder)
	private void OtherWayPoints()
	{
		// placeholder for any future helper methods
	}
	#endregion
}
