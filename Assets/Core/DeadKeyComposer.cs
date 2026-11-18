using System;
using System.Collections.Generic;

namespace Teclado.Core
{
    // Teclas muertas con la semántica de Windows: ´ + e = é, ´ + espacio = ´, ´ + t = ´t.
    public class DeadKeyComposer : IComposer
    {
        readonly LayoutData layout;
        string pending;

        public DeadKeyComposer(LayoutData layout) => this.layout = layout;

        public ComposeResult Process(KeyStroke key)
        {
            if (pending == null)
            {
                if (!key.IsDead) return new ComposeResult(key.Text, "");
                pending = key.Text;
                return new ComposeResult("", pending);
            }

            var table = layout.FindDead(pending);
            string to = null;
            bool found = table != null && table.TryGet(key.Text, out to);
            string dead = pending;
            pending = null;

            if (found) return new ComposeResult(to, "");
            // Muerta + muerta sin entrada: Windows escribe las dos (´ ´ → ´´) y no deja nada pendiente.
            return new ComposeResult(key.Text == " " ? dead : dead + key.Text, "");
        }

        public bool TryBackspace(out ComposeResult result)
        {
            result = ComposeResult.None;
            if (pending == null) return false;
            pending = null;
            return true;
        }

        public string Flush()
        {
            string s = pending ?? "";
            pending = null;
            return s;
        }

        public void Cancel() => pending = null;
        public IReadOnlyList<string> Candidates => Array.Empty<string>();
        public ComposeResult SelectCandidate(int index) => ComposeResult.None;
    }
}
