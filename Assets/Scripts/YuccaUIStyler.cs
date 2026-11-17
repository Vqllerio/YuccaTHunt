#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu: Tools > Yucca > Apply UI Style
// Generates the UI sprites (Assets/UI/Generated), restyles the Canvas objects
// by name, and attaches the UI helper scripts. Safe to run more than once.
// One Ctrl+Z undoes the scene changes of a run.
public static class YuccaUIStyler
{
    // ----- palette: change these and run again -----
    static readonly Color PanelColor = new Color32(18, 20, 46, 235);
    static readonly Color SummaryColor = new Color32(12, 14, 34, 248);
    static readonly Color Accent = new Color32(255, 166, 48, 255);
    static readonly Color OnAccent = new Color32(20, 22, 43, 255);
    static readonly Color SoftWhite = new Color32(240, 240, 248, 255);

    const string SpriteDir = "Assets/UI/Generated";

    class SpriteSet
    {
        public Sprite card, reticle, pip, scrim, gear, check;
    }

    static SpriteSet sp;

    [MenuItem("Tools/Yucca/Apply UI Style")]
    public static void Apply()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[YuccaUIStyler] No Canvas in the open scene.");
            return;
        }

        sp = BuildSprites();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Apply Yucca UI Style");
        int group = Undo.GetCurrentGroup();

        var root = EnsureChild(canvas.transform, "SafeAreaRoot");
        Stretch(root);
        AddByName(root.gameObject, "SafeAreaFitter");

        StyleBackdrop(canvas);
        StyleHud(canvas, root);
        StyleHint(canvas, root);
        StyleGear(canvas, root);
        StyleMemory(canvas, root);
        StyleIntro(canvas);
        StyleComplete(canvas);
        StyleSettings(canvas);

        Order(canvas.transform, "TopScrim", "BottomScrim", "ScanReticle", "SafeAreaRoot");

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[YuccaUIStyler] Done. Press Play to check, then save the scene.");
    }

    // ---------- sections ----------

    static void StyleBackdrop(Canvas c)
    {
        var top = EnsureChild(c.transform, "TopScrim");
        Place(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
              Vector2.zero, new Vector2(0, 460));
        SetImage(top, Color.black, sp.scrim, Image.Type.Simple);

        var bottom = EnsureChild(c.transform, "BottomScrim");
        Place(bottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f),
              new Vector2(0, 280), new Vector2(0, 560));
        SetImage(bottom, Color.black, sp.scrim, Image.Type.Simple);
        bottom.localScale = new Vector3(1, -1, 1);

        var reticle = EnsureChild(c.transform, "ScanReticle");
        Place(reticle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              new Vector2(0, 60), new Vector2(660, 660));
        var img = SetImage(reticle, Accent, sp.reticle, Image.Type.Simple);
        img.preserveAspect = true;
        AddByName(reticle.gameObject, "ScanReticle");
    }

    static void StyleHud(Canvas c, RectTransform root)
    {
        var progress = Find(c, "ProgressText");
        var checklist = Find(c, "ChecklistText");
        if (progress == null || checklist == null) { Warn("ProgressText / ChecklistText"); return; }

        var card = EnsureChild(root, "HudCard");
        Place(card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
              new Vector2(32, -32), new Vector2(560, 0));
        SetImage(card, PanelColor, sp.card, Image.Type.Sliced);
        Layout(card, new RectOffset(32, 32, 26, 26), 14, TextAnchor.UpperLeft);
        Fit(card);

        Reparent(progress, card);
        Reparent(checklist, card);

        var pips = EnsureChild(card, "ProgressPips");
        var row = GetOrAdd<HorizontalLayoutGroup>(pips);
        Undo.RecordObject(row, "Pips layout");
        row.spacing = 14;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        var le = GetOrAdd<LayoutElement>(pips);
        Undo.RecordObject(le, "Pips height");
        le.preferredHeight = 40;

        var comp = AddByName(pips.gameObject, "ProgressPips");
        SetObjectField(comp, "pipSprite", sp.pip);

        progress.SetSiblingIndex(0);
        pips.SetSiblingIndex(1);
        checklist.SetSiblingIndex(2);

        StyleText(progress, 42, Accent, TextAlignmentOptions.Left, FontStyles.Bold);
        StyleText(checklist, 34, SoftWhite, TextAlignmentOptions.TopLeft, FontStyles.Normal);
    }

    static void StyleHint(Canvas c, RectTransform root)
    {
        var hint = Find(c, "HintText");
        if (hint == null) { Warn("HintText"); return; }

        var bar = EnsureChild(root, "HintBar");
        Place(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
              new Vector2(0, 48), new Vector2(960, 130));
        SetImage(bar, PanelColor, sp.card, Image.Type.Sliced);

        Reparent(hint, bar);
        Stretch(hint);
        hint.offsetMin = new Vector2(34, 10);
        hint.offsetMax = new Vector2(-34, -10);
        StyleText(hint, 38, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
    }

    static void StyleGear(Canvas c, RectTransform root)
    {
        var gear = Find(c, "GearButton");
        if (gear == null) { Warn("GearButton"); return; }

        Reparent(gear, root);
        Place(gear, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
              new Vector2(-32, -32), new Vector2(112, 112));
        var img = gear.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, "Gear");
            img.sprite = sp.card;
            img.type = Image.Type.Sliced;
            img.color = PanelColor;
        }
        AddByName(gear.gameObject, "ButtonPressFx");

        var label = gear.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            Undo.RecordObject(label.gameObject, "Hide gear label");
            label.gameObject.SetActive(false);
        }

        var icon = EnsureChild(gear, "GearImage");
        Stretch(icon);
        icon.offsetMin = new Vector2(24, 24);
        icon.offsetMax = new Vector2(-24, -24);
        var iconImg = SetImage(icon, Accent, sp.gear, Image.Type.Simple);
        iconImg.preserveAspect = true;
    }

    static void StyleMemory(Canvas c, RectTransform root)
    {
        var panel = Find(c, "MemoryPanel");
        if (panel == null) { Warn("MemoryPanel"); return; }

        Reparent(panel, root);
        Place(panel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
              new Vector2(0, 200), new Vector2(960, 0));
        SetImage(panel, PanelColor, sp.card, Image.Type.Sliced);
        Layout(panel, new RectOffset(40, 40, 32, 32), 0, TextAnchor.MiddleCenter);
        Fit(panel);

        var pop = AddByName(panel.gameObject, "UiPopIn");
        SetFloatField(pop, "slideFromY", -50f);
        SetFloatField(pop, "soundVolume", 0f);

        var text = Find(c, "MemoryText");
        if (text != null)
            StyleText(text, 38, Color.white, TextAlignmentOptions.Center, FontStyles.Italic);
    }

    static void StyleIntro(Canvas c)
    {
        var panel = Find(c, "IntroPanel");
        if (panel == null) { Warn("IntroPanel"); return; }

        Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, new Vector2(900, 0));
        SetImage(panel, SummaryColor, sp.card, Image.Type.Sliced);
        Layout(panel, new RectOffset(60, 60, 60, 60), 44, TextAnchor.UpperCenter);
        Fit(panel);
        AddByName(panel.gameObject, "UiPopIn");

        var text = Find(c, "IntroText");
        if (text != null)
        {
            StyleText(text, 42, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
            AddByName(text.gameObject, "TypewriterText");
        }

        var begin = Find(c, "BeginButton");
        if (begin != null) StyleButton(begin, Accent, 140);
    }

    static void StyleComplete(Canvas c)
    {
        var comp = AddByName(c.gameObject, "QuestCompleteScreen");
        if (comp == null) return;

        var legacy = Find(c, "SummaryPanel");
        if (legacy != null) SetObjectField(comp, "legacySummaryPanel", legacy.gameObject);

        SetObjectField(comp, "cardSprite", sp.card);
        SetObjectField(comp, "pipSprite", sp.pip);
        SetObjectField(comp, "checkSprite", sp.check);

        var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/yucca_portrait.png");
        if (portrait != null) SetObjectField(comp, "portraitSprite", portrait);

        var hint = Find(c, "HintText");
        if (hint != null)
        {
            var tmp = hint.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                SetObjectField(comp, "hintText", tmp);
                if (tmp.font != null) SetObjectField(comp, "font", tmp.font);
            }
        }

        // Carry over the closing audio and switch the old text-only summary off.
        var oldType = System.Type.GetType("QuestSummaryUI, Assembly-CSharp");
        if (oldType != null)
        {
            var old = c.GetComponent(oldType) as Behaviour;
            if (old != null)
            {
                var so = new SerializedObject(old);
                var audio = so.FindProperty("closingAudio");
                if (audio != null && audio.objectReferenceValue != null)
                    SetObjectField(comp, "closingAudio", audio.objectReferenceValue);
                Undo.RecordObject(old, "Disable old summary");
                old.enabled = false;
            }
        }
    }

    static void StyleSettings(Canvas c)
    {
        var panel = Find(c, "SettingsPanel");
        if (panel == null) { Warn("SettingsPanel"); return; }

        Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, new Vector2(760, 0));
        SetImage(panel, PanelColor, sp.card, Image.Type.Sliced);
        Layout(panel, new RectOffset(52, 52, 52, 52), 30, TextAnchor.UpperCenter);
        Fit(panel);
        AddByName(panel.gameObject, "UiPopIn");
        AddByName(panel.gameObject, "PanelDimmer");

        var reset = Find(c, "ResetButton");
        var mute = Find(c, "MuteButton");
        if (reset != null) StyleButton(reset, Accent, 130);
        if (mute != null) StyleButton(mute, SoftWhite, 130);
    }

    // ---------- sprite generation ----------

    static SpriteSet BuildSprites()
    {
        Directory.CreateDirectory(SpriteDir);
        return new SpriteSet
        {
            card = MakeSprite("yucca_card", 128, 128, CardPixels(128, 40f, 3f, 0.88f),
                              new Vector4(44, 44, 44, 44)),
            reticle = MakeSprite("yucca_reticle", 256, 256, ReticlePixels(256, 12, 70, 10), Vector4.zero),
            pip = MakeSprite("yucca_pip", 64, 64, CirclePixels(64), Vector4.zero),
            scrim = MakeSprite("yucca_scrim", 4, 128, ScrimPixels(4, 128), Vector4.zero),
            gear = MakeSprite("yucca_gear", 128, 128, GearPixels(128), Vector4.zero),
            check = MakeSprite("yucca_check", 128, 128, CheckPixels(128), Vector4.zero)
        };
    }

    static Sprite MakeSprite(string name, int w, int h, Color[] pixels, Vector4 border)
    {
        string path = SpriteDir + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = border;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Color[] CardPixels(int s, float radius, float stroke, float fillAlpha)
    {
        var px = new Color[s * s];
        float half = s / 2f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float inside = Mathf.Clamp01(0.5f - d);
                float ring = Mathf.Clamp01(d + stroke + 0.5f);
                px[y * s + x] = new Color(1f, 1f, 1f, inside * Mathf.Lerp(fillAlpha, 1f, ring));
            }
        }
        return px;
    }

    static Color[] ReticlePixels(int s, int margin, int arm, int thick)
    {
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                bool nearL = x >= margin && x < margin + arm;
                bool nearR = x >= s - margin - arm && x < s - margin;
                bool nearB = y >= margin && y < margin + arm;
                bool nearT = y >= s - margin - arm && y < s - margin;
                bool bandL = x >= margin && x < margin + thick;
                bool bandR = x >= s - margin - thick && x < s - margin;
                bool bandB = y >= margin && y < margin + thick;
                bool bandT = y >= s - margin - thick && y < s - margin;

                bool on = ((nearL || nearR) && (bandB || bandT)) ||
                          ((nearB || nearT) && (bandL || bandR));
                px[y * s + x] = new Color(1f, 1f, 1f, on ? 1f : 0f);
            }
        }
        return px;
    }

    static Color[] CirclePixels(int s)
    {
        var px = new Color[s * s];
        float half = s / 2f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dist = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude - (half - 1f);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - dist));
            }
        }
        return px;
    }

    static Color[] GearPixels(int s)
    {
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x + 0.5f) / s * 2f - 1f;
                float ny = (y + 0.5f) / s * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float th = Mathf.Atan2(ny, nx);
                float tooth = 0.5f + 0.5f * (float)System.Math.Tanh(4.0 * Mathf.Sin(8f * th));
                float outer = Mathf.Lerp(0.70f, 0.94f, tooth);
                float aOuter = Mathf.Clamp01((outer - r) * s * 0.5f + 0.5f);
                float aHole = Mathf.Clamp01((r - 0.30f) * s * 0.5f + 0.5f);
                px[y * s + x] = new Color(1f, 1f, 1f, aOuter * aHole);
            }
        }
        return px;
    }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return (p - (a + ab * t)).magnitude;
    }

    static Color[] CheckPixels(int s)
    {
        var px = new Color[s * s];
        var a1 = new Vector2(-0.42f, 0.02f);
        var a2 = new Vector2(-0.12f, -0.28f);
        var a3 = new Vector2(0.46f, 0.34f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x + 0.5f) / s * 2f - 1f;
                float ny = (y + 0.5f) / s * 2f - 1f;
                var p = new Vector2(nx, ny);
                float disc = Mathf.Clamp01((0.94f - p.magnitude) * s * 0.5f + 0.5f);
                float d = Mathf.Min(SegDist(p, a1, a2), SegDist(p, a2, a3));
                float tick = Mathf.Clamp01((0.085f - d) * s * 0.5f + 0.5f);
                px[y * s + x] = new Color(1f, 1f, 1f, disc * (1f - tick));
            }
        }
        return px;
    }

    static Color[] ScrimPixels(int w, int h)
    {
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.Pow(y / (h - 1f), 1.7f) * 0.85f;
            for (int x = 0; x < w; x++) px[y * w + x] = new Color(0f, 0f, 0f, a);
        }
        return px;
    }

    // ---------- helpers ----------

    static void StyleButton(RectTransform rt, Color fill, float height)
    {
        var img = rt.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, "Button");
            img.sprite = sp.card;
            img.type = Image.Type.Sliced;
            img.color = fill;
        }

        var le = GetOrAdd<LayoutElement>(rt);
        Undo.RecordObject(le, "Button size");
        le.preferredHeight = height;
        le.minHeight = height;

        AddByName(rt.gameObject, "ButtonPressFx");

        var label = rt.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            Stretch((RectTransform)label.transform);
            StyleText((RectTransform)label.transform, 46, OnAccent,
                      TextAlignmentOptions.Center, FontStyles.Bold);
        }
    }

    static void StyleText(RectTransform rt, float size, Color color,
                          TextAlignmentOptions align, FontStyles style)
    {
        var t = rt.GetComponent<TextMeshProUGUI>();
        if (t == null) return;
        Undo.RecordObject(t, "Style text");
        t.fontSize = size;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        EditorUtility.SetDirty(t);
    }

    static Image SetImage(RectTransform rt, Color color, Sprite sprite, Image.Type type)
    {
        var img = GetOrAdd<Image>(rt);
        Undo.RecordObject(img, "Image style");
        img.sprite = sprite;
        img.type = type;
        img.color = color;
        img.raycastTarget = false;
        EditorUtility.SetDirty(img);
        return img;
    }

    static void Layout(RectTransform rt, RectOffset padding, float spacing, TextAnchor align)
    {
        var g = GetOrAdd<VerticalLayoutGroup>(rt);
        Undo.RecordObject(g, "Layout");
        g.padding = padding;
        g.spacing = spacing;
        g.childAlignment = align;
        g.childControlWidth = true;
        g.childControlHeight = true;
        g.childForceExpandWidth = true;
        g.childForceExpandHeight = false;
    }

    static void Fit(RectTransform rt)
    {
        var f = GetOrAdd<ContentSizeFitter>(rt);
        Undo.RecordObject(f, "Fit");
        f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot,
                      Vector2 pos, Vector2 size)
    {
        Undo.RecordObject(rt, "Place");
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rt)
    {
        Undo.RecordObject(rt, "Stretch");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void Reparent(RectTransform rt, RectTransform parent)
    {
        if (rt.parent == parent) return;
        Undo.SetTransformParent(rt, parent, "Reparent");
        rt.localScale = Vector3.one;
    }

    static void Order(Transform parent, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            foreach (Transform t in parent)
            {
                if (t.name != names[i]) continue;
                t.SetSiblingIndex(i);
                break;
            }
        }
    }

    static RectTransform Find(Canvas c, string name)
    {
        foreach (var t in c.GetComponentsInChildren<RectTransform>(true))
            if (t.name == name) return t;
        return null;
    }

    static RectTransform EnsureChild(Transform parent, string name)
    {
        foreach (Transform t in parent)
            if (t.name == name) return (RectTransform)t;

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static T GetOrAdd<T>(Component host) where T : Component
    {
        var c = host.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(host.gameObject);
    }

    static Component AddByName(GameObject go, string typeName)
    {
        var type = System.Type.GetType(typeName + ", Assembly-CSharp");
        if (type == null)
        {
            Debug.LogWarning("[YuccaUIStyler] Script '" + typeName +
                             "' not found in Assembly-CSharp. Add " + typeName +
                             ".cs to Assets/Scripts and run again.");
            return null;
        }
        var existing = go.GetComponent(type);
        return existing != null ? existing : Undo.AddComponent(go, type);
    }

    static void SetObjectField(Component comp, string field, Object value)
    {
        if (comp == null) return;
        var so = new SerializedObject(comp);
        var p = so.FindProperty(field);
        if (p == null) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetFloatField(Component comp, string field, float value)
    {
        if (comp == null) return;
        var so = new SerializedObject(comp);
        var p = so.FindProperty(field);
        if (p == null) return;
        p.floatValue = value;
        so.ApplyModifiedProperties();
    }

    static void Warn(string what)
    {
        Debug.LogWarning("[YuccaUIStyler] Skipped: could not find " + what + " under the Canvas.");
    }
}
#endif
