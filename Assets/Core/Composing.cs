using System;
using System.Collections.Generic;

namespace Teclado.Core
{
    public readonly struct KeyStroke
    {
        public readonly int ScanCode;
        public readonly string Text;
        public readonly bool IsDead;
        public KeyStroke(int scanCode, string text, bool isDead) { ScanCode = scanCode; Text = text ?? ""; IsDead = isDead; }
    }

    public readonly struct ComposeResult
    {
        public readonly string Commit;
        public readonly string Preedit;
        public ComposeResult(string commit, string preedit) { Commit = commit ?? ""; Preedit = preedit ?? ""; }
        public static readonly ComposeResult None = new ComposeResult("", "");
    }

    // Convierte teclas en texto confirmado + texto provisional. Cada familia de idiomas tiene el suyo
    // (directo, teclas muertas, Hangul; más adelante Pinyin/Romaji con candidatos).
    public interface IComposer
    {
        ComposeResult Process(KeyStroke key);
        bool TryBackspace(out ComposeResult result);   // false: no hay composición, borra el búfer
        string Flush();                                // confirma lo pendiente
        void Cancel();
        IReadOnlyList<string> Candidates { get; }
        ComposeResult SelectCandidate(int index);
    }

    public static class ComposerFactory
    {
        static readonly Dictionary<ComposerKind, Func<LayoutData, IComposer>> creators = new Dictionary<ComposerKind, Func<LayoutData, IComposer>>
        {
            { ComposerKind.Direct, _ => new DirectComposer() },
            { ComposerKind.DeadKey, l => new DeadKeyComposer(l) },
        };

        public static void Register(ComposerKind kind, Func<LayoutData, IComposer> create) => creators[kind] = create;

        public static IComposer Create(LayoutData layout) => creators[layout.composer](layout);
    }
}
