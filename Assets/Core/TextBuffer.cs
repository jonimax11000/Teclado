using System;

namespace Teclado.Core
{
    // Texto confirmado + cursor lógico + texto provisional (preedit) en el cursor.
    // Lo que recibe el integrador (CommittedText, Committed) nunca contiene el preedit.
    public class TextBuffer
    {
        readonly Func<string, bool> isRtl;

        public string CommittedText { get; private set; } = "";
        public string Preedit { get; private set; } = "";
        public int Caret { get; private set; }
        public string Text => CommittedText.Insert(Clamped, Preedit);
        public int PreeditStart => Clamped;

        public event Action Changed;
        public event Action<string> Committed;

        public TextBuffer(Func<string, bool> isRtl = null) => this.isRtl = isRtl ?? (_ => false);

        // El cursor puede haber quedado obsoleto si el texto cambió por fuera.
        int Clamped => Math.Min(Math.Max(Caret, 0), CommittedText.Length);

        public void Commit(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            int at = Clamped;
            CommittedText = CommittedText.Insert(at, s);
            Caret = OutOfLigature(at + s.Length, 1);
            Changed?.Invoke();
            Committed?.Invoke(s);
        }

        public void SetPreedit(string s)
        {
            s ??= "";
            if (s == Preedit) return;
            Preedit = s;
            Changed?.Invoke();
        }

        public bool DeleteBackward()
        {
            int at = Clamped - 1;
            if (at < 0) return false;
            CommittedText = CommittedText.Remove(at, 1);
            Caret = OutOfLigature(at, 1);
            Changed?.Invoke();
            return true;
        }

        // En un párrafo RTL, la flecha izquierda avanza en el texto lógico.
        public void MoveVisual(bool left)
        {
            int step = (left ^ isRtl(CommittedText)) ? -1 : 1;
            Caret = OutOfLigature(Clamp(Clamped + step), step);
            Changed?.Invoke();
        }

        public void SetCaret(int i)
        {
            Caret = OutOfLigature(Clamp(i), 1);
            Changed?.Invoke();
        }

        public void ReplaceAll(string text)
        {
            CommittedText = text ?? "";
            Preedit = "";
            Caret = CommittedText.Length;
            Changed?.Invoke();
        }

        int Clamp(int i) => Math.Min(Math.Max(i, 0), CommittedText.Length);

        int OutOfLigature(int i, int step) => Ligatures.IsInside(CommittedText, i) ? Clamp(i + step) : i;
    }
}
