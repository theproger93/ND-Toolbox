using UnityEditor;
using TMPro.EditorUtilities;
using UnityEngine;

[CustomEditor(typeof(Material))]
[CanEditMultipleObjects]
public class TMP_SoftMaskShaderGUI : TMP_SDFShaderGUI
{
    // Soft Mask Properties (updated to match shader)
    private MaterialProperty _softMaskTex;
    private MaterialProperty _softMaskFade;
    private MaterialProperty _softMaskScale;
    private MaterialProperty _softMaskOffset;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // Find the Soft Mask properties
        _softMaskTex = FindProperty("_SoftMaskTex", properties);
        _softMaskFade = FindProperty("_SoftMaskFade", properties);
        _softMaskScale = FindProperty("_SoftMaskScale", properties);
        _softMaskOffset = FindProperty("_SoftMaskOffset", properties);

        // Draw the default TMP UI first
        base.OnGUI(materialEditor, properties);

        // Add Soft Mask section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Soft Mask", EditorStyles.boldLabel);

        materialEditor.TexturePropertySingleLine(
            new GUIContent("Mask Texture"), 
            _softMaskTex);
        
        materialEditor.ShaderProperty(_softMaskFade, "Fade Strength");
        materialEditor.ShaderProperty(_softMaskScale, "Scale");
        materialEditor.ShaderProperty(_softMaskOffset, "Offset");
    }
}