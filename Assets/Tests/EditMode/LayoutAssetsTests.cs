using System.Linq;
using NUnit.Framework;
using Teclado.Core;
using UnityEditor;
using UnityEngine;

public class LayoutAssetsTests
{
    static LayoutData[] All() => AssetDatabase.FindAssets("t:KeyboardLayout", new[] { "Assets/Layouts" })
        .Select(g => AssetDatabase.LoadAssetAtPath<KeyboardLayout>(AssetDatabase.GUIDToAssetPath(g)).data)
        .OrderBy(l => l.order).ToArray();

    static LayoutData Get(string name) => All().Single(l => l.displayName == name);

    static string Dead(LayoutData l, string dead, string from) => l.FindDead(dead).TryGet(from, out var to) ? to : null;

    [Test] public void Hay8_OrdenCorrecto()
    {
        CollectionAssert.AreEqual(new[] { "English", "Español", "Français", "Deutsch", "Русский", "한국어", "Ελληνικά", "العربية" },
            All().Select(l => l.displayName).ToArray());
    }

    [Test] public void Espanol_TildeMuerta()
    {
        var es = Get("Español");
        var k = es.Find(0x28);
        Assert.AreEqual("´", k.text[0]);
        Assert.IsTrue(k.dead[0]);
        Assert.AreEqual("é", Dead(es, "´", "e"));
        Assert.AreEqual(ComposerKind.DeadKey, es.composer);
        Assert.AreEqual(PhysicalTemplate.ISO, es.template);
    }

    [Test] public void Frances_Circunflejo() => Assert.AreEqual("ê", Dead(Get("Français"), "^", "e"));
    [Test] public void Aleman_Circunflejo() => Assert.AreEqual("ô", Dead(Get("Deutsch"), "^", "o"));

    [Test] public void Griego_Tonos()
    {
        var el = Get("Ελληνικά");
        Assert.AreEqual("ά", Dead(el, "΄", "α"));
        Assert.AreEqual("ϊ", Dead(el, "¨", "ι"));
        Assert.AreEqual("ΐ", Dead(el, "΅", "ι"));
    }

    [Test] public void Coreano_Jamo()
    {
        var ko = Get("한국어");
        Assert.AreEqual("ㅎ", ko.Find(0x22).text[0]);
        Assert.AreEqual("ㅃ", ko.Find(0x10).text[1]);
        Assert.AreEqual("ㅂ", ko.Find(0x10).text[0]);
        Assert.AreEqual("1", ko.Find(0x02).text[0]);
        Assert.AreEqual(ComposerKind.Hangul, ko.composer);
    }

    [Test] public void Arabe_Rtl_YLigadura()
    {
        var ar = Get("العربية");
        Assert.IsTrue(ar.isRtl);
        Assert.AreEqual("لا", ar.Find(0x30).text[0]);
        Assert.AreEqual("\u0651", ar.Find(0x2B).text[1]);
    }
}
