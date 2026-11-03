using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

// One-time switch of the app's UI font from Poppins to Montserrat. Poppins has no Cyrillic and
// lacks most Vietnamese letters, so TextMeshPro borrowed them from the Chinese fallback font,
// which looked bold and out of place mid-word. Montserrat is a geometric sans close to Poppins
// that covers Latin, Vietnamese and Cyrillic (including Ukrainian).
//
// It swaps the source font inside each existing Poppins SDF asset and then renames the asset.
// The asset ID stays the same, so no scene, prefab, weight-table (<b>, <i>) or fallback
// reference changes. Safe to delete this file once the switch is done.
static class SwitchToMontserrat
{
    const string Family = "Montserrat";
    const string FontsDir = "Assets/Fonts/";
    static readonly string[] Weights = { "Regular", "Italic", "SemiBold", "Bold", "BoldItalic" };

    // Font feature lists hold records keyed by glyph number, which differ between fonts.
    static readonly string[] FeatureLists =
    {
        "m_MultipleSubstitutionRecords",
        "m_LigatureSubstitutionRecords",
        "m_GlyphPairAdjustmentRecords",
        "m_MarkToBaseAdjustmentRecords",
        "m_MarkToMarkAdjustmentRecords",
    };

    [MenuItem("SaySuomi/Fonts/Switch Poppins to Montserrat")]
    static void Switch()
    {
        if (
            !EditorUtility.DisplayDialog(
                "Switch to " + Family,
                $"Replace Poppins with {Family} in the five Poppins font assets? "
                    + "Scenes and prefabs keep their references, and line spacing stays as it is. "
                    + "Undo is through git only.",
                "Switch",
                "Cancel"
            )
        )
        {
            return;
        }

        var report = new StringBuilder($"SwitchTo{Family} report:\n");
        FontEngine.InitializeFontEngine();
        foreach (string weight in Weights)
        {
            report.AppendLine("  " + SwitchOne(weight));
        }
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Switch to " + Family, report.ToString(), "OK");
    }

    static string SwitchOne(string weight)
    {
        string assetPath = $"{FontsDir}Poppins-{weight} SDF.asset";
        string fontPath = $"{FontsDir}{Family}-{weight}.ttf";
        string newName = $"{Family}-{weight} SDF";

        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (asset == null)
        {
            return $"{weight}: skipped, {assetPath} not found (already switched?)";
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            return $"{weight}: skipped, {fontPath} is not imported yet (press Ctrl+R, then run again)";
        }

        // Real metrics of the new font at the asset's sampling size, as TMP's own
        // CreateFontAsset does.
        int pointSize = Mathf.RoundToInt(asset.faceInfo.pointSize);
        FontEngineError error = FontEngine.LoadFontFace(font, pointSize);
        if (error != FontEngineError.Success)
        {
            return $"{weight}: FAILED to load {fontPath} ({error})";
        }
        FaceInfo face = FontEngine.GetFaceInfo();

        // Keep Poppins's line spacing (1.5 em). Montserrat's own is 1.22 em, which would make
        // every paragraph about a fifth denser and shift layouts tuned for Poppins. Letter
        // size is unaffected: both fonts have a cap height of 0.70 em.
        float oldLineHeight = asset.faceInfo.lineHeight;
        float newLineHeight = face.lineHeight;
        face.lineHeight = oldLineHeight;

        // Drop the Poppins glyphs, characters, atlas contents and font features.
        asset.ClearFontAssetData(true);

        var so = new SerializedObject(asset);
        string fontGuid = AssetDatabase.AssetPathToGUID(fontPath);
        so.FindProperty("m_SourceFontFile").objectReferenceValue = font;
        so.FindProperty("m_SourceFontFileGUID").stringValue = fontGuid;
        so.FindProperty("m_CreationSettings.sourceFontFileGUID").stringValue = fontGuid;
        foreach (string list in FeatureLists)
        {
            SerializedProperty records = so.FindProperty("m_FontFeatureTable." + list);
            if (records != null)
            {
                records.arraySize = 0;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        asset.faceInfo = face;
        // Vietnamese and Cyrillic need more glyphs than one 1024 atlas holds at 90 pt.
        asset.isMultiAtlasTexturesEnabled = true;
        asset.material.name = $"{Family}-{weight} Atlas Material";
        if (asset.atlasTextures != null && asset.atlasTextures.Length > 0)
        {
            asset.atlasTextures[0].name = $"{Family}-{weight} Atlas";
        }
        asset.ReadFontAssetDefinition();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, asset);

        string renameError = AssetDatabase.RenameAsset(assetPath, newName);

        so.Update();
        int pairs = so.FindProperty("m_FontFeatureTable.m_GlyphPairAdjustmentRecords").arraySize;
        return $"{weight}: {face.familyName} {face.styleName}, line height kept at {oldLineHeight:F1} "
            + $"(font's own {newLineHeight:F1}), ascent {face.ascentLine:F1}, scale {face.scale}, "
            + $"old kerning pairs left {pairs}, fallbacks {asset.fallbackFontAssetTable?.Count ?? 0}, "
            + (
                string.IsNullOrEmpty(renameError)
                    ? $"renamed to {newName}"
                    : $"RENAME FAILED: {renameError}"
            );
    }
}
