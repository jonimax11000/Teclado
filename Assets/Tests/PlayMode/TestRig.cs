using RTLTMPro;
using TMPro;
using UnityEngine;

// Fixture compartido: KeyboardControlator con un TMP_InputField cuyo texto es RTLTextMeshPro.
public class TestRig
{
    public readonly GameObject Root;
    public readonly TMP_InputField Field;
    public readonly RTLTextMeshPro Rtl;
    public readonly KeyboardControlator Controlator;

    public TestRig()
    {
        Root = new GameObject("Canvas", typeof(Canvas));
        var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.transform.SetParent(Root.transform, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(600, 60);
        Field = go.GetComponent<TMP_InputField>();
        var textGo = Field.textComponent.gameObject;
        Object.DestroyImmediate(textGo.GetComponent<TextMeshProUGUI>());
        Rtl = textGo.AddComponent<RTLTextMeshPro>();
        Rtl.Farsi = false;
        Rtl.PreserveNumbers = true;
        Field.textComponent = Rtl;
        Controlator = Root.AddComponent<KeyboardControlator>();
        Controlator.field = Field;
    }

    public void Type(params string[] keys)
    {
        foreach (var k in keys) Controlator.InsertChar(k);
    }

    public void Destroy() => Object.Destroy(Root);
}
