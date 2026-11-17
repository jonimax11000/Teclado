using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

public class KeyboardButtonTests
{
    TestRig rig;
    GameObject kbGo;

    [UnitySetUp] public IEnumerator SetUp() { rig = new TestRig(); yield return null; }
    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); Object.Destroy(kbGo); yield return null; }

    // Keyboard → tecla (KeyboardButton) → botón (CanvasButton + etiqueta TMP).
    IEnumerator Key(Keyboard.KeyboardType type, string[] normal, string[] caps, string alter, bool special, string label, System.Action<Keyboard, CanvasButton> act)
    {
        kbGo = new GameObject("Keyboard");
        var kb = kbGo.AddComponent<Keyboard>();
        kb.controlator = rig.Controlator;
        kb._Keyboardtype = type;
        var keyGo = new GameObject("key");
        keyGo.transform.SetParent(kbGo.transform, false);
        var key = keyGo.AddComponent<KeyboardButton>();
        key.normalForms = normal;
        key.CapForms = caps;
        key.alterForm = alter;
        key.special_letter = special;
        var btnGo = new GameObject("btn");
        btnGo.transform.SetParent(keyGo.transform, false);
        var btn = btnGo.AddComponent<CanvasButton>();
        btn.onPress = new UnityEvent();     // en escena los crea la deserialización
        btn.onRelease = new UnityEvent();
        btnGo.AddComponent<TextMeshProUGUI>().text = label;
        yield return null;   // Start de Keyboard, KeyboardButton y CanvasButton
        act(kb, btn);
        yield return null;
    }

    [UnityTest] public IEnumerator Arabe_TeclaLamAlef_InsertaDosCodePoints()
    {
        yield return Key(Keyboard.KeyboardType.Arabic, new[] { "لا" }, new[] { "لآ" }, "", false, "لا", (kb, b) => b.onRelease.Invoke());
        Assert.AreEqual("لا", rig.Field.text);
    }

    [UnityTest] public IEnumerator Arabe_Shift_UsaCapForms()
    {
        yield return Key(Keyboard.KeyboardType.Arabic, new[] { "لا" }, new[] { "لآ" }, "", false, "لا", (kb, b) => { kb.CapsPressed(); b.onRelease.Invoke(); });
        Assert.AreEqual("لآ", rig.Field.text);
    }

    [UnityTest] public IEnumerator Arabe_LetraSimple()
    {
        yield return Key(Keyboard.KeyboardType.Arabic, new[] { "ض" }, new[] { "َ" }, "", false, "ض", (kb, b) => b.onRelease.Invoke());
        Assert.AreEqual("ض", rig.Field.text);
    }

    [UnityTest] public IEnumerator Especial_DeVariosCaracteres_NoSeConecta()
    {
        yield return Key(Keyboard.KeyboardType.Arabic, new string[0], new string[0], "", true, "Space", (kb, b) => b.onRelease.Invoke());
        Assert.AreEqual("", rig.Field.text);
    }

    [UnityTest] public IEnumerator Default_LetraSimple_SinCambios()
    {
        yield return Key(Keyboard.KeyboardType.Default, new[] { "a" }, new[] { "A" }, "a", false, "a", (kb, b) => b.onRelease.Invoke());
        Assert.AreEqual("a", rig.Field.text);
    }
}
