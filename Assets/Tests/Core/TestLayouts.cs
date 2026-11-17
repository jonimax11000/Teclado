using Teclado.Core;

// Distribuciones mínimas construidas en código para los tests del núcleo.
public static class TestLayouts
{
    public const int Acute = 0x28, Grave = 0x1A, E = 0x12, U = 0x16, A = 0x1E, T = 0x14;

    static KeyDef Key(int sc, string b, string s, bool deadB = false, bool deadS = false, CapsMode caps = CapsMode.Affects)
        => new KeyDef { scanCode = sc, text = new[] { b, s, "", "" }, dead = new[] { deadB, deadS, false, false }, caps = caps };

    static DeadKeyTable Table(string dead, params string[] pairs)
    {
        var t = new DeadKeyTable { deadChar = dead };
        for (int i = 0; i < pairs.Length; i += 2) t.entries.Add(new DeadKeyEntry { from = pairs[i], to = pairs[i + 1] });
        return t;
    }

    public static LayoutData SpanishLike(string name = "Español")
    {
        var l = new LayoutData { displayName = name, composer = ComposerKind.DeadKey };
        l.keys.Add(Key(Acute, "´", "¨", true, true, CapsMode.None));
        l.keys.Add(Key(Grave, "`", "^", true, true, CapsMode.None));
        l.keys.Add(Key(E, "e", "E"));
        l.keys.Add(Key(U, "u", "U"));
        l.keys.Add(Key(A, "a", "A"));
        l.keys.Add(Key(T, "t", "T"));
        l.deadKeys.Add(Table("´", "a", "á", "e", "é", "E", "É", " ", "´"));
        l.deadKeys.Add(Table("¨", "u", "ü"));
        l.deadKeys.Add(Table("^", "a", "â"));
        l.deadKeys.Add(Table("`", "a", "à"));
        return l;
    }

    public static LayoutData DirectLike()
    {
        var l = new LayoutData { displayName = "Direct", composer = ComposerKind.Direct };
        l.keys.Add(Key(E, "e", "E"));
        return l;
    }
}
