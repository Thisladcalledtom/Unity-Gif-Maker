using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GifMakerInspector : Editor
{
    private bool PNGAttributesFoldout;
    private bool GIFAttributesFoldout;

    public override void OnInspectorGUI()
    {
        //draws the original properties established in `GeneratePictures`
        DrawDefaultInspector();

        GIFMaker nfts = (GIFMaker)target;

        PNGAttributesFoldout = EditorGUILayout.Foldout(PNGAttributesFoldout, "PNG Generation");
        GIFAttributesFoldout = EditorGUILayout.Foldout(GIFAttributesFoldout, "GIF Generation");

        GUILayout.Space(20);

        if (PNGAttributesFoldout)
        {
            if (GUILayout.Button("Generate PNG From Prefab"))
            {
                nfts.GeneratePictureFromPrefab();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Generate PNG's From File Directory"))
            {
                nfts.GeneratePicturesFromFileDirectory();
            }
        }

        if (GIFAttributesFoldout)
        {
            if (GUILayout.Button("Generate GIF From Prefab"))
            {
                nfts.GenerateGifFromPrefab();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Generate GIFS From File Directory"))
            {
                nfts.GenerateGifsFromFileDirectory();
            }
        }
    }
}
