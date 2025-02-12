using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FindNonCompressedTextures : EditorWindow
{
    private List<string> nonMultipleOf4Textures = new List<string>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/ND Toolbox/Optimization/Find Textures Not Multiple of 4")]
    public static void ShowWindow()
    {
        GetWindow<FindNonCompressedTextures>("Find Textures Not Multiple of 4");
    }

    private void OnGUI()
    {
        GUILayout.Label("Find Textures with Width/Height Not Multiple of 4", EditorStyles.boldLabel);

        if (GUILayout.Button("Find Textures"))
        {
            FindTexturesNotMultipleOf4();
        }

        GUILayout.Space(10);
        GUILayout.Label("Found Textures:", EditorStyles.boldLabel);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        if (nonMultipleOf4Textures.Count == 0)
        {
            GUILayout.Label("No textures found with width/height not a multiple of 4!", EditorStyles.helpBox);
        }
        else
        {
            foreach (var texturePath in nonMultipleOf4Textures)
            {
                if (GUILayout.Button(texturePath, EditorStyles.linkLabel))
                {
                    // Select the texture in the Project window
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                }
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("Visit nikdorn.com"))
        {
            Application.OpenURL("https://nikdorn.com");
        }
    }

    private void FindTexturesNotMultipleOf4()
    {
        nonMultipleOf4Textures.Clear();

        // Find all Texture2D assets in the project
        string[] allTextureGUIDs = AssetDatabase.FindAssets("t:Texture2D");
        foreach (string guid in allTextureGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Skip textures in the Packages folder
            if (path.StartsWith("Packages/"))
            {
                continue;
            }

            // Load the texture to check its dimensions
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                // Check if width or height is not a multiple of 4
                if (texture.width % 4 != 0 || texture.height % 4 != 0)
                {
                    nonMultipleOf4Textures.Add(path);
                }
            }
        }

        // Refresh the window to display the results
        Repaint();
    }
}