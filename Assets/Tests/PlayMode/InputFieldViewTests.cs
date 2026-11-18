using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static TestLayouts;

public class InputFieldViewTests
{
    TestRig rig;
    [UnitySetUp] public IEnumerator SetUp() { rig = new TestRig(); rig.Controller.Session.SetLayout(SpanishLike()); yield return null; }
    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); yield return null; }

    [Test] public void Preedit_EnCampoNoEnCommitted()
    {
        rig.Controller.Session.PressKey(Acute);
        Assert.AreEqual("´", rig.Field.text);
        Assert.AreEqual("", rig.Controller.Buffer.CommittedText);
    }

    [UnityTest] public IEnumerator CambioExterno_SinPreeditFantasma()
    {
        rig.Controller.Session.PressKey(Acute);
        rig.Field.text = "";
        yield return null;
        Assert.AreEqual("", rig.Controller.Buffer.Preedit);
        rig.Controller.Session.PressKey(E);
        Assert.AreEqual("e", rig.Field.text);
    }

    [UnityTest] public IEnumerator SetTextWithoutNotify_NoRompe()
    {
        rig.Type("a", "b", "c");
        rig.Field.SetTextWithoutNotify("");
        yield return null;
        Assert.DoesNotThrow(() => rig.Controller.OnBackspace());
        rig.Controller.Session.PressKey(E);
        Assert.AreEqual("e", rig.Field.text);
    }

    [UnityTest] public IEnumerator PreeditView_SubrayaRango()
    {
        var go = new GameObject("Underline", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(rig.Field.textViewport, false);
        var view = rig.Field.gameObject.AddComponent<PreeditView>();
        view.input = rig.Input;
        view.underline = (RectTransform)go.transform;
        yield return null;
        rig.Controller.Session.PressKey(Acute);
        yield return null;
        Assert.Greater(view.underline.sizeDelta.x, 0f);
        Assert.IsTrue(go.GetComponent<Image>().enabled);
        rig.Controller.Session.PressKey(E);
        yield return null;
        Assert.IsFalse(go.GetComponent<Image>().enabled);
    }
}
