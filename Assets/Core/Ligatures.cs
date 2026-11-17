namespace Teclado.Core
{
    public static class Ligatures
    {
        // La ligadura lam-alef se dibuja como un solo glifo: el cursor nunca puede quedar entre ambas letras.
        public static bool IsInside(string text, int index)
        {
            if (index <= 0 || index >= text.Length || text[index - 1] != 'ل') return false;
            char next = text[index];
            return next == 'ا' || next == 'أ' || next == 'إ' || next == 'آ';
        }
    }
}
