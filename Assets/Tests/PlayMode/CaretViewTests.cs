using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CaretViewTests
{
    const float Tol = 2f;
    TestRig rig;

    [UnitySetUp] public IEnumerator SetUp() { rig = new TestRig(); yield return null; }
    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); yield return null; }

    TMP_CharacterInfo[] Visible()
    {
        rig.Rtl.ForceMeshUpdate();
        var ti = rig.Rtl.textInfo;
        return ti.characterInfo.Take(ti.characterCount).Where(c => c.isVisible && c.character != '\u200B').OrderBy(c => c.bottomLeft.x).ToArray();
    }

    float X(int caret, bool rtlWhenEmpty = false)
    {
        rig.Rtl.ForceMeshUpdate();
        return CaretMapper.CaretX(rig.Rtl, rig.Field.text, caret, rtlWhenEmpty);
    }

    [Test] public void Arabe_CursorAlFinal_AIzquierdaDelUltimoGlifoVisual()
    {
        rig.Type("س", "ل", "ا", "م");
        var g = Visible();
        Assert.AreEqual(3, g.Length, "س ﻼ م");
        Assert.LessOrEqual(X(4), g.Min(c => c.bottomLeft.x) + Tol);
    }

    [Test] public void Arabe_CursorAlInicio_ADerecha()
    {
        rig.Type("س", "ل", "ا", "م");
        var g = Visible();
        Assert.GreaterOrEqual(X(0), g.Max(c => c.topRight.x) - Tol);
    }

    [Test] public void Arabe_CursorTrasLigadura()
    {
        rig.Type("س", "ل", "ا", "م");
        var g = Visible();             // izq→der: م, ﻼ, س
        float x = X(3);
        Assert.GreaterOrEqual(x, g[0].topRight.x - Tol, "a la derecha de م");
        Assert.LessOrEqual(x, g[1].bottomLeft.x + Tol, "a la izquierda de ﻼ");
    }

    [Test] public void Arabe_ConEspacio_CursorAlFinal()
    {
        rig.Type("س", "ل", " ", "م");
        var g = Visible();
        Assert.LessOrEqual(X(4), g.Min(c => c.bottomLeft.x) + Tol);
        float x3 = X(3);               // tras el espacio, antes de م
        Assert.GreaterOrEqual(x3, g[0].topRight.x - Tol);
    }

    [Test] public void Latin_IgualQueHoy()
    {
        rig.Type("a", "b", "c");
        rig.Rtl.ForceMeshUpdate();
        Assert.AreEqual(rig.Rtl.textInfo.characterInfo[0].xAdvance, X(1), 0.5f);
        Assert.AreEqual(rig.Rtl.textInfo.characterInfo[0].origin, X(0), 0.5f);
    }

    [Test] public void Vacio_TecladoArabe_BordeDerecho()
    {
        Assert.AreEqual(rig.Rtl.rectTransform.rect.xMax, X(0, true), 1f);
    }

    [Test] public void Vacio_TecladoLatino_BordeIzquierdo()
    {
        Assert.AreEqual(rig.Rtl.rectTransform.rect.xMin, X(0, false), 1f);
    }

    [UnityTest] public IEnumerator CaretView_SigueAlControlador()
    {
        var caretGo = new GameObject("Caret", typeof(RectTransform), typeof(Image));
        caretGo.transform.SetParent(rig.Field.textViewport, false);
        var view = rig.Field.gameObject.AddComponent<CaretView>();
        view.controlator = rig.Controlator;
        view.caret = (RectTransform)caretGo.transform;
        yield return null;
        rig.Type("س", "ل");
        yield return null;
        rig.Rtl.ForceMeshUpdate();
        float expected = rig.Rtl.rectTransform.TransformPoint(new Vector3(CaretMapper.CaretX(rig.Rtl, rig.Field.text, 2, false), 0, 0)).x;
        Assert.AreEqual(expected, view.caret.position.x, 0.5f);
        Assert.AreEqual(0f, rig.Field.caretColor.a, "el cursor de TMP_InputField debe quedar oculto");
    }
}
