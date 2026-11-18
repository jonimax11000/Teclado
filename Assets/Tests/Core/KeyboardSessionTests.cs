using NUnit.Framework;
using Teclado.Core;
using static TestLayouts;

public class KeyboardSessionTests
{
    TextBuffer buffer;
    KeyboardSession s;

    [SetUp] public void SetUp()
    {
        buffer = new TextBuffer();
        s = new KeyboardSession(buffer);
        s.SetLayout(SpanishLike());
    }

    [Test] public void MuertaYLetra()
    {
        s.PressKey(Acute); s.PressKey(E);
        Assert.AreEqual("é", buffer.CommittedText);
    }

    [Test] public void MuertaMuestraPreedit()
    {
        s.PressKey(Acute);
        Assert.AreEqual("", buffer.CommittedText);
        Assert.AreEqual("´", buffer.Text);
    }

    [Test] public void ShiftEnMedioDeMuerta()
    {
        s.PressKey(Acute); s.Shift(0); s.PressKey(E);
        Assert.AreEqual("É", buffer.CommittedText);
    }

    [Test] public void ShiftUnSoloUso()
    {
        s.Shift(0); s.PressKey(E); s.PressKey(E);
        Assert.AreEqual("Ee", buffer.CommittedText);
    }

    [Test] public void Espacio_TrasMuerta()
    {
        s.PressKey(Acute); s.Space();
        Assert.AreEqual("´", buffer.CommittedText);
    }

    [Test] public void MuertaYFlecha_ConfirmaAcento()
    {
        s.ExternalTextChanged("ab");
        s.SetCaret(1);
        s.PressKey(Acute);
        s.Left();
        Assert.AreEqual("a´b", buffer.CommittedText);
        Assert.AreEqual("", buffer.Preedit);
        Assert.AreEqual(1, buffer.Caret);
    }

    [Test] public void MuertaYCambioDeLayout()
    {
        s.PressKey(Acute);
        s.SetLayout(DirectLike());
        Assert.AreEqual("´", buffer.CommittedText);
        Assert.AreEqual("", buffer.Preedit);
    }

    [Test] public void MuertaYSetCaret()
    {
        s.PressKey(Acute);
        s.SetCaret(0);
        Assert.AreEqual("´", buffer.CommittedText);
        Assert.AreEqual(0, buffer.Caret);
    }

    [Test] public void CambioExterno_CancelaComposicion()
    {
        s.PressKey(Acute);
        s.ExternalTextChanged("");
        Assert.AreEqual("", buffer.Text);
        s.PressKey(E);
        Assert.AreEqual("e", buffer.CommittedText);
    }

    [Test] public void Backspace_ConMuerta_SoloCancela()
    {
        s.PressKey(E); s.PressKey(Acute); s.Backspace();
        Assert.AreEqual("e", buffer.Text);
    }

    [Test] public void Backspace_SinComposicion_BorraBufer()
    {
        s.PressKey(E); s.PressKey(E); s.Backspace();
        Assert.AreEqual("e", buffer.CommittedText);
    }

    [Test] public void TeclaInexistente_NoHaceNada()
    {
        Assert.DoesNotThrow(() => s.PressKey(0x99));
        Assert.AreEqual("", buffer.Text);
    }

    [Test] public void Especiales_ConsumenUnSoloUso()
    {
        s.Shift(0); s.Space(); s.PressKey(E);
        Assert.AreEqual(" e", buffer.CommittedText);
    }

    [Test] public void SinLayout_NoRompe()
    {
        var empty = new KeyboardSession(new TextBuffer());
        Assert.DoesNotThrow(() => { empty.PressKey(E); empty.Space(); empty.Backspace(); empty.Left(); });
    }

    // Revisión final I2: cambiar de idioma suelta Shift/AltGr de un solo uso.
    [Test] public void CambioDeLayout_SueltaUnSoloUso()
    {
        s.AltGr(); s.Shift(0);
        s.SetLayout(DirectLike());
        Assert.IsFalse(s.State.AltGr);
        Assert.IsFalse(s.State.Shift);
    }

    [Test] public void TeclaSinTextoEnLaCapa_SueltaUnSoloUso()
    {
        s.AltGr();
        s.PressKey(E);   // AltGr+e no existe en SpanishLike
        Assert.IsFalse(s.State.AltGr);
        s.PressKey(E);
        Assert.AreEqual("e", buffer.CommittedText);
    }
}
