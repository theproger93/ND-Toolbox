
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ARCLINK
{
    private const string URL = "https://nikdorn.com/aspect-ratio-resolution-calculator/";

    [MenuItem("Tools/ND Toolbox/Online Tools/Aspect Ratio and Resolution Calculator")]
    private static void OpenSpriteSheetPacker()
    {
        Application.OpenURL(URL);
    }
}
#endif