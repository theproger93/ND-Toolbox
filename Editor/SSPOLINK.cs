
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SSPOLINK
{
    private const string URL = "https://nikdorn.com/free-sprite-sheet-packer-online/";

    [MenuItem("Tools/ND Toolbox/Online Tools/Sprite Sheet Packer Online")]
    private static void OpenSpriteSheetPacker()
    {
        Application.OpenURL(URL);
    }
}
#endif