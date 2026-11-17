using TMPro;
using UnityEngine;

// Mantiene el TMP_InputField igual que el búfer (texto confirmado + preedit) y detecta cambios externos.
public class InputFieldView : MonoBehaviour
{
    public TMP_InputField field;
    public KeyboardController controller;

    bool writing;

    void Start()
    {
        if (!string.IsNullOrEmpty(field.text)) controller.Session.ExternalTextChanged(field.text);
        controller.Buffer.Changed += Sync;
        field.onValueChanged.AddListener(OnFieldChanged);
        Sync();
    }

    void OnDestroy()
    {
        if (controller != null) controller.Buffer.Changed -= Sync;
        if (field != null) field.onValueChanged.RemoveListener(OnFieldChanged);
    }

    // SetTextWithoutNotify no avisa: se reconcilia en el siguiente frame.
    void Update()
    {
        if (field.text != controller.Buffer.Text) controller.Session.ExternalTextChanged(field.text);
    }

    void Sync()
    {
        writing = true;
        field.text = controller.Buffer.Text;
        writing = false;
    }

    void OnFieldChanged(string text)
    {
        if (!writing) controller.Session.ExternalTextChanged(text);
    }
}
