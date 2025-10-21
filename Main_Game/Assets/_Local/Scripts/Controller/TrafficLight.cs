using UnityEngine;

/// <summary>
/// Simple traffic light cycle. You can expand it to support direction-based green phases.
/// This version toggles globally green/red for the intersection.
/// </summary>
public class TrafficLight : MonoBehaviour
{
	[Tooltip("Green duration in seconds.")]
	public float greenTime = 8f;
	[Tooltip("Yellow duration in seconds.")]
	public float yellowTime = 2f;
	[Tooltip("Red duration in seconds.")]
	public float redTime = 8f;

	public bool IsGreen { get; private set; }

	private float timer = 0f;
	private enum Phase { Green, Yellow, Red }
	private Phase phase = Phase.Green;

	private void Start()
	{
		phase = Phase.Green;
		IsGreen = true;
		timer = 0f;
	}

	private void Update()
	{
		timer += Time.deltaTime;
		switch (phase)
		{
		case Phase.Green:
			if (timer >= greenTime)
			{
				phase = Phase.Yellow;
				timer = 0f;
				IsGreen = false;
			}
			break;
		case Phase.Yellow:
			if (timer >= yellowTime)
			{
				phase = Phase.Red;
				timer = 0f;
				IsGreen = false;
			}
			break;
		case Phase.Red:
			if (timer >= redTime)
			{
				phase = Phase.Green;
				timer = 0f;
				IsGreen = true;
			}
			break;
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Color c = Color.gray;
		if (Application.isPlaying)
		{
			if (phase == Phase.Green) c = Color.green;
			else if (phase == Phase.Yellow) c = Color.yellow;
			else c = Color.red;
		}
		Gizmos.color = c;
		Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);
	}
#endif
}
