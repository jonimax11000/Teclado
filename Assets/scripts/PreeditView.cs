using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Subraya el texto provisional (tecla muerta pendiente, sílaba coreana en formación).
public class PreeditView : MonoBehaviour
{
    public InputFieldView input;
    public RectTransform underline;

    Image image;

    void Start() => image = underline.GetComponent<Image>();

    void LateUpdate()
    {
        var buffer = input.controller.Buffer;
        if (buffer.Preedit.Length == 0)
        {
            image.enabled = false;
            underline.sizeDelta = new Vector2(0, underline.sizeDelta.y);
            return;
        }
        TMP_Text text = input.field.textComponent;
        text.ForceMeshUpdate();
        bool rtl = input.controller.ActiveKeyboardIsRtl;
        float a = CaretMapper.CaretX(text, buffer.Text, buffer.PreeditStart, rtl);
        float b = CaretMapper.CaretX(text, buffer.Text, buffer.PreeditStart + buffer.Preedit.Length, rtl);
        var rect = text.rectTransform.rect;
        float y = rect.center.y - text.fontSize * 0.6f;
        underline.position = text.rectTransform.TransformPoint(new Vector3((a + b) / 2, y, 0));
        underline.sizeDelta = new Vector2(Mathf.Abs(b - a), 2);
        image.color = text.color;
        image.enabled = true;
    }
}
