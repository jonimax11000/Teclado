using RTLTMPro;

// Texto listo para mostrar en un TMP normal (etiquetas de teclas): letras unidas y en orden visual.
public static class RtlShaper
{
    static readonly FastStringBuilder Output = new FastStringBuilder(RTLSupport.DefaultBufferSize);

    public static bool NeedsShaping(string logical) => !string.IsNullOrEmpty(logical) && TextUtils.IsRTLInput(logical);

    public static string Shape(string logical)
    {
        if (!NeedsShaping(logical)) return logical;
        Output.Clear();
        RTLSupport.FixRTL(logical, Output, false, true, true);
        Output.Reverse();
        return Output.ToString();
    }
}
