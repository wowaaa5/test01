#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.TextCore;
using System.Linq;
using UnityEngine.UI;
using System.Net;

public class EmojiAssetGenerator : EditorWindow
{
    private const string ManualFontAssetPathKey = "EmojiAssetGenerator.ManualFontAssetPath";
    private const string ManualTmpFontAssetPathKey = "EmojiAssetGenerator.ManualTmpFontAssetPath";
    private const string ManualOsFontNameKey = "EmojiAssetGenerator.ManualOsFontName";
    private const string UseCdnFallbackKey = "EmojiAssetGenerator.UseCdnFallback";

    private Font manualFontAsset;
    private TMP_FontAsset manualTmpFontAsset;
    private string manualOsFontName;
    private bool useCdnFallback = true;

    [MenuItem("Tools/Dialogue/Bake Emoji")]
    public static void OpenSettingsWindow()
    {
        var window = GetWindow<EmojiAssetGenerator>("Emoji Bake Settings");
        window.minSize = new Vector2(420f, 160f);
        window.Show();
    }

    public static void BakeAsset()
    {
        // 1. Find config
        string[] guids = AssetDatabase.FindAssets("t:EmojiMappingConfig");
        if (guids.Length == 0) return;
        string configPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        EmojiMappingConfig config = AssetDatabase.LoadAssetAtPath<EmojiMappingConfig>(configPath);

        string targetFolder = "Assets/Sprites/BakedEmojis";
        if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

        string atlasPath = $"{targetFolder}/EmojiAtlas.png";
        string spriteAssetPath = $"{targetFolder}/DialogueEmojis.asset";

        List<Texture2D> texturesToPack = new List<Texture2D>();
        List<TextureTargetData> targets = new List<TextureTargetData>();

        // Gather all main list configurations
        for (int i = 0; i < config.entries.Count; i++)
        {
            var entry = config.entries[i];
            Texture2D tex = ProcessEntryToTexture(entry);
            if (tex == null) continue;

            texturesToPack.Add(tex);
            targets.Add(new TextureTargetData
            {
                name = entry.keyword,
                unicodeStr = entry.emojiText,
                fallbackCodePoint = 0x1F600 + i
            });
        }

        // Gather the global fallback configuration
        Texture2D fallbackTex = ProcessEntryToTexture(config.fallbackEntry);
        if (fallbackTex != null)
        {
            texturesToPack.Add(fallbackTex);
            targets.Add(new TextureTargetData
            {
                name = "global_fallback_entry",
                unicodeStr = config.fallbackEntry.emojiText,
                fallbackCodePoint = 0x1F4AC
            });
        }

        if (texturesToPack.Count == 0) return;

        // 2. Pack everything cleanly into a fresh RGBA atlas canvas
        Texture2D atlas = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        // PackTextures handles multi-resolution packing smoothly and outputs true scaled Rects
        Rect[] uvRects = atlas.PackTextures(texturesToPack.ToArray(), 4, 1024);

        // Write texture cache directly out to clear project assets
        File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
        AssetDatabase.Refresh();

        // 3. Force clean import parameters for crisp UI scaling
        TextureImporter importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;

            var spriteMeta = new List<SpriteMetaData>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                Rect uvRect = uvRects[i];
                int posX = Mathf.RoundToInt(uvRect.x * atlas.width);
                int posY = Mathf.RoundToInt(uvRect.y * atlas.height);
                int sizeW = Mathf.RoundToInt(uvRect.width * atlas.width);
                int sizeH = Mathf.RoundToInt(uvRect.height * atlas.height);

                spriteMeta.Add(new SpriteMetaData
                {
                    name = string.IsNullOrEmpty(targets[i].name) ? $"emoji_{i}" : targets[i].name,
                    rect = new Rect(posX, posY, sizeW, sizeH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }

#pragma warning disable CS0618
            importer.spritesheet = spriteMeta.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        Texture2D savedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

        // 4. Create or fetch the static TMPro reference asset
        TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteAssetPath);
        if (spriteAsset == null)
        {
            spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(spriteAsset, spriteAssetPath);
        }

        spriteAsset.spriteSheet = savedAtlas;
        spriteAsset.spriteCharacterTable.Clear();
        spriteAsset.spriteGlyphTable.Clear();

        // 5. Generate and override TMPro's target material configuration
        string materialPath = $"{targetFolder}/EmojiMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("TextMeshPro/Sprite"));
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        mat.SetTexture(ShaderUtilities.ID_MainTex, savedAtlas);
        spriteAsset.material = mat;

        // 6. Map glyph entries back onto their dynamic packed dimensions
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            Rect uvRect = uvRects[i];

            // Convert normalization ratios directly back to pixel space mapping coordinates
            int posX = Mathf.RoundToInt(uvRect.x * savedAtlas.width);
            int posY = Mathf.RoundToInt(uvRect.y * savedAtlas.height);
            int sizeW = Mathf.RoundToInt(uvRect.width * savedAtlas.width);
            int sizeH = Mathf.RoundToInt(uvRect.height * savedAtlas.height);

            int unicodePoint = string.IsNullOrEmpty(t.unicodeStr) ? t.fallbackCodePoint : char.ConvertToUtf32(t.unicodeStr, 0);

            // SENIOR APPLICABILITY: Force strict glyph scaling metrics to ensure line-height alignments
            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
            {
                index = (uint)i,
                // Layout definition: width, height, horizontalBearingX, horizontalBearingY, horizontalAdvance
                metrics = new GlyphMetrics(sizeW, sizeH, 0, sizeH * .8f, sizeW * 1.15f),
                glyphRect = new GlyphRect(posX, posY, sizeW, sizeH),
                scale = 1.35f,
                atlasIndex = 0
            };
            spriteAsset.spriteGlyphTable.Add(glyph);

            TMP_SpriteCharacter character = new TMP_SpriteCharacter((uint)unicodePoint, glyph)
            {
                name = string.IsNullOrEmpty(t.name) ? "fallback" : t.name
            };
            spriteAsset.spriteCharacterTable.Add(character);
        }

        spriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(mat);

        // Clean up memory from temporary textures
        foreach (var tex in texturesToPack) DestroyImmediate(tex);
        DestroyImmediate(atlas);

        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked config successfully! {targets.Count - 1} entries + 1 fallback mapped to a clean, pixel-perfect layout schema.");
    }

