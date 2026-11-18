using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyboardGeneratorTests
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    Scene scene;

    [SetUp] public void SetUp()
    {
        scene = EditorSceneManager.OpenScene(ScenePath);
        KeyboardGenerator.Apply(scene);
    }

    // Reabrir sin guardar descarta los cambios del test.
    [TearDown] public void TearDown() => EditorSceneManager.OpenScene(ScenePath);

    T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    Transform Root(string mode) => scene.GetRootGameObjects().First(g => g.name == "Keyboards").transform.Find(mode);

    KeyboardView[] Views(string mode) => Root(mode).Cast<Transform>().Select(t => t.GetComponent<KeyboardView>()).Where(v => v != null).ToArray();

    static readonly string[] Names = { "English", "Español", "Français", "Deutsch", "Русский", "한국어", "Ελληνικά", "العربية" };

    [Test] public void OchoIdiomas_DosModos()
    {
        foreach (var mode in new[] { "ByRigidBodies", "ByCanvas" })
            CollectionAssert.AreEquivalent(Names, Views(mode).Select(v => v.layout.data.displayName).ToArray(), mode);
        // Las clases antiguas se borran en esta tarea: se buscan por nombre.
        var old = All<MonoBehaviour>().Where(m => m != null && (m.GetType().Name == "Keyboard" || m.GetType().Name == "KeyboardControlator" || m.GetType().Name == "KeyboardButton"));
        Assert.AreEqual(0, old.Count());
        Assert.AreEqual(0, All<Transform>().Sum(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)), "sin scripts perdidos");
    }

    [Test] public void PlantillaCorrecta()
    {
        var es = Views("ByCanvas").Single(v => v.layout.data.displayName == "Español");
        Assert.IsTrue(es.GetComponentsInChildren<KeyView>(true).Any(k => k.scanCode == 0x56 && k.gameObject.activeSelf));
        var en = Views("ByCanvas").Single(v => v.layout.data.displayName == "English");
        Assert.IsFalse(en.GetComponentsInChildren<KeyView>(true).Any(k => k.scanCode == 0x56));
    }

    [Test] public void SinTeclasDeTilde()
    {
        var banned = new[] { "closed", "open", "diaeresis", "circumflex", "circunflex", "virgulilla", "dieresis_accented" };
        foreach (var v in Views("ByRigidBodies").Concat(Views("ByCanvas")))
            foreach (Transform t in v.transform.Find("Buttons/Specials"))
                Assert.IsFalse(banned.Contains(t.name.ToLowerInvariant()), $"{v.name}/{t.name}");
    }

    [Test] public void Selector_8EnOrden()
    {
        var sel = All<LanguageSelector>().Single();
        CollectionAssert.AreEqual(Names, sel.entries.Select(e => e.layout.data.displayName).ToArray());
        Assert.IsTrue(sel.entries.All(e => e.rigid != null && e.canvas != null));
        Assert.NotNull(sel.dropdown);
        Assert.NotNull(sel.controller);
        var dd = sel.dropdown;
        Assert.IsTrue(Enumerable.Range(0, dd.onValueChanged.GetPersistentEventCount())
            .Any(i => dd.onValueChanged.GetPersistentMethodName(i) == "Select" && dd.onValueChanged.GetPersistentTarget(i) == sel));
    }

    [Test] public void CampoMontado()
    {
        var input = All<InputFieldView>().Single();
        Assert.NotNull(input.field);
        Assert.NotNull(input.controller);
        Assert.IsTrue(input.TryGetComponent(out CaretView caret) && caret.input == input && caret.caret != null);
        Assert.IsTrue(input.TryGetComponent(out FieldPointer pointer) && pointer.input == input);
        Assert.IsTrue(input.TryGetComponent(out PreeditView pre) && pre.input == input && pre.underline != null);
    }

    [Test] public void ModoRigidoCanvas_Recableado()
    {
        var sw = All<KeyboardModeSwitcher>().Single();
        Assert.NotNull(sw.rigidRoot);
        Assert.NotNull(sw.canvasRoot);
    }

    [Test] public void EspecialesConTarget()
    {
        var es = Views("ByCanvas").Single(v => v.layout.data.displayName == "Español");
        Assert.AreSame(All<KeyboardController>().Single(), es.controller);
        var caps = es.transform.Find("Buttons/Specials/Caps").GetComponentsInChildren<CanvasButton>(true);
        Assert.IsTrue(caps.Any(b => Enumerable.Range(0, b.onRelease.GetPersistentEventCount()).Concat(Enumerable.Range(0, 0))
            .Any(i => b.onRelease.GetPersistentMethodName(i) == "OnShift" && b.onRelease.GetPersistentTarget(i) == es))
            || caps.Any(b => Enumerable.Range(0, b.onPress.GetPersistentEventCount())
            .Any(i => b.onPress.GetPersistentMethodName(i) == "OnShift" && b.onPress.GetPersistentTarget(i) == es)));
    }

    [Test] public void HuecosSinDefinir_Desactivados()
    {
        var ru = Views("ByRigidBodies").Single(v => v.layout.data.displayName == "Русский");
        foreach (var k in ru.GetComponentsInChildren<KeyView>(true))
        {
            var def = ru.layout.data.Find(k.scanCode);
            bool defined = def != null && def.text.Any(t => !string.IsNullOrEmpty(t));
            Assert.AreEqual(defined, k.gameObject.activeSelf, $"0x{k.scanCode:X2}");
        }
    }

    [Test] public void Idempotente()
    {
        KeyboardGenerator.Apply(scene);
        Assert.AreEqual(8, Views("ByRigidBodies").Length);
        Assert.AreEqual(8, Views("ByCanvas").Length);
        Assert.AreEqual(8, All<LanguageSelector>().Single().entries.Count);
        Assert.AreEqual(1, All<InputFieldView>().Single().field.textViewport.Cast<Transform>().Count(t => t.name == "Underline"));
        Assert.AreEqual(1, All<KeyboardController>().Length);
    }
}
