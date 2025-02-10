#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class SpriteConverterWindow : EditorWindow
{
    private Texture2D spriteTexture;
    private int currentWidth;
    private int currentHeight;
    private int upscaledWidth;
    private int upscaledHeight;
    private bool overrideOriginal = false;

    private enum Alignment { Center, Top, Bottom, Left, Right }
    private Alignment alignment = Alignment.Center;

    [MenuItem("Tools/ND Toolbox/Optimization/Sprite Size Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteConverterWindow>("Sprite Size Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Resize Sprite to Fit DXT5 Format", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        spriteTexture = (Texture2D)EditorGUILayout.ObjectField("Sprite Texture", spriteTexture, typeof(Texture2D), false);

        if (spriteTexture != null)
        {
            currentWidth = spriteTexture.width;
            currentHeight = spriteTexture.height;

            upscaledWidth = GetNextMultipleOf4(currentWidth);
            upscaledHeight = GetNextMultipleOf4(currentHeight);

            EditorGUILayout.LabelField("Current Size:", $"{currentWidth} x {currentHeight}");

            GUIStyle greenTextStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green }
            };
            EditorGUILayout.LabelField("Upscaled Size:", $"{upscaledWidth} x {upscaledHeight}", greenTextStyle);

            EditorGUILayout.Space();

            GUILayout.Label("Add Pixels Alignment:");
            alignment = (Alignment)EditorGUILayout.EnumPopup("Alignment", alignment);

            EditorGUILayout.Space();

            overrideOriginal = EditorGUILayout.Toggle("Override Original", overrideOriginal);

            GUIContent convertButtonContent = new GUIContent(" Resize and Save", EditorGUIUtility.IconContent("CanvasScaler Icon").image);
            if (GUILayout.Button(convertButtonContent, GUILayout.Height(40)))
            {
                ConvertAndSaveSprite();
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Visit NikDorn.com", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://NikDorn.com");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Drag and drop a sprite texture to begin.", MessageType.Info);
        }
    }

    private void ConvertAndSaveSprite()
    {
        if (spriteTexture == null)
        {
            Debug.LogError("No sprite selected for conversion.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(spriteTexture);
        string directory = Path.GetDirectoryName(assetPath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
        string newFileName = overrideOriginal ? fileNameWithoutExtension + ".png" : fileNameWithoutExtension + "_DXT5_BC3.png";
        string newFilePath = Path.Combine(directory, newFileName);

        Texture2D upscaledTexture = new Texture2D(upscaledWidth, upscaledHeight, spriteTexture.format, false);

        int xOffset = 0;
        int yOffset = 0;

        switch (alignment)
        {
            case Alignment.Center:
                xOffset = (upscaledWidth - currentWidth) / 2;
                yOffset = (upscaledHeight - currentHeight) / 2;
                break;
            case Alignment.Top:
                xOffset = (upscaledWidth - currentWidth) / 2;
                yOffset = upscaledHeight - currentHeight;
                break;
            case Alignment.Bottom:
                xOffset = (upscaledWidth - currentWidth) / 2;
                yOffset = 0;
                break;
            case Alignment.Left:
                xOffset = 0;
                yOffset = (upscaledHeight - currentHeight) / 2;
                break;
            case Alignment.Right:
                xOffset = upscaledWidth - currentWidth;
                yOffset = (upscaledHeight - currentHeight) / 2;
                break;
        }

        for (int y = 0; y < upscaledHeight; y++)
        {
            for (int x = 0; x < upscaledWidth; x++)
            {
                upscaledTexture.SetPixel(x, y, Color.clear);
            }
        }

        Color[] originalPixels = spriteTexture.GetPixels();
        upscaledTexture.SetPixels(xOffset, yOffset, currentWidth, currentHeight, originalPixels);
        upscaledTexture.Apply();

        byte[] pngData = upscaledTexture.EncodeToPNG();
        File.WriteAllBytes(newFilePath, pngData);
        AssetDatabase.Refresh();

        Debug.Log($"Sprite converted and saved as: {newFilePath}");
    }

    private int GetNextMultipleOf4(int value)
    {
        return (value % 4 == 0) ? value : value + (4 - (value % 4));
    }
}
#endif