    private void OnEnable()
    {
        string fontPath = EditorPrefs.GetString(ManualFontAssetPathKey, string.Empty);
        manualFontAsset = string.IsNullOrEmpty(fontPath) ? null : AssetDatabase.LoadAssetAtPath<Font>(fontPath);

        string tmpFontPath = EditorPrefs.GetString(ManualTmpFontAssetPathKey, string.Empty);
        manualTmpFontAsset = string.IsNullOrEmpty(tmpFontPath) ? null : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(tmpFontPath);

        manualOsFontName = EditorPrefs.GetString(ManualOsFontNameKey, string.Empty);
        useCdnFallback = EditorPrefs.GetBool(UseCdnFallbackKey, true);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Optional Manual Font Override", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Set a manual font to try first during emoji rasterization. If empty, automatic fallback fonts are used.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        manualTmpFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Manual TMP Font", manualTmpFontAsset, typeof(TMP_FontAsset), false);
        manualFontAsset = (Font)EditorGUILayout.ObjectField("Manual Font Asset", manualFontAsset, typeof(Font), false);
        manualOsFontName = EditorGUILayout.TextField("Manual OS Font Name", manualOsFontName ?? string.Empty);
        useCdnFallback = EditorGUILayout.Toggle("Use CDN Fallback", useCdnFallback);

        if (EditorGUI.EndChangeCheck())
        {
            SaveManualFontSettings(manualFontAsset, manualTmpFontAsset, manualOsFontName, useCdnFallback);
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Bake Emoji Sprite Asset"))
        {
            BakeAsset();
        }
    }

    private static Texture2D ProcessEntryToTexture(EmojiMappingConfig.EmojiEntry entry)
    {
        // Path A: Prioritize custom high-fidelity graphics sheets
        if (entry.emojiSprite != null)
        {
            Sprite sprite = entry.emojiSprite;
            Texture2D sourceTexture = sprite.texture;

            // Handle unreadable source textures cleanly
            string sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
            var texImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            bool wasReadable = texImporter != null && texImporter.isReadable;

            if (texImporter != null && !wasReadable)
            {
                texImporter.isReadable = true;
                texImporter.SaveAndReimport();
            }

            Texture2D croppedTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
            Color[] pixels = sourceTexture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
            croppedTex.SetPixels(pixels);
            croppedTex.Apply();

            // Revert asset state transparency settings
            if (texImporter != null && !wasReadable)
            {
                texImporter.isReadable = false;
                texImporter.SaveAndReimport();
            }

            return croppedTex;
        }

        // Path B: Fallback to string processing - rasterize unicode blocks cleanly using OS engines
        if (!string.IsNullOrEmpty(entry.emojiText))
        {
            return RasterizeUnicodeToTexture(entry.emojiText);
        }

        return null;
    }

    private static Texture2D RasterizeUnicodeToTexture(string rawText)
    {
        int size = 128;
        var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);

