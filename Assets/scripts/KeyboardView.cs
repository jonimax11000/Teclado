using Teclado.Core;
using UnityEngine;

// Raíz de un teclado (rígido o canvas): une sus teclas con la distribución y la sesión.
public class KeyboardView : MonoBehaviour
{
    public KeyboardLayout layout;
    public KeyboardController controller;

    KeyView[] keys;
    bool started, subscribed;

    void Start()
    {
        started = true;
        Subscribe();
    }

    void OnEnable()
    {
        if (started) Subscribe();
    }

    void OnDisable() => Unsubscribe();

    void Subscribe()
    {
        if (subscribed || layout == null || controller == null) return;
        keys = GetComponentsInChildren<KeyView>(true);
        foreach (var k in keys) k.Pressed += OnKey;
        controller.Session.State.Changed += Refresh;
        subscribed = true;
        Refresh();
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        foreach (var k in keys) if (k != null) k.Pressed -= OnKey;
        if (controller != null) controller.Session.State.Changed -= Refresh;
        subscribed = false;
    }

    void OnKey(int scanCode) => controller.Session.PressKey(scanCode);

    // Teclas especiales: la plantilla las cablea a su propio KeyboardView, que reenvía al controlador.
    public void OnShift() => controller.OnShift();
    public void OnAltGr() => controller.OnAltGr();
    public void OnSpace() => controller.OnSpace();
    public void OnBackspace() => controller.OnBackspace();
    public void OnLeft() => controller.OnLeft();
    public void OnRight() => controller.OnRight();

    public void Refresh()
    {
        var state = controller.Session.State;
        foreach (var k in keys)
        {
            var def = layout.data.Find(k.scanCode);
            bool defined = def != null && System.Array.Exists(def.text, t => !string.IsNullOrEmpty(t));
            if (!defined)
            {
                k.gameObject.SetActive(false);
                continue;
            }
            k.SetLabel(def.text[(int)state.Resolve(def)]);
        }
    }
}
