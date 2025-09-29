// TMPAnimatedTextPro.cs
// Controller/driver that works alongside your existing TMPAnimatedTextAdvanced.
// Watches runtime text changes, detects animation tags, and triggers a rebuild
// on the base component. Now also forwards the *current* text to the base so
// "rebuild even without tags" behaves correctly.

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TMPTextAnimatorController : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("If true, this will add TMPAnimatedTextAdvanced to the same GameObject if it is missing.")]
    public bool autoAddAdvanced = true;

    [Tooltip("If true, will auto-rebuild when TMP text changes at runtime or in play mode.")]
    public bool autoRebuildOnTextChange = true;

    [Tooltip("Detect custom animation tags in the text (e.g., <anim>, <wobble>, etc.) and rebuild when they appear.")]
    public bool detectAnimationTags = true;

    [Tooltip("If no animation tags are found but you still want to rebuild (e.g., for AutoMatch effects), enable this.")]
    public bool rebuildEvenWithoutTags = true;

    [Header("Tag Detection")]
    [Tooltip("Regex used to detect animation tags. Customize to match your tag syntax.")]
    public string animationTagPattern = @"<\s*(?:anim|fx|tw|wobble|shake|pulse|glitch)\b[^>]*>";

    [Header("Debug")]
    [SerializeField] private bool _log;
    [SerializeField] private bool _verboseLog;

    private TMP_Text _tmp;
    private Component _advanced; // No hard reference; we use SendMessage/reflection.
    private Regex _tagRegex;
    private string _lastText;
    private bool _suppressTextEvent;

    void Reset() => TryInit();
    void Awake() => TryInit();

    void OnEnable()
    {
        TryInit();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMPTextChanged);
        _lastText = _tmp != null ? _tmp.text : null;

        if (Application.isPlaying && autoRebuildOnTextChange)
        {
            // Handle whatever text is present at startup.
            HandleTextChanged(force: true);
        }
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMPTextChanged);
    }

    void TryInit()
    {
        if (_tmp == null) _tmp = GetComponent<TMP_Text>();
        if (_tagRegex == null)
        {
            try { _tagRegex = new Regex(animationTagPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase); }
            catch { _tagRegex = new Regex(animationTagPattern, RegexOptions.IgnoreCase); }
        }
        EnsureAdvancedPresence();
    }

    void EnsureAdvancedPresence()
    {
        if (_advanced == null)
        {
            _advanced = GetComponent("TMPAnimatedTextAdvanced");

            if (_advanced == null && autoAddAdvanced)
            {
                var type = GetTypeByName("TMPAnimatedTextAdvanced");
                if (type != null)
                {
                    _advanced = gameObject.AddComponent(type);
                    Log("Added TMPAnimatedTextAdvanced automatically.");
                }
                else
                {
                    LogWarning("TMPAnimatedTextAdvanced not found in project. Auto-add is enabled but the type couldn't be located.");
                }
            }
        }
    }

    static Type GetTypeByName(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }

    void OnTMPTextChanged(UnityEngine.Object obj)
    {
        if (!autoRebuildOnTextChange) return;
        if (_suppressTextEvent) return;
        if (obj != _tmp) return;
        HandleTextChanged(force: false);
    }

    void HandleTextChanged(bool force)
    {
        if (_tmp == null) return;

        var current = _tmp.text ?? string.Empty;
        if (!force && string.Equals(current, _lastText)) return;

        bool hasAnimTags = detectAnimationTags && _tagRegex != null && _tagRegex.IsMatch(current);
        if (_verboseLog) Log($"Text changed. hasAnimTags={hasAnimTags}, length={current.Length}");

        // in HandleTextChanged(bool force)
        if (hasAnimTags || rebuildEvenWithoutTags)
        {
            RebuildAdvanced(current, hasAnimTags);  // pass current text + tag info
        }

        _lastText = current;
    }

    // >>> CHANGE: Accept the new source text and set it on the base before rebuild.
    void RebuildAdvanced(string newSourceText, bool hasAnimTags)
    {
        EnsureAdvancedPresence();
        if (_advanced == null)
        {
            LogWarning("Rebuild requested but TMPAnimatedTextAdvanced is missing.");
            return;
        }

        _suppressTextEvent = true;

        // Always push the latest text to the base first (tags or not).
        _advanced.SendMessage("SetTaggedText", newSourceText, SendMessageOptions.DontRequireReceiver);
        TrySetMember(_advanced, "SourceText", newSourceText);

        // If NO tags: force ManualTags (no automatch), and clear any existing ranges.
        if (!hasAnimTags)
        {
            TrySetEnum(_advanced, "targetMode", "ManualTags");
            TryClearRanges(_advanced);              // <<< important
        }

        // Rebuild from the (new) source
        _advanced.SendMessage("RebuildFromSource", SendMessageOptions.DontRequireReceiver);

        // Reset TMP mesh so any previous deformations are discarded
        if (_tmp != null) _tmp.ForceMeshUpdate(true, true);

        // Let the base refresh any geometry cache if it has one
        _advanced.SendMessage("CacheFrameGeometry", SendMessageOptions.DontRequireReceiver);

        _suppressTextEvent = false;

        Log("Rebuilt base with current text; cleared ranges for plain text.");
    }



    


    // Reflection helper: set "SourceText" field or property if present.
    static void TrySetMember(Component comp, string name, string value)
    {
        if (comp == null) return;
        var t = comp.GetType();

        // Field
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(string))
        {
            f.SetValue(comp, value);
            return;
        }
        // Property (settable)
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(string))
        {
            p.SetValue(comp, value, null);
        }
    }
    static void TrySetEnum(Component comp, string fieldOrProp, string enumName)
    {
        if (comp == null) return;
        var t = comp.GetType();

        // Field
        var f = t.GetField(fieldOrProp, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType.IsEnum)
        {
            var val = Enum.Parse(f.FieldType, enumName, ignoreCase: true);
            f.SetValue(comp, val);
            return;
        }

        // Property
        var p = t.GetProperty(fieldOrProp, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType.IsEnum)
        {
            var val = Enum.Parse(p.PropertyType, enumName, ignoreCase: true);
            p.SetValue(comp, val, null);
        }
    }

    // Clear the base component's _ranges list via reflection (if present)
    static void TryClearRanges(Component comp)
    {
        if (comp == null) return;
        var t = comp.GetType();
        var f = t.GetField("_ranges", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f == null) return;
        var list = f.GetValue(comp) as System.Collections.IList;
        list?.Clear();
    }

    /// <summary> Safe way to set text at runtime that triggers a rebuild once. </summary>
    public void SetText(string text, bool rebuild = true)
    {
        if (_tmp == null) TryInit();
        if (_tmp == null) return;

        _suppressTextEvent = true;
        _tmp.text = text ?? string.Empty;
        _suppressTextEvent = false;

        if (rebuild) HandleTextChanged(force: true);
    }

    /// <summary> Formatting helper: string.Format then SetText. </summary>
    public void SetText(string format, params object[] args)
    {
        if (_tmp == null) TryInit();
        if (_tmp == null) return;

        string newText = string.Format(format, args);
        SetText(newText, rebuild: true);
    }

    /// <summary> Manually trigger a refresh/rebuild. </summary>
    public void RefreshNow() => HandleTextChanged(force: true);

    [ContextMenu("Refresh Now")]
    void ContextRefresh()
    {
        RefreshNow();
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
    }

    void Log(string msg)
    {
        if (_log) Debug.Log($"[TMPTextAnimatorController] {msg}", this);
    }

    void LogWarning(string msg)
    {
        Debug.LogWarning($"[TMPTextAnimatorController] {msg}", this);
    }
}
