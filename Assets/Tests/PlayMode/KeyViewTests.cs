using System.Collections;
using System.Linq;
using NUnit.Framework;
using Teclado.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using static TestLayouts;

public class KeyViewTests
{
    TestRig rig;
    GameObject kbGo;
    KeyboardView keyboard;

    [UnitySetUp] public IEnumerator SetUp() { rig = new TestRig(); yield return null; }
    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); Object.Destroy(kbGo); yield return null; }

    // KeyboardView → teclas (KeyView + CanvasButton + etiqueta), una por scancode.
    IEnumerator Build(LayoutData data, params int[] scancodes)
    {
        kbGo = new GameObject("Keyboard");
        kbGo.SetActive(false);
        keyboard = kbGo.AddComponent<KeyboardView>();
        var layout = ScriptableObject.CreateInstance<KeyboardLayout>();
        layout.data = data;
        keyboard.layout = layout;
        keyboard.controller = rig.Controller;
        foreach (int sc in scancodes)
        {
            var keyGo = new GameObject("key" + sc);
            keyGo.transform.SetParent(kbGo.transform, false);
            var btn = keyGo.AddComponent<CanvasButton>();
            btn.onPress = new UnityEvent();     // en escena los crea la deserialización
            btn.onRelease = new UnityEvent();
            var key = keyGo.AddComponent<KeyView>();
            key.scanCode = sc;
            key.label = keyGo.AddComponent<TextMeshProUGUI>();
        }
        rig.Controller.Session.SetLayout(data);
        kbGo.SetActive(true);
        yield return null;
    }

    KeyView Key(int sc) => kbGo.GetComponentsInChildren<KeyView>(true).Single(k => k.scanCode == sc);

    [UnityTest] public IEnumerator Etiqueta_SegunCapa()
    {
        yield return Build(SpanishLike(), E);
        Assert.AreEqual("e", Key(E).label.text);
        rig.Controller.OnShift();
        Assert.AreEqual("E", Key(E).label.text);
    }

    [UnityTest] public IEnumerator Muerta_MuestraAcento()
    {
        yield return Build(SpanishLike(), Acute);
        Assert.AreEqual("´", Key(Acute).label.text);
    }

    [UnityTest] public IEnumerator Marca_SobreCirculo()
    {
        var data = SpanishLike();
        data.keys.Add(new KeyDef { scanCode = 0x10, text = new[] { "َ", "", "", "" }, dead = new bool[4] });
        yield return Build(data, 0x10);
        Assert.AreEqual("◌َ", Key(0x10).label.text);
    }

    [UnityTest] public IEnumerator Arabe_LamAlefUnida()
    {
        var data = new LayoutData { composer = ComposerKind.Direct, isRtl = true };
        data.keys.Add(new KeyDef { scanCode = 0x30, text = new[] { "لا", "لآ", "", "" }, dead = new bool[4] });
        yield return Build(data, 0x30);
        Assert.IsTrue(Key(0x30).label.text.IndexOf('ﻻ') >= 0, "comparación ordinal");
        Key(0x30).GetComponent<CanvasButton>().onRelease.Invoke();
        Assert.AreEqual("لا", rig.Controller.Buffer.CommittedText);
    }

    [UnityTest] public IEnumerator TeclaNoDefinida_Desactiva()
    {
        yield return Build(SpanishLike(), E, 0x56);
        Assert.IsFalse(Key(0x56).gameObject.activeSelf);
        Assert.IsTrue(Key(E).gameObject.activeSelf);
    }

    [UnityTest] public IEnumerator CapaVacia_EtiquetaVaciaSinDesactivar()
    {
        yield return Build(SpanishLike(), E);
        rig.Controller.OnAltGr();
        Assert.AreEqual("", Key(E).label.text);
        Assert.IsTrue(Key(E).gameObject.activeSelf);
    }

    [UnityTest] public IEnumerator Pulsar_Escribe()
    {
        yield return Build(SpanishLike(), E);
        Key(E).GetComponent<CanvasButton>().onRelease.Invoke();
        Assert.AreEqual("e", rig.Controller.Buffer.CommittedText);
    }

    [UnityTest] public IEnumerator Desactivado_NoEscucha()
    {
        yield return Build(SpanishLike(), E);
        kbGo.SetActive(false);
        Key(E).GetComponent<CanvasButton>().onRelease.Invoke();
        Assert.AreEqual("", rig.Controller.Buffer.CommittedText);
    }

    [UnityTest] public IEnumerator Especiales_ReenvianAlControlador()
    {
        yield return Build(SpanishLike(), E);
        keyboard.OnShift();
        Key(E).GetComponent<CanvasButton>().onRelease.Invoke();
        keyboard.OnSpace();
        Key(E).GetComponent<CanvasButton>().onRelease.Invoke();
        keyboard.OnBackspace();
        Assert.AreEqual("E ", rig.Controller.Buffer.CommittedText);
    }
}
