// Compara lo que escribía cada teclado hecho a mano con su distribución real de Windows y lo deja en un informe.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Teclado.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ParityReport
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    const string ReportPath = "docs/superpowers/reports/2026-11-17-paridad-teclados.md";
    static readonly string[] RowNames = { "Numbers", "Row 1", "Row 2", "Row 3" };

    [MenuItem("Teclado/Informe de paridad")]
    public static void Write()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var controlator = Object.FindAnyObjectByType<KeyboardControlator>(FindObjectsInactive.Include);
        var oldKeyboards = new SerializedObject(controlator).FindProperty("RigidBodies_Keyboards");
        var layouts = AssetDatabase.FindAssets("t:KeyboardLayout", new[] { "Assets/Layouts" })
            .Select(g => AssetDatabase.LoadAssetAtPath<KeyboardLayout>(AssetDatabase.GUIDToAssetPath(g)).data)
            .OrderBy(l => l.order).ToList();

        var md = new StringBuilder();
        md.AppendLine("# Paridad: teclados actuales frente a distribuciones de Windows\n");
        md.AppendLine("Generado por *Teclado → Informe de paridad*. Por idioma, solo las teclas cuya capa base o Shift cambia. «—» = vacío.");
        md.AppendLine("Las teclas de tilde (Closed, Open, Diaeresis, Circumflex, Virgulilla, Dieresis_Accented) desaparecen en todos: las tildes pasan a ser teclas muertas.\n");
        int total = 0;

        foreach (var layout in layouts)
        {
            var old = (GameObject)oldKeyboards.GetArrayElementAtIndex(layout.order).objectReferenceValue;
            var rows = TemplateScancodes.Rows(layout.template);
            var diffs = new List<string>();
            for (int r = 0; r < 4; r++)
            {
                var rowT = r == 0 ? old.transform.Find("Buttons/Normal/Numbers") : old.transform.Find("Buttons/Normal/Letters/" + RowNames[r]);
                for (int i = 0; i < rows[r].Length && i < rowT.childCount; i++)
                {
                    var kb = rowT.GetChild(i).GetComponentInChildren<KeyboardButton>(true);
                    string oldBase = First(kb?.normalForms), oldShift = First(kb?.CapForms);
                    var def = layout.Find(rows[r][i]);
                    string newBase = def?.text[0] ?? "", newShift = def?.text[1] ?? "";
                    if (oldBase == newBase && oldShift == newShift) continue;
                    diffs.Add($"| {RowNames[r]} {i + 1} | `{Show(oldBase)}` / `{Show(oldShift)}` | `{Show(newBase)}` / `{Show(newShift)}`{(def != null && (def.dead[0] || def.dead[1]) ? " (muerta)" : "")} |");
                }
            }

            md.AppendLine($"## {layout.displayName} — plantilla {layout.template}, {diffs.Count} teclas cambian\n");
            bool hasAltGr = layout.keys.Any(k => !string.IsNullOrEmpty(k.text[2]) || !string.IsNullOrEmpty(k.text[3]));
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateBuilder.PathFor(layout.template, "ByRigidBodies"));
            bool templateHasAltGr = template != null && template.transform.Find("Buttons/Specials/ALT") != null;
            if (hasAltGr && !templateHasAltGr)
                md.AppendLine("> La distribución tiene capa AltGr, pero esta plantilla no tiene tecla ALT: esos caracteres no se pueden escribir.\n");
            if (diffs.Count > 0)
            {
                md.AppendLine("| Tecla | Antes (base / Shift) | Ahora (base / Shift) |\n|---|---|---|");
                diffs.ForEach(d => md.AppendLine(d));
                md.AppendLine();
            }
            total += diffs.Count;
        }
        md.AppendLine($"**Total:** {total} teclas cambian.");

        string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, md.ToString());
        Debug.Log($"[Tpl] Informe de paridad: {total} teclas cambian → {ReportPath}");
    }

    static string First(string[] a) => a != null && a.Length > 0 ? a[0] ?? "" : "";
    static string Show(string s) => string.IsNullOrEmpty(s) ? "—" : s.Replace("`", "ˋ").Replace("|", "\\|");
}
