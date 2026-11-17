using UnityEngine;
using UnityEngine.EventSystems;

// Tocar el campo (rayo XR o puntero) coloca el cursor lógico donde se toca.
public class FieldPointer : MonoBehaviour, IPointerClickHandler
{
    public KeyboardControlator controlator;

    public void OnPointerClick(PointerEventData eventData)
    {
        var text = controlator.field.textComponent;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, eventData.position, eventData.pressEventCamera, out var local))
            return;
        text.ForceMeshUpdate();
        controlator.SetCaret(CaretMapper.LogicalIndexAt(text, controlator.field.text, local, controlator.ActiveKeyboardIsRtl));
    }
}
