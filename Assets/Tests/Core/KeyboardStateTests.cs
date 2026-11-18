using NUnit.Framework;
using Teclado.Core;

public class KeyboardStateTests
{
    static KeyDef Letter => new KeyDef { scanCode = 0x12, caps = CapsMode.Affects };
    static KeyDef Digit => new KeyDef { scanCode = 0x02, caps = CapsMode.None };

    [Test] public void Resolve_Base() => Assert.AreEqual(Layer.Base, new KeyboardState().Resolve(Letter));

    [Test] public void Shift_UnSoloUso()
    {
        var s = new KeyboardState();
        s.TapShift(0);
        Assert.AreEqual(Layer.Shift, s.Resolve(Letter));
        s.ConsumeOneShot();
        Assert.AreEqual(Layer.Base, s.Resolve(Letter));
    }

    [Test] public void DobleToque_BloqMayus()
    {
        var s = new KeyboardState();
        s.TapShift(0); s.TapShift(0.2);
        Assert.IsTrue(s.CapsLock);
        Assert.IsFalse(s.Shift);
    }

    [Test] public void TripleToque_SinBloqueo()
    {
        var s = new KeyboardState();
        s.TapShift(0); s.TapShift(0.2); s.TapShift(0.3);
        Assert.IsFalse(s.CapsLock);
        Assert.IsFalse(s.Shift);
    }

    [Test] public void ToqueLentoNoBloquea()
    {
        var s = new KeyboardState();
        s.TapShift(0); s.TapShift(1.0);
        Assert.IsFalse(s.CapsLock);
        Assert.IsFalse(s.Shift);
    }

    [Test] public void BloqMayus_RespetaCap()
    {
        var s = new KeyboardState();
        s.TapShift(0); s.TapShift(0.2);
        Assert.AreEqual(Layer.Shift, s.Resolve(Letter));
        Assert.AreEqual(Layer.Base, s.Resolve(Digit));
    }

    [Test] public void BloqMayusMasShift_LoDesactiva()
    {
        var s = new KeyboardState();
        s.TapShift(0); s.TapShift(0.2);
        s.TapShift(5);
        Assert.IsFalse(s.CapsLock);
        Assert.IsFalse(s.Shift);
    }

    [Test] public void AltGr()
    {
        var s = new KeyboardState();
        s.TapAltGr();
        Assert.AreEqual(Layer.AltGr, s.Resolve(Letter));
        s.TapShift(0);
        Assert.AreEqual(Layer.ShiftAltGr, s.Resolve(Letter));
        s.TapShift(3); s.TapShift(3.1);   // Bloq Mayús
        s.ConsumeOneShot();
        Assert.IsFalse(s.AltGr);
        Assert.IsFalse(s.Shift);
        Assert.IsTrue(s.CapsLock);
    }

    [Test] public void Changed_SeLanza()
    {
        var s = new KeyboardState();
        int n = 0;
        s.Changed += () => n++;
        s.TapShift(0); s.TapAltGr(); s.ConsumeOneShot();
        Assert.AreEqual(3, n);
    }

    // Revisión final I1: en Windows, Bloq Mayús (Cap=1) no afecta a las capas AltGr; solo Cap con bit 4.
    [Test] public void BloqMayus_NoAfectaAltGr_SalvoCapAltGr()
    {
        var s = new KeyboardState();
        s.TapShift(0); s.TapShift(0.2);
        s.TapAltGr();
        Assert.AreEqual(Layer.AltGr, s.Resolve(Letter));
        Assert.AreEqual(Layer.ShiftAltGr, s.Resolve(new KeyDef { caps = CapsMode.AffectsAltGr }));
    }
}
