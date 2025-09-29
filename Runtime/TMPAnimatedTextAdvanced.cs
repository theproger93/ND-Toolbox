// Copyright (c) 2025 Nik Dornberg — nikdorn.com
// Author: Nik Dornberg

// TMPAnimatedTextAdvanced.cs (Tag-aware single component)
// What’s new:
// - Listens to TMP text changes.
// - If incoming TMP.text contains animation tags (wave/shake/etc.), it treats that as SourceText,
//   parses/strips tags, sets clean text to TMP, and rebuilds ranges.
// - If no tags are present, it does nothing (clean text passes through).
// - Guard prevents event recursion so Unity doesn’t freeze.
//
// Drop this in place of your existing TMPAnimatedTextAdvanced.cs.
// Remove any extra controller component from the same object.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMPAnimatedTextAdvanced : MonoBehaviour
{
    public enum TargetMode { AutoMatch, ManualTags }
    public enum EffectKind
    {
        Wave, HorizontalWave, Ripple,
        Shake, Bounce, Typewriter,
        Rainbow, ColorPulse,
        ScalePulse, Spin, Swirl,
        Wobble, Skew, Fade,
        FlipX, StretchY, Glitch
    }

    [Header("Source Text")]
    [TextArea(3, 10)] public string SourceText;

    [Header("Targeting")]
    public TargetMode targetMode = TargetMode.ManualTags;

    [Header("Auto-Match Settings")]
    public string matchText = "word";
    public bool matchCase = false;
    public bool wholeWord = false;
    public bool applyToAllMatches = true;
    public int matchIndex = 0;

    [Header("Playback")]
    public bool playOnEnable = true;
    public bool useUnscaledTime = false;
    public bool runInEditMode = true;

    [Header("Effect Selection")]
    public EffectKind selectedEffect = EffectKind.Wave;

    [Serializable]
    public class EffectParams
    {
        public float amp = 6f;
        public float speed = 2f;
        public float freq = 2f;
        public float duration = 0f; // 0 = loop forever
        public float phase = 0f;
        public float angle = 15f; // deg for skew/flip
        public float scale = 1.1f; // target scale for scale-pulse
        public float fade = 1f;    // mix amount for color/alpha effects
        public Color colorA = Color.white;
        public Color colorB = new Color(1f, 0.3f, 0.3f);
    }

    public EffectParams effect = new EffectParams();

    TMP_Text _tmp;
    float _startTime;

    readonly List<EffectRange> _ranges = new List<EffectRange>();

    TMP_TextInfo _ti;
    Vector3[][] _originalVertsPerMesh;

    // ---------- New: tag detection + recursion guard ----------
    [Tooltip("Regex to detect animation tags in incoming TMP.text.")]
    public string tagDetectPattern = @"<\s*(wave|hwave|ripple|shake|bounce|type(?:writer)?|rainbow|colorpulse|scale(?:pulse)?|spin|swirl|wobble|skew|fade|flipx|stretchy|glitch)\b";
    Regex _tagDetectRx;
    bool _suppressTMPEvent;
    string _lastSeenTMPText;

    void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
        TryCompileTagRegex();
        // Do not force a rebuild here—wait for OnEnable/OnValidate or explicit calls.
        if (string.IsNullOrEmpty(SourceText)) SourceText = _tmp.text;
    }

    void OnEnable()
    {
        if (_tmp == null) _tmp = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
        EditorApplication.update += EditorTick;
#endif
        _lastSeenTMPText = _tmp != null ? _tmp.text : null;

        if (playOnEnable)
        {
            // If SourceText already has tags at startup, parse/build now.
            if (!string.IsNullOrEmpty(SourceText) && HasAnimTags(SourceText))
                RebuildFromSource();
            else if (!string.IsNullOrEmpty(_tmp.text) && HasAnimTags(_tmp.text))
            {
                // Startup: TMP already has tagged text (e.g., localization system wrote it).
                SourceText = _tmp.text;
                RebuildFromSource();
            }
        }
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        _originalVertsPerMesh = null; // release refs that might be stale after exit
    }

#if UNITY_EDITOR
    void EditorTick()
    {
        if (!runInEditMode || Application.isPlaying) return;
        if (this == null || _tmp == null) return;
        _tmp.ForceMeshUpdate();
        LateUpdate();
        SceneView.RepaintAll();
        InternalEditorUtility.RepaintAllViews();
    }
