using NUnit.Framework;
using Teclado.Core;

public class KlcParserTests
{
    // Columnas en orden no trivial (como KBDGR/KBDA2): 0 1 6 2 7
    const string Header = "KBD\tTEST\t\"Test\"\n\nSHIFTSTATE\n\n0\t// Column 4\n1\t// Column 5\n6\t// Column 6\n2\t// Column 7\n7\t// Column 8\n\nLAYOUT\t\t// dead '@'\n\n//SC\tVK_\t\tCap\t0\t1\t6\t2\t7\n";

    static LayoutData Parse(string layoutLines, string tail = "") => KlcParser.Parse(Header + layoutLines + "\n" + tail + "\nENDKBD\n");

    [Test] public void Capas()
    {
        var k = Parse("10\tQ\t\t1\tq\tQ\t0040\t-1\t-1\t\t// q").Find(0x10);
        Assert.AreEqual("q", k.text[(int)Layer.Base]);
        Assert.AreEqual("Q", k.text[(int)Layer.Shift]);
        Assert.AreEqual("@", k.text[(int)Layer.AltGr]);
        Assert.AreEqual("", k.text[(int)Layer.ShiftAltGr]);
        Assert.AreEqual(CapsMode.Affects, k.caps);
    }

    [Test] public void Hex_YColumnasQueFaltan()
    {
        var k = Parse("29\tOEM_5\t\t0\t00ba\t00aa\t005c\t\t\t// º").Find(0x29);
        Assert.AreEqual("º", k.text[0]);
        Assert.AreEqual("ª", k.text[1]);
        Assert.AreEqual("\\", k.text[2]);
        Assert.AreEqual("", k.text[3]);
        Assert.AreEqual(CapsMode.None, k.caps);
    }

    [Test] public void Muerta()
    {
        var k = Parse("28\tOEM_7\t\t0\t00b4@\t00a8@\t007b\t-1\t-1").Find(0x28);
        Assert.AreEqual("´", k.text[0]);
        Assert.IsTrue(k.dead[0]);
        Assert.IsTrue(k.dead[1]);
        Assert.IsFalse(k.dead[2]);
    }

    [Test] public void ControlIgnorado()
    {
        var k = Parse("1a\tOEM_4\t\t0\t0060\t005e\t005b\t001b\t-1").Find(0x1A);
        Assert.AreEqual("[", k.text[2]);
    }

    [Test] public void Deadkey()
    {
        var l = Parse("28\tOEM_7\t\t0\t00b4@\t00a8@\t-1\t-1\t-1",
            "DEADKEY\t00b4\t// AGUDO\n\n0061\t00e1\t// a -> á\n0020\t00b4\t// espacio\n\nDEADKEY\t00a8\n\n0075\t00fc\n\nKEYNAME\n\n01\tEsc\n");
        Assert.IsTrue(l.FindDead("´").TryGet("a", out var to)); Assert.AreEqual("á", to);
        Assert.IsTrue(l.FindDead("´").TryGet(" ", out to)); Assert.AreEqual("´", to);
        Assert.IsTrue(l.FindDead("¨").TryGet("u", out to)); Assert.AreEqual("ü", to);
    }

    [Test] public void Ligadura()
    {
        var l = Parse("30\tB\t\t0\t%%\t%%\t-1\t-1\t-1",
            "LIGATURE\n\n//VK_\tMod#\tChar0\tChar1\n\nB\t0\t0644\t0627\t\t// lam alef\nB\t1\t0644\t0622\n");
        var k = l.Find(0x30);
        Assert.AreEqual("لا", k.text[0]);
        Assert.AreEqual("لآ", k.text[1]);
    }

    [Test] public void ConservaMetadatos()
    {
        var into = new LayoutData { displayName = "Español", order = 1, template = PhysicalTemplate.ISO, composer = ComposerKind.DeadKey, systemLanguage = "Spanish" };
        var l = KlcParser.Parse(Header + "12\tE\t\t1\te\tE\t-1\t-1\t-1\nENDKBD\n", into);
        Assert.AreSame(into, l);
        Assert.AreEqual("Español", l.displayName);
        Assert.AreEqual(1, l.order);
        Assert.AreEqual(ComposerKind.DeadKey, l.composer);
        Assert.AreEqual("e", l.Find(0x12).text[0]);
    }

    [Test] public void Reimportar_NoDuplica()
    {
        var into = new LayoutData();
        string text = Header + "12\tE\t\t1\te\tE\t-1\t-1\t-1\nENDKBD\n";
        KlcParser.Parse(text, into);
        KlcParser.Parse(text, into);
        Assert.AreEqual(1, into.keys.Count);
    }

    // U+20C1 (riyal saudí, Unicode 17): el runtime no lo reconoce y ninguna fuente lo dibuja.
    [Test] public void NoAsignado_Ignorado()
    {
        var k = Parse("1f\tS\t\t1\ts\tS\t20c1\t-1\t-1").Find(0x1F);
        Assert.AreEqual("", k.text[(int)Layer.AltGr]);
        Assert.AreEqual("s", k.text[0]);
    }
}
