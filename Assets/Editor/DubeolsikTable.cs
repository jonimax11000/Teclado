using Teclado.Core;

// Distribución coreana Dubeolsik (KS X 5002): jamo de compatibilidad sobre las teclas de letras de EE. UU.
public static class DubeolsikTable
{
    static readonly (int sc, string normal, string shift)[] Keys =
    {
        (0x10, "ㅂ", "ㅃ"), (0x11, "ㅈ", "ㅉ"), (0x12, "ㄷ", "ㄸ"), (0x13, "ㄱ", "ㄲ"), (0x14, "ㅅ", "ㅆ"),
        (0x15, "ㅛ", "ㅛ"), (0x16, "ㅕ", "ㅕ"), (0x17, "ㅑ", "ㅑ"), (0x18, "ㅐ", "ㅒ"), (0x19, "ㅔ", "ㅖ"),
        (0x1E, "ㅁ", "ㅁ"), (0x1F, "ㄴ", "ㄴ"), (0x20, "ㅇ", "ㅇ"), (0x21, "ㄹ", "ㄹ"), (0x22, "ㅎ", "ㅎ"),
        (0x23, "ㅗ", "ㅗ"), (0x24, "ㅓ", "ㅓ"), (0x25, "ㅏ", "ㅏ"), (0x26, "ㅣ", "ㅣ"),
        (0x2C, "ㅋ", "ㅋ"), (0x2D, "ㅌ", "ㅌ"), (0x2E, "ㅊ", "ㅊ"), (0x2F, "ㅍ", "ㅍ"), (0x30, "ㅠ", "ㅠ"),
        (0x31, "ㅜ", "ㅜ"), (0x32, "ㅡ", "ㅡ"),
    };

    public static void ApplyTo(LayoutData layout)
    {
        foreach (var (sc, normal, shift) in Keys)
        {
            var k = layout.Find(sc);
            if (k == null) continue;
            k.text[(int)Layer.Base] = normal;
            k.text[(int)Layer.Shift] = shift;
            k.caps = CapsMode.None;
        }
    }
}
