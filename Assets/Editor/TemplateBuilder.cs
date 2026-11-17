// Crea las plantillas físicas (ISO, ANSI, ANSI-14) a partir de los teclados hechos a mano de la escena.
using System;
using System.Collections.Generic;
using System.Linq;
using Teclado.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TemplateBuilder
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    public const string Folder = "Assets/Prefabs/Templates";

    static readonly (PhysicalTemplate t, string source)[] Sources =
    {
        (PhysicalTemplate.ISO, "QWERTY ESP"), (PhysicalTemplate.ANSI, "QWERTY US"), (PhysicalTemplate.ANSI14, "JCUKEN"),
    };

    public static readonly string[] Modes = { "ByRigidBodies", "ByCanvas" };

    static readonly string[] AccentKeys = { "closed", "open", "diaeresis", "circumflex", "circunflex", "virgulilla", "dieresis_accented" };

    static readonly Dictionary<string, string> SpecialMethods = new Dictionary<string, string>
    {
        { "CapsPressed", nameof(KeyboardView.OnShift) }, { "AlterPressed", nameof(KeyboardView.OnAltGr) },
        { "insertSpace", nameof(KeyboardView.OnSpace) }, { "deleteChar", nameof(KeyboardView.OnBackspace) },
        { "LeftPressed", nameof(KeyboardView.OnLeft) }, { "RightPressed", nameof(KeyboardView.OnRight) },
    };

    public static string PathFor(PhysicalTemplate t, string mode) => $"{Folder}/{t}_{(mode == "ByRigidBodies" ? "Rigid" : "Canvas")}.prefab";

    [MenuItem("Teclado/Crear plantillas")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Templates");
        foreach (var (t, source) in Sources)
            foreach (var mode in Modes)
            {
                Build(FindKeyboard(mode, source), t, PathFor(t, mode));
                Debug.Log($"[Tpl] {PathFor(t, mode)} desde {mode}/{source}");
            }
        AssetDatabase.SaveAssets();
    }

    public static GameObject FindKeyboard(string mode, string name)
    {
        var root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == "Keyboards");
        return root.transform.Find($"{mode}/{name}").gameObject;
    }

    // Trabaja sobre un clon: el teclado original de la escena no se toca.
    public static GameObject Build(GameObject sourceKeyboard, PhysicalTemplate t, string prefabPath)
    {
        var clone = Object.Instantiate(sourceKeyboard);
        clone.name = t.ToString();
        clone.SetActive(true);
        try
        {
            Object.DestroyImmediate(clone.GetComponent<Keyboard>());
            var view = clone.AddComponent<KeyboardView>();

            var rows = TemplateScancodes.Rows(t);
            var rowTransforms = new[]
            {
                clone.transform.Find("Buttons/Normal/Numbers"), clone.transform.Find("Buttons/Normal/Letters/Row 1"),
                clone.transform.Find("Buttons/Normal/Letters/Row 2"), clone.transform.Find("Buttons/Normal/Letters/Row 3"),
            };
            for (int r = 0; r < 4; r++)
            {
                if (rowTransforms[r].childCount != rows[r].Length)
                    throw new InvalidOperationException($"{sourceKeyboard.name} fila {r}: {rowTransforms[r].childCount} teclas, la plantilla {t} espera {rows[r].Length}");
                for (int i = 0; i < rows[r].Length; i++)
                    MakeKey(rowTransforms[r].GetChild(i).gameObject, rows[r][i]);
            }

            var specials = clone.transform.Find("Buttons/Specials");
            foreach (var s in specials.Cast<Transform>().Where(s => AccentKeys.Contains(s.name.ToLowerInvariant())).ToList())
                Object.DestroyImmediate(s.gameObject);
            foreach (Transform s in specials) Rewire(s.gameObject, view);

            foreach (var leftover in clone.GetComponentsInChildren<KeyboardButton>(true)) Object.DestroyImmediate(leftover);

            return PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(clone);
        }
    }

    static void MakeKey(GameObject keyGo, int scanCode)
    {
        foreach (var kb in keyGo.GetComponentsInChildren<KeyboardButton>(true)) Object.DestroyImmediate(kb);
        var key = keyGo.AddComponent<KeyView>();
        key.scanCode = scanCode;
        key.label = keyGo.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
    }

    // Sustituye las llamadas a Keyboard (CapsPressed, insertSpace…) por las de KeyboardView.
    static void Rewire(GameObject special, KeyboardView view)
    {
        foreach (var e in Events(special))
            for (int i = e.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                string method = e.GetPersistentMethodName(i);
                var target = e.GetPersistentTarget(i);
                bool old = target == null || target is Keyboard || target is KeyboardControlator;
                if (!old) continue;
                UnityEventTools.RemovePersistentListener(e, i);
                if (SpecialMethods.TryGetValue(method, out var replacement))
                    UnityEventTools.AddVoidPersistentListener(e, (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), view, replacement));
            }
    }

    static IEnumerable<UnityEventBase> Events(GameObject go)
    {
        foreach (var b in go.GetComponentsInChildren<CanvasButton>(true)) { yield return b.onPress; yield return b.onRelease; }
        foreach (var b in go.GetComponentsInChildren<ButtonVR>(true)) { yield return b.onPress; yield return b.onRelease; yield return b.OnStay; }
    }
}
