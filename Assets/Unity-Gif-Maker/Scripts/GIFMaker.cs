using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

public class GIFMaker : MonoBehaviour
{
    private readonly List<GameObject> prefabs = new List<GameObject>();
    private Camera cam;

    [Tooltip("The size of the render texture used. Larger sizes will yield more crisp results, but will take longer.")]
    [SerializeField] private RenderTextureSize renderTextureWidth;
    [SerializeField] private RenderTextureSize renderTextureHeight;

    [Space]

    [Tooltip("If this is enabled and a material is selected. use the skybox for a backdrop. disabled = black background.")]
    [SerializeField] private bool allowSkyBox = true;
    [SerializeField] private Material skyBoxMaterial;

    [Space]

    [SerializeField] private bool transparentBackground = false;

    [Tooltip("Set the camera to perspective, or orthographic.")]
    [SerializeField] private bool isPerspectiveCamera = false;

    [Header("Assign a prefab for manual NFT creation.")]
    [SerializeField] private GameObject prefabToSnap;

    [Space]

    public float cameraZPosition = -5f;
    [Header("The Pos and Rot of the Prefab / s")]
    [SerializeField] private Vector3 prefabPosition;
    [SerializeField] private Vector3 prefabRotation;

    [Space]

    [SerializeField] private string LoadObjectsFromPath = "Parts/Bot";
    [SerializeField] private string PartPicturesPath = "Part_Pictures";
    [SerializeField] private string AutoGenSavePath = "AutoGen";
    [SerializeField] private string ManualGenSavePath = "ManualGen";
    [SerializeField] private string GIF_SavePath = "GIF_Frames";


    [Header("GIF Attributes")]
    [Range(1, 50)] [SerializeField] private int numOfFrames = 10;
    [Range(1, 10)] [SerializeField] private int rotationSpeedMultiplier = 1;
    [SerializeField] private bool xRotation;
    [SerializeField] private bool yRotation;
    [SerializeField] private bool zRotation;


    [Range(1, 50)] [SerializeField] private int DelayPerFrame = 5;


    public Process ExecuteCommand(string command)
    {
        ProcessStartInfo ProcessInfo;
        Process process;

        ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
        ProcessInfo.CreateNoWindow = true;
        ProcessInfo.UseShellExecute = true;

        process = Process.Start(ProcessInfo);
        process.EnableRaisingEvents = true;

        return process;
    }


    private RenderTexture CreateRenderTexture()
    {
        RenderTexture tex = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32) //for 24 bit depth with stencil.
        {
            antiAliasing = 2
        };
        return tex;
    }


    //generates all NFTS from prefabs within a path
    public void GeneratePicturesFromFileDirectory()
    {
        //populates a list of gameobjects grabbed from Resources.
        GrabPrefabsFromResources();

        for (int i = 0; i < prefabs.Count; i++)
        {
            DirectoryInfo info = BuildDirectory(Application.dataPath, PartPicturesPath, AutoGenSavePath);
            FileInfo fileInfo = new FileInfo(Path.Combine(info.FullName, prefabs[i].name + ".png"));
            TakeSnapShot(fileInfo, prefabs[i], false);
        }

        //refreshes the database so that the newly generated sprites are visible straight away.
        AssetDatabase.Refresh();
        prefabs.Clear();
    }

    //generates a single inputted prefab
    public void GeneratePictureFromPrefab()
    {
        DirectoryInfo info = BuildDirectory(Application.dataPath, PartPicturesPath, ManualGenSavePath);
        FileInfo fileInfo = new FileInfo(Path.Combine(info.FullName, prefabToSnap.name + ".png"));
        TakeSnapShot(fileInfo, prefabToSnap, false);

        AssetDatabase.Refresh();
    }

    public void GenerateGifFromPrefab()
    {
        RotateObject(prefabToSnap);

        DirectoryInfo info = BuildDirectory(Application.dataPath, PartPicturesPath, GIF_SavePath, prefabToSnap.name + "_Frames");
        CreateGIF(info, prefabToSnap);

        AssetDatabase.Refresh();
    }

    public void GenerateGifsFromFileDirectory()
    {
        GrabPrefabsFromResources();

        foreach (GameObject obj in prefabs)
        {
            RotateObject(obj);

            DirectoryInfo info = BuildDirectory(Application.dataPath, PartPicturesPath, GIF_SavePath, obj.name + "_Frames");
            CreateGIF(info, obj);
        }

        AssetDatabase.Refresh();
        prefabs.Clear();
    }

    private void CreateGIF(DirectoryInfo info, GameObject obj)
    {
        string strCmdText;
        if (transparentBackground)
        {
            strCmdText = $"magick convert -resize 768x576 -delay {DelayPerFrame} -loop 0 -alpha set -dispose previous \"{info.FullName}/*.png\" \"{Application.dataPath}/{obj.name}.gif\"";
        }
        else
        {
            strCmdText = $"magick convert -resize 768x576 -delay {DelayPerFrame} -loop 0 \"{info.FullName}/*.png\" \"{Application.dataPath}/{obj.name}.gif\"";
        }

        Process process = ExecuteCommand(strCmdText);
        //deletes the frames directory after the gif has been generated
        process.Exited += new EventHandler((sender, e) => DeleteDirectory(info));
    }

    private void DeleteDirectory(DirectoryInfo info)
    {
        info.Delete(true);
        UnityEngine.Debug.Log("DeleteDir Ran!!");
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
            DirectoryInfo info = BuildDirectory(Application.dataPath, PartPicturesPath, GIF_SavePath, obj.name + "_Frames");
            FileInfo fileInfo = new FileInfo(Path.Combine(info.FullName, $"{i:000}.png"));

            float time = (float)i / numOfFrames;
            Vector3 per = endValues * time;
            obj.transform.rotation = Quaternion.Euler(per);

            TakeSnapShot(fileInfo, obj, true);
        }
    }

    private void TakeSnapShot(FileInfo info, GameObject prefab, bool autoRotate = false)
    {
        if (cam == null) cam = GetComponent<Camera>();

        //BuildDirectory(Application.dataPath, "Assets", "Part_Pictures", info.FullName, prefab.name);

        cam.clearFlags = allowSkyBox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.orthographic = !isPerspectiveCamera;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100.0f;

        GameObject prefabClone;
        if (autoRotate)
        {
            prefabClone = Instantiate(prefab, prefabPosition, prefab.transform.rotation);
        }
        else
        {
            prefabClone = Instantiate(prefab, prefabPosition, Quaternion.Euler(prefabRotation));
        }

        foreach (var renderer in prefabClone.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.receiveShadows = true;
        }

        Vector3 camOffsetPos = cam.transform.position;
        camOffsetPos.z = cameraZPosition;
        cam.transform.position = camOffsetPos;

        cam.targetTexture = CreateRenderTexture();
        RenderTexture.active = cam.targetTexture;
        GL.Clear(true, true, transparentBackground ? Color.clear : Color.black);

        cam.Render();

        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.ARGB32, true, true);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        prefabClone.SetActive(false);
        DestroyImmediate(prefabClone);

        RenderTexture.active.Release();


        byte[] itemTexBytes = image.EncodeToPNG();
        File.WriteAllBytes(info.FullName, itemTexBytes);

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
        GameObject[] prefabsInLocation = Resources.LoadAll<GameObject>(LoadObjectsFromPath);
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
