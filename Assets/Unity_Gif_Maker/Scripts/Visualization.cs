using NaughtyAttributes;
using UnityEngine;

[ExecuteInEditMode]
public class Visualization : MonoBehaviour
{
	private GIFMaker gifMaker;
	private GameObject assignedPrefab;
	private GameObject objectVisualization;

	[Button("Disable Visualization", EButtonEnableMode.Always, ButtonPlacement.Top)]
	public void DisableVisualization()
	{
		gifMaker.visualizationEnabled = false;
		DestroyImmediate(objectVisualization);
		DestroyImmediate(this);
	}

	private void Awake()
	{
		gifMaker = GetComponent<GIFMaker>();

		if (gifMaker.GetAssignedPrefab() != null)
		{
			assignedPrefab = gifMaker.GetAssignedPrefab();

			GameObject inSceneVisualization = GameObject.Find("VisualizationObject");
			if (inSceneVisualization != null)
			{
				objectVisualization = inSceneVisualization;
				return;
			}

			objectVisualization = Instantiate(assignedPrefab);
			objectVisualization.name = "VisualizationObject";
		}
	}

	void Start()
	{

	}

	void Update()
	{
		if (assignedPrefab == null || assignedPrefab != gifMaker.GetAssignedPrefab())
		{
			if (objectVisualization != null) { DestroyImmediate(objectVisualization); }
			assignedPrefab = gifMaker.GetAssignedPrefab();
			objectVisualization = Instantiate(assignedPrefab);
			objectVisualization.name = "VisualizationObject";
			return;
		}

		if (objectVisualization == null) return;

		objectVisualization.transform.position = gifMaker.GetAssignedPrefabsPosition();
		objectVisualization.transform.rotation = gifMaker.GetAssignedPrefabsRotation();
		objectVisualization.transform.localScale = gifMaker.GetAssignedPrefabsScale();

	}
}
