#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class RichTextWizard : EditorWindow
{
    
    string _text = "Your Text";    
    string _resultView;
    Color _color;
    float _fontSizePoints = 30;
    float _fontSizeProcent = 120;
    string _fontName;
    string _fontMaterial;
    float _lineHeight;
    float _rotate;
    float _characterSpacing;
    float _verticalOffset;
    private bool showTabContent;
    Vector2 scrollPosition = Vector2.zero;
    int selGridInt = 0;
    string[] selStrings = { "Font Size in points (default,static)", "Font Size in % from TMP Font Size (Dynamic)" };
    private const string _helpBox = "1. Add rotate tag in front of your text" +
                                    "\n2. Enable RTL in TextMeshPro - Text" +
                                    "\n3. Rotate game object to 90° by Z axsis" + 
                                    "\n4. Use Character space for line height" + 
                                    "\n5. Use Line height for character space";

    // Search and Replace
    private string _searchText;
    private string _replaceText;

    [MenuItem("Tools/ND Toolbox/Text Mesh Pro/RichText Wizard")]
    public static void ShowWindow()
    {
        GetWindow<RichTextWizard>("RichText Wizard");
    }

    private float DrawFloatField(string labelName, float value, float width)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(labelName);
        var newValue  = EditorGUILayout.FloatField(value, GUILayout.Width(width));
        EditorGUILayout.EndHorizontal();
        return newValue;
    }
    void OnGUI()
    {   
        GUILayout.Space(10);
        GUIStyle headLine = new GUIStyle(GUI.skin.label);
        headLine.fontSize = 14;
        headLine.fontStyle = FontStyle.Bold;
        headLine.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Text Mesh Pro Rich text Editor", headLine);
        GUILayout.Space(10);
        GUILayout.Label("Text to modify");
        _text = EditorGUILayout.TextArea(_text, GUILayout.Height(50));        
        GUILayout.Space(20);
        GUILayout.Label("Result - set html tag to see result");
        _resultView = EditorGUILayout.TextArea(_text, GUILayout.Height(50));

        GUILayout.Label("============================================");
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.MaxWidth(550), GUILayout.MaxHeight(850));
        
        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1, EditorStyles.radioButton);
        GUILayout.Space(10);
        if (selGridInt == 0)
        {
            _fontSizePoints = DrawFloatField("Font Size in points(Static)", _fontSizePoints, 60);
            if (GUILayout.Button("Size tag - Size in points (default)"))
            {
                _text = EditorGUILayout.TextField("", "<size=" + _fontSizePoints + ">" + _text + "</size>");
            }
        }
        else
        {
            _fontSizeProcent = DrawFloatField("Font Size in % (Dynamic)", _fontSizeProcent, 60);
            if (GUILayout.Button("Size tag - Size in % from font size"))
            {
                _text = EditorGUILayout.TextField("", "<size=" + _fontSizeProcent + "%>" + _text + "</size>");
            }
        }
        GUILayout.Space(10);
        _color = EditorGUILayout.ColorField("Set color for color tag", _color);
        if (GUILayout.Button("Color tag"))
        {
            _text = EditorGUILayout.TextField("", "<color=#" + ColorUtility.ToHtmlStringRGB(_color) + ">" + _text + "</color>");
        }
        if (GUILayout.Button("Sprite tag - Use name"))
        {
            _text = EditorGUILayout.TextField("", "<sprite name=" + '\"' + _text + '\"' + ">");
        }
        GUILayout.Label("============================================");
        
        _characterSpacing = DrawFloatField("Character Spacing in points", _characterSpacing, 60);
        if (GUILayout.Button("Character Spacing tag"))
        {
            _text = EditorGUILayout.TextField("", "<cspace="+ _characterSpacing + ">" + _text + "</cspace>");
        }
        _verticalOffset = DrawFloatField("Vertical Offset in em", _verticalOffset, 60);
        if (GUILayout.Button("Vertical Offset tag"))
        {
            _text = EditorGUILayout.TextField("", "<voffset=" + _verticalOffset + "em>" + _text + "</voffset>");
        }
        _lineHeight = DrawFloatField("Line Height in % (Dynamic)", _lineHeight, 60);
        if (GUILayout.Button("Line Height tag"))
        {
            _text = EditorGUILayout.TextField("", "<line-height="+ _lineHeight + "%>" + _text);
        }
        _rotate = DrawFloatField("Character rotation", _rotate, 60);
        if (GUILayout.Button("Rotation tag"))
        {
            _text = EditorGUILayout.TextField("", "<rotate="+ _rotate + ">" + _text +"</rotate>");
        }
        GUILayout.Label("============================================");
        showTabContent = EditorGUILayout.Foldout(showTabContent, "How to add vertcal text ?");
        if (showTabContent)
        {
            EditorGUILayout.HelpBox(_helpBox, MessageType.Info);
        }
        
        GUIContent captureButtonContent = new GUIContent("   Vertical text", EditorGUIUtility.IconContent("d_align_vertically").image);
        if (GUILayout.Button(captureButtonContent))
        {
            _text = EditorGUILayout.TextField("", "<rotate=-90>");
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Bold tag - Apply bold"))
        {
            _text = EditorGUILayout.TextField("", "<b>" + _text + "</b>");
        }
        if (GUILayout.Button("Italic  tag - Apply italic"))
        {
            _text = EditorGUILayout.TextField("", "<i>" + _text + "</i>");
        }
        if (GUILayout.Button("Strikethrough tag"))
        {
            _text = EditorGUILayout.TextField("", "<s>" + _text + "</s>");
        }
        if (GUILayout.Button("Underline tag"))
        {
            _text = EditorGUILayout.TextField("", "<u>" + _text + "</u>");
        }
        if (GUILayout.Button("Lowercase tag"))
        {
            _text = EditorGUILayout.TextField("", "<lowercase>" + _text + "</lowercase>");
        }
        if (GUILayout.Button("Uppercase tag"))
        {
            _text = EditorGUILayout.TextField("", "<uppercase>" + _text + "</uppercase>");
        }
        if (GUILayout.Button("Smallcaps tag"))
        {
            _text = EditorGUILayout.TextField("", "<smallcaps>" + _text + "</smallcaps>");
        }
        if (GUILayout.Button("Subscript tag"))
        {
            _text = EditorGUILayout.TextField("", "<sup>" + _text + "</sup>");
        }
        if (GUILayout.Button("Superscript tag"))
        {
            _text = EditorGUILayout.TextField("", "<sub>" + _text + "</sub>");
        }
        if (GUILayout.Button("Mark tag - HEX colors + alpha"))
        {
            _text = EditorGUILayout.TextField("", "<mark=#ffff00aa>" + _text + "</mark>");
        }
        GUILayout.Space(20);
        GUILayout.Label("Before using Font tag make sure your font is in");
        GUILayout.Label("TextMesh Pro>Recurses>Fonts & Materials >>folder<<");
        _fontName = EditorGUILayout.TextField("Font name", _fontName);
        _fontMaterial = EditorGUILayout.TextField("Font material name", _fontMaterial);
        if (GUILayout.Button("Font tag - Change text font"))
        {
            _text = EditorGUILayout.TextField("", "<font=" + '\"' + _fontName + '\"' + " material=" + '\"' + _fontMaterial + '\"' + ">" + _text + "</font>");
        }

        // Search and Replace
        EditorGUILayout.LabelField("Search and Replace", EditorStyles.boldLabel);
        _searchText = EditorGUILayout.TextField(new GUIContent("Search", "Enter text to search for"), _searchText);
        _replaceText = EditorGUILayout.TextField(new GUIContent("Replace", "Enter text to replace with"), _replaceText);
        if (GUILayout.Button(new GUIContent("Replace", "Replace all occurrences of the search text")))
        {
            _text = _text.Replace(_searchText, _replaceText);
        }
       
        GUILayout.EndScrollView();
        
        GUILayout.Space(20);
        if (GUILayout.Button("Visit nikdorn.com"))
        {
            Application.OpenURL("https://nikdorn.com");
        }
    }
}
#endif
