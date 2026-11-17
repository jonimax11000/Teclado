using System;
using System.Collections.Generic;

namespace Teclado.Core
{
    // Autómata Dubeolsik: une jamo de compatibilidad (U+3131–U+318E) en sílabas (U+AC00–U+D7A3).
    public class HangulComposer : IComposer
    {
        const string Initials = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        const string Medials = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        const string Finals = "\0ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

        static readonly Dictionary<(char, char), char> CompoundVowels = new Dictionary<(char, char), char>
        {
            { ('ㅗ', 'ㅏ'), 'ㅘ' }, { ('ㅗ', 'ㅐ'), 'ㅙ' }, { ('ㅗ', 'ㅣ'), 'ㅚ' },
            { ('ㅜ', 'ㅓ'), 'ㅝ' }, { ('ㅜ', 'ㅔ'), 'ㅞ' }, { ('ㅜ', 'ㅣ'), 'ㅟ' }, { ('ㅡ', 'ㅣ'), 'ㅢ' },
        };

        static readonly Dictionary<(char, char), char> DoubleFinals = new Dictionary<(char, char), char>
        {
            { ('ㄱ', 'ㅅ'), 'ㄳ' }, { ('ㄴ', 'ㅈ'), 'ㄵ' }, { ('ㄴ', 'ㅎ'), 'ㄶ' }, { ('ㄹ', 'ㄱ'), 'ㄺ' },
            { ('ㄹ', 'ㅁ'), 'ㄻ' }, { ('ㄹ', 'ㅂ'), 'ㄼ' }, { ('ㄹ', 'ㅅ'), 'ㄽ' }, { ('ㄹ', 'ㅌ'), 'ㄾ' },
            { ('ㄹ', 'ㅍ'), 'ㄿ' }, { ('ㄹ', 'ㅎ'), 'ㅀ' }, { ('ㅂ', 'ㅅ'), 'ㅄ' },
        };

        struct Syllable
        {
            public char Cho, Jung, Jong;   // '\0' = vacío
            public bool IsEmpty => Cho == '\0' && Jung == '\0';
        }

        Syllable current;
        readonly Stack<Syllable> history = new Stack<Syllable>();   // estados anteriores de la sílaba, para Borrar

        static bool IsConsonant(char c) => Initials.IndexOf(c) >= 0 || (c >= 'ㄱ' && c <= 'ㅎ');
        static bool IsVowel(char c) => Medials.IndexOf(c) >= 0;
        static bool CanBeFinal(char c) => c != '\0' && Finals.IndexOf(c) > 0;

        public ComposeResult Process(KeyStroke key)
        {
            if (key.Text.Length != 1 || !(IsConsonant(key.Text[0]) || IsVowel(key.Text[0])))
                return new ComposeResult(CommitCurrent() + key.Text, "");

            char j = key.Text[0];
            string commit = IsVowel(j) ? AddVowel(j) : AddConsonant(j);
            return new ComposeResult(commit, Render(current));
        }

        string AddConsonant(char j)
        {
            var s = current;
            if (s.Jung == '\0')
            {
                if (s.Cho == '\0') { Push(new Syllable { Cho = j }); return ""; }
                string c = CommitCurrent();
                Push(new Syllable { Cho = j });
                return c;
            }
            if (s.Jong == '\0')
            {
                if (s.Cho != '\0' && CanBeFinal(j)) { s.Jong = j; Push(s); return ""; }
            }
            else if (DoubleFinals.TryGetValue((s.Jong, j), out char dbl))
            {
                s.Jong = dbl;
                Push(s);
                return "";
            }
            string committed = CommitCurrent();
            Push(new Syllable { Cho = j });
            return committed;
        }

        string AddVowel(char j)
        {
            var s = current;
            if (s.Jung == '\0')
            {
                s.Jung = j;
                Push(s);
                return "";
            }
            if (s.Jong == '\0')
            {
                if (CompoundVowels.TryGetValue((s.Jung, j), out char cv)) { s.Jung = cv; Push(s); return ""; }
                string c = CommitCurrent();
                Push(new Syllable { Jung = j });
                return c;
            }

            // La consonante final (o la segunda mitad de una doble) pasa a la sílaba siguiente.
            char moving = s.Jong;
            s.Jong = '\0';
            foreach (var kv in DoubleFinals)
                if (kv.Value == moving) { s.Jong = kv.Key.Item1; moving = kv.Key.Item2; break; }
            current = s;
            string committed = CommitCurrent();
            Push(new Syllable { Cho = moving });
            Push(new Syllable { Cho = moving, Jung = j });
            return committed;
        }

        void Push(Syllable next)
        {
            history.Push(current);
            current = next;
        }

        string CommitCurrent()
        {
            string s = Render(current);
            current = default;
            history.Clear();
            return s;
        }

        static string Render(Syllable s)
        {
            if (s.Cho != '\0' && s.Jung != '\0')
            {
                int l = Initials.IndexOf(s.Cho), v = Medials.IndexOf(s.Jung), t = s.Jong == '\0' ? 0 : Finals.IndexOf(s.Jong);
                return ((char)(0xAC00 + (l * 21 + v) * 28 + t)).ToString();
            }
            if (s.Cho != '\0') return s.Cho.ToString();
            return s.Jung != '\0' ? s.Jung.ToString() : "";
        }

        public bool TryBackspace(out ComposeResult result)
        {
            result = ComposeResult.None;
            if (current.IsEmpty) return false;
            current = history.Count > 0 ? history.Pop() : default;
            result = new ComposeResult("", Render(current));
            return true;
        }

        public string Flush() => CommitCurrent();

        public void Cancel()
        {
            current = default;
            history.Clear();
        }

        public IReadOnlyList<string> Candidates => Array.Empty<string>();
        public ComposeResult SelectCandidate(int index) => ComposeResult.None;
    }
}
