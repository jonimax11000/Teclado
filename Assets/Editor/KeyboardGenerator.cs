// Genera en la escena un teclado rígido y uno de canvas por cada distribución de Assets/Layouts,
// a partir de las plantillas físicas. Idempotente. La primera vez migra la escena antigua
// (Keyboard / KeyboardButton / KeyboardControlator), que se reconoce por nombre de tipo.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Teclado.Core;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class KeyboardGenerator
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    const string OldController = "KeyboardControlator";
    const string OldKeyboard = "Keyboard";

    // Scancodes que existen en alguna plantilla: el resto del KLC (numérico, espacio…) no se avisa.
    static readonly HashSet<int> TemplateScans = new HashSet<int>(
        Enum.GetValues(typeof(PhysicalTemplate)).Cast<PhysicalTemplate>().SelectMany(t => TemplateScancodes.Rows(t).SelectMany(r => r)));

    public static string TemplatePath(PhysicalTemplate t, string mode) =>
        $"Assets/Prefabs/Templates/{t}_{(mode == "ByRigidBodies" ? "Rigid" : "Canvas")}.prefab";

    [MenuItem("Teclado/Generar teclados")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Apply(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Gen] Teclados generados.");
    }

    public static void Apply(Scene scene)
    {
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).Where(m => m != null).ToList();
        var old = all.FirstOrDefault(m => m.GetType().Name == OldController);
        var controller = all.OfType<KeyboardController>().FirstOrDefault();
        if (controller == null && old == null) throw new InvalidOperationException("No hay KeyboardController ni KeyboardControlator en la escena");

        var host = controller != null ? controller.gameObject : old.gameObject;
        var oldSo = old != null ? new SerializedObject(old) : null;
        controller = Ensure<KeyboardController>(host);

        var switcher = Ensure<KeyboardModeSwitcher>(host);
        if (oldSo != null)
        {
            switcher.rigidRoot = (GameObject)oldSo.FindProperty("RigidKeyboards").objectReferenceValue;
            switcher.canvasRoot = (GameObject)oldSo.FindProperty("CanvasKeyboards").objectReferenceValue;
        }

        var selector = Ensure<LanguageSelector>(host);
        selector.controller = controller;
        if (oldSo != null)
            selector.dropdown = ((GameObject)oldSo.FindProperty("Languaje_selector_Dropdown").objectReferenceValue).GetComponent<TMP_Dropdown>();

        var field = oldSo != null
            ? (TMP_InputField)oldSo.FindProperty("field").objectReferenceValue
            : all.OfType<InputFieldView>().First().field;
        MountField(field, controller);

        // Posición de los teclados antiguos, por índice de idioma (solo la primera vez).
        var oldRigid = OldArray(oldSo, "RigidBodies_Keyboards");
        var oldCanvas = OldArray(oldSo, "Canvas_Keyboards");

        Rewire(all, switcher, selector);

        var layouts = AssetDatabase.FindAssets("t:KeyboardLayout", new[] { "Assets/Layouts" })
            .Select(g => AssetDatabase.LoadAssetAtPath<KeyboardLayout>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(l => l.data.order).ToList();

        selector.entries = layouts.Select(layout => new LanguageSelector.Entry
        {
            layout = layout,
            rigid = Generate(layout, switcher.rigidRoot.transform, "ByRigidBodies", Get(oldRigid, layout.data.order), controller),
            canvas = Generate(layout, switcher.canvasRoot.transform, "ByCanvas", Get(oldCanvas, layout.data.order), controller),
        }).ToList();

        foreach (var root in new[] { switcher.rigidRoot.transform, switcher.canvasRoot.transform })
            foreach (var t in root.Cast<Transform>().ToList())
                if (t.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == OldKeyboard))
                    Object.DestroyImmediate(t.gameObject);
        if (old != null) Object.DestroyImmediate(old);
        // KeyboardButton sueltos fuera de los teclados (p. ej. el botón de cambiar a canvas).
        foreach (var leftover in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                     .Where(m => m != null && m.GetType().Name == "KeyboardButton").ToList())
            Object.DestroyImmediate(leftover);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(switcher);
        EditorUtility.SetDirty(selector);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static T Ensure<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

    static List<GameObject> OldArray(SerializedObject so, string name)
    {
        var list = new List<GameObject>();
        var p = so?.FindProperty(name);
        if (p != null) for (int i = 0; i < p.arraySize; i++) list.Add((GameObject)p.GetArrayElementAtIndex(i).objectReferenceValue);
        return list;
    }

    static GameObject Get(List<GameObject> list, int i) => i < list.Count ? list[i] : null;

    static void MountField(TMP_InputField field, KeyboardController controller)
    {
        var input = Ensure<InputFieldView>(field.gameObject);
        input.field = field;
        input.controller = controller;

        var caret = Ensure<CaretView>(field.gameObject);
        caret.input = input;
        caret.caret = Child(field.textViewport, "Caret");

        Ensure<FieldPointer>(field.gameObject).input = input;

        var preedit = Ensure<PreeditView>(field.gameObject);
        preedit.input = input;
        preedit.underline = Child(field.textViewport, "Underline");

        foreach (var c in new Component[] { input, caret, preedit, field.GetComponent<FieldPointer>() }) EditorUtility.SetDirty(c);
    }

    static RectTransform Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return (RectTransform)t;
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().raycastTarget = false;
        return (RectTransform)go.transform;
    }

    // Los eventos de escena que llamaban a la clase antigua pasan a las nuevas; el resto se quita.
    static void Rewire(List<MonoBehaviour> all, KeyboardModeSwitcher switcher, LanguageSelector selector)
    {
        foreach (var component in all.Where(m => !InsideOldKeyboard(m.transform)))
            foreach (var e in Events(component))
            {
                bool changed = false;
                for (int i = e.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    var target = e.GetPersistentTarget(i);
                    if (target == null || !(target.GetType().Name == OldController || target.GetType().Name == OldKeyboard)) continue;
                    string method = e.GetPersistentMethodName(i);
                    UnityEventTools.RemovePersistentListener(e, i);
                    changed = true;
                    if (method == "ChangeToCanvas") UnityEventTools.AddVoidPersistentListener(e, switcher.ChangeToCanvas);
                    else if (method == "ChangeToRigid") UnityEventTools.AddVoidPersistentListener(e, switcher.ChangeToRigid);
                    else if (method == "ChangeLanguage" && e is UnityEvent<int> intEvent) UnityEventTools.AddPersistentListener(intEvent, selector.Select);
                }
                if (changed) EditorUtility.SetDirty(component);
            }
    }

    static bool InsideOldKeyboard(Transform t)
    {
        for (; t != null; t = t.parent)
            if (t.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == OldKeyboard)) return true;
        return false;
    }

    static IEnumerable<UnityEventBase> Events(Component c)
    {
        for (var type = c.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (typeof(UnityEventBase).IsAssignableFrom(f.FieldType) && f.GetValue(c) is UnityEventBase e)
                    yield return e;
    }

    static GameObject Generate(KeyboardLayout layout, Transform root, string mode, GameObject oldKeyboard, KeyboardController controller)
    {
        var view = root.Cast<Transform>().Select(t => t.GetComponent<KeyboardView>()).FirstOrDefault(v => v != null && v.layout == layout);
        if (view == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath(layout.data.template, mode));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            if (oldKeyboard != null)
            {
                go.transform.localPosition = oldKeyboard.transform.localPosition;
                go.transform.localRotation = oldKeyboard.transform.localRotation;
                go.transform.localScale = oldKeyboard.transform.localScale;
            }
            go.SetActive(false);   // LanguageSelector activa el idioma elegido
            view = go.GetComponent<KeyboardView>();
        }
        view.gameObject.name = layout.data.displayName;
        view.layout = layout;
        view.controller = controller;
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        PrefabUtility.RecordPrefabInstancePropertyModifications(view.gameObject);

        var slots = new HashSet<int>();
        foreach (var key in view.GetComponentsInChildren<KeyView>(true))
        {
            slots.Add(key.scanCode);
            var def = layout.data.Find(key.scanCode);
            bool defined = def != null && def.text.Any(t => !string.IsNullOrEmpty(t));
            key.gameObject.SetActive(defined);
            if (defined)
            {
                key.gameObject.name = string.IsNullOrEmpty(def.text[0]) ? $"0x{key.scanCode:X2}" : def.text[0];
                key.SetLabel(def.text[0]);   // vista previa en el editor; en ejecución la refresca KeyboardView
                if (key.label != null) PrefabUtility.RecordPrefabInstancePropertyModifications(key.label);
            }
            else Debug.Log($"[Gen] aviso: {layout.data.displayName}/{mode}: la tecla 0x{key.scanCode:X2} no está en la distribución, desactivada");
            PrefabUtility.RecordPrefabInstancePropertyModifications(key.gameObject);
        }
        foreach (var k in layout.data.keys.Where(k => TemplateScans.Contains(k.scanCode) && !slots.Contains(k.scanCode)))
            Debug.Log($"[Gen] aviso: {layout.data.displayName}/{mode}: la plantilla {layout.data.template} no tiene la tecla 0x{k.scanCode:X2}");
        return view.gameObject;
    }
}
