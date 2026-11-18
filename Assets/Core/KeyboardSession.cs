namespace Teclado.Core
{
    // Une distribución activa, estado de modificadores, compositor y búfer de texto.
    public class KeyboardSession
    {
        readonly TextBuffer buffer;

        public LayoutData Layout { get; private set; }
        public KeyboardState State { get; } = new KeyboardState();
        public IComposer Composer { get; private set; } = new DirectComposer();

        public KeyboardSession(TextBuffer buffer) => this.buffer = buffer;

        public void SetLayout(LayoutData layout)
        {
            FlushComposer();
            State.ConsumeOneShot();
            Layout = layout;
            Composer = layout != null ? ComposerFactory.Create(layout) : new DirectComposer();
        }

        public void PressKey(int scanCode)
        {
            var key = Layout?.Find(scanCode);
            if (key == null) return;
            int layer = (int)State.Resolve(key);
            string text = key.text[layer];
            if (string.IsNullOrEmpty(text))
            {
                State.ConsumeOneShot();   // un AltGr sin carácter no debe quedarse enganchado
                return;
            }
            Apply(Composer.Process(new KeyStroke(scanCode, text, key.dead[layer])));
            State.ConsumeOneShot();
        }

        // Shift y AltGr no tocan la composición: con una tecla muerta pendiente, Shift + e sigue dando É.
        public void Shift(double nowSeconds) => State.TapShift(nowSeconds);
        public void AltGr() => State.TapAltGr();

        public void Space()
        {
            Apply(Composer.Process(new KeyStroke(0x39, " ", false)));
            State.ConsumeOneShot();
        }

        public void Backspace()
        {
            if (Composer.TryBackspace(out var r)) buffer.SetPreedit(r.Preedit);
            else buffer.DeleteBackward();
            State.ConsumeOneShot();
        }

        public void Left() => Move(() => buffer.MoveVisual(left: true));
        public void Right() => Move(() => buffer.MoveVisual(left: false));
        public void SetCaret(int index) => Move(() => buffer.SetCaret(index));

        public void ExternalTextChanged(string text)
        {
            Composer.Cancel();
            buffer.ReplaceAll(text);
        }

        void Move(System.Action move)
        {
            FlushComposer();
            move();
            State.ConsumeOneShot();
        }

        void FlushComposer()
        {
            string pending = Composer.Flush();
            buffer.SetPreedit("");
            buffer.Commit(pending);
        }

        void Apply(ComposeResult r)
        {
            buffer.SetPreedit("");
            buffer.Commit(r.Commit);
            buffer.SetPreedit(r.Preedit);
        }
    }
}
