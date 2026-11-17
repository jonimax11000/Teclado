using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class KeyboardControlatorTests
{
    TestRig rig;
    KeyboardControlator C => rig.Controlator;
    string Text => rig.Field.text;

    [UnitySetUp] public IEnumerator SetUp() { rig = new TestRig(); yield return null; }
    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); yield return null; }

    [Test] public void Escribir_SeGuardaLogico()
    {
        rig.Type("س", "ل", "ا", "م");
        Assert.AreEqual("سلام", Text);
        Assert.AreEqual(4, C.Caret);
    }

    [Test] public void LigaduraNoCorrompe()
    {
        rig.Type("س", "ل", "ا", "م");
        C.DeleteChar(); C.DeleteChar();
        Assert.AreEqual("سل", Text);
        C.LeftPressed();                 // RTL: avanza en lógico; ya está al final
        Assert.AreEqual(2, C.Caret);
        C.RightPressed();                // RTL: retrocede
        Assert.AreEqual(1, C.Caret);
        C.InsertChar("ي");
        Assert.AreEqual("سيل", Text);
    }

    [Test] public void FlechasEnLatin_SinCambios()
    {
        rig.Type("a", "b");
        C.LeftPressed();
        Assert.AreEqual(1, C.Caret);
        C.RightPressed();
        Assert.AreEqual(2, C.Caret);
    }

    [Test] public void SaltaLigadura()
    {
        rig.Type("س", "ل", "ا");
        C.SetCaret(2);
        Assert.AreEqual(3, C.Caret);
        C.RightPressed();                // RTL: retrocede desde 3, salta el interior (2)
        Assert.AreEqual(1, C.Caret);
    }

    [Test] public void Extremos()
    {
        C.DeleteChar(); C.LeftPressed(); C.RightPressed();
        Assert.AreEqual("", Text);
        Assert.AreEqual(0, C.Caret);
        rig.Type("a");
        C.LeftPressed(); C.LeftPressed(); C.DeleteChar();
        Assert.AreEqual("a", Text);
        Assert.AreEqual(0, C.Caret);
        C.SetCaret(99);
        Assert.AreEqual(1, C.Caret);
        C.SetCaret(-5);
        Assert.AreEqual(0, C.Caret);
    }

    [Test] public void DosCodePoints_BorraUnoCadaVez()
    {
        C.InsertChar("َّ");
        Assert.AreEqual(2, C.Caret);
        C.DeleteChar();
        Assert.AreEqual("ّ", Text);
    }

    [Test] public void CambioExterno_CursorAlFinal()
    {
        rig.Type("س", "ل");
        C.SetCaret(0);
        rig.Field.text = "abc";
        Assert.AreEqual(3, C.Caret);
        C.InsertChar("d");
        Assert.AreEqual("abcd", Text);
    }

    [Test] public void MixtoTrasCambiarIdioma()
    {
        rig.Type("س", "ل", "a");
        Assert.AreEqual("سلa", Text);
        Assert.AreEqual(3, C.Caret);
    }

    [Test] public void CaretChanged_SeLanza()
    {
        int n = 0;
        C.CaretChanged += () => n++;
        rig.Type("a");
        C.LeftPressed();
        Assert.GreaterOrEqual(n, 2);
    }
}