#endif

    void TryCompileTagRegex()
    {
        try { _tagDetectRx = new Regex(tagDetectPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled); }
        catch { _tagDetectRx = new Regex(tagDetectPattern, RegexOptions.IgnoreCase); }
    }

    bool HasAnimTags(string s) => !string.IsNullOrEmpty(s) && _tagDetectRx != null && _tagDetectRx.IsMatch(s);

    void OnTextChanged(UnityEngine.Object obj)
    {
        if (obj != _tmp) return;
        if (_suppressTMPEvent) return;

        string current = _tmp.text ?? string.Empty;
        if (current == _lastSeenTMPText) return;
        _lastSeenTMPText = current;

        // If someone assigned TMP.text (localization, gameplay), and it contains animation tags,
        // treat that as the new SourceText, parse/strip, and rebuild. Otherwise, ignore.
        if (HasAnimTags(current))
        {
            SourceText = current;
            _suppressTMPEvent = true;
            RebuildFromSource();
            _suppressTMPEvent = false;
        }
        else
        {
            // No tags -> leave as is (clean text). Just refresh geometry cache.
            CacheFrameGeometry();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_tmp == null) _tmp = GetComponent<TMP_Text>();
        if (!isActiveAndEnabled || _tmp == null) return;

        // Avoid running while entering/exiting Play Mode or during script reload
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Defer one tick so TMP has a valid textInfo
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !_tmp || !isActiveAndEnabled) return;
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
            RebuildFromSource();
        };
    }
#endif

    float Now()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && runInEditMode)
            return (float)EditorApplication.timeSinceStartup;
