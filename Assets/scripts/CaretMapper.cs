using System.Collections.Generic;
using RTLTMPro;
using TMPro;
using UnityEngine;

// Traduce entre el texto lógico (field.text) y los glifos que dibuja RTLTextMeshPro,
// que van invertidos y con la ligadura لا ocupando un glifo para dos letras.
public static class CaretMapper
{
    static readonly FastStringBuilder Shaped = new FastStringBuilder(RTLSupport.DefaultBufferSize);

    // La ligadura lam-alef se dibuja como un solo glifo: el cursor nunca puede quedar entre ambas letras.
    public static bool IsInsideLigature(string logical, int index) => Teclado.Core.Ligatures.IsInside(logical, index);

    // x del cursor en espacio local de shown.rectTransform.
    public static float CaretX(TMP_Text shown, string logical, int caret, bool rtlWhenEmpty)
    {
        if (string.IsNullOrEmpty(logical))
            return rtlWhenEmpty ? shown.rectTransform.rect.xMax : shown.rectTransform.rect.xMin;

        var chars = Chars(shown);
        if (chars.Count == 0)
            return rtlWhenEmpty ? shown.rectTransform.rect.xMax : shown.rectTransform.rect.xMin;

        if (!TextUtils.IsRTLInput(logical))
            return caret <= 0 ? chars[0].origin : chars[System.Math.Min(caret, chars.Count) - 1].xAdvance;

        // RTL: los glifos a la derecha del cursor son los que produce el prefijo lógico.
        chars.Sort((a, b) => Right(b).CompareTo(Right(a)));
        int k = GlyphsRightOf(shown, logical, System.Math.Min(System.Math.Max(caret, 0), logical.Length));
        return k == 0 ? Right(chars[0]) : Left(chars[System.Math.Min(k, chars.Count) - 1]);
    }

    // Índice lógico para un punto en espacio local de shown.rectTransform. Nunca cae dentro de una ligadura.
    public static int LogicalIndexAt(TMP_Text shown, string logical, Vector3 localPoint, bool rtlWhenEmpty)
    {
        if (string.IsNullOrEmpty(logical)) return 0;
        var chars = Chars(shown);
        if (chars.Count == 0) return 0;

        if (!TextUtils.IsRTLInput(logical))
        {
            int n = System.Math.Min(chars.Count, logical.Length);
            for (int i = 0; i < n; i++)
                if (localPoint.x < Center(chars[i])) return i;
            return logical.Length;
        }

        // RTL: cuántos glifos quedan a la derecha del punto, y la posición lógica que deja ese número a su derecha.
        int right = 0;
        foreach (var c in chars) if (Center(c) > localPoint.x) right++;
        int best = 0, bestDiff = int.MaxValue;
        for (int i = 0; i <= logical.Length; i++)
        {
            if (IsInsideLigature(logical, i)) continue;
            int diff = System.Math.Abs(GlyphsRightOf(shown, logical, i) - right);
            if (diff < bestDiff) { best = i; bestDiff = diff; }
        }
        return best;
    }

    static float Center(TMP_CharacterInfo c) => (c.bottomLeft.x + c.topRight.x) / 2;

    // Glifos a la derecha del cursor lógico i en un párrafo RTL. Los tramos de dígitos y
    // letras latinas se dibujan en orden de lectura (izq→der), no invertidos.
    public static int GlyphsRightOf(TMP_Text shown, string logical, int i)
    {
        bool insideLtrRun = i > 0 && i < logical.Length && IsLtr(logical[i - 1]) && IsLtr(logical[i]);
        if (!insideLtrRun) return ShownLength(shown, logical.Substring(0, i));
        int start = i, end = i;
        while (start > 0 && IsLtr(logical[start - 1])) start--;
        while (end < logical.Length && IsLtr(logical[end])) end++;
        return ShownLength(shown, logical.Substring(0, start)) + (end - i);
    }

    static bool IsLtr(char c) => char.IsDigit(c) || TextUtils.IsEnglishLetter(c);

    static List<TMP_CharacterInfo> Chars(TMP_Text shown)
    {
        var ti = shown.textInfo;
        var list = new List<TMP_CharacterInfo>(ti.characterCount);
        for (int i = 0; i < ti.characterCount; i++)
            if (ti.characterInfo[i].character != '​') list.Add(ti.characterInfo[i]);
        return list;
    }

    static float Left(TMP_CharacterInfo c) => c.bottomLeft.x;
    static float Right(TMP_CharacterInfo c) => c.topRight.x;

    static int ShownLength(TMP_Text shown, string logicalPrefix)
    {
        if (logicalPrefix.Length == 0) return 0;
        var rtl = shown as RTLTextMeshPro;
        Shaped.Clear();
        RTLSupport.FixRTL(logicalPrefix, Shaped, rtl != null && rtl.Farsi, rtl == null || rtl.FixTags, rtl == null || rtl.PreserveNumbers);
        int n = 0;
        foreach (char ch in Shaped.ToString()) if (ch != '​') n++;
        return n;
    }
}
