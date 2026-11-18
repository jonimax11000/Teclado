namespace Teclado.Core
{
    // Scancodes de cada fila de las plantillas físicas, de izquierda a derecha (Numbers, Row 1, Row 2, Row 3).
    public static class TemplateScancodes
    {
        static readonly int[] Numbers = { 0x29, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D };
        static readonly int[] Row1 = { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B };
        static readonly int[] Row2 = { 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28 };
        static readonly int[] Row3 = { 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35 };
        const int Backslash = 0x2B, Iso102 = 0x56;

        public static int[][] Rows(PhysicalTemplate t)
        {
            switch (t)
            {
                case PhysicalTemplate.ISO: return new[] { Numbers, Row1, Append(Row2, Backslash), Prepend(Iso102, Row3) };
                case PhysicalTemplate.ANSI: return new[] { Numbers, Append(Row1, Backslash), Row2, Row3 };
                default: return new[] { Append(Numbers, Backslash), Row1, Row2, Row3 };
            }
        }

        static int[] Append(int[] a, int x) { var r = new int[a.Length + 1]; a.CopyTo(r, 0); r[a.Length] = x; return r; }
        static int[] Prepend(int x, int[] a) { var r = new int[a.Length + 1]; r[0] = x; a.CopyTo(r, 1); return r; }
    }
}
