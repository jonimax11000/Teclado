using UnityEngine;
using UnityEngine.EventSystems;

// Tocar el campo (rayo XR o puntero) coloca el cursor lógico donde se toca.
public class FieldPointer : MonoBehaviour, IPointerClickHandler
{
    public InputFieldView input;

    public void OnPointerClick(PointerEventData eventData)
    {
        var text = input.field.textComponent;
        var controller = input.controller;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, eventData.position, eventData.pressEventCamera, out var local))
            return;
        text.ForceMeshUpdate();
        controller.SetCaret(CaretMapper.LogicalIndexAt(text, controller.Buffer.Text, local, controller.ActiveKeyboardIsRtl));
    }
}