        var cameraGo = new GameObject("__EmojiBakeCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var canvasGo = new GameObject("__EmojiBakeCanvas")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var textGo = new GameObject("__EmojiBakeText")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var tmpTextGo = new GameObject("__EmojiBakeTMPText")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        try
        {
            int layer = 31;

            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.cullingMask = 1 << layer;
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 10f;
            cam.transform.position = new Vector3(0f, 0f, -1f);

            canvasGo.layer = layer;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.pixelPerfect = true;

            textGo.transform.SetParent(canvasGo.transform, false);
            textGo.layer = layer;
            tmpTextGo.transform.SetParent(canvasGo.transform, false);
            tmpTextGo.layer = layer;

            var text = textGo.AddComponent<Text>();
            text.text = rawText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontSize = 100;
            text.color = Color.white;
            text.supportRichText = false;
            text.font = null;
            text.enabled = false;

            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmpText = tmpTextGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = rawText;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.textWrappingMode = TextWrappingModes.NoWrap;
            tmpText.richText = false;
            tmpText.fontSize = 100;
            tmpText.color = Color.white;
            tmpText.enabled = false;

            var tmpRect = tmpText.rectTransform;
            tmpRect.anchorMin = Vector2.zero;
            tmpRect.anchorMax = Vector2.one;
            tmpRect.offsetMin = Vector2.zero;
            tmpRect.offsetMax = Vector2.zero;

            var candidateFonts = GetEmojiFallbackFontNames();
            bool rendered = false;

            var manualTmpFont = GetManualTmpFontAsset();
            if (manualTmpFont != null)
            {
                rendered = TryRenderWithTmpFont(manualTmpFont, tmpText, text, cam, rt, output, size);
            }

            var manualFont = GetManualFontAsset();
            if (manualFont != null && !rendered)
            {
                rendered = TryRenderWithFont(manualFont, text, cam, rt, output, size);
            }

            for (int i = 0; i < candidateFonts.Length && !rendered; i++)
            {
                var candidate = Font.CreateDynamicFontFromOSFont(candidateFonts[i], 96);
                if (candidate == null) continue;
                rendered = TryRenderWithFont(candidate, text, cam, rt, output, size);
            }

            if (!rendered && IsCdnFallbackEnabled())
            {
                if (TryLoadTwemojiTexture(rawText, size, out Texture2D twemoji))
                {
                    output.SetPixels32(twemoji.GetPixels32());
                    output.Apply();
                    DestroyImmediate(twemoji);
                    rendered = true;
                }
            }

            if (!rendered)
            {
                DrawFallbackTexture(output, rawText);
                Debug.LogWarning($"[EmojiAssetGenerator] Could not rasterize '{rawText}' from system fonts or Twemoji. Assign an explicit sprite for this entry.");
            }
        }
        finally
        {
            if (cameraGo != null) DestroyImmediate(cameraGo);
            if (canvasGo != null) DestroyImmediate(canvasGo);
            if (textGo != null) DestroyImmediate(textGo);
            if (tmpTextGo != null) DestroyImmediate(tmpTextGo);
            RenderTexture.ReleaseTemporary(rt);
        }

        return output;
    }

    private static string[] GetEmojiFallbackFontNames()
    {
        var names = new List<string>();
        string manualOsFont = EditorPrefs.GetString(ManualOsFontNameKey, string.Empty)?.Trim();
        if (!string.IsNullOrEmpty(manualOsFont)) names.Add(manualOsFont);

        names.AddRange(new[]
        {
            "Segoe UI Symbol",
            "Segoe UI Emoji",
            "Apple Color Emoji",
            "Noto Color Emoji",
            "Noto Emoji",
            "Arial Unicode MS",
            "Arial"
        });

        return names.Distinct().ToArray();
    }

    private static bool TryRenderWithFont(Font candidate, Text text, Camera cam, RenderTexture rt, Texture2D output, int size)
    {
        if (candidate == null) return false;

        text.enabled = true;
        text.font = candidate;
        candidate.RequestCharactersInTexture(text.text, text.fontSize, FontStyle.Normal);

        Canvas.ForceUpdateCanvases();
        cam.Render();

        var oldActive = RenderTexture.active;
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        output.Apply();
        RenderTexture.active = oldActive;

        if (HasVisiblePixels(output)) return true;

        ClearTexture(output);
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = oldActive;
        return false;
    }

    private static bool TryRenderWithTmpFont(TMP_FontAsset candidate, TextMeshProUGUI tmpText, Text legacyText, Camera cam, RenderTexture rt, Texture2D output, int size)
    {
        if (candidate == null) return false;

        legacyText.enabled = false;
        tmpText.enabled = true;
        tmpText.font = candidate;
        tmpText.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();
        cam.Render();

        var oldActive = RenderTexture.active;
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        output.Apply();
        RenderTexture.active = oldActive;

        if (HasVisiblePixels(output)) return true;

        tmpText.enabled = false;
        ClearTexture(output);
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = oldActive;
        return false;
    }

