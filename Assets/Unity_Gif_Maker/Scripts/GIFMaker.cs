using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Camera))]
public class GIFMaker : MonoBehaviour
{
	public bool visualizationEnabled;
	[Button("EnableVisualization", EButtonEnableMode.Always, ButtonPlacement.Top)]
	public void EnableVisualization()
	{
		if (gameObject.GetComponent<Visualization>() == null)
		{
			visualizationEnabled = true;
			gameObject.AddComponent<Visualization>();
		}
	}


	private readonly List<GameObject> prefabs = new List<GameObject>();
	private Camera cam;

	[BoxGroup("Camera Z Position")] public float cameraZPosition = -5f;

	[Tooltip("The size of the render texture used. Larger sizes will yield more crisp results, but will take longer.")]
	[BoxGroup("Render Texture")][SerializeField] private RenderTextureSize renderTextureWidth;
	[BoxGroup("Render Texture")][SerializeField] private RenderTextureSize renderTextureHeight;

	[Space]

	[Tooltip("Set whether the scene light should be enabled.")]
	[BoxGroup("Scene Initialization")][SerializeField] private bool useDirectionalLight = true;
	private Light directionalLight;
	[Tooltip("If this is enabled and a material is selected. use the skybox for a backdrop. disabled = black background.")]
	[BoxGroup("Scene Initialization")][SerializeField] private bool allowSkyBox = true;
	[BoxGroup("Scene Initialization")][SerializeField] private Material skyBoxMaterial;
	[Tooltip("Gives the pictures and gifs a solid background colour if not using a skybox.")]
	[BoxGroup("Scene Initialization")][SerializeField] private Color noSkyboxBackgroundColour;
	[Tooltip("If this is true, the global post processing volume will be used")]
	[BoxGroup("Scene Initialization")][SerializeField] private bool allowPostProcessing = false;
	[Tooltip("Set the camera to perspective, or orthographic.")]
	[BoxGroup("Scene Initialization")][SerializeField] private bool isPerspectiveCamera = false;

	[Foldout("Prefab Assignment")][SerializeField] private GameObject prefabToSnap;
	[Foldout("Prefab Assignment")][SerializeField] private Vector3 prefabPosition;
	[Foldout("Prefab Assignment")][SerializeField] private Vector3 prefabRotation;
	[Foldout("Prefab Assignment")][SerializeField] private Vector3 prefabScale = Vector3.one;

	[Space]

	private const string DefaultLoadPath = "Prefabs";
	private const string DefaultSavePath = "Unity_Gif_Maker/Generation";
	private const string GIFSavePath = "GIFS";
	private const string PictureSavePath = "Pictures";
	private const string GIF_FramePath = "Temp";

	[Foldout("GIF Attributes")][Range(1, 50)][SerializeField] private int numOfFrames = 10;
	[Foldout("GIF Attributes")][Range(1, 20)][SerializeField] private int rotationSpeedMultiplier = 1;
	[Foldout("GIF Attributes")][SerializeField] private bool invertRotation;
	[Foldout("GIF Attributes")][SerializeField] private bool xRotation;
	[Foldout("GIF Attributes")][SerializeField] private bool yRotation;
	[Foldout("GIF Attributes")][SerializeField] private bool zRotation;
	[Foldout("GIF Attributes")][Range(1, 50)][SerializeField] private int DelayPerFrame = 5;
	[Foldout("GIF Attributes")][SerializeField] private bool deleteTempFolderOnCompletion = true;


	public GameObject GetAssignedPrefab() => prefabToSnap;
	public Vector3 GetAssignedPrefabsPosition() => prefabPosition;
	public Quaternion GetAssignedPrefabsRotation() => Quaternion.Euler(prefabRotation);
	public Vector3 GetAssignedPrefabsScale() => prefabScale;

	public Process ExecuteCommand(string command)
	{
		ProcessStartInfo ProcessInfo;
		Process process;

		ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
		{
			CreateNoWindow = true,
			UseShellExecute = true
		};

		process = Process.Start(ProcessInfo);
		process.EnableRaisingEvents = true;
		return process;
	}


