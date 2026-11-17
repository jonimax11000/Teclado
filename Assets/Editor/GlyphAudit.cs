// Auditoría temporal: ¿qué caracteres de cada teclado no puede dibujar su fuente (con fallbacks)?
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GlyphAudit
{
    static bool Covered(TMP_FontAsset f, List<TMP_FontAsset> global, char ch) =>
        f.HasCharacter(ch, true, true) || global.Any(g => g != null && g.HasCharacter(ch, true, true));

    [MenuItem("Teclado/Auditar glifos")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Pruebas VR.unity");
        var global = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
        var fieldFont = Object.FindAnyObjectByType<KeyboardControlator>(FindObjectsInactive.Include).field.textComponent.font;
        foreach (var kb in Object.FindObjectsByType<Keyboard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var missing = new SortedSet<string>();
            var fieldMissing = new SortedSet<string>();
            var fonts = new SortedSet<string>();
            int keys = 0;
            foreach (var b in kb.GetComponentsInChildren<KeyboardButton>(true))
            {
                keys++;
                var label = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null || label.font == null) { missing.Add("<sin etiqueta/fuente:" + b.name + ">"); continue; }
                var all = (b.normalForms ?? new string[0]).Concat(b.CapForms ?? new string[0]).Append(b.alterForm ?? "");
                foreach (var s in all)
                foreach (char ch in s)
                {
                    if (char.IsWhiteSpace(ch)) continue;
                    fonts.Add(label.font.name + (label.font.atlasPopulationMode == AtlasPopulationMode.Dynamic ? "(din)" : "(est)"));
                    if (!Covered(fieldFont, global, ch) && !global.Any(f => f != null && f.HasCharacter(ch, true, true))) fieldMissing.Add($"{ch}");
                    if (label.font.HasCharacter(ch, true, true)) continue;
                    if (global.Any(f => f != null && f.HasCharacter(ch, true, true))) continue;
                    missing.Add($"{ch}(U+{(int)ch:X4})");
                }
            }
            // Árabe: el campo dibuja formas contextuales (inicial/medial/final, ligaduras), no las letras base.
            if (kb._Keyboardtype == Keyboard.KeyboardType.Arabic)
                foreach (var b in kb.GetComponentsInChildren<KeyboardButton>(true))
                foreach (var s in (b.normalForms ?? new string[0]).Concat(b.CapForms ?? new string[0]).Append(b.alterForm ?? ""))
                foreach (var v in new[] { s, "ب" + s, s + "ب", "ب" + s + "ب" })
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    var shaped = new RTLTMPro.FastStringBuilder(64);
                    RTLTMPro.RTLSupport.FixRTL(v, shaped, false, true, true);
                    foreach (char ch in shaped.ToString())
                        if (!char.IsWhiteSpace(ch) && ch != '\u200B' && !Covered(fieldFont, global, ch)) fieldMissing.Add($"{ch}(U+{(int)ch:X4})");
                }
            string root = kb.transform.parent ? kb.transform.parent.name + "/" + kb.name : kb.name;
            Debug.Log($"[Audit] {root} type={kb._Keyboardtype} teclas={keys} fuentes={string.Join(",", fonts)}\n   etiquetas faltan={missing.Count(m => !m.StartsWith("<"))}: {string.Join(" ", missing.Where(m => !m.StartsWith("<")).Take(40))}\n   CAMPO faltan={fieldMissing.Count}: {string.Join("", fieldMissing.Take(80))}");
        }
        var field = Object.FindAnyObjectByType<KeyboardControlator>(FindObjectsInactive.Include).field;
        Debug.Log($"[Audit] campo: fuente={field.textComponent.font?.name} fallbacks={string.Join(",", field.textComponent.font?.fallbackFontAssetTable?.Select(f => f?.name) ?? new string[0])} global={string.Join(",", global.Select(f => f?.name))}");
    }
}
