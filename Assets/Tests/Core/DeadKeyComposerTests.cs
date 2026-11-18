using NUnit.Framework;
using Teclado.Core;

public class DeadKeyComposerTests
{
    IComposer c;
    string committed;

    [SetUp] public void SetUp() { c = new DeadKeyComposer(TestLayouts.SpanishLike()); committed = ""; }

    static KeyStroke K(string text, bool dead = false) => new KeyStroke(0, text, dead);
    ComposeResult P(string text, bool dead = false) { var r = c.Process(K(text, dead)); committed += r.Commit; return r; }

    [Test] public void Acento_e() { P("´", true); P("e"); Assert.AreEqual("é", committed); }
    [Test] public void Acento_Espacio() { P("´", true); P(" "); Assert.AreEqual("´", committed); }
    [Test] public void Acento_Consonante() { P("´", true); P("t"); Assert.AreEqual("´t", committed); }
    [Test] public void Dieresis_u() { P("¨", true); P("u"); Assert.AreEqual("ü", committed); }
    [Test] public void Circunflejo_a() { P("^", true); P("a"); Assert.AreEqual("â", committed); }
    [Test] public void Grave_a() { P("`", true); P("a"); Assert.AreEqual("à", committed); }

    [Test] public void MuertaMuestraPreedit()
    {
        var r = P("´", true);
        Assert.AreEqual("", r.Commit);
        Assert.AreEqual("´", r.Preedit);
    }

    // Revisión final M1: en Windows, muerta + muerta sin entrada escribe las dos y no deja nada pendiente.
    [Test] public void DosMuertas()
    {
        P("´", true);
        var r = P("¨", true);
        Assert.AreEqual("´¨", r.Commit);
        Assert.AreEqual("", r.Preedit);
    }

    [Test] public void Backspace_Cancela()
    {
        P("´", true);
        Assert.IsTrue(c.TryBackspace(out var r));
        Assert.AreEqual("", r.Preedit);
        Assert.IsFalse(c.TryBackspace(out _));
    }

    [Test] public void Flush_ConfirmaAcento()
    {
        P("´", true);
        Assert.AreEqual("´", c.Flush());
        Assert.AreEqual("", c.Flush());
    }

    [Test] public void Cancel_Descarta() { P("´", true); c.Cancel(); Assert.AreEqual("", c.Flush()); }

    [Test] public void SinMuerta_Pasa() { P("e"); Assert.AreEqual("e", committed); }

    [Test] public void Directo_Pasa()
    {
        var d = new DirectComposer();
        var r = d.Process(K("ж"));
        Assert.AreEqual("ж", r.Commit);
        Assert.AreEqual("", r.Preedit);
        Assert.IsFalse(d.TryBackspace(out _));
        Assert.AreEqual(0, d.Candidates.Count);
    }

    [Test] public void Factory_CreaSegunKind()
    {
        Assert.IsInstanceOf<DeadKeyComposer>(ComposerFactory.Create(TestLayouts.SpanishLike()));
        Assert.IsInstanceOf<DirectComposer>(ComposerFactory.Create(TestLayouts.DirectLike()));
    }
}
