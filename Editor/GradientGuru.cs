using UnityEngine;
using UnityEditor;
using System.IO;

public class GradientGuru : EditorWindow
{
#if UNITY_EDITOR
    private Gradient gradient = new Gradient()
    {
        colorKeys = new GradientColorKey[]
    {
        new GradientColorKey(Color.white, 0f),
        new GradientColorKey(Color.black, 1f)
    },
        alphaKeys = new GradientAlphaKey[]
    {
        new GradientAlphaKey(1f, 0f),
        new GradientAlphaKey(1f, 1f)
    }
    };

    private bool invertColors;
    private int textureSize = 256;
    private float rotationDegrees; // Using degrees for rotation
    private string saveFolderPath = "Assets/Textures";

    private Texture2D previewTexture;

    [MenuItem("Tools/ND Toolbox/GradientGuru")]
    static void Init()
    {
        GradientGuru window = (GradientGuru)EditorWindow.GetWindow(typeof(GradientGuru));
        window.Show();
    }

    void OnEnable()
    {
        previewTexture = new Texture2D(textureSize, textureSize);
        saveFolderPath = "Assets/Textures";
    }

    void OnGUI()
    {
        GUILayout.Label("Convert Your Favorite Unity Gradients To Textures", EditorStyles.boldLabel);

        gradient = EditorGUILayout.GradientField("Gradient", gradient);
        invertColors = EditorGUILayout.Toggle("Invert Colors", invertColors);
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);

        
        rotationDegrees = EditorGUILayout.Slider("Rotation (degrees)", rotationDegrees, 0f, 360f);

        
        saveFolderPath = EditorGUILayout.TextField("Save Folder Path", saveFolderPath);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Preview", EditorStyles.boldLabel);

        
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(previewTexture, GUILayout.Width(256), GUILayout.Height(256));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.BeginHorizontal();

        
        if (GUILayout.Button("Generate Gradient"))
        {
            GenerateGradient();
        }

        if (GUILayout.Button("Save"))
        {
            SaveTexture();
        }

        GUILayout.EndHorizontal();
        GUIStyle linkButton = new GUIStyle(GUI.skin.button);
        linkButton.hover.textColor = Color.green;
        linkButton.fontSize = 10;
        if (GUILayout.Button("NikDorn.com", linkButton))
        {
            Application.OpenURL("https://nikdorn.com/");
        }
    }

    void GenerateGradient()
    {
        float rotationRadians = rotationDegrees * Mathf.Deg2Rad;

        for (int x = 0; x < previewTexture.width; x++)
        {
            for (int y = 0; y < previewTexture.height; y++)
            {
                float normalizedX = (x - previewTexture.width / 2.0f) / (float)(previewTexture.width - 1);
                float normalizedY = (y - previewTexture.height / 2.0f) / (float)(previewTexture.height - 1);

                float rotatedX = Mathf.Cos(rotationRadians) * normalizedX - Mathf.Sin(rotationRadians) * normalizedY;
                float rotatedY = Mathf.Sin(rotationRadians) * normalizedX + Mathf.Cos(rotationRadians) * normalizedY;

                rotatedX = rotatedX + 0.5f;  
                rotatedY = rotatedY + 0.5f;

                Color pixelColor = gradient.Evaluate(rotatedX);

                if (invertColors)
                {
                    pixelColor = new Color(1f - pixelColor.r, 1f - pixelColor.g, 1f - pixelColor.b, pixelColor.a);
                }

                previewTexture.SetPixel(x, y, pixelColor);
            }
        }

        previewTexture.Apply();
        Repaint(); 
    }

    void SaveTexture()
    {
        string fileName = "GradientTexture.png";
        string currentFolderPath = saveFolderPath;

        if (!AssetDatabase.IsValidFolder(currentFolderPath))
        {
            string parentFolder = Path.GetDirectoryName(currentFolderPath);
            string newFolderName = Path.GetFileName(currentFolderPath);
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }

        string[] existingFiles = Directory.GetFiles(currentFolderPath, "GradientTexture*.png");
        int version = existingFiles.Length + 1;

        string finalPath = Path.Combine(currentFolderPath, fileName);
        if (version > 1)
        {
            finalPath = Path.Combine(currentFolderPath, "GradientTexture" + version + ".png");
        }

        ResizeTexture(previewTexture, textureSize, textureSize);

        System.IO.File.WriteAllBytes(finalPath, previewTexture.EncodeToPNG());
        AssetDatabase.Refresh();
    }

    void ResizeTexture(Texture2D texture, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        RenderTexture.active = rt;
        Graphics.Blit(texture, rt);
        texture.GetPixel(targetWidth, targetHeight);
        texture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
    }
#endif
}
