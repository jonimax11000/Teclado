using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ArabicKeyboardSetupTests
{
    const string ScenePath = "Assets/Scenes/Pruebas VR.unity";
    Scene scene;
    KeyboardControlator controlator;

    [SetUp] public void SetUp()
    {
        scene = EditorSceneManager.OpenScene(ScenePath);
        ArabicKeyboardSetup.Apply(scene);
        controlator = Object.FindAnyObjectByType<KeyboardControlator>(FindObjectsInactive.Include);
    }

    // Reabrir sin guardar descarta los cambios del test.
    [TearDown] public void TearDown() => EditorSceneManager.OpenScene(ScenePath);

    GameObject[] ArabicRoots()
    {
        var so = new SerializedObject(controlator);
        return new[] { "RigidBodies_Keyboards", "Canvas_Keyboards" }
            .Select(p => (GameObject)so.FindProperty(p).GetArrayElementAtIndex(7).objectReferenceValue).ToArray();
    }

    KeyboardButton KeyAt(GameObject root, string row, int i)
    {
        var rowT = row == "Numbers" ? root.transform.Find("Buttons/Normal/Numbers") : root.transform.Find("Buttons/Normal/Letters/" + row);
        return rowT.GetChild(i).GetComponentInChildren<KeyboardButton>(true);
    }

    [Test] public void TecladosArabes_SinLatinYTipoArabe()
    {
        foreach (var root in ArabicRoots())
        {
            Assert.AreEqual(Keyboard.KeyboardType.Arabic, root.GetComponent<Keyboard>()._Keyboardtype, root.name);
            foreach (var k in root.GetComponentsInChildren<KeyboardButton>(true).Where(k => !k.special_letter))
                Assert.IsFalse(Regex.IsMatch(k.normalForms[0], "[A-Za-zñçÑÇ]"), $"{root.name}/{k.name}: {k.normalForms[0]}");
        }
    }

    [Test] public void TeclasConcretas()
    {
        foreach (var root in ArabicRoots())
        {
            var la = KeyAt(root, "Row 3", 5);
            Assert.AreEqual("لا", la.normalForms[0]);
            Assert.AreEqual("لآ", la.CapForms[0]);
            var thal = KeyAt(root, "Row 2", 11);
            Assert.AreEqual("ذ", thal.normalForms[0]);
            Assert.AreEqual("ّ", thal.CapForms[0]);
            Assert.AreEqual(">", KeyAt(root, "Numbers", 0).normalForms[0]);
            Assert.AreEqual("َّ", KeyAt(root, "Row 1", 0).alterForm);
            Assert.AreEqual("ض", KeyAt(root, "Row 1", 0).GetComponentInChildren<TextMeshProUGUI>(true).text);
        }
    }

    [Test] public void SinTeclasDeAcentos()
    {
        var banned = new[] { "closed", "open", "diaeresis", "circumflex", "circunflex", "virgulilla" };
        foreach (var root in ArabicRoots())
            foreach (Transform t in root.transform.Find("Buttons/Specials"))
                Assert.IsFalse(banned.Contains(t.name.ToLowerInvariant()), t.name);
    }

    [Test] public void CampoUsaRtl()
    {
        var field = controlator.field;
        Assert.IsInstanceOf<RTLTextMeshPro>(field.textComponent);
        var rtl = (RTLTextMeshPro)field.textComponent;
        Assert.IsFalse(rtl.Farsi);
        Assert.IsTrue(rtl.PreserveNumbers);
        var view = field.GetComponent<CaretView>();
        Assert.NotNull(view);
        Assert.NotNull(view.caret);
        Assert.AreEqual(controlator, view.controlator);
        Assert.AreEqual(controlator, field.GetComponent<FieldPointer>().controlator);
    }

    [Test] public void DropdownTieneArabe()
    {
        var so = new SerializedObject(controlator);
        var dd = ((GameObject)so.FindProperty("Languaje_selector_Dropdown").objectReferenceValue).GetComponent<TMP_Dropdown>();
        Assert.AreEqual(8, dd.options.Count);
        Assert.AreEqual("العربية", dd.options[7].text);
        Assert.IsInstanceOf<RTLTextMeshPro>(dd.captionText);
        Assert.IsInstanceOf<RTLTextMeshPro>(dd.itemText);
    }

    [Test] public void SinFormadorDeSilabas()
    {
        Assert.IsFalse(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
            .Any(m => m != null && m.GetType().Name == "FormadorDeSilabasUnity"));
    }

    [Test] public void FuenteArabeEnFallbackGlobal_YMultiAtlas()
    {
        var arabic = TMP_Settings.fallbackFontAssets.FirstOrDefault(f => f != null && f.name == "NotoSansArabic SDF");
        Assert.NotNull(arabic);
        Assert.IsTrue(arabic.HasCharacter('ﻻ', false, true), "ligadura لا");
        Assert.IsTrue(controlator.field.textComponent.font.isMultiAtlasTexturesEnabled);
    }

    [Test] public void Idempotente()
    {
        ArabicKeyboardSetup.Apply(scene);
        TecladosArabes_SinLatinYTipoArabe();
        TeclasConcretas();
        DropdownTieneArabe();
        Assert.AreEqual(1, controlator.field.textViewport.Cast<Transform>().Count(t => t.name == "Caret"));
        Assert.AreEqual(1, TMP_Settings.fallbackFontAssets.Count(f => f != null && f.name == "NotoSansArabic SDF"));
    }
}
