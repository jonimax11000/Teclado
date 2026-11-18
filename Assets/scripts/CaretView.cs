using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cursor visible propio: el de TMP_InputField no sirve con texto RTL.
public class CaretView : MonoBehaviour
{
    public InputFieldView input;
    public RectTransform caret;
    public float blinkPeriod = 0.5f;

    TMP_Text text;
    Image image;
    HorizontalAlignmentOptions originalAlignment;
    bool dirty = true;
    float blinkStart;

    void Start()
    {
        var field = input.field;
        text = field.textComponent;
        image = caret.GetComponent<Image>();
        originalAlignment = text.horizontalAlignment;
        field.customCaretColor = true;
        field.caretColor = Color.clear;
        input.controller.CaretChanged += MarkDirty;
    }

    void OnDestroy()
    {
        if (input != null && input.controller != null) input.controller.CaretChanged -= MarkDirty;
    }

    void MarkDirty()
    {
        dirty = true;
        blinkStart = Time.unscaledTime;
    }

    void LateUpdate()
    {
        if (text == null) return;
        var controller = input.controller;
        var buffer = controller.Buffer;
        string logical = buffer.Text;
        bool rtl = TextUtils.IsRTLInput(logical) || (logical.Length == 0 && controller.ActiveKeyboardIsRtl);
        var align = rtl ? HorizontalAlignmentOptions.Right : originalAlignment;
        if (text.horizontalAlignment != align) { text.horizontalAlignment = align; dirty = true; }

        if (dirty)
        {
            text.ForceMeshUpdate();
            // El cursor se dibuja tras el texto provisional.
            float x = CaretMapper.CaretX(text, logical, buffer.PreeditStart + buffer.Preedit.Length, controller.ActiveKeyboardIsRtl);
            var rect = text.rectTransform.rect;
            caret.position = text.rectTransform.TransformPoint(new Vector3(x, rect.center.y, 0));
            caret.sizeDelta = new Vector2(2, text.fontSize * 1.2f);
            image.color = text.color;
            dirty = false;
        }

        image.enabled = ((Time.unscaledTime - blinkStart) % (2 * blinkPeriod)) < blinkPeriod;
    }
}
