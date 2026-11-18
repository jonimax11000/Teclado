// Lee un fichero .klc de Windows (Microsoft Keyboard Layout Creator) y lo convierte en LayoutData.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Teclado.Core;

public static class KlcParser
{
    // Estados de SHIFTSTATE que usamos: 0 base, 1 Shift, 6 Ctrl+Alt (AltGr), 7 Shift+Ctrl+Alt.
    static readonly Dictionary<int, Layer> StateToLayer = new Dictionary<int, Layer>
    {
        { 0, Layer.Base }, { 1, Layer.Shift }, { 6, Layer.AltGr }, { 7, Layer.ShiftAltGr },
    };

    static readonly string[] Sections =
    {
        "KBD", "COPYRIGHT", "COMPANY", "LOCALENAME", "LOCALEID", "VERSION", "ATTRIBUTES", "SHIFTSTATE", "LAYOUT",
        "LIGATURE", "DEADKEY", "KEYNAME", "KEYNAME_EXT", "KEYNAME_DEAD", "DESCRIPTIONS", "LANGUAGENAMES", "ENDKBD",
    };

    public static LayoutData Parse(string klcText, LayoutData into = null)
    {
        var layout = into ?? new LayoutData();
        layout.keys.Clear();
        layout.deadKeys.Clear();

        var states = new List<int>();
        var vkToScan = new Dictionary<string, int>();
        string section = null;
        DeadKeyTable dead = null;

        foreach (var raw in klcText.Replace("\r", "").Split('\n'))
        {
            string line = StripComment(raw).Trim();
            if (line.Length == 0) continue;
            var tok = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (Sections.Contains(tok[0]))
            {
                section = tok[0];
                if (section == "DEADKEY")
                {
                    dead = new DeadKeyTable { deadChar = Char(tok[1]) };
                    layout.deadKeys.Add(dead);
                }
                continue;
            }

            switch (section)
            {
                case "SHIFTSTATE":
                    states.Add(int.Parse(tok[0]));
                    break;
                case "LAYOUT":
                    if (tok[0] == "-1") break;   // línea de continuación de SGCap
                    var key = new KeyDef { scanCode = int.Parse(tok[0], NumberStyles.HexNumber), caps = Caps(tok[2]) };
                    vkToScan[tok[1]] = key.scanCode;
                    for (int c = 0; c < states.Count && 3 + c < tok.Length; c++)
                    {
                        if (!StateToLayer.TryGetValue(states[c], out var layer)) continue;
                        string v = tok[3 + c];
                        bool isDead = v.EndsWith("@") && v.Length > 1;
                        if (isDead) v = v.Substring(0, v.Length - 1);
                        key.text[(int)layer] = v == "%%" ? "" : Char(v);
                        key.dead[(int)layer] = isDead;
                    }
                    layout.keys.Add(key);
                    break;
                case "LIGATURE":
                    if (!vkToScan.TryGetValue(tok[0], out int sc)) break;
                    int col = int.Parse(tok[1]);
                    if (col >= states.Count || !StateToLayer.TryGetValue(states[col], out var lig)) break;
                    layout.Find(sc).text[(int)lig] = string.Concat(tok.Skip(2).Select(Char));
                    break;
                case "DEADKEY":
                    if (tok.Length >= 2) dead.entries.Add(new DeadKeyEntry { from = Char(tok[0]), to = Char(tok[1]) });
                    break;
            }
        }
        return layout;
    }

    static string StripComment(string line)
    {
        int i = line.IndexOf("//", StringComparison.Ordinal);
        return i < 0 ? line : line.Substring(0, i);
    }

    static CapsMode Caps(string token)
    {
        if (token == "SGCap") return CapsMode.Affects;
        if (!int.TryParse(token, out int v)) return CapsMode.None;
        if ((v & 4) != 0) return CapsMode.AffectsAltGr;   // CAPLOKALTGR
        return (v & 1) != 0 ? CapsMode.Affects : CapsMode.None;
    }

    // Un token es «-1» (vacío), un code point hexadecimal de 4+ cifras o un carácter literal.
    static string Char(string token)
    {
        if (token == "-1") return "";
        if (token.Length >= 4 && int.TryParse(token, NumberStyles.HexNumber, null, out int cp))
        {
            if (cp < 0x20) return "";
            string s = char.ConvertFromUtf32(cp);
            // Caracteres más nuevos que el Unicode del runtime (p. ej. U+20C1): no se pueden dibujar ni procesar.
            return CharUnicodeInfo.GetUnicodeCategory(s, 0) == UnicodeCategory.OtherNotAssigned ? "" : s;
        }
        return token.Length == 1 && token[0] < 0x20 ? "" : token;
    }
}
