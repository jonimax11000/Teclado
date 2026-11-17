using System.Collections.Generic;
using RTLTMPro;
using TMPro;

// Traduce entre el texto lógico (field.text) y los glifos que dibuja RTLTextMeshPro,
// que van invertidos y con la ligadura لا ocupando un glifo para dos letras.
public static class CaretMapper
{
    static readonly FastStringBuilder Shaped = new FastStringBuilder(RTLSupport.DefaultBufferSize);

    // La ligadura lam-alef se dibuja como un solo glifo: el cursor nunca puede quedar entre ambas letras.
    public static bool IsInsideLigature(string logical, int index)
    {
        if (index <= 0 || index >= logical.Length || logical[index - 1] != 'ل') return false;
        char next = logical[index];
        return next == 'ا' || next == 'أ' || next == 'إ' || next == 'آ';
    }

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
        int k = ShownLength(shown, logical.Substring(0, caret));
        return k == 0 ? Right(chars[0]) : Left(chars[System.Math.Min(k, chars.Count) - 1]);
    }

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