    private static Font GetManualFontAsset()
    {
        string path = EditorPrefs.GetString(ManualFontAssetPathKey, string.Empty);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
    }

    private static TMP_FontAsset GetManualTmpFontAsset()
    {
        string path = EditorPrefs.GetString(ManualTmpFontAssetPathKey, string.Empty);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }

    private static bool IsCdnFallbackEnabled()
    {
        return EditorPrefs.GetBool(UseCdnFallbackKey, true);
    }

    private static void SaveManualFontSettings(Font font, TMP_FontAsset tmpFont, string osFontName, bool useCdnFallback)
    {
        if (font == null)
        {
            EditorPrefs.DeleteKey(ManualFontAssetPathKey);
        }
        else
        {
            string path = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(path))
            {
                EditorPrefs.DeleteKey(ManualFontAssetPathKey);
            }
            else
            {
                EditorPrefs.SetString(ManualFontAssetPathKey, path);
            }
        }

        if (tmpFont == null)
        {
            EditorPrefs.DeleteKey(ManualTmpFontAssetPathKey);
        }
        else
        {
            string tmpPath = AssetDatabase.GetAssetPath(tmpFont);
            if (string.IsNullOrEmpty(tmpPath))
            {
                EditorPrefs.DeleteKey(ManualTmpFontAssetPathKey);
            }
            else
            {
                EditorPrefs.SetString(ManualTmpFontAssetPathKey, tmpPath);
            }
        }

        if (string.IsNullOrWhiteSpace(osFontName))
        {
            EditorPrefs.DeleteKey(ManualOsFontNameKey);
        }
        else
        {
            EditorPrefs.SetString(ManualOsFontNameKey, osFontName.Trim());
        }

        EditorPrefs.SetBool(UseCdnFallbackKey, useCdnFallback);
    }

    private static void ClearTexture(Texture2D texture)
    {
        var clear = new Color32(0, 0, 0, 0);
        var pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private static bool HasVisiblePixels(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0 || pixels[i].r > 0 || pixels[i].g > 0 || pixels[i].b > 0) return true;
        }

        return false;
    }

    private static void DrawFallbackTexture(Texture2D texture, string rawText)
    {
        int size = texture.width;
        Color clear = Color.clear;
        Color stroke = new Color(1f, 1f, 1f, 0.9f);

        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int i = 0; i < size; i++)
        {
            pixels[(size / 4) * size + i] = stroke;
            pixels[(3 * size / 4) * size + i] = stroke;
            pixels[i * size + (size / 4)] = stroke;
            pixels[i * size + (3 * size / 4)] = stroke;
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private static bool TryLoadTwemojiTexture(string rawText, int targetSize, out Texture2D output)
    {
        output = null;
        var codepointName = BuildTwemojiCodepointString(rawText, keepVariationSelectors: true);
        var fallbackName = BuildTwemojiCodepointString(rawText, keepVariationSelectors: false);

        string[] urls =
        {
            $"https://cdnjs.cloudflare.com/ajax/libs/twemoji/14.0.2/72x72/{codepointName}.png",
            $"https://cdnjs.cloudflare.com/ajax/libs/twemoji/14.0.2/72x72/{fallbackName}.png"
        };

        for (int i = 0; i < urls.Length; i++)
        {
            if (TryDownloadTexture(urls[i], out Texture2D downloaded))
            {
                output = ResizeTexture(downloaded, targetSize, targetSize);
                DestroyImmediate(downloaded);
                return output != null;
            }
        }

        return false;
    }

    private static bool TryDownloadTexture(string url, out Texture2D texture)
    {
        texture = null;
        try
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;
            request.UserAgent = "Unity EmojiAssetGenerator";

            using var response = (HttpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            if (stream == null) return false;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            byte[] bytes = ms.ToArray();

            var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!loaded.LoadImage(bytes, false))
            {
                DestroyImmediate(loaded);
                return false;
            }

            texture = loaded;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var old = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = old;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private static string BuildTwemojiCodepointString(string text, bool keepVariationSelectors)
    {
        var codepoints = new List<int>();
        for (int i = 0; i < text.Length; i++)
        {
            int cp;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                cp = char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }
            else
            {
                cp = text[i];
            }

            if (!keepVariationSelectors && (cp == 0xFE0E || cp == 0xFE0F))
            {
                continue;
            }

            codepoints.Add(cp);
        }

        return string.Join("-", codepoints.Select(cp => cp.ToString("x")));
    }

    private struct TextureTargetData { public string name; public string unicodeStr; public int fallbackCodePoint; }
}
#endif