
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SpriteSlicerMaster : EditorWindow
{

    private Texture2D inputSprite;
    private float alphaThreshold = 0.5f;
    private bool showTabContent;
    private const string _helpBox = "1. Drag and drop your Sprite into Sprite field." +
            "\n2. Adjust the Alpha Threshold slider to determine the transparency threshold for the slice." +
            "\n3. Once you've set your threshold, click the Slice Sprite button." +
            "\n4. The plugin will create two new textures within the same sprite, one for opaque parts and another for transparent parts. They will be named with (_Opaque) and (_Transparent) suffixes." +
            "\n5. If you wish to add a custom outline to the opaque part of the sprite, follow these steps" +
            "\n5.1 Select the (_Opaque) sprite you just created in your project." +
            "\n5.2 Open the Sprite Editor (usually by clicking the (Sprite Editor) button in the inspector)." +
            "\n5.3 In the Sprite Editor's (Custom Outline) tab, adjust the (Outline Tolerance) to 1 Click Apply to save your changes.";

    [MenuItem("Tools/ND Toolbox/Optimization/Sprite Slice Master")]
    static void Init()
    {
        SpriteSlicerMaster window = (SpriteSlicerMaster)EditorWindow.GetWindow(typeof(SpriteSlicerMaster));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Sprite Slice Master", EditorStyles.boldLabel);

        inputSprite = (Texture2D)EditorGUILayout.ObjectField("Input Sprite", inputSprite, typeof(Texture2D), false);
        alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", alphaThreshold, 0f, 1f);

        if (GUILayout.Button("Slice Sprite"))
        {
            SliceSprite(inputSprite, alphaThreshold);
        }
        showTabContent = EditorGUILayout.Foldout(showTabContent, "How to use ?");
        if (showTabContent)
        {
            EditorGUILayout.HelpBox(_helpBox, MessageType.Info);
        }
        GUIStyle linkButton = new GUIStyle(GUI.skin.button);
        linkButton.hover.textColor = Color.green;
        linkButton.fontSize = 8;
        if (GUILayout.Button("NikDorn.com", linkButton))
        {
            Application.OpenURL("https://nikdorn.com/");
        }
    }

    void SliceSprite(Texture2D sprite, float threshold)
    {       
        Texture2D opaqueTexture = new Texture2D(sprite.width, sprite.height);

        Texture2D transparentTexture = new Texture2D(sprite.width, sprite.height);

        for (int x = 0; x < sprite.width; x++)
        {
            for (int y = 0; y < sprite.height; y++)
            {
                Color pixelColor = sprite.GetPixel(x, y);
                                
                if (pixelColor.a >= threshold)
                {
                    opaqueTexture.SetPixel(x, y, pixelColor);
                    transparentTexture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    opaqueTexture.SetPixel(x, y, Color.clear);
                    transparentTexture.SetPixel(x, y, pixelColor);
                }
            }
        }

        opaqueTexture.Apply();
        transparentTexture.Apply();
        string spriteName = inputSprite.name;
        SaveTextureAsAsset(opaqueTexture, spriteName + "_Opaque");
        SaveTextureAsAsset(transparentTexture, spriteName + "_Transparent");
    }

    void SaveTextureAsAsset(Texture2D texture, string assetName)
    {
        string spriteFolderPath = AssetDatabase.GetAssetPath(inputSprite);
        spriteFolderPath = System.IO.Path.GetDirectoryName(spriteFolderPath);
        
        string fullPath = spriteFolderPath + "/" + assetName + ".png";


        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.Refresh();
    }

}
#endif
