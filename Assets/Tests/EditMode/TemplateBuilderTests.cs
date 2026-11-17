using System.Linq;
using NUnit.Framework;
using Teclado.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TemplateBuilderTests
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    const string TmpFolder = "Assets/Tests/Tmp";

    [SetUp] public void SetUp()
    {
        EditorSceneManager.OpenScene(ScenePath);
        if (!AssetDatabase.IsValidFolder(TmpFolder)) AssetDatabase.CreateFolder("Assets/Tests", "Tmp");
    }

    [TearDown] public void TearDown()
    {
        AssetDatabase.DeleteAsset(TmpFolder);
        EditorSceneManager.OpenScene(ScenePath);
    }

    // GameObject.Find no encuentra objetos inactivos: se busca desde la raíz.
    static GameObject Source(string mode, string name) => TemplateBuilder.FindKeyboard(mode, name);

    static GameObject Build(string mode, string name, PhysicalTemplate t) =>
        TemplateBuilder.Build(Source(mode, name), t, $"{TmpFolder}/{t}_{mode}.prefab");

    static KeyView KeyAt(GameObject root, string row, int i) =>
        (row == "Numbers" ? root.transform.Find("Buttons/Normal/Numbers") : root.transform.Find("Buttons/Normal/Letters/" + row))
        .GetChild(i).GetComponent<KeyView>();

    [Test] public void ISO_ScancodesEnOrden()
    {
        var p = Build("ByCanvas", "QWERTY ESP", PhysicalTemplate.ISO);
        Assert.AreEqual(0x56, KeyAt(p, "Row 3", 0).scanCode);
        Assert.AreEqual(0x2B, KeyAt(p, "Row 2", 11).scanCode);
        Assert.AreEqual(0x29, KeyAt(p, "Numbers", 0).scanCode);
        Assert.NotNull(KeyAt(p, "Row 1", 0).label);
        Assert.IsTrue(p.GetComponent<KeyboardView>() != null);
        Assert.IsTrue(p.GetComponent<Keyboard>() == null);
        Assert.AreEqual(0, p.GetComponentsInChildren<KeyboardButton>(true).Length);
    }

    [Test] public void ANSI14_BarraEnNumeros()
    {
        var p = Build("ByRigidBodies", "JCUKEN", PhysicalTemplate.ANSI14);
        Assert.AreEqual(0x2B, KeyAt(p, "Numbers", 13).scanCode);
    }

    [Test] public void SinTeclasDeTilde()
    {
        var p = Build("ByRigidBodies", "QWERTY ESP", PhysicalTemplate.ISO);
        var banned = new[] { "closed", "open", "diaeresis", "circumflex", "circunflex", "virgulilla", "dieresis_accented" };
        foreach (Transform t in p.transform.Find("Buttons/Specials"))
            Assert.IsFalse(banned.Contains(t.name.ToLowerInvariant()), t.name);
    }

    [Test] public void Especiales_Recableadas()
    {
        var p = Build("ByCanvas", "QWERTY ESP", PhysicalTemplate.ISO);
        var view = p.GetComponent<KeyboardView>();
        var caps = p.transform.Find("Buttons/Specials/Caps");
        var events = caps.GetComponentsInChildren<CanvasButton>(true).SelectMany(b => new[] { b.onPress, b.onRelease })
            .Concat(caps.GetComponentsInChildren<ButtonVR>(true).SelectMany(b => new[] { b.onPress, b.onRelease, b.OnStay }));
        bool found = false;
        foreach (var e in events)
            for (int i = 0; i < e.GetPersistentEventCount(); i++)
            {
                Assert.AreNotEqual("CapsPressed", e.GetPersistentMethodName(i));
                if (e.GetPersistentMethodName(i) == "OnShift" && e.GetPersistentTarget(i) == view) found = true;
            }
        Assert.IsTrue(found, "Caps → KeyboardView.OnShift");
    }

    [Test] public void NoTocaElOriginal()
    {
        Build("ByCanvas", "QWERTY US", PhysicalTemplate.ANSI);
        Assert.IsTrue(Source("ByCanvas", "QWERTY US").GetComponent<Keyboard>() != null);
    }
}
