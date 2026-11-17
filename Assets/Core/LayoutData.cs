using System;
using System.Collections.Generic;

namespace Teclado.Core
{
    public enum Layer { Base = 0, Shift = 1, AltGr = 2, ShiftAltGr = 3 }
    public enum CapsMode { None = 0, Affects = 1 }
    public enum PhysicalTemplate { ISO, ANSI, ANSI14 }
    public enum ComposerKind { Direct, DeadKey, Hangul }

    [Serializable]
    public class KeyDef
    {
        public int scanCode;
        public string[] text = { "", "", "", "" };
        public bool[] dead = new bool[4];
        public CapsMode caps;
    }

    [Serializable]
    public class DeadKeyEntry
    {
        public string from;
        public string to;
    }

    [Serializable]
    public class DeadKeyTable
    {
        public string deadChar;
        public List<DeadKeyEntry> entries = new List<DeadKeyEntry>();

        public bool TryGet(string from, out string to)
        {
            foreach (var e in entries)
                if (e.from == from) { to = e.to; return true; }
            to = null;
            return false;
        }
    }

    // Distribución de un idioma, sin lógica: la importa KlcImporter desde un .klc de Windows.
    [Serializable]
    public class LayoutData
    {
        public string displayName;
        public int order;
        public string systemLanguage;   // nombre de UnityEngine.SystemLanguage (el núcleo no referencia Unity)
        public PhysicalTemplate template;
        public ComposerKind composer;
        public bool isRtl;
        public List<KeyDef> keys = new List<KeyDef>();
        public List<DeadKeyTable> deadKeys = new List<DeadKeyTable>();

        public KeyDef Find(int scanCode) => keys.Find(k => k.scanCode == scanCode);
        public DeadKeyTable FindDead(string deadChar) => deadKeys.Find(d => d.deadChar == deadChar);
    }
}
