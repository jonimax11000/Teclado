public static class CaretMapper
{
    // La ligadura lam-alef se dibuja como un solo glifo: el cursor nunca puede quedar entre ambas letras.
    public static bool IsInsideLigature(string logical, int index)
    {
        if (index <= 0 || index >= logical.Length || logical[index - 1] != 'ل') return false;
        char next = logical[index];
        return next == 'ا' || next == 'أ' || next == 'إ' || next == 'آ';
    }
}