	private RenderTexture CreateRenderTexture()
	{
		RenderTexture tex = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 32, RenderTextureFormat.ARGB32) //for 24 bit depth with stencil.
		{
			antiAliasing = 2
		};
		return tex;
	}

	private void SetDirectionalLight()
	{
		if (directionalLight == null)
		{
			Light dLight = FindObjectOfType<Light>();
			if (dLight == null)
			{
				Debug.LogError("No Lights exist in the scene.");
				return;
			}


			bool isDirectional = dLight.type == LightType.Directional;
			if (dLight && !isDirectional)
			{
				Debug.LogError("No directional Light exists in the scene");
				return;
			}

			directionalLight = dLight;
		}

		directionalLight.enabled = useDirectionalLight;
	}


	[Foldout("Prefab Assignment"), Button]
	//generates all PNG's from prefabs within a path
	public void GeneratePicturesFromFileDirectory()
	{
		//populates a list of gameobjects grabbed from Resources.
		GrabPrefabsFromResources();
		SetDirectionalLight();

		for (int i = 0; i < prefabs.Count; i++)
		{
			DirectoryInfo info = BuildDirectory(Application.dataPath, DefaultSavePath, PictureSavePath);
			FileInfo fileInfo = new FileInfo(Path.Combine(info.FullName, prefabs[i].name + ".png"));
			TakeSnapShot(fileInfo, prefabs[i], false);
		}

		//refreshes the database so that the newly generated sprites are visible straight away.
		AssetDatabase.Refresh();
		prefabs.Clear();
	}


	[Foldout("Prefab Assignment"), Button]
	public void GeneratePictureFromPrefab()
	{
		SetDirectionalLight();
		DirectoryInfo info = BuildDirectory(Application.dataPath, DefaultSavePath, PictureSavePath);
		FileInfo fileInfo = new FileInfo(Path.Combine(info.FullName, prefabToSnap.name + ".png"));
		TakeSnapShot(fileInfo, prefabToSnap, false);

		AssetDatabase.Refresh();
	}


	[Foldout("GIF Attributes"), Button]
	public void GenerateGifFromPrefab()
	{
		SetDirectionalLight();
		RotateObject(prefabToSnap);

		DirectoryInfo info = BuildDirectory(Application.dataPath, GIF_FramePath, prefabToSnap.name + "_Frames");
		CreateGIF(info, prefabToSnap);
	}


	[Foldout("GIF Attributes"), Button]
	public void GenerateGifsFromFileDirectory()
	{
		SetDirectionalLight();
		GrabPrefabsFromResources();

		foreach (GameObject obj in prefabs)
		{
			RotateObject(obj);

			DirectoryInfo info = BuildDirectory(Application.dataPath, GIF_FramePath, obj.name + "_Frames");
			CreateGIF(info, obj);
		}
		prefabs.Clear();
	}

	private void CreateGIF(DirectoryInfo info, GameObject obj)
	{
		string strCmdText;
		string path = Path.Combine(Application.dataPath, DefaultSavePath, GIFSavePath, $"{obj.name}.gif");
		strCmdText = $"magick convert -set dispose background -background none -resize 768x576 -delay {DelayPerFrame} -loop 0 \"{info.FullName}/*.png\" \"{path}\"";

		Process process = ExecuteCommand(strCmdText);

		//deletes the frames directory after the gif has been generated
		process.Exited += new EventHandler((sender, e) =>
		{
			DeleteDirectory(info);
			if (gameObject.GetComponent<Visualization>() == null)
				gameObject.AddComponent<Visualization>();
		});
	}

	private void DeleteDirectory(DirectoryInfo info)
	{
		if (deleteTempFolderOnCompletion)
		{
			info.Delete(true);
		}

		AssetDatabase.Refresh();
	}

	private void RotateObject(GameObject obj)
	{
		//assign each rotation value by numOfFrames.
		Vector3 endValues = Vector3.zero;
		if (xRotation) endValues.x = rotationSpeedMultiplier * 360;
		if (yRotation) endValues.y = rotationSpeedMultiplier * 360;
		if (zRotation) endValues.z = rotationSpeedMultiplier * 360;

		//for each frame, Rotate object and snap a picture of it, and save it in the specified location.
		for (int i = 0; i < numOfFrames; i++)
		{
			DirectoryInfo info = BuildDirectory(Application.dataPath, GIF_FramePath, obj.name + "_Frames");
			FileInfo fileInfo = new FileInfo(Path.Combine(info.FullName, $"{i:000}.png"));

			float time = (float)i / numOfFrames;
			Vector3 per = endValues * time;
			obj.transform.rotation = invertRotation ? Quaternion.Euler(per) : Quaternion.Inverse(Quaternion.Euler(per));
			TakeSnapShot(fileInfo, obj, true);
		}
	}

	private void TakeSnapShot(FileInfo info, GameObject prefab, bool autoRotate = false)
	{
		GameObject visualizationObject = GameObject.Find("VisualizationObject");
		if (visualizationObject != null)
		{
			DestroyImmediate(visualizationObject);
		}
		if (visualizationEnabled)
		{
			Visualization visualization = GetComponent<Visualization>();
			if (visualization != null)
			{
				DestroyImmediate(visualization);
			}
		}

		if (cam == null) cam = GetComponent<Camera>();

		if (allowSkyBox && skyBoxMaterial == null) { throw new Exception("No skybox material assigned yet you are trying to use one."); }
		if (allowSkyBox && skyBoxMaterial != null) { RenderSettings.skybox = skyBoxMaterial; }


		cam.backgroundColor = Color.clear;
		cam.clearFlags = allowSkyBox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
		cam.orthographic = !isPerspectiveCamera;
		cam.nearClipPlane = 0.01f;
		cam.farClipPlane = 100.0f;

		GameObject prefabClone = Instantiate(prefab, prefabPosition, autoRotate ? prefab.transform.rotation : Quaternion.Euler(prefabRotation));
		prefabClone.transform.localScale = prefabScale;

		foreach (var renderer in prefabClone.GetComponentsInChildren<MeshRenderer>()) { renderer.receiveShadows = true; }

		Vector3 camOffsetPos = cam.transform.position;
		camOffsetPos.z = cameraZPosition;
		cam.transform.position = camOffsetPos;

		cam.targetTexture = CreateRenderTexture();
		RenderTexture.active = cam.targetTexture;
		GL.Clear(true, true, Color.clear);

		cam.Render();

		Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.ARGB32, true, true);
		image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
		image.Apply();

		prefabClone.SetActive(false);
		DestroyImmediate(prefabClone);

		RenderTexture.active.Release();


		byte[] itemTexBytes = image.EncodeToPNG();
		File.WriteAllBytes(info.FullName, itemTexBytes);

		if (gameObject.GetComponent<Visualization>() == null && visualizationEnabled)
		{
			gameObject.AddComponent<Visualization>();
		}

	}

	private DirectoryInfo BuildDirectory(params string[] pathParts)
	{
		string combinedPath = Path.Combine(pathParts);
		DirectoryInfo info = new DirectoryInfo(combinedPath);
		if (!info.Exists) info.Create();

		return info;
	}

	private void GrabPrefabsFromResources()
	{
		GameObject[] prefabsInLocation = Resources.LoadAll<GameObject>(DefaultLoadPath);
		foreach (GameObject prefab in prefabsInLocation)
		{
			prefabs.Add(prefab);
		}

		UnityEngine.Debug.Log($"Amount of prefabs: {prefabs.Count}");
	}

}

[System.Serializable]
public enum RenderTextureSize
{
	[InspectorName("256")]
	a = 256,
	[InspectorName("512")]
	b = 512,
	[InspectorName("1024")]
	c = 1024,
	[InspectorName("2048")]
	d = 2048,
	[InspectorName("4096")]
	e = 4096,
	[InspectorName("8192")]
	f = 8192
}
