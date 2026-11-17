using System;
using Teclado.Core;
using UnityEngine;

// Presentador: dueño del búfer de texto y de la sesión de teclado. Las teclas especiales de la escena llaman a On*.
public class KeyboardController : MonoBehaviour
{
    TextBuffer buffer;
    KeyboardSession session;

    public TextBuffer Buffer => buffer ??= new TextBuffer(RTLTMPro.TextUtils.IsRTLInput);
    public KeyboardSession Session => session ??= new KeyboardSession(Buffer);

    public int Caret => Buffer.Caret;
    public void SetCaret(int index) => Session.SetCaret(index);
    public event Action CaretChanged { add => Buffer.Changed += value; remove => Buffer.Changed -= value; }
    public bool ActiveKeyboardIsRtl => Session.Layout != null && Session.Layout.isRtl;

    public void OnShift() => Session.Shift(Time.unscaledTimeAsDouble);
    public void OnAltGr() => Session.AltGr();
    public void OnSpace() => Session.Space();
    public void OnBackspace() => Session.Backspace();
    public void OnLeft() => Session.Left();
    public void OnRight() => Session.Right();
}
