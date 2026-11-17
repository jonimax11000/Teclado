using NUnit.Framework;
using Teclado.Core;

public class HangulComposerTests
{
    IComposer c;
    string committed, preedit;

    [SetUp] public void SetUp() { c = new HangulComposer(); committed = preedit = ""; }

    void Type(string jamo)
    {
        foreach (char j in jamo)
        {
            var r = c.Process(new KeyStroke(0, j.ToString(), false));
            committed += r.Commit;
            preedit = r.Preedit;
        }
    }

    string All => committed + preedit;

    [Test] public void Hangeul() { Type("ㅎㅏㄴㄱㅡㄹ"); Assert.AreEqual("한글", All); }
    [Test] public void GaGa() { Type("ㄱㅏㄱㅏ"); Assert.AreEqual("가가", All); }
    [Test] public void DalGa() { Type("ㄷㅏㄹㄱㅏ"); Assert.AreEqual("달가", All); }
    [Test] public void Wa() { Type("ㅇㅗㅏ"); Assert.AreEqual("와", All); }
    [Test] public void DobleFinal() { Type("ㄷㅏㄹㄱ"); Assert.AreEqual("", committed); Assert.AreEqual("닭", preedit); }

    [Test] public void Backspace_Descompone()
    {
        Type("ㅎㅏㄴ");
        Assert.IsTrue(c.TryBackspace(out var r)); Assert.AreEqual("하", r.Preedit);
        Assert.IsTrue(c.TryBackspace(out r)); Assert.AreEqual("ㅎ", r.Preedit);
        Assert.IsTrue(c.TryBackspace(out r)); Assert.AreEqual("", r.Preedit);
        Assert.IsFalse(c.TryBackspace(out _));
    }

    [Test] public void BackspaceTrasConfirmar()
    {
        Type("ㅎㅏㄴ");
        Assert.AreEqual("한", c.Flush());
        Assert.IsFalse(c.TryBackspace(out _));
    }

    [Test] public void BackspaceTrasSepararFinal()
    {
        Type("ㄱㅏㄱㅏ");                       // 가 confirmado, 가 en preedit
        Assert.IsTrue(c.TryBackspace(out var r));
        Assert.AreEqual("ㄱ", r.Preedit);
    }

    [Test] public void NoJamo_Confirma()
    {
        Type("ㅎㅏ");
        var r = c.Process(new KeyStroke(0, "1", false));
        Assert.AreEqual("하1", r.Commit);
        Assert.AreEqual("", r.Preedit);
    }

    [Test] public void ConsonantesSolas() { Type("ㄱㄱ"); Assert.AreEqual("ㄱ", committed); Assert.AreEqual("ㄱ", preedit); }
    [Test] public void DdNoEsFinal() { Type("ㄱㅏㄸ"); Assert.AreEqual("가", committed); Assert.AreEqual("ㄸ", preedit); }
    [Test] public void VocalSola() { Type("ㅏ"); Assert.AreEqual("ㅏ", preedit); }
    [Test] public void Cancel_Descarta() { Type("ㅎㅏ"); c.Cancel(); Assert.AreEqual("", c.Flush()); }
    [Test] public void Factory_Hangul() => Assert.IsInstanceOf<HangulComposer>(ComposerFactory.Create(new LayoutData { composer = ComposerKind.Hangul }));
}
