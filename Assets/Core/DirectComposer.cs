using System;
using System.Collections.Generic;

namespace Teclado.Core
{
    public class DirectComposer : IComposer
    {
        public ComposeResult Process(KeyStroke key) => new ComposeResult(key.Text, "");
        public bool TryBackspace(out ComposeResult result) { result = ComposeResult.None; return false; }
        public string Flush() => "";
        public void Cancel() { }
        public IReadOnlyList<string> Candidates => Array.Empty<string>();
        public ComposeResult SelectCandidate(int index) => ComposeResult.None;
    }
}
