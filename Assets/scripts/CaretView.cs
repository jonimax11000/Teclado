using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cursor visible propio: el de TMP_InputField no sirve con texto RTL.
public class CaretView : MonoBehaviour
{
    public KeyboardControlator controlator;
    public RectTransform caret;
    public float blinkPeriod = 0.5f;

    TMP_Text text;
    Image image;
    HorizontalAlignmentOptions originalAlignment;
    bool dirty = true;
    float blinkStart;

    void Start()
    {
        var field = controlator.field;
        text = field.textComponent;
        image = caret.GetComponent<Image>();
        originalAlignment = text.horizontalAlignment;
        field.customCaretColor = true;
        field.caretColor = Color.clear;
        controlator.CaretChanged += MarkDirty;
    }

    void OnDestroy()
    {
        if (controlator != null) controlator.CaretChanged -= MarkDirty;
    }

    void MarkDirty()
    {
        dirty = true;
        blinkStart = Time.unscaledTime;
    }

    void LateUpdate()
    {
        if (text == null) return;
        string logical = controlator.field.text;
        bool rtl = TextUtils.IsRTLInput(logical) || (logical.Length == 0 && controlator.ActiveKeyboardIsRtl);
        var align = rtl ? HorizontalAlignmentOptions.Right : originalAlignment;
        if (text.horizontalAlignment != align) { text.horizontalAlignment = align; dirty = true; }

        if (dirty)
        {
            text.ForceMeshUpdate();
            float x = CaretMapper.CaretX(text, logical, controlator.Caret, controlator.ActiveKeyboardIsRtl);
            var rect = text.rectTransform.rect;
            caret.position = text.rectTransform.TransformPoint(new Vector3(x, rect.center.y, 0));
            caret.sizeDelta = new Vector2(2, text.fontSize * 1.2f);
            image.color = text.color;
            dirty = false;
        }

        image.enabled = ((Time.unscaledTime - blinkStart) % (2 * blinkPeriod)) < blinkPeriod;
    }
}
