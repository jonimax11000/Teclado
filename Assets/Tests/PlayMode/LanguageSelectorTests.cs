using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using static TestLayouts;

public class LanguageSelectorTests
{
    TestRig rig;
    LanguageSelector selector;
    readonly List<GameObject> created = new List<GameObject>();

    [UnitySetUp] public IEnumerator SetUp()
    {
        rig = new TestRig();
        var ddGo = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        ddGo.transform.SetParent(rig.Root.transform, false);
        selector = rig.Root.AddComponent<LanguageSelector>();
        selector.dropdown = ddGo.GetComponent<TMP_Dropdown>();
        selector.controller = rig.Controller;
        // Desordenados a propósito: el selector ordena por order.
        foreach (var (name, order) in new[] { ("B", 1), ("A", 0), ("C", 2) })
        {
            var layout = ScriptableObject.CreateInstance<KeyboardLayout>();
            layout.data = SpanishLike(name);
            layout.data.order = order;
            var rigid = new GameObject(name + "R"); var canvas = new GameObject(name + "C");
            created.Add(rigid); created.Add(canvas);
            selector.entries.Add(new LanguageSelector.Entry { layout = layout, rigid = rigid, canvas = canvas });
        }
        yield return null;
    }

    [UnityTearDown] public IEnumerator TearDown() { rig.Destroy(); created.ForEach(Object.Destroy); yield return null; }

    [Test] public void Opciones_PorOrder()
    {
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, selector.dropdown.options.Select(o => o.text).ToArray());
    }

    [Test] public void Select_ActivaSoloElPar()
    {
        selector.Select(1);
        var e = selector.entries;
        Assert.IsTrue(e[1].rigid.activeSelf && e[1].canvas.activeSelf);
        Assert.IsFalse(e[0].rigid.activeSelf || e[0].canvas.activeSelf || e[2].rigid.activeSelf);
        Assert.AreSame(e[1].layout.data, rig.Controller.Session.Layout);
        Assert.AreEqual("B", rig.Controller.Session.Layout.displayName);
    }
}
