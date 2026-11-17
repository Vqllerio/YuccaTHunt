#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menus:
//   Tools > Yucca > Add Background Music
//   Tools > Yucca > Capture Yucca Portrait (and a turned-around variant)
public static class YuccaAssetTools
{
    const string SpriteDir = "Assets/UI/Generated";
    const string PortraitPath = SpriteDir + "/yucca_portrait.png";

    // ---------- background music ----------

    [MenuItem("Tools/Yucca/Set Up Audio (music + sound effects)")]
    public static void SetUpAudio()
    {
        var pad = FindClip("music_pad");
        var bass = FindClip("music_bass");
        var arp = FindClip("music_arp");
        var drums = FindClip("music_drums");
        var glitch = FindClip("music_glitch");
        var ending = FindClip("music_ending");
        var click = FindClip("sfx_click");
        var open = FindClip("sfx_open");
        var begin = FindClip("sfx_begin");
        var victory = FindClip("sfx_victory");
        var scan = FindClip("sfx_scan");

        if (pad == null)
        {
            Debug.LogError("[YuccaAssetTools] music_pad.ogg not found. Copy the Assets/Audio folder from the zip into your project first.");
            return;
        }

        // Stems must be fully loaded so they stay perfectly in sync.
        foreach (var c in new[] { pad, bass, arp, drums, glitch })
            Import(c, AudioClipLoadType.CompressedInMemory, 0.6f);
        Import(ending, AudioClipLoadType.Streaming, 0.6f);
        Import(victory, AudioClipLoadType.CompressedInMemory, 0.7f);
        foreach (var c in new[] { click, open, begin, scan })
            Import(c, AudioClipLoadType.DecompressOnLoad, 0.7f);

        var mmType = System.Type.GetType("MusicManager, Assembly-CSharp");
        var sfxType = System.Type.GetType("SfxPlayer, Assembly-CSharp");
        if (mmType == null || sfxType == null)
        {
            Debug.LogError("[YuccaAssetTools] MusicManager.cs / SfxPlayer.cs not found in Assembly-CSharp. " +
                           "Copy Assets/Scripts from the zip, let Unity compile, then run this again.");
            return;
        }

        GameObject settingsPanel = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == "SettingsPanel" && rt.gameObject.scene.IsValid() && rt.gameObject.scene.isLoaded)
            {
                settingsPanel = rt.gameObject;
                break;
            }
        }

        // Music
        var mm = Object.FindAnyObjectByType(mmType, FindObjectsInactive.Include) as Component;
        if (mm == null)
        {
            var go = new GameObject("MusicManager");
            Undo.RegisterCreatedObjectUndo(go, "Create MusicManager");
            mm = Undo.AddComponent(go, mmType);
        }
        var so = new SerializedObject(mm);
        SetRef(so, "pad", pad);
        SetRef(so, "bass", bass);
        SetRef(so, "arp", arp);
        SetRef(so, "drums", drums);
        SetRef(so, "glitch", glitch);
        SetRef(so, "ending", ending);
        SetRef(so, "clip", null);
        if (settingsPanel != null) SetRef(so, "settingsPanel", settingsPanel);
        so.ApplyModifiedProperties();

        // Old single-track setups left a looping AudioSource behind. Harmless, but remove it.
        foreach (var oldSrc in mm.gameObject.GetComponents<AudioSource>())
            Undo.DestroyObjectImmediate(oldSrc);

        // Sound effects
        var sfx = Object.FindAnyObjectByType(sfxType, FindObjectsInactive.Include) as Component;
        if (sfx == null)
        {
            var go = new GameObject("SfxPlayer");
            Undo.RegisterCreatedObjectUndo(go, "Create SfxPlayer");
            go.AddComponent<AudioSource>();
            sfx = Undo.AddComponent(go, sfxType);
        }
        var so2 = new SerializedObject(sfx);
        SetRef(so2, "clickClip", click);
        SetRef(so2, "openClip", open);
        SetRef(so2, "beginClip", begin);
        SetRef(so2, "victoryClip", victory);
        so2.ApplyModifiedProperties();

        // Scan chime: fill in ScanAudio only if its clip is empty
        var scanType = System.Type.GetType("ScanAudio, Assembly-CSharp");
        if (scanType != null && scan != null)
        {
            foreach (var sa in Object.FindObjectsByType(scanType, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so3 = new SerializedObject(sa);
                var pr = so3.FindProperty("successClip");
                if (pr != null && pr.objectReferenceValue == null)
                {
                    pr.objectReferenceValue = scan;
                    so3.ApplyModifiedProperties();
                }
            }
        }

        // Dim the screen behind the settings menu
        if (settingsPanel != null)
        {
            var dimType = System.Type.GetType("PanelDimmer, Assembly-CSharp");
            if (dimType != null && settingsPanel.GetComponent(dimType) == null)
                Undo.AddComponent(settingsPanel, dimType);
        }

        EditorSceneManager.MarkSceneDirty(mm.gameObject.scene);
        Debug.Log("[YuccaAssetTools] Audio ready: layered music, ending theme, victory fanfare, UI sounds" +
                  (settingsPanel == null ? " (no SettingsPanel found: no ducking or dimming)." : "."));
    }

    // Keeps the old menu name working.
    [MenuItem("Tools/Yucca/Add Background Music")]
    public static void AddMusic() { SetUpAudio(); }

    static AudioClip FindClip(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    static void Import(AudioClip clip, AudioClipLoadType load, float quality)
    {
        if (clip == null) return;
        string path = AssetDatabase.GetAssetPath(clip);
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;
        var settings = importer.defaultSampleSettings;
        settings.loadType = load;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = quality;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = false;
        importer.SaveAndReimport();
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
    }

    // ---------- cleanup ----------

    [MenuItem("Tools/Yucca/Remove Missing Scripts In Scene")]
    public static void RemoveMissingScripts()
    {
        if (EditorUtility.scriptCompilationFailed)
        {
            Debug.LogError("[YuccaAssetTools] Fix the compile errors first. While scripts fail to " +
                           "compile, working components can look 'missing' and would be removed.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Remove missing scripts",
                "This removes components whose script no longer exists, from every object in the open scene. " +
                "Save or check in the scene first. Continue?", "Remove", "Cancel"))
            return;

        int removed = 0;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (EditorUtility.IsPersistent(go)) continue;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        if (removed > 0) EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[YuccaAssetTools] Removed " + removed + " missing script component(s).");
    }

    // ---------- Yucca portrait ----------

    [MenuItem("Tools/Yucca/Capture Yucca Portrait")]
    public static void CapturePortrait() { Capture(0f); }

    [MenuItem("Tools/Yucca/Capture Yucca Portrait (turned around)")]
    public static void CapturePortraitTurned() { Capture(180f); }

    [MenuItem("Tools/Yucca/Capture Yucca Portrait (turn 90)")]
    public static void CapturePortraitTurn90() { Capture(90f); }

    [MenuItem("Tools/Yucca/Capture Yucca Portrait (turn -90)")]
    public static void CapturePortraitTurnMinus90() { Capture(-90f); }

    static void Capture(float yaw)
    {
        GameObject source = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "YuccaMascot" && go.scene.IsValid() && go.scene.isLoaded)
            {
                source = go;
                break;
            }
        }
        if (source == null)
        {
            Debug.LogError("[YuccaAssetTools] No object named 'YuccaMascot' in the open scene.");
            return;
        }

        var copy = Object.Instantiate(source);
        copy.name = "YuccaPortraitTemp";
        copy.SetActive(true);
        copy.transform.SetParent(null);
        copy.transform.position = new Vector3(5000f, 5000f, 5000f);
        copy.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        copy.transform.localScale = source.transform.lossyScale;

        foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null) continue;
            if (ps.gameObject != copy) Object.DestroyImmediate(ps.gameObject);
            else Object.DestroyImmediate(ps);
        }
        foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) Object.DestroyImmediate(mb);

        var renderers = copy.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Object.DestroyImmediate(copy);
            Debug.LogError("[YuccaAssetTools] YuccaMascot has no renderers to capture.");
            return;
        }
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        var camGo = new GameObject("YuccaPortraitCam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.12f;
        cam.transform.position = bounds.center + new Vector3(0f, 0f, -20f);
        cam.transform.rotation = Quaternion.identity;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 60f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.allowHDR = false;

        var lightGo = new GameObject("YuccaPortraitLight");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(25f, 25f, 0f);

        const int size = 512;
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        Directory.CreateDirectory(SpriteDir);
        File.WriteAllBytes(PortraitPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(copy);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(lightGo);

        AssetDatabase.ImportAsset(PortraitPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(PortraitPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
        Selection.activeObject = sprite;
        EditorGUIUtility.PingObject(sprite);
        Debug.Log("[YuccaAssetTools] Portrait saved to " + PortraitPath +
                  ". If Yucca is facing away, run the 'turned around' version. " +
                  "Then run Tools > Yucca > Apply UI Style to use it on the completion screen.");
    }
}
#endif
