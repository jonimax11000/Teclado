using System;

namespace Teclado.Core
{
    // Shift y AltGr de un solo uso (en VR no se puede mantener una tecla); doble toque en Shift = Bloq Mayús.
    public class KeyboardState
    {
        public const double DoubleTapSeconds = 0.4;

        public bool Shift { get; private set; }
        public bool AltGr { get; private set; }
        public bool CapsLock { get; private set; }
        public event Action Changed;

        double lastShiftTap = double.NegativeInfinity;

        public void TapShift(double nowSeconds)
        {
            if (CapsLock)
            {
                CapsLock = false;
                Shift = false;
            }
            else if (nowSeconds - lastShiftTap < DoubleTapSeconds)
            {
                CapsLock = true;
                Shift = false;
                nowSeconds = double.NegativeInfinity;   // un tercer toque no cuenta como doble
            }
            else
            {
                Shift = !Shift;
            }
            lastShiftTap = nowSeconds;
            Changed?.Invoke();
        }

        public void TapAltGr()
        {
            AltGr = !AltGr;
            Changed?.Invoke();
        }

        public Layer Resolve(KeyDef key)
        {
            // Como en Windows: Bloq Mayús solo afecta a AltGr si la tecla lo declara (CAPLOKALTGR).
            bool caps = CapsLock && (AltGr ? key.caps == CapsMode.AffectsAltGr : key.caps != CapsMode.None);
            bool shift = Shift ^ caps;
            return (Layer)((AltGr ? 2 : 0) + (shift ? 1 : 0));
        }

        public void ConsumeOneShot()
        {
            if (!Shift && !AltGr) return;
            Shift = false;
            AltGr = false;
            Changed?.Invoke();
        }
    }
}
