// Comprueba que cada carácter que puede escribir cada distribución tiene glifo en las fuentes que lo dibujan:
// etiquetas de su plantilla (rígida y canvas) y campo de texto, con los fallbacks globales de TMP.
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GlyphAudit
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";

    [MenuItem("Teclado/Auditar glifos")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var missing = Missing();
        Debug.Log(missing.Count == 0 ? "[Audit] Todos los caracteres tienen glifo." : "[Audit] Faltan glifos:\n" + string.Join("\n", missing));
    }

    public static List<string> Missing()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var fieldFont = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<InputFieldView>(true)).Single().field.textComponent.font;
        var global = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
        var result = new List<string>();

        foreach (var layout in AssetDatabase.FindAssets("t:KeyboardLayout", new[] { "Assets/Layouts" })
                     .Select(g => AssetDatabase.LoadAssetAtPath<KeyboardLayout>(AssetDatabase.GUIDToAssetPath(g)).data))
        {
            var texts = layout.keys.SelectMany(k => k.text)
                .Concat(layout.deadKeys.SelectMany(d => d.entries.Select(e => e.to).Append(d.deadChar)))
                .Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();

            var labelFonts = new[] { "ByRigidBodies", "ByCanvas" }
                .Select(mode => AssetDatabase.LoadAssetAtPath<GameObject>(KeyboardGenerator.TemplatePath(layout.template, mode)))
                .SelectMany(p => p.GetComponentsInChildren<KeyView>(true).Select(k => k.label?.font))
                .Where(f => f != null).Distinct().ToList();

            var fieldTexts = new List<string>(texts);
            if (layout.isRtl)   // el campo dibuja formas contextuales y ligaduras, no las letras base
                foreach (var t in texts)
                    foreach (var v in new[] { t, "ب" + t, t + "ب", "ب" + t + "ب" })
                    {
                        var shaped = new FastStringBuilder(64);
                        RTLSupport.FixRTL(v, shaped, false, true, true);
                        fieldTexts.Add(shaped.ToString());
                    }

            var bad = new SortedSet<string>();
            foreach (var font in labelFonts)
                foreach (char c in texts.SelectMany(t => t)) if (!Covered(font, global, c)) bad.Add($"etiqueta[{font.name}] {Describe(c)}");
            foreach (char c in fieldTexts.SelectMany(t => t)) if (!Covered(fieldFont, global, c)) bad.Add($"campo {Describe(c)}");
            result.AddRange(bad.Select(b => $"{layout.displayName}: {b}"));
        }
        return result;
    }

    static bool Covered(TMP_FontAsset font, List<TMP_FontAsset> global, char c) =>
        char.IsWhiteSpace(c) || char.IsControl(c) || c == '​' || c == '◌'
        || font.HasCharacter(c, true, true) || global.Any(g => g != null && g.HasCharacter(c, true, true));

    static string Describe(char c) => $"{c} (U+{(int)c:X4}, {CharUnicodeInfo.GetUnicodeCategory(c)})";
}