#endif
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    // ===== Public API =====
    public void RebuildFromSource()
    {
        _ranges.Clear();

        if (_tmp == null) _tmp = GetComponent<TMP_Text>();
        if (_tmp == null) return; 

        if (targetMode == TargetMode.ManualTags)
        {
            string display = Parse(SourceText, out _, _ranges);
            _tmp.text = display;
        }
        else
        {
            _tmp.text = SourceText;
            BuildAutoMatchRanges();
        }

        _tmp.ForceMeshUpdate(true, true);
        _ti = _tmp.textInfo;
        if (_ti == null) return;

        CacheFrameGeometry();
        _startTime = Now();
    }

    void BuildAutoMatchRanges()
    {
        _ranges.Clear();
        if (string.IsNullOrEmpty(matchText) || string.IsNullOrEmpty(_tmp.text)) return;

        string hay = _tmp.text;
        string needle = matchCase ? matchText : matchText.ToLowerInvariant();
        string src = matchCase ? hay : hay.ToLowerInvariant();

        List<(int start, int end)> matches = new List<(int, int)>();
        int index = 0;
        while (true)
        {
            index = src.IndexOf(needle, index);
            if (index < 0) break;

            if (wholeWord)
            {
                bool leftOk = index == 0 || !char.IsLetterOrDigit(hay[index - 1]);
                int rightPos = index + matchText.Length;
                bool rightOk = rightPos >= hay.Length || !char.IsLetterOrDigit(hay[rightPos]);
                if (!(leftOk && rightOk)) { index += 1; continue; }
            }

            int start = VisibleIndexFromSourceIndex(hay, index);
            int end = VisibleIndexFromSourceIndex(hay, index + matchText.Length - 1);
            if (start >= 0 && end >= start)
                matches.Add((start, end));

            index += 1;
        }

        if (matches.Count == 0) return;

        if (!applyToAllMatches)
        {
            int i = Mathf.Clamp(matchIndex, 0, matches.Count - 1);
            AddRangeFromSelection(matches[i].start, matches[i].end);
        }
        else
        {
            foreach (var m in matches)
                AddRangeFromSelection(m.start, m.end);
        }
    }

    void AddRangeFromSelection(int start, int end)
    {
        var er = new EffectRange { Type = selectedEffect, start = start, end = end };
        er.fParams["amp"] = effect.amp;
        er.fParams["speed"] = effect.speed;
        er.fParams["freq"] = effect.freq;
        er.fParams["duration"] = effect.duration;
        er.fParams["phase"] = effect.phase;
        er.fParams["angle"] = effect.angle;
        er.fParams["scale"] = effect.scale;
        er.fParams["fade"] = effect.fade;
        er.colorA = effect.colorA;
        er.colorB = effect.colorB;
        _ranges.Add(er);
    }

    int VisibleIndexFromSourceIndex(string src, int srcIndex)
    {
        int visible = 0;
        for (int i = 0; i < src.Length && i <= srcIndex; )
        {
            if (src[i] == '<')
            {
                int close = src.IndexOf('>', i + 1);
                if (close < 0) break;
                i = close + 1;
                continue;
            }
            if (i == srcIndex) return visible;
            visible++;
            i++;
        }
        return -1;
    }

    void CacheFrameGeometry()
    {
        if (_tmp == null) return;

        _ti = _tmp.textInfo;
        if (_ti == null || _ti.meshInfo == null) return;

        var mi = _ti.meshInfo;
        if (mi.Length == 0) return;

        if (_originalVertsPerMesh == null || _originalVertsPerMesh.Length != mi.Length)
            _originalVertsPerMesh = new Vector3[mi.Length][];

        for (int i = 0; i < mi.Length; i++)
        {
            var src = mi[i].vertices;
            if (src == null) continue;

            if (_originalVertsPerMesh[i] == null || _originalVertsPerMesh[i].Length != src.Length)
                _originalVertsPerMesh[i] = new Vector3[src.Length];

            Array.Copy(src, _originalVertsPerMesh[i], src.Length);
        }
    }

    void LateUpdate()
    {
        if (_tmp == null) return;
        if (!Application.isPlaying && !runInEditMode) return;

        _tmp.ForceMeshUpdate();
        _ti = _tmp.textInfo;
        if (_ti.characterCount == 0) return;

        CacheFrameGeometry();
        float t = Now() - _startTime;

        for (int r = 0; r < _ranges.Count; r++)
        {
            var range = _ranges[r];
            if (!range.Valid(_ti.characterCount)) continue;
            switch (range.Type)
            {
                case EffectKind.Wave:           ApplyWave(range, t); break;
                case EffectKind.HorizontalWave: ApplyHorizontalWave(range, t); break;
                case EffectKind.Ripple:         ApplyRipple(range, t); break;
                case EffectKind.Shake:          ApplyShake(range, t); break;
                case EffectKind.Bounce:         ApplyBounce(range, t); break;
                case EffectKind.Typewriter:     ApplyTypewriter(range, t); break;
                case EffectKind.Rainbow:        ApplyRainbow(range, t); break;
                case EffectKind.ColorPulse:     ApplyColorPulse(range, t); break;
                case EffectKind.ScalePulse:     ApplyScalePulse(range, t); break;
                case EffectKind.Spin:           ApplySpin(range, t); break;
                case EffectKind.Swirl:          ApplySwirl(range, t); break;
                case EffectKind.Wobble:         ApplyWobble(range, t); break;
                case EffectKind.Skew:           ApplySkew(range, t); break;
                case EffectKind.Fade:           ApplyFade(range, t); break;
                case EffectKind.FlipX:          ApplyFlipX(range, t); break;
                case EffectKind.StretchY:       ApplyStretchY(range, t); break;
                case EffectKind.Glitch:         ApplyGlitch(range, t); break;
            }
        }

        for (int m = 0; m < _ti.meshInfo.Length; m++)
        {
            var mi = _ti.meshInfo[m];
            mi.mesh.vertices = mi.vertices;
            mi.mesh.colors32 = mi.colors32;
            _tmp.UpdateGeometry(mi.mesh, m);
        }
    }

    #region Effects
    // (Effects unchanged from your version; omitted here for brevity in this comment.)
    // --- Paste your existing Apply* methods here ---

    void ApplyWave(EffectRange e, float t)
    {
        float dur = e.Get("duration", 0f);
        float localT = dur > 0 ? Mathf.Repeat(t, dur) : t;
        float amp = e.Get("amp", 6f);
        float freq = e.Get("freq", 2f);
        float phase = e.Get("phase", 0f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float offY = Mathf.Sin((localT * freq + (i - e.start) * (phase + 0.15f)) * Mathf.PI * 2f) * amp;
            Vector3 offset = new Vector3(0, offY, 0);
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + offset;
        }
    }

    void ApplyHorizontalWave(EffectRange e, float t)
    {
        float amp = e.Get("amp", 6f);
        float freq = e.Get("freq", 2f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float offX = Mathf.Sin((t + (i - e.start) * 0.12f) * freq * Mathf.PI * 2f) * amp;
            Vector3 offset = new Vector3(offX, 0, 0);
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + offset;
        }
    }

    void ApplyRipple(EffectRange e, float t)
    {
        float amp = e.Get("amp", 6f);
        float speed = e.Get("speed", 2f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float r = (i - e.start);
            float off = Mathf.Sin((t * speed - r * 0.25f) * Mathf.PI * 2f) * amp;
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + new Vector3(0, off, 0);
        }
    }

    void ApplyShake(EffectRange e, float t)
    {
        float amp = e.Get("amp", 2f);
        float speed = e.Get("speed", 12f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float s = (i * 13.37f) + t * speed;
            float dx = (Mathf.PerlinNoise(s, 0.123f) - 0.5f) * 2f * amp;
            float dy = (Mathf.PerlinNoise(0.456f, s) - 0.5f) * 2f * amp;
            Vector3 offset = new Vector3(dx, dy, 0);
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + offset;
        }
    }

    void ApplyBounce(EffectRange e, float t)
    {
        float amp = e.Get("amp", 6f);
        float freq = e.Get("freq", 2f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float phase = (i - e.start) * 0.1f;
            float y = Mathf.Abs(Mathf.Sin((t + phase) * freq * Mathf.PI * 2f)) * amp;
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + new Vector3(0, y, 0);
        }
    }

    void ApplyTypewriter(EffectRange e, float t)
    {
        float cps = Mathf.Max(1f, e.Get("speed", 24f));
        int len = Mathf.Max(0, e.end - e.start + 1);
        if (len == 0 || cps <= 0f || _ti == null || _ti.characterCount == 0) return;

        int loopCount = Mathf.Max(0, Mathf.FloorToInt(e.Get("loop", 1f))); // 0 = endless
        float cycleTime = len / cps;

        float phaseT;
        if (loopCount == 0)                       // endless loop
            phaseT = Mathf.Repeat(t, cycleTime);
        else
        {
            float total = loopCount * cycleTime;
            phaseT = (t >= total) ? cycleTime     // final pass: fully shown and stop
                                  : Mathf.Repeat(t, cycleTime);
        }

        // ---- CHANGE: use Ceil on normalized progress so last char appears during each cycle ----
        float progress = (cycleTime > 0f) ? Mathf.Clamp01(phaseT / cycleTime) : 1f; // 0..1
        int shown = Mathf.Clamp(Mathf.CeilToInt(progress * len), 0, len);
        int cutoff = e.start + shown - 1;

        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue;
            var ch = _ti.characterInfo[i];
            if (!ch.isVisible) continue;

            int m = ch.materialReferenceIndex;
            int v = ch.vertexIndex;
            var cols = _ti.meshInfo[m].colors32;

            byte a = (byte)(i <= cutoff ? 255 : 0);
            for (int k = 0; k < 4; k++)
            {
                var c = cols[v + k];
                c.a = a;
                cols[v + k] = c;
            }
        }
    }


    void ApplyRainbow(EffectRange e, float t)
    {
        float speed = e.Get("speed", 0.2f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float h = Mathf.Repeat((i - e.start) * 0.08f + t * speed, 1f);
            Color col = Color.HSVToRGB(h, 1f, 1f);
            var cols = _ti.meshInfo[m].colors32; Color32 col32 = col;
            for (int k = 0; k < 4; k++) cols[v + k] = col32;
        }
    }

    void ApplyColorPulse(EffectRange e, float t)
    {
        float speed = e.Get("speed", 1f);
        float w = (Mathf.Sin(t * speed * Mathf.PI * 2f) * 0.5f + 0.5f) * e.Get("fade", 1f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            Color col = Color.Lerp(e.colorA, e.colorB, w);
            var cols = _ti.meshInfo[m].colors32; Color32 col32 = col;
            for (int k = 0; k < 4; k++) cols[v + k] = col32;
        }
    }

    void ApplyScalePulse(EffectRange e, float t)
    {
        float amp = Mathf.Max(0f, e.Get("amp", 0.2f));
        float freq = e.Get("freq", 2f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            Vector3 center = (_originalVertsPerMesh[m][v] + _originalVertsPerMesh[m][v + 2]) * 0.5f;
            float s = 1f + Mathf.Sin((t + (i - e.start) * 0.05f) * freq * Mathf.PI * 2f) * amp;
            for (int k = 0; k < 4; k++)
            {
                var orig = _originalVertsPerMesh[m][v + k];
                _ti.meshInfo[m].vertices[v + k] = center + (orig - center) * s;
            }
        }
    }

    void ApplySpin(EffectRange e, float t)
    {
        float speed = e.Get("speed", 1f); // rev/s
        float degPerSec = speed * 360f;
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            Vector3 center = (_originalVertsPerMesh[m][v] + _originalVertsPerMesh[m][v + 2]) * 0.5f;
            float ang = (t + (i - e.start) * 0.03f) * degPerSec * Mathf.Deg2Rad;
            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
            for (int k = 0; k < 4; k++)
            {
                var p = _originalVertsPerMesh[m][v + k] - center;
                var r = new Vector3(p.x * cos - p.y * sin, p.x * sin + p.y * cos, 0);
                _ti.meshInfo[m].vertices[v + k] = center + r;
            }
        }
    }

    void ApplySwirl(EffectRange e, float t)
    {
        float baseSpeed = e.Get("speed", 0.8f) * 360f;
        float amp = e.Get("amp", 0.3f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            Vector3 center = (_originalVertsPerMesh[m][v] + _originalVertsPerMesh[m][v + 2]) * 0.5f;
            float idx = (i - e.start);
            float ang = (baseSpeed * (0.5f + Mathf.Sin(idx * 0.2f) * 0.5f)) * t * Mathf.Deg2Rad;
            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
            for (int k = 0; k < 4; k++)
            {
                var p = _originalVertsPerMesh[m][v + k] - center;
                var r = new Vector3(p.x * cos - p.y * sin, p.x * sin + p.y * cos, 0);
                _ti.meshInfo[m].vertices[v + k] = center + r * (1f + Mathf.Sin(t + idx * 0.1f) * amp);
            }
        }
    }

    void ApplyWobble(EffectRange e, float t)
    {
        float amp = e.Get("amp", 2f);
        float speed = e.Get("speed", 3f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float s = (i - e.start) * 0.25f + t * speed;
            float dx = Mathf.Sin(s) * amp;
            float dy = Mathf.Cos(s * 1.3f) * amp * 0.5f;
            Vector3 off = new Vector3(dx, dy, 0);
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + off;
        }
    }

    void ApplySkew(EffectRange e, float t)
    {
        float angle = e.Get("angle", 15f) * Mathf.Deg2Rad;
        float speed = e.Get("speed", 1f);
        float s = Mathf.Sin(t * speed) * angle;
        float tan = Mathf.Tan(s);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            for (int k = 0; k < 4; k++)
            {
                var orig = _originalVertsPerMesh[m][v + k];
                float newX = orig.x + orig.y * tan;
                _ti.meshInfo[m].vertices[v + k] = new Vector3(newX, orig.y, orig.z);
            }
        }
    }

    void ApplyFade(EffectRange e, float t)
    {
        float speed = e.Get("speed", 1f);
        float fade = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(t * speed)); // 0..1
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            var cols = _ti.meshInfo[m].colors32;
            byte a = (byte)Mathf.RoundToInt(fade * 255f);
            for (int k = 0; k < 4; k++) { var c = cols[v + k]; c.a = a; cols[v + k] = c; }
        }
    }

    void ApplyFlipX(EffectRange e, float t)
    {
        float speed = e.Get("speed", 1f);
        float deg = e.Get("angle", 180f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            Vector3 center = (_originalVertsPerMesh[m][v] + _originalVertsPerMesh[m][v + 2]) * 0.5f;
            float ang = Mathf.PingPong(t * speed * deg, deg) * Mathf.Deg2Rad; // 0..deg
            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
            for (int k = 0; k < 4; k++)
            {
                var p = _originalVertsPerMesh[m][v + k] - center;
                var r = new Vector3(p.x, p.y * cos, p.y * sin);
                _ti.meshInfo[m].vertices[v + k] = center + r;
            }
        }
    }

    void ApplyStretchY(EffectRange e, float t)
    {
        float amp = e.Get("amp", 0.3f);
        float freq = e.Get("freq", 2f);
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            Vector3 center = (_originalVertsPerMesh[m][v] + _originalVertsPerMesh[m][v + 2]) * 0.5f;
            float s = 1f + Mathf.Sin((t + (i - e.start) * 0.1f) * freq * Mathf.PI * 2f) * amp;
            for (int k = 0; k < 4; k++)
            {
                var o = _originalVertsPerMesh[m][v + k];
                var d = o - center;
                _ti.meshInfo[m].vertices[v + k] = center + new Vector3(d.x, d.y * s, d.z);
            }
        }
    }

    void ApplyGlitch(EffectRange e, float t)
    {
        float amp = e.Get("amp", 2f);
        float speed = e.Get("speed", 20f);
        float cutProb = 0.1f;
        UnityEngine.Random.InitState((int)(t * 1000f));
        for (int i = e.start; i <= e.end; i++)
        {
            if (i < 0 || i >= _ti.characterCount) continue; var ch = _ti.characterInfo[i]; if (!ch.isVisible) continue;
            int m = ch.materialReferenceIndex; int v = ch.vertexIndex;
            float dx = (Mathf.PerlinNoise(i * 1.234f, t * speed) - 0.5f) * 2f * amp;
            float dy = (Mathf.PerlinNoise(t * speed, i * 2.345f) - 0.5f) * 2f * amp;
            Vector3 off = new Vector3(dx, dy, 0);
            for (int k = 0; k < 4; k++) _ti.meshInfo[m].vertices[v + k] = _originalVertsPerMesh[m][v + k] + off;
            if (UnityEngine.Random.value < cutProb * Time.deltaTime * Mathf.Max(1f, speed))
            {
                var cols = _ti.meshInfo[m].colors32; for (int k = 0; k < 4; k++) { var c = cols[v + k]; c.a = 0; cols[v + k] = c; }
            }
        }
    }
    #endregion

    #region Parser (ManualTags mode)
    class EffectRange
    {
        public EffectKind Type;
        public int start;
        public int end;
        public Dictionary<string, float> fParams = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> sParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float Get(string key, float def) => fParams.TryGetValue(key, out var v) ? v : def;
        public bool Valid(int charCount) => start >= 0 && end >= start && start < charCount;
    }

    static readonly Dictionary<string, EffectKind> _nameToType = new Dictionary<string, EffectKind>(StringComparer.OrdinalIgnoreCase)
    {
        {"wave", EffectKind.Wave},
        {"hwave", EffectKind.HorizontalWave},
        {"ripple", EffectKind.Ripple},
        {"shake", EffectKind.Shake},
        {"bounce", EffectKind.Bounce},
        {"type", EffectKind.Typewriter},
        {"typewriter", EffectKind.Typewriter},
        {"rainbow", EffectKind.Rainbow},
        {"colorpulse", EffectKind.ColorPulse},
        {"scale", EffectKind.ScalePulse},
        {"scalepulse", EffectKind.ScalePulse},
        {"spin", EffectKind.Spin},
        {"swirl", EffectKind.Swirl},
        {"wobble", EffectKind.Wobble},
        {"skew", EffectKind.Skew},
        {"fade", EffectKind.Fade},
        {"flipx", EffectKind.FlipX},
        {"stretchy", EffectKind.StretchY},
        {"glitch", EffectKind.Glitch},
    };

    string Parse(string src, out List<(int srcIndex, int visibleIndex)> map, List<EffectRange> dst)
    {
        map = new List<(int, int)>(src.Length);
        var display = new StringBuilder(src.Length);
        var stack = new Stack<(EffectKind type, Dictionary<string, string> attrs, int startVisible)>();
        int visible = 0;
        for (int i = 0; i < src.Length; )
        {
            char c = src[i];
            if (c == '<')
            {
                int close = src.IndexOf('>', i + 1);
                if (close == -1) { display.Append(c); i++; visible++; continue; }
                string tagContent = src.Substring(i + 1, close - i - 1);
                bool closing = tagContent.StartsWith("/");
                if (closing) tagContent = tagContent.Substring(1);

                if (TryParseTag(tagContent, out string name, out var attrs) && _nameToType.TryGetValue(name, out var et))
                {
                    if (!closing)
                    {
                        stack.Push((et, attrs, visible));
                    }
                    else if (stack.Count > 0)
                    {
                        var (tEt, tAttrs, startVis) = stack.Pop();
                        if (tEt == et)
                        {
                            var er = new EffectRange { Type = tEt, start = startVis, end = Mathf.Max(startVis, visible - 1) };
                            foreach (var kv in tAttrs)
                            {
                                if (kv.Key.Equals("colorA", StringComparison.OrdinalIgnoreCase) || kv.Key.Equals("colorB", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (ColorUtility.TryParseHtmlString(kv.Value, out var cc))
                                    {
                                        if (kv.Key.Equals("colorA", StringComparison.OrdinalIgnoreCase)) er.colorA = cc; else er.colorB = cc;
                                        continue;
                                    }
                                }
                                if (float.TryParse(kv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) er.fParams[kv.Key] = f;
                                else er.sParams[kv.Key] = kv.Value;
                            }
                            dst.Add(er);
                        }
                    }
                    i = close + 1;
                    continue;
                }
                else
                {
                    // Keep unknown TMP rich-text tag EXACT as in source (preserve '/', attributes, etc.)
                    display.Append(src, i, (close - i) + 1);
                    i = close + 1; continue;
                }
            }
            display.Append(c);
            map.Add((i, visible));
            i++; visible++;
        }
        while (stack.Count > 0)
        {
            var (tEt, tAttrs, startVis) = stack.Pop();
            var er = new EffectRange { Type = tEt, start = startVis, end = Mathf.Max(startVis, visible - 1) };
            foreach (var kv in tAttrs)
            {
                if (float.TryParse(kv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) er.fParams[kv.Key] = f; else er.sParams[kv.Key] = kv.Value;
            }
            dst.Add(er);
        }
        return display.ToString();
    }

    static bool TryParseTag(string content, out string name, out Dictionary<string, string> attrs)
    {
        attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        name = null; if (string.IsNullOrWhiteSpace(content)) return false;
        int sp = content.IndexOf(' ');
        if (sp < 0) { name = content.Trim(); return true; }
        name = content.Substring(0, sp).Trim();
        string rest = content.Substring(sp + 1);
        int i = 0;
        while (i < rest.Length)
        {
            while (i < rest.Length && char.IsWhiteSpace(rest[i])) i++;
            int eq = rest.IndexOf('=', i); if (eq < 0) break;
            string key = rest.Substring(i, eq - i).Trim(); i = eq + 1;
            if (i >= rest.Length) { attrs[key] = ""; break; }
            char ch = rest[i]; string val;
            if (ch == '\"')
            {
                int endq = rest.IndexOf('\"', i + 1);
                if (endq < 0) { val = rest.Substring(i + 1); i = rest.Length; }
                else { val = rest.Substring(i + 1, endq - i - 1); i = endq + 1; }
            }
            else
            {
                int space = i; while (space < rest.Length && !char.IsWhiteSpace(rest[space])) space++;
                val = rest.Substring(i, space - i); i = space;
            }
            attrs[key] = val;
        }
        return true;
    }
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(TMPAnimatedTextAdvanced))]
public class TMPAnimatedTextAdvancedEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var t = (TMPAnimatedTextAdvanced)target;
        serializedObject.Update();

        EditorGUILayout.LabelField("Source Text", EditorStyles.boldLabel);
        t.SourceText = EditorGUILayout.TextArea(t.SourceText, GUILayout.MinHeight(60));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
        t.targetMode = (TMPAnimatedTextAdvanced.TargetMode)EditorGUILayout.EnumPopup("Target Mode", t.targetMode);
        if (t.targetMode == TMPAnimatedTextAdvanced.TargetMode.AutoMatch)
        {
            t.matchText = EditorGUILayout.TextField("Match Text", t.matchText);
            EditorGUILayout.BeginHorizontal();
            t.matchCase = EditorGUILayout.ToggleLeft("Match Case", t.matchCase);
            t.wholeWord = EditorGUILayout.ToggleLeft("Whole Word", t.wholeWord);
            EditorGUILayout.EndHorizontal();
            t.applyToAllMatches = EditorGUILayout.Toggle("Apply To All", t.applyToAllMatches);
            if (!t.applyToAllMatches)
                t.matchIndex = Mathf.Max(0, EditorGUILayout.IntField("Match Index", t.matchIndex));
        }
        else
        {
            EditorGUILayout.HelpBox("ManualTags: Use custom tags like <wave amp=6>hello</wave> or <colorpulse colorA=#ffffff colorB=#ff3333 speed=2>color!</colorpulse>", MessageType.Info);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
        t.playOnEnable = EditorGUILayout.Toggle("Play On Enable", t.playOnEnable);
        t.useUnscaledTime = EditorGUILayout.Toggle("Use Unscaled Time", t.useUnscaledTime);
        t.runInEditMode = EditorGUILayout.Toggle("Preview In Editor", t.runInEditMode);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
        t.selectedEffect = (TMPAnimatedTextAdvanced.EffectKind)EditorGUILayout.EnumPopup("Kind", t.selectedEffect);
        DrawEffectControls(t);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Apply / Rebuild"))
        {
            t.RebuildFromSource();
            EditorUtility.SetDirty(t);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawEffectControls(TMPAnimatedTextAdvanced t)
    {
        var e = t.effect; var mini = EditorStyles.miniLabel;
        switch (t.selectedEffect)
        {
            case TMPAnimatedTextAdvanced.EffectKind.Wave:
                e.amp = EditorGUILayout.Slider("Amplitude", e.amp, 0f, 30f);
                e.freq = EditorGUILayout.Slider("Frequency", e.freq, 0f, 10f);
                e.phase = EditorGUILayout.Slider("Phase", e.phase, -2f, 2f);
                e.duration = EditorGUILayout.Slider("Loop Duration", e.duration, 0f, 20f);
                EditorGUILayout.LabelField("Vertical sine per char.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.HorizontalWave:
                e.amp = EditorGUILayout.Slider("Amplitude", e.amp, 0f, 30f);
                e.freq = EditorGUILayout.Slider("Frequency", e.freq, 0f, 10f);
                EditorGUILayout.LabelField("Horizontal sine per char.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Ripple:
                e.amp = EditorGUILayout.Slider("Height", e.amp, 0f, 30f);
                e.speed = EditorGUILayout.Slider("Wave Speed", e.speed, 0f, 10f);
                EditorGUILayout.LabelField("Ripple across characters.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Shake:
                e.amp = EditorGUILayout.Slider("Amplitude", e.amp, 0f, 20f);
                e.speed = EditorGUILayout.Slider("Speed", e.speed, 0f, 40f);
                EditorGUILayout.LabelField("Perlin jitter in X/Y.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Bounce:
                e.amp = EditorGUILayout.Slider("Height", e.amp, 0f, 30f);
                e.freq = EditorGUILayout.Slider("Frequency", e.freq, 0f, 10f);
                EditorGUILayout.LabelField("Up/down bounce with stagger.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Typewriter:
                e.speed = EditorGUILayout.Slider("Chars / Second", e.speed, 1f, 60f);
                EditorGUILayout.LabelField("Reveals characters over time.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Rainbow:
                e.speed = EditorGUILayout.Slider("Hue Speed", e.speed, 0f, 5f);
                EditorGUILayout.LabelField("Cycles vertex colors in HSV.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.ColorPulse:
                e.speed = EditorGUILayout.Slider("Pulse Speed", e.speed, 0f, 10f);
                e.fade = EditorGUILayout.Slider("Mix Amount", e.fade, 0f, 1f);
                e.colorA = EditorGUILayout.ColorField("Color A", e.colorA);
                e.colorB = EditorGUILayout.ColorField("Color B", e.colorB);
                EditorGUILayout.LabelField("Lerps vertex colors A↔B.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.ScalePulse:
                e.amp = EditorGUILayout.Slider("Scale Amplitude", e.amp, 0f, 1f);
                e.freq = EditorGUILayout.Slider("Frequency", e.freq, 0f, 10f);
                EditorGUILayout.LabelField("Scales around glyph center.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Spin:
                e.speed = EditorGUILayout.Slider("Revolutions / Second", e.speed, 0f, 5f);
                EditorGUILayout.LabelField("Rotates each glyph.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Swirl:
                e.speed = EditorGUILayout.Slider("Angular Speed", e.speed, 0f, 5f);
                e.amp = EditorGUILayout.Slider("Radial Pulse", e.amp, 0f, 1f);
                EditorGUILayout.LabelField("Spin + radial breathing.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Wobble:
                e.amp = EditorGUILayout.Slider("Amplitude", e.amp, 0f, 20f);
                e.speed = EditorGUILayout.Slider("Speed", e.speed, 0f, 10f);
                EditorGUILayout.LabelField("Organic wobble in X/Y.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Skew:
                e.angle = EditorGUILayout.Slider("Max Skew (deg)", e.angle, -45f, 45f);
                e.speed = EditorGUILayout.Slider("Speed", e.speed, 0f, 10f);
                EditorGUILayout.LabelField("Shears glyphs horizontally.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Fade:
                e.speed = EditorGUILayout.Slider("Pulse Speed", e.speed, 0f, 10f);
                EditorGUILayout.LabelField("Alpha pulsing 0..1.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.FlipX:
                e.speed = EditorGUILayout.Slider("Flip Speed", e.speed, 0f, 5f);
                e.angle = EditorGUILayout.Slider("Max Angle", e.angle, 0f, 360f);
                EditorGUILayout.LabelField("Rotates around X axis (simulated).", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.StretchY:
                e.amp = EditorGUILayout.Slider("Stretch Amount", e.amp, 0f, 1f);
                e.freq = EditorGUILayout.Slider("Frequency", e.freq, 0f, 10f);
                EditorGUILayout.LabelField("Vertical stretch pulse.", mini);
                break;
            case TMPAnimatedTextAdvanced.EffectKind.Glitch:
                e.amp = EditorGUILayout.Slider("Jitter", e.amp, 0f, 20f);
                e.speed = EditorGUILayout.Slider("Chaos", e.speed, 0f, 40f);
                EditorGUILayout.LabelField("Jitter + occasional dropout.", mini);
                break;
        }
    }
}
#endif
