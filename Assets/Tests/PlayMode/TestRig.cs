using RTLTMPro;
using TMPro;
using UnityEngine;

// Fixture compartido: KeyboardController + InputFieldView sobre un TMP_InputField con RTLTextMeshPro.
public class TestRig
{
    public readonly GameObject Root;
    public readonly TMP_InputField Field;
    public readonly RTLTextMeshPro Rtl;
    public readonly KeyboardController Controller;
    public readonly InputFieldView Input;

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
        Controller = Root.AddComponent<KeyboardController>();
        Input = go.AddComponent<InputFieldView>();
        Input.field = Field;
        Input.controller = Controller;
    }

    // Texto confirmado directamente (sin distribución), como escribir con un teclado directo.
    public void Type(params string[] keys)
    {
        foreach (var k in keys) Controller.Buffer.Commit(k);
    }

    public void Destroy() => Object.Destroy(Root);
}
