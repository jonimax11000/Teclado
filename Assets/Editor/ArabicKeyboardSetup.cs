// Configura en la escena los dos teclados árabes (rígido y canvas) con la distribución
// Windows Arabic (102) y prepara el campo de texto para RTL. Idempotente.
using System;
using System.Collections.Generic;
using System.Linq;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ArabicKeyboardSetup
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    const string LayoutPath = "Assets/Editor/Layouts/Arabic102.json";
    const string FontPath = "Assets/Fonts/NotoSansArabic/NotoSansArabic-Regular.ttf";
    const string FontAssetPath = "Assets/Fonts/NotoSansArabic/NotoSansArabic SDF.asset";
    const int ArabicIndex = 7;
    const string ArabicOption = "العربية";
    static readonly string[] AccentKeys = { "closed", "open", "diaeresis", "circumflex", "circunflex", "virgulilla" };

    [Serializable] class Layout { public Row[] rows; }
    [Serializable] class Row { public string name; public Key[] keys; }
    [Serializable] class Key { public string @base; public string shift; public string altgr; }

    [MenuItem("Teclado/Configurar teclado árabe")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Apply(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Teclado árabe configurado.");
    }

    public static void Apply(Scene scene)
    {
        var controlator = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<KeyboardControlator>(true)).Single();
        var layout = JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath).text);
        var so = new SerializedObject(controlator);

        foreach (var arrayName in new[] { "RigidBodies_Keyboards", "Canvas_Keyboards" })
        {
            var root = (GameObject)so.FindProperty(arrayName).GetArrayElementAtIndex(ArabicIndex).objectReferenceValue;
            ConfigureKeyboard(root, layout);
        }

        var arabicFont = GetOrCreateArabicFont();
        AddGlobalFallback(arabicFont);
        ConfigureField(controlator);
        ConfigureDropdown((GameObject)so.FindProperty("Languaje_selector_Dropdown").objectReferenceValue);
        RemoveSyllablePrototype(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void ConfigureKeyboard(GameObject root, Layout layout)
    {
        var kbSo = new SerializedObject(root.GetComponent<Keyboard>());
        kbSo.FindProperty("_Keyboardtype").enumValueIndex = (int)Keyboard.KeyboardType.Arabic;
        kbSo.ApplyModifiedPropertiesWithoutUndo();

        foreach (var row in layout.rows)
        {
            var rowT = row.name == "Numbers"
                ? root.transform.Find("Buttons/Normal/Numbers")
                : root.transform.Find("Buttons/Normal/Letters/" + row.name);
            if (rowT == null || rowT.childCount != row.keys.Length)
                throw new InvalidOperationException($"{root.name}/{row.name}: esperaba {row.keys.Length} teclas, hay {(rowT ? rowT.childCount : -1)}");

            for (int i = 0; i < row.keys.Length; i++)
                ConfigureKey(rowT.GetChild(i).gameObject, row.keys[i]);
        }

        var specials = root.transform.Find("Buttons/Specials");
        foreach (var t in specials.Cast<Transform>().Where(t => AccentKeys.Contains(t.name.ToLowerInvariant())).ToList())
            Object.DestroyImmediate(t.gameObject);
    }

    static void ConfigureKey(GameObject keyGo, Key key)
    {
        keyGo.name = key.@base;
        PrefabUtility.RecordPrefabInstancePropertyModifications(keyGo);

        var button = keyGo.GetComponentInChildren<KeyboardButton>(true);
        var bSo = new SerializedObject(button);
        SetArray(bSo.FindProperty("normalForms"), key.@base);
        SetArray(bSo.FindProperty("CapForms"), key.shift);
        bSo.FindProperty("alterForm").stringValue = key.altgr;
        bSo.FindProperty("special_letter").boolValue = false;
        bSo.FindProperty("not_vowel").boolValue = true;
        bSo.ApplyModifiedPropertiesWithoutUndo();

        var label = keyGo.GetComponentInChildren<TextMeshProUGUI>(true);
        var lSo = new SerializedObject(label);
        lSo.FindProperty("m_text").stringValue = key.@base;
        lSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray(SerializedProperty array, string value)
    {
        array.arraySize = 1;
        array.GetArrayElementAtIndex(0).stringValue = value;
    }

    static void ConfigureField(KeyboardControlator controlator)
    {
        var field = controlator.field;
        if (!(field.textComponent is RTLTextMeshPro))
            field.textComponent = SwapToRtl((TextMeshProUGUI)field.textComponent);
        var rtl = (RTLTextMeshPro)field.textComponent;
        rtl.Farsi = false;
        rtl.PreserveNumbers = true;
        EnableMultiAtlas(rtl.font);
        if (rtl.font != null)
            foreach (var f in rtl.font.fallbackFontAssetTable ?? new List<TMP_FontAsset>()) EnableMultiAtlas(f);

        var caret = field.textViewport.Find("Caret");
        if (caret == null)
        {
            var go = new GameObject("Caret", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(field.textViewport, false);
            go.GetComponent<Image>().raycastTarget = false;
            caret = go.transform;
        }

        var view = field.GetComponent<CaretView>() ?? field.gameObject.AddComponent<CaretView>();
        view.controlator = controlator;
        view.caret = (RectTransform)caret;
        var pointer = field.GetComponent<FieldPointer>() ?? field.gameObject.AddComponent<FieldPointer>();
        pointer.controlator = controlator;
        EditorUtility.SetDirty(field);
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(pointer);
    }

    static void ConfigureDropdown(GameObject go)
    {
        var dd = go.GetComponent<TMP_Dropdown>();
        if (dd.options.Count == ArabicIndex)
            dd.options.Add(new TMP_Dropdown.OptionData(ArabicOption));
        else if (dd.options.Count > ArabicIndex)
            dd.options[ArabicIndex].text = ArabicOption;

        if (!(dd.captionText is RTLTextMeshPro)) dd.captionText = SwapToRtl((TextMeshProUGUI)dd.captionText);
        if (!(dd.itemText is RTLTextMeshPro)) dd.itemText = SwapToRtl((TextMeshProUGUI)dd.itemText);
        ((RTLTextMeshPro)dd.captionText).Farsi = false;
        ((RTLTextMeshPro)dd.itemText).Farsi = false;
        EditorUtility.SetDirty(dd);
    }

    // TextMeshProUGUI y RTLTextMeshPro son Graphic: no pueden convivir en el mismo GameObject.
    static RTLTextMeshPro SwapToRtl(TextMeshProUGUI old)
    {
        var go = old.gameObject;
        var font = old.font;
        var material = old.fontSharedMaterial;
        var size = old.fontSize;
        var style = old.fontStyle;
        var color = old.color;
        var alignment = old.alignment;
        var margin = old.margin;
        var autoSize = old.enableAutoSizing;
        var sizeMin = old.fontSizeMin;
        var sizeMax = old.fontSizeMax;
        var raycast = old.raycastTarget;
        var rich = old.richText;
        var wrapping = old.textWrappingMode;
        var overflow = old.overflowMode;
        var text = old.text;

        Object.DestroyImmediate(old);
        var rtl = go.AddComponent<RTLTextMeshPro>();
        rtl.font = font;
        rtl.fontSharedMaterial = material;
        rtl.fontSize = size;
        rtl.fontStyle = style;
        rtl.color = color;
        rtl.alignment = alignment;
        rtl.margin = margin;
        rtl.enableAutoSizing = autoSize;
        rtl.fontSizeMin = sizeMin;
        rtl.fontSizeMax = sizeMax;
        rtl.raycastTarget = raycast;
        rtl.richText = rich;
        rtl.textWrappingMode = wrapping;
        rtl.overflowMode = overflow;
        rtl.text = text;
        return rtl;
    }

    static void RemoveSyllablePrototype(Scene scene)
    {
        foreach (var m in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                     .Where(m => m != null && m.GetType().Name == "FormadorDeSilabasUnity").ToList())
            Object.DestroyImmediate(m);
    }

    static TMP_FontAsset GetOrCreateArabicFont()
    {
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fa == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            fa = TMP_FontAsset.CreateFontAsset(font);
            fa.name = "NotoSansArabic SDF";
            AssetDatabase.CreateAsset(fa, FontAssetPath);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (var tex in fa.atlasTextures)
            {
                tex.name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
        }
        fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        EnableMultiAtlas(fa);
        return fa;
    }

    // Un atlas dinámico lleno deja de dibujar glifos nuevos (salen □).
    static void EnableMultiAtlas(TMP_FontAsset fa)
    {
        if (fa == null || fa.atlasPopulationMode != AtlasPopulationMode.Dynamic || fa.isMultiAtlasTexturesEnabled) return;
        fa.isMultiAtlasTexturesEnabled = true;
        EditorUtility.SetDirty(fa);
    }

    static void AddGlobalFallback(TMP_FontAsset fa)
    {
        var settings = TMP_Settings.instance;
        var sso = new SerializedObject(settings);
        var list = sso.FindProperty("m_fallbackFontAssets");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == fa) return;
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = fa;
        sso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }
}
