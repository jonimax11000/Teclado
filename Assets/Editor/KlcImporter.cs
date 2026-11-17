// Importa ficheros .klc de Windows a assets KeyboardLayout (Assets/Layouts).
using System.IO;
using System.Text;
using Teclado.Core;
using UnityEditor;
using UnityEngine;

public static class KlcImporter
{
    const string LayoutsFolder = "Assets/Layouts";
    const string KlcFolder = "Klc";   // en la raíz del proyecto, ignorado por git

    // Distribuciones incluidas en el asset. Para añadir un idioma: una línea aquí, o el menú de importar.
    static readonly (string name, string klc, int order, string systemLanguage, PhysicalTemplate template, ComposerKind composer, bool rtl)[] Builtin =
    {
        ("English", "kbdus", 0, "English", PhysicalTemplate.ANSI, ComposerKind.Direct, false),
        ("Español", "kbdsp", 1, "Spanish", PhysicalTemplate.ISO, ComposerKind.DeadKey, false),
        ("Français", "kbdfr", 2, "French", PhysicalTemplate.ISO, ComposerKind.DeadKey, false),
        ("Deutsch", "kbdgr", 3, "German", PhysicalTemplate.ISO, ComposerKind.DeadKey, false),
        ("Русский", "kbdru", 4, "Russian", PhysicalTemplate.ANSI14, ComposerKind.Direct, false),
        ("한국어", "kbdus", 5, "Korean", PhysicalTemplate.ANSI, ComposerKind.Hangul, false),
        ("Ελληνικά", "kbdhe", 6, "Greek", PhysicalTemplate.ANSI14, ComposerKind.DeadKey, false),
        ("العربية", "kbda2", 7, "Arabic", PhysicalTemplate.ISO, ComposerKind.Direct, true),
    };

    [MenuItem("Teclado/Importar distribución .klc…")]
    static void ImportOne()
    {
        string path = EditorUtility.OpenFilePanel("Fichero .klc", KlcFolder, "klc");
        if (string.IsNullOrEmpty(path)) return;
        string name = Path.GetFileNameWithoutExtension(path);
        var asset = LoadOrCreate(name);
        KlcParser.Parse(Read(path), asset.data);
        if (string.IsNullOrEmpty(asset.data.displayName)) asset.data.displayName = name;
        Save(asset);
        Selection.activeObject = asset;
    }

    public static void ImportAll()
    {
        foreach (var b in Builtin)
        {
            var asset = LoadOrCreate(b.name);
            var d = asset.data;
            d.displayName = b.name;
            d.order = b.order;
            d.systemLanguage = b.systemLanguage;
            d.template = b.template;
            d.composer = b.composer;
            d.isRtl = b.rtl;
            KlcParser.Parse(Read(Path.Combine(KlcFolder, b.klc + ".klc")), d);
            if (b.composer == ComposerKind.Hangul) DubeolsikTable.ApplyTo(d);
            Save(asset);
            Debug.Log($"[Klc] {b.name}: {d.keys.Count} teclas, {d.deadKeys.Count} muertas");
        }
        AssetDatabase.SaveAssets();
    }

    static string Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        bool utf16 = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;
        return utf16 ? Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2) : Encoding.UTF8.GetString(bytes);
    }

    static KeyboardLayout LoadOrCreate(string name)
    {
        string path = $"{LayoutsFolder}/{name}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<KeyboardLayout>(path);
        if (asset != null) return asset;
        if (!AssetDatabase.IsValidFolder(LayoutsFolder)) AssetDatabase.CreateFolder("Assets", "Layouts");
        asset = ScriptableObject.CreateInstance<KeyboardLayout>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void Save(KeyboardLayout asset)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }
}
