using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Teclado.Core;

public class TextBufferTests
{
    TextBuffer b;

    [SetUp] public void SetUp() => b = new TextBuffer(s => s.Any(c => c >= '؀' && c <= 'ۿ'));

    void Type(params string[] ks) { foreach (var k in ks) b.Commit(k); }

    [Test] public void Escribir_SeGuardaLogico()
    {
        Type("س", "ل", "ا", "م");
        Assert.AreEqual("سلام", b.CommittedText);
        Assert.AreEqual(4, b.Caret);
    }

    [Test] public void LigaduraNoCorrompe()
    {
        Type("س", "ل", "ا", "م");
        b.DeleteBackward(); b.DeleteBackward();
        Assert.AreEqual("سل", b.CommittedText);
        b.MoveVisual(left: true);            // RTL: avanza; ya está al final
        Assert.AreEqual(2, b.Caret);
        b.MoveVisual(left: false);           // RTL: retrocede
        Assert.AreEqual(1, b.Caret);
        b.Commit("ي");
        Assert.AreEqual("سيل", b.CommittedText);
    }

    [Test] public void FlechasEnLatin()
    {
        Type("a", "b");
        b.MoveVisual(left: true);
        Assert.AreEqual(1, b.Caret);
        b.MoveVisual(left: false);
        Assert.AreEqual(2, b.Caret);
    }

    [Test] public void SaltaLigadura()
    {
        Type("س", "ل", "ا");
        b.SetCaret(2);
        Assert.AreEqual(3, b.Caret);
        b.MoveVisual(left: false);
        Assert.AreEqual(1, b.Caret);
    }

    [Test] public void Extremos()
    {
        Assert.IsFalse(b.DeleteBackward());
        b.MoveVisual(true); b.MoveVisual(false);
        Assert.AreEqual("", b.CommittedText);
        Assert.AreEqual(0, b.Caret);
        Type("a");
        b.MoveVisual(true); b.MoveVisual(true);
        Assert.IsFalse(b.DeleteBackward());
        Assert.AreEqual("a", b.CommittedText);
        b.SetCaret(99); Assert.AreEqual(1, b.Caret);
        b.SetCaret(-5); Assert.AreEqual(0, b.Caret);
    }

    [Test] public void DosCodePoints_BorraUnoCadaVez()
    {
        b.Commit("َّ");
        Assert.AreEqual(2, b.Caret);
        b.DeleteBackward();
        Assert.AreEqual("ّ", b.CommittedText);
    }

    [Test] public void InsertarAntesDeAlef_NoQuedaDentroDeLigadura()
    {
        Type("س", "ا", "م");
        b.SetCaret(1);
        b.Commit("ل");
        Assert.AreEqual("سلام", b.CommittedText);
        Assert.AreEqual(3, b.Caret);
        b.Commit("ي");
        Assert.AreEqual("سلايم", b.CommittedText);
    }

    [Test] public void BorrarFormaLigadura_CursorFuera()
    {
        Type("ل", "ب", "ا");
        b.SetCaret(2);
        b.DeleteBackward();
        Assert.AreEqual("لا", b.CommittedText);
        Assert.AreEqual(2, b.Caret);
    }

    [Test] public void CursorObsoleto_NoRompe()
    {
        b.ReplaceAll("abc");
        Assert.AreEqual(3, b.Caret);
        b.ReplaceAll("");
        Assert.DoesNotThrow(() => Assert.IsFalse(b.DeleteBackward()));
        b.Commit("x");
        Assert.AreEqual("x", b.CommittedText);
    }

    [Test] public void Preedit_NoEstaEnCommitted()
    {
        b.Commit("a");
        b.SetPreedit("´");
        Assert.AreEqual("a´", b.Text);
        Assert.AreEqual("a", b.CommittedText);
        Assert.AreEqual(1, b.PreeditStart);
    }

    [Test] public void Committed_SoloTextoConfirmado()
    {
        var got = new List<string>();
        b.Committed += got.Add;
        b.Commit("a");
        b.SetPreedit("´");
        b.SetPreedit("");
        b.Commit("");
        CollectionAssert.AreEqual(new[] { "a" }, got);
    }

    [Test] public void ReplaceAll_LimpiaPreedit()
    {
        b.SetPreedit("´");
        b.ReplaceAll("hola");
        Assert.AreEqual("", b.Preedit);
        Assert.AreEqual("hola", b.Text);
        Assert.AreEqual(4, b.Caret);
    }

    [Test] public void Changed_SeLanza()
    {
        int n = 0;
        b.Changed += () => n++;
        b.Commit("a"); b.SetPreedit("´"); b.DeleteBackward(); b.MoveVisual(true);
        Assert.GreaterOrEqual(n, 4);
    }
}
