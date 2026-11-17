using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public class FieldPointerTests
{
    TestRig rig;

    [UnitySetUp] public IEnumerator SetUp() { rig = new TestRig(); yield return null; }
    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); yield return null; }

    // Glifos visibles ordenados de izquierda a derecha.
    TMP_CharacterInfo[] Glyphs()
    {
        rig.Rtl.ForceMeshUpdate();
        var ti = rig.Rtl.textInfo;
        return ti.characterInfo.Take(ti.characterCount).Where(c => c.isVisible && c.character != '​').OrderBy(c => c.bottomLeft.x).ToArray();
    }

    int At(float x) => CaretMapper.LogicalIndexAt(rig.Rtl, rig.Field.text, new Vector3(x, 0, 0), false);

    [Test] public void TocarMitadIzquierdaDeLam_EnSalm()
    {
        rig.Type("س", "ل", "م");
        var g = Glyphs();                     // izq→der: م ﻠ ﺳ
        var lam = g[1];
        float w = lam.topRight.x - lam.bottomLeft.x;
        Assert.AreEqual(2, At(lam.bottomLeft.x + w * 0.25f));
        Assert.AreEqual(1, At(lam.bottomLeft.x + w * 0.75f));
    }

    [Test] public void TocarLigadura_NuncaDentro()
    {
        rig.Type("س", "ل", "ا", "م");
        var lig = Glyphs()[1];                // izq→der: م ﻼ ﺳ
        for (float t = 0.05f; t < 1f; t += 0.1f)
        {
            int i = At(Mathf.Lerp(lig.bottomLeft.x, lig.topRight.x, t));
            Assert.That(i == 1 || i == 3, $"t={t} dio {i}");
        }
    }

    [Test] public void TocarFueraDelTexto()
    {
        rig.Type("س", "ل", "ا", "م");
        var g = Glyphs();
        Assert.AreEqual(4, At(g.First().bottomLeft.x - 50));
        Assert.AreEqual(0, At(g.Last().topRight.x + 50));

        rig.Field.text = "abc";
        g = Glyphs();
        Assert.AreEqual(0, At(g.First().bottomLeft.x - 50));
        Assert.AreEqual(3, At(g.Last().topRight.x + 50));
    }

    [Test] public void TocarCampoVacio()
    {
        Assert.AreEqual(0, At(0));
        Assert.AreEqual(0, CaretMapper.LogicalIndexAt(rig.Rtl, "", Vector3.zero, true));
    }

    [UnityTest] public IEnumerator OnPointerClick_MueveElCursor()
    {
        var pointer = rig.Field.gameObject.AddComponent<FieldPointer>();
        pointer.input = rig.Input;
        rig.Type("a", "b", "c");
        yield return null;
        var first = Glyphs()[0];
        Vector3 world = rig.Rtl.rectTransform.TransformPoint(new Vector3(first.bottomLeft.x + 1, (first.bottomLeft.y + first.topRight.y) / 2, 0));
        var data = new PointerEventData(null) { position = world };   // canvas overlay: mundo == pantalla
        pointer.OnPointerClick(data);
        Assert.AreEqual(0, rig.Controller.Caret);
    }

    // Revisión final #6
    [Test] public void TocarEntreDigitos()
    {
        rig.Type("س", "1", "2", "3");
        var g = Glyphs();                     // izq→der: 1 2 3 س
        float between = (g[0].topRight.x + g[1].bottomLeft.x) / 2;
        Assert.AreEqual(2, At(between));
    }
}
