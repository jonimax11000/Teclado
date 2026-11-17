using NUnit.Framework;
using RTLTMPro;
using UnityEngine;

public class SmokeTests
{
    [Test]
    public void CompilaYRtlDisponible()
    {
        var go = new GameObject("rtl");
        var rtl = go.AddComponent<RTLTextMeshPro>();
        rtl.text = "سل";
        Assert.AreNotEqual("سل", rtl.text, "lo mostrado debe estar unido");
        Object.DestroyImmediate(go);
    }
}
