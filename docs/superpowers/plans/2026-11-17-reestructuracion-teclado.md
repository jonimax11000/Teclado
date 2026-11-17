# Reestructuración del teclado — plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Distribuciones como datos importadas de Windows (`.klc`), núcleo sin Unity con compositores (directo, teclas muertas, Hangul) y preedit, y los 16 teclados regenerados desde 3 plantillas físicas.

**Architecture:** Modelo–Vista–Presentador guiado por datos.
- `Teclado.Core` (C# puro, sin Unity): `LayoutData`, `KeyboardState`, `IComposer` y sus implementaciones, `TextBuffer`, `KeyboardSession`.
- `Teclado.Runtime` (MonoBehaviours): `KeyboardLayout` (ScriptableObject), `KeyView`, `KeyboardView`, `KeyboardController`, `InputFieldView`, `PreeditView`, `LanguageSelector`, `KeyboardModeSwitcher`, más lo ya hecho (`CaretView`, `FieldPointer`, `CaretMapper`, `RtlShaper`).
- `Teclado.Editor`: `KlcImporter`, `TemplateBuilder`, `KeyboardGenerator`, `GlyphAudit`.

**Tech Stack:** Unity 6000.3.6f1, uGUI 2.0/TMP, RTLTMPro 4.0.0, Unity Test Framework 1.6.

**Spec:** `docs/superpowers/specs/2026-11-17-reestructuracion-teclado-design.md`

## Global Constraints

- Unity cerrado mientras se ejecuta. La escena y los prefabs solo se cambian con scripts de Editor (el hook bloquea editar `.unity`, `.prefab` y `.meta` a mano).
- `Teclado.Core` tiene `"noEngineReferences": true`.
- El texto que recibe el integrador (`TextBuffer.CommittedText`, evento `Committed`) nunca contiene preedit, formas de presentación árabes ni rich text.
- Cada idioma escribe lo mismo que su distribución de Windows para la misma secuencia de teclas, dentro de lo que define la tabla `.klc`.
- Añadir un idioma no modifica clases existentes. El único registro permitido es `ComposerFactory.Register`, y solo para compositores nuevos.
- Los ficheros `.klc` no se commitean: se descargan a `Klc/` (en la raíz del proyecto, ignorado por git). Sí se commitean los assets `Assets/Layouts/*.asset`.
- Commits terminan con `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

**Helper de Unity** (crear en el workspace del plan como `ut.sh`; mismo uso que en el plan del árabe):

```bash
#!/usr/bin/env bash
# ut.sh compile | ut.sh test PlayMode|EditMode [filtro] | ut.sh exec Clase.Metodo
U=~/Unity/Hub/Editor/6000.3.6f1/Editor/Unity; P=/home/jonimax11000/Desktop/Teclado; O=$(dirname "$0")/out; mkdir -p $O
case "$1" in
  compile) $U -batchmode -nographics -projectPath $P -logFile $O/compile.log -quit; echo "exit=$?"; grep "error CS" $O/compile.log | sort -u | head -20 ;;
  exec) $U -batchmode -nographics -projectPath $P -executeMethod "$2" -logFile $O/exec.log -quit; echo "exit=$?"; grep -E "error CS|Exception|\[Gen\]|\[Klc\]|\[Tpl\]|\[Audit\]" $O/exec.log | sort -u | head -40 ;;
  test) F=""; [ -n "$3" ] && F="-testFilter $3"; rm -f $O/r.xml
        $U -batchmode -nographics -projectPath $P -runTests -testPlatform $2 $F -testResults $O/r.xml -logFile $O/t.log; echo "exit=$?"
        grep "error CS" $O/t.log | sort -u | head -20; [ -f $O/r.xml ] || { echo "NO RESULTS"; exit 1; }
        grep -oE '<test-run [^>]*' $O/r.xml | grep -oE '(total|passed|failed)="[0-9]+"' | tr '\n' ' '; echo
        grep -oE 'methodname="[^"]*"[^>]*result="(Failed)"' $O/r.xml | grep -oE 'methodname="[^"]*"'
        ! grep -q '<test-run[^>]*failed="[1-9]' $O/r.xml ;;
esac
```

Lección de la rama anterior: en tests de glifos usa comparación **ordinal** (`IndexOf(char)` / `==`). `StringAssert.Contains` iguala `ﻻ` y `لا`.

## Review Focus

1. **Tecla muerta pendiente y luego flecha, toque en el campo o cambio de idioma:** se confirma el acento suelto, no se pierde ni se queda colgando. → test en Task 5.
2. **Triple toque en Shift y Shift con Bloq Mayús activo:** el tercer toque deja Shift de un solo uso sin Bloq Mayús, y un toque con Bloq Mayús activo lo desactiva. → test en Task 1.
3. **Coreano, borrar después de confirmar una sílaba:** se borra el carácter anterior completo, no se recompone. → test en Task 4.
4. **El integrador vacía `field.text` en mitad de una composición:** no queda preedit fantasma y la siguiente tecla empieza composición nueva. → test en Task 7.
5. **Plantilla sin la tecla que el layout define (OEM_102 en ANSI) o tecla de plantilla sin definir:** sin excepción; la tecla sobrante queda desactivada y se avisa. → test en Task 9.

---

### Task 1: `Teclado.Core`: modelo de datos y `KeyboardState`

**Files:**
- Create: `Assets/Core/Teclado.Core.asmdef` (`noEngineReferences: true`, `autoReferenced: true`)
- Create: `Assets/Core/LayoutData.cs`, `Assets/Core/KeyboardState.cs`
- Create: `Assets/Tests/Core/Teclado.Tests.Core.asmdef` (EditMode: `includePlatforms: ["Editor"]`, refs `Teclado.Core`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; nunit; `UNITY_INCLUDE_TESTS`)
- Test: `Assets/Tests/Core/KeyboardStateTests.cs`

**Interfaces (namespace `Teclado.Core`, todo `[Serializable]` de `System`):**
- Produces:
  - `public enum Layer { Base = 0, Shift = 1, AltGr = 2, ShiftAltGr = 3 }`
  - `public enum CapsMode { None = 0, Affects = 1 }` (`SGCap` del KLC se importa como `Affects`)
  - `public enum PhysicalTemplate { ISO, ANSI, ANSI14 }`
  - `public enum ComposerKind { Direct, DeadKey, Hangul }`
  - `public class KeyDef { public int scanCode; public string[] text = new string[4]; public bool[] dead = new bool[4]; public CapsMode caps; }`
  - `public class DeadKeyEntry { public string from; public string to; }`
  - `public class DeadKeyTable { public string deadChar; public List<DeadKeyEntry> entries = new(); public bool TryGet(string from, out string to); }`
  - `public class LayoutData { public string displayName; public int order; public string systemLanguage; public PhysicalTemplate template; public ComposerKind composer; public bool isRtl; public List<KeyDef> keys = new(); public List<DeadKeyTable> deadKeys = new(); public KeyDef Find(int scanCode); public DeadKeyTable FindDead(string deadChar); }` (`systemLanguage` es el nombre del enum `UnityEngine.SystemLanguage` como `string`, porque el núcleo no referencia Unity)
  - `public class KeyboardState { public bool Shift { get; } public bool AltGr { get; } public bool CapsLock { get; } public event Action Changed; public void TapShift(double nowSeconds); public void TapAltGr(); public Layer Resolve(KeyDef key); public void ConsumeOneShot(); public const double DoubleTapSeconds = 0.4; }`
- Regla de `Resolve`: `shift = Shift XOR (CapsLock && key.caps == Affects)`; capa = `(AltGr ? 2 : 0) + (shift ? 1 : 0)`.
- Regla de `TapShift`:
  - Si `CapsLock` está activo: lo desactiva y deja `Shift = false`.
  - Si no, y el toque anterior fue hace menos de `DoubleTapSeconds`: `CapsLock = true`, `Shift = false`.
  - Si no: invierte `Shift`.

- [ ] **Step 1: Tests**
  - `Resolve_Base`: estado vacío → `Layer.Base`.
  - `Shift_UnSoloUso`: `TapShift(0)` → `Resolve == Shift`; `ConsumeOneShot()` → `Resolve == Base`.
  - `DobleToque_BloqMayus`: `TapShift(0); TapShift(0.2)` → `CapsLock`, `!Shift`.
  - `TripleToque_SinBloqueo`: `TapShift(0); TapShift(0.2); TapShift(0.3)` → `!CapsLock`, `!Shift`.
  - `ToqueLentoNoBloquea`: `TapShift(0); TapShift(1.0)` → `!CapsLock`, `!Shift` (dos toques sueltos se anulan).
  - `BloqMayus_RespetaCap`: con `CapsLock`, una tecla `caps = Affects` → `Shift`; una tecla `caps = None` (`1`) → `Base`.
  - `BloqMayusMasShift`: con `CapsLock` y `TapShift(5)` → `!CapsLock` (lo desactiva).
  - `AltGr`: `TapAltGr()` → `AltGr`; con Shift → `ShiftAltGr`; `ConsumeOneShot()` limpia ambos y no toca `CapsLock`.
  - `Changed_SeLanza` en cada cambio.
- [ ] **Step 2:** `ut.sh test EditMode KeyboardStateTests` → FAIL (no compila).
- [ ] **Step 3:** Implementa.
- [ ] **Step 4:** `ut.sh test EditMode KeyboardStateTests` → PASS.
- [ ] **Step 5:** Commit `Core: modelo de distribución y KeyboardState`.

### Task 2: `TextBuffer`

**Files:**
- Create: `Assets/Core/TextBuffer.cs`, `Assets/Core/Ligatures.cs`
- Modify: `Assets/scripts/CaretMapper.cs`: `IsInsideLigature` delega en `Teclado.Core.Ligatures.IsInside` (misma firma pública, para no romper llamadas)
- Modify: `Assets/scripts/Teclado.Runtime.asmdef` (añadir ref `Teclado.Core`)
- Test: `Assets/Tests/Core/TextBufferTests.cs`

**Interfaces:**
- Produces:
  - `public static class Ligatures { public static bool IsInside(string text, int index); }` (lógica actual de `CaretMapper.IsInsideLigature`)
  - `public class TextBuffer`:
    - `public TextBuffer(Func<string, bool> isRtl = null)`
    - `public string CommittedText { get; }` · `public string Preedit { get; }` · `public int Caret { get; }` (índice en `CommittedText`)
    - `public string Text => CommittedText.Insert(Caret, Preedit)` · `public int PreeditStart => Caret`
    - `public event Action Changed` · `public event Action<string> Committed`
    - `public void Commit(string s)` (inserta en el cursor, avanza, sale de ligadura, lanza `Committed(s)` si no está vacío)
    - `public void SetPreedit(string s)`
    - `public bool DeleteBackward()` (`false` si `Caret == 0`)
    - `public void MoveVisual(bool left)` (si `isRtl(CommittedText)`, izquierda = +1 lógico; salta el interior de ligaduras)
    - `public void SetCaret(int i)` (acota y sale de ligadura hacia delante)
    - `public void ReplaceAll(string text)` (cambio externo: texto nuevo, preedit vacío, cursor al final)
  - Todas las operaciones acotan `Caret` a `CommittedText.Length` antes de usarlo (lección del `SetTextWithoutNotify`).

- [ ] **Step 1: Tests.** Portan los de `KeyboardControlatorTests`, con `isRtl = s => s.Any(c => c >= '؀' && c <= 'ۿ')` en el test:
  - `Escribir_SeGuardaLogico` (`سلام`, cursor 4)
  - `LigaduraNoCorrompe` (borrar ×2 → `سل`; izquierda en RTL → 2; derecha → 1; insertar `ي` → `سيل`)
  - `FlechasEnLatin`, `SaltaLigadura` (`سلا`, `SetCaret(2)` → 3; derecha → 1)
  - `Extremos`, `DosCodePoints_BorraUnoCadaVez`, `InsertarAntesDeAlef_NoQuedaDentroDeLigadura`, `BorrarFormaLigadura_CursorFuera`
  - `CursorObsoleto_NoRompe`: `ReplaceAll("abc")`, cursor 3, luego simula texto acortado vía `ReplaceAll("")` → `DeleteBackward()` devuelve `false` sin excepción
  - Nuevos:
    - `Preedit_NoEstaEnCommitted`: `Commit("a"); SetPreedit("´")` → `Text == "a´"`, `CommittedText == "a"`, `PreeditStart == 1`
    - `Committed_SoloTextoConfirmado`: el evento recibe `"a"` y nunca `"´"`
- [ ] **Step 2:** `ut.sh test EditMode TextBufferTests` → FAIL.
- [ ] **Step 3:** Implementa. Mueve la lógica de ligaduras a `Ligatures` y deja `CaretMapper.IsInsideLigature` como envoltorio de una línea.
- [ ] **Step 4:** `ut.sh test EditMode` (Core) y `ut.sh test PlayMode` (los 36 actuales siguen en verde) → PASS.
- [ ] **Step 5:** Commit `Core: TextBuffer con preedit`.

### Task 3: Compositores directo y de teclas muertas

**Files:**
- Create: `Assets/Core/Composing.cs` (`KeyStroke`, `ComposeResult`, `IComposer`, `ComposerFactory`), `Assets/Core/DirectComposer.cs`, `Assets/Core/DeadKeyComposer.cs`
- Test: `Assets/Tests/Core/DeadKeyComposerTests.cs`, `Assets/Tests/Core/TestLayouts.cs` (layouts mínimos construidos en código)

**Interfaces:**
- Produces:
  - `public readonly struct KeyStroke { public readonly int ScanCode; public readonly string Text; public readonly bool IsDead; public KeyStroke(int sc, string text, bool isDead); }`
  - `public readonly struct ComposeResult { public readonly string Commit; public readonly string Preedit; public ComposeResult(string commit, string preedit); public static readonly ComposeResult None; }`
  - `public interface IComposer`:
    - `ComposeResult Process(KeyStroke key)`
    - `bool TryBackspace(out ComposeResult result)` (`false` si no hay composición; entonces borra el búfer)
    - `string Flush()` · `void Cancel()`
    - `IReadOnlyList<string> Candidates { get; }` · `ComposeResult SelectCandidate(int index)`
  - `public static class ComposerFactory { public static void Register(ComposerKind kind, Func<LayoutData, IComposer> create); public static IComposer Create(LayoutData layout); }` (registra `Direct`, `DeadKey` y `Hangul` en un constructor estático)
- Semántica de `DeadKeyComposer` (Windows):
  - Tecla muerta → `Preedit = deadChar`.
  - Siguiente tecla con entrada en la tabla → `Commit = to`.
  - Espacio (`" "`) → `Commit` = entrada `0020` de la tabla, o `deadChar` si no existe.
  - Siguiente tecla sin entrada → `Commit = deadChar + texto`.
  - Otra tecla muerta sin entrada → `Commit = deadChar`, y la nueva queda pendiente.
  - `TryBackspace` con pendiente → `true`, cancela y deja el preedit vacío.
  - `Flush` → devuelve `deadChar` y limpia.

- [ ] **Step 1: Tests** con `TestLayouts.SpanishLike()`: `´`(dead) → {a→á, e→é, 0020→´}; `¨`(dead) → {u→ü}; `^`(dead) → {a→â}; `` ` ``(dead) → {a→à}.
  - `Acento_e`: `´`,`e` → commit `é`.
  - `Acento_Espacio`: `´`,`" "` → `´`.
  - `Acento_Consonante`: `´`,`t` → `´t`.
  - `Dieresis_u`, `Circunflejo_a`, `Grave_a`.
  - `DosMuertas`: `´`,`¨` → commit `´`, preedit `¨`.
  - `Backspace_Cancela`: `´`, `TryBackspace` → `true`, preedit `""`; sin pendiente → `false`.
  - `Flush_ConfirmaAcento`.
  - `Directo_Pasa`: `DirectComposer` devuelve el texto tal cual.
  - `Factory_CreaSegunKind`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementa. **Step 4:** PASS (`ut.sh test EditMode`).
- [ ] **Step 5:** Commit `Core: compositores directo y de teclas muertas`.

### Task 4: `HangulComposer`

**Files:**
- Create: `Assets/Core/HangulComposer.cs`
- Test: `Assets/Tests/Core/HangulComposerTests.cs`

**Interfaces:**
- Consumes: `IComposer`, `KeyStroke` (Task 3). El texto de la tecla es un jamo de compatibilidad (U+3131–U+318E).
- Algoritmo (no lo determinan las firmas):
  - Pila de jamo de la sílaba actual: inicial, vocal (compuesta), final (doble).
  - Sílaba = `0xAC00 + (L*21 + V)*28 + T`, con los índices estándar de 19 iniciales, 21 vocales y 28 finales.
  - Vocales compuestas: ㅗ+ㅏ=ㅘ, ㅗ+ㅐ=ㅙ, ㅗ+ㅣ=ㅚ, ㅜ+ㅓ=ㅝ, ㅜ+ㅔ=ㅞ, ㅜ+ㅣ=ㅟ, ㅡ+ㅣ=ㅢ.
  - Finales dobles: ㄱ+ㅅ=ㄳ, ㄴ+ㅈ=ㄵ, ㄴ+ㅎ=ㄶ, ㄹ+ㄱ=ㄺ, ㄹ+ㅁ=ㄻ, ㄹ+ㅂ=ㄼ, ㄹ+ㅅ=ㄽ, ㄹ+ㅌ=ㄾ, ㄹ+ㅍ=ㄿ, ㄹ+ㅎ=ㅀ, ㅂ+ㅅ=ㅄ.
  - ㄸ ㅃ ㅉ no pueden ser finales.
  - Vocal tras final: la final (o la segunda mitad de una doble) pasa a ser inicial de la sílaba nueva y se confirma la anterior.
  - Un carácter que no es jamo confirma la sílaba y pasa tal cual.
  - `TryBackspace` quita el último jamo de la pila.

- [ ] **Step 1: Tests** (secuencias de jamo; el resultado es el commit acumulado más el preedit):
  - `Hangeul`: ㅎㅏㄴㄱㅡㄹ → `한글`
  - `GaGa`: ㄱㅏㄱㅏ → `가가`
  - `DalGa`: ㄷㅏㄹㄱㅏ → `달가`
  - `Wa`: ㅇㅗㅏ → `와`
  - `DobleFinal`: ㄷㅏㄹㄱ → preedit `닭`
  - `Backspace_Descompone`: ㅎㅏㄴ, `TryBackspace` → preedit `하`; otra vez → `ㅎ`; otra → `""` y `true`; otra → `false`
  - `BackspaceTrasConfirmar` (Review Focus 3): ㅎㅏㄴ, `Flush()` → `한`; `TryBackspace` → `false` (lo borra el búfer entero)
  - `NoJamo_Confirma`: ㅎㅏ, `"1"` → commit `하1`
  - `ConsonantesSolas`: ㄱㄱ → commit `ㄱ`, preedit `ㄱ`
  - `DdNoEsFinal`: ㅏ… (ㄱㅏㄸ) → commit `가`, preedit `ㄸ`
- [ ] **Step 2:** FAIL. **Step 3:** Implementa y registra en `ComposerFactory`. **Step 4:** PASS.
- [ ] **Step 5:** Commit `Core: compositor Hangul (Dubeolsik)`.

### Task 5: `KeyboardSession`

**Files:**
- Create: `Assets/Core/KeyboardSession.cs`
- Test: `Assets/Tests/Core/KeyboardSessionTests.cs`

**Interfaces:**
- Consumes: Tasks 1–4.
- Produces: `public class KeyboardSession`:
  - `public KeyboardSession(TextBuffer buffer)`
  - `public LayoutData Layout { get; }` · `public KeyboardState State { get; }` · `public IComposer Composer { get; }`
  - `public void SetLayout(LayoutData layout)` (confirma lo pendiente y crea el compositor con la factoría)
  - `public void PressKey(int scanCode)`:
    - Resuelve la capa con `State`. Un texto vacío no hace nada.
    - Pasa `KeyStroke` al compositor y aplica `Commit`/`Preedit` al búfer.
    - Llama a `State.ConsumeOneShot()`.
  - `public void Shift(double now)` · `public void AltGr()`: no tocan la composición.
  - `public void Space()`: es `PressKey`-equivalente con texto `" "`; pasa por el compositor y consume el modo de un solo uso.
  - `public void Backspace()`: `TryBackspace` del compositor, o si no, `buffer.DeleteBackward()`; consume el modo de un solo uso.
  - `public void Left()` · `public void Right()` · `public void SetCaret(int i)`: confirman lo pendiente, mueven y consumen el modo de un solo uso.
  - `public void ExternalTextChanged(string text)`: `Composer.Cancel()` y `buffer.ReplaceAll(text)`.

- [ ] **Step 1: Tests** con `TestLayouts.SpanishLike()` (añade scancodes: `0x28`=`´`/`¨` dead, `0x12`=`e`/`E`, `0x16`=`u`/`U`):
  - `MuertaYLetra`: `PressKey(0x28)`, `PressKey(0x12)` → `CommittedText == "é"`.
  - `ShiftEnMedioDeMuerta`: `PressKey(0x28)`, `Shift(0)`, `PressKey(0x12)` → `"É"` (la entrada `E→É` existe en la tabla).
  - `ShiftUnSoloUso`: `Shift(0)`, `PressKey(0x12)`, `PressKey(0x12)` → `"Ee"`.
  - `MuertaYFlecha_ConfirmaAcento` (Review Focus 1): `"ab"` y cursor 1; `PressKey(0x28)`, `Left()` → `CommittedText == "a´b"`, preedit vacío, cursor 1.
  - `MuertaYCambioDeLayout`: `PressKey(0x28)`, `SetLayout(otro)` → `"´"`.
  - `MuertaYSetCaret`: igual con `SetCaret(0)`.
  - `CambioExterno_CancelaComposicion`: `PressKey(0x28)`, `ExternalTextChanged("")` → `Text == ""`; `PressKey(0x12)` → `"e"`.
  - `Backspace_SinComposicion_BorraBufer`.
  - `TeclaInexistente_NoHaceNada`: `PressKey(0x99)`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementa. **Step 4:** PASS.
- [ ] **Step 5:** Commit `Core: KeyboardSession`.

### Task 6: `KlcImporter` y las 8 distribuciones

**Files:**
- Create: `Assets/scripts/KeyboardLayout.cs` (`[CreateAssetMenu] public class KeyboardLayout : ScriptableObject { public LayoutData data = new(); }`)
- Create: `Assets/Core/TemplateScancodes.cs` (ver Interfaces; lo consume Task 8)
- Create: `Assets/Editor/KlcParser.cs` (puro: texto → `LayoutData`), `Assets/Editor/KlcImporter.cs` (menú *Teclado/Importar distribución .klc…* y `ImportAll()` para batchmode), `Assets/Editor/DubeolsikTable.cs`
- Create: `Assets/Layouts/{English,Español,Français,Deutsch,Русский,한국어,Ελληνικά,العربية}.asset`
- Modify: `.gitignore` (añadir `/Klc/`)
- Test: `Assets/Tests/EditMode/KlcParserTests.cs`, `Assets/Tests/EditMode/LayoutAssetsTests.cs`

**Interfaces:**
- Produces:
  - `TemplateScancodes` (namespace `Teclado.Core`), con la definición exacta de Task 8.
  - `public static class KlcParser { public static LayoutData Parse(string klcText, LayoutData into = null); }`. Si se pasa `into`, conserva `displayName`, `order`, `systemLanguage`, `template`, `composer` e `isRtl`.
  - `public static class DubeolsikTable { public static void ApplyTo(LayoutData layout); }`:
    - Base: `10..19` = ㅂㅈㄷㄱㅅㅛㅕㅑㅐㅔ; `1E..26` = ㅁㄴㅇㄹㅎㅗㅓㅏㅣ; `2C..32` = ㅋㅌㅊㅍㅠㅜㅡ.
    - Shift: `10..14` = ㅃㅉㄸㄲㅆ, `18` = ㅒ, `19` = ㅖ; el resto repite la base.
    - Pone `caps = None` en esas teclas.
  - `KlcImporter.ImportAll()`: lee `Klc/<file>.klc` para la tabla de abajo, actualiza o crea cada asset y loguea `[Klc] <nombre>: N teclas, M muertas`.
- Reglas del parser:
  - El fichero es UTF-16 (detecta el BOM).
  - Las columnas salen de `SHIFTSTATE`; usa los estados 0, 1, 6 y 7.
  - En `LAYOUT`, un token de 4 hex es un code point; si no, es un carácter literal.
  - `-1` es vacío. El sufijo `@` marca tecla muerta. `%%` es ligadura, que se resuelve con `LIGATURE`, buscando el VK y luego el scancode.
  - `Cap`: `1` → `Affects`; `SGCap` → `Affects`, y se ignora la línea de continuación.
  - `DEADKEY xxxx`: pares `from to`.
  - Se ignoran los códigos < U+0020.

| Asset | `.klc` (kbdlayout.info `/<id>/download/klc`) | order | systemLanguage | template | composer | isRtl |
|---|---|---|---|---|---|---|
| English | kbdus | 0 | English | ANSI | Direct | no |
| Español | kbdsp | 1 | Spanish | ISO | DeadKey | no |
| Français | kbdfr | 2 | French | ISO | DeadKey | no |
| Deutsch | kbdgr | 3 | German | ISO | DeadKey | no |
| Русский | kbdru | 4 | Russian | ANSI14 | Direct | no |
| 한국어 | kbdus + `DubeolsikTable` | 5 | Korean | ANSI | Hangul | no |
| Ελληνικά | kbdhe | 6 | Greek | ANSI14 | DeadKey | no |
| العربية | kbda2 | 7 | Arabic | ISO | Direct | sí |

- [ ] **Step 1: Tests del parser** con fragmentos `.klc` sintéticos en el propio test:
  - `Capas`: una línea `10 Q 1 q Q -1 -1 -1` → base `q`, shift `Q`, caps `Affects`.
  - `Hex`: `29 OEM_5 0 00ba 00aa -1 005c -1` → `º`, `ª`, AltGr `\`.
  - `Muerta`: `28 OEM_7 0 00b4@ 00a8@ -1 007b -1` → `dead[0]` y `dead[1]`.
  - `Deadkey`: `DEADKEY 00b4` + `0061 00e1` + `0020 00b4` → la tabla de `´` tiene a→á y espacio→´.
  - `Ligadura`: `30 B 0 %% ...` + `LIGATURE` `B 0 0644 0627` → base `لا`.
  - `ConservaMetadatos`: `Parse(text, into)` mantiene `displayName` y `order`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementa `KlcParser`, `KlcImporter` y `DubeolsikTable`.
- [ ] **Step 4:** Descarga los 7 `.klc` a `Klc/` (`curl -sLf https://kbdlayout.info/<id>/download/klc -o Klc/<id>.klc`). Ejecuta `ut.sh exec KlcImporter.ImportAll`. Esperado: 8 líneas `[Klc]` sin `Exception`.
- [ ] **Step 5: Tests de assets** (`LayoutAssetsTests`, carga desde `Assets/Layouts`):
  - `Hay8_OrdenCorrecto`: por `order`, los `displayName` son exactamente English, Español, Français, Deutsch, Русский, 한국어, Ελληνικά, العربية.
  - `Espanol_TildeMuerta`: scancode `0x28` base `´` muerta; la tabla de `´` tiene `e→é`.
  - `Frances_Circunflejo`: la tabla de `^` tiene `e→ê`. `Aleman_Circunflejo`: `o→ô`.
  - `Griego_Tonos`: `΄`+`α`→`ά`, `¨`+`ι`→`ϊ`, `΅`+`ι`→`ΐ`.
  - `Coreano_Jamo`: `0x22` (G) base `ㅎ`, `0x10` shift `ㅃ`.
  - `Arabe_IgualQueJson`: para cada fila de `Assets/Editor/Layouts/Arabic102.json`, con el orden de scancodes de plantilla ISO (Task 8), base/shift/altgr coinciden.
- [ ] **Step 6:** `ut.sh test EditMode` → PASS.
- [ ] **Step 7:** Commit `Distribuciones como datos: importador .klc y 8 layouts`.

### Task 7: Vistas y presentador en runtime

**Files:**
- Create: `Assets/scripts/KeyboardController.cs`, `Assets/scripts/InputFieldView.cs`, `Assets/scripts/PreeditView.cs`, `Assets/scripts/KeyView.cs`, `Assets/scripts/KeyboardView.cs`, `Assets/scripts/LanguageSelector.cs`, `Assets/scripts/KeyboardModeSwitcher.cs`
- Modify: `Assets/scripts/CaretView.cs`, `Assets/scripts/FieldPointer.cs` (dependen de `InputFieldView` en vez de `KeyboardControlator`)
- Modify: `Assets/Tests/PlayMode/TestRig.cs` (monta `KeyboardController` + `InputFieldView` en vez de `KeyboardControlator`)
- Modify: `Assets/Tests/PlayMode/CaretViewTests.cs`, `FieldPointerTests.cs` (adaptar al rig)
- Test: `Assets/Tests/PlayMode/InputFieldViewTests.cs`, `Assets/Tests/PlayMode/KeyViewTests.cs`, `Assets/Tests/PlayMode/LanguageSelectorTests.cs`
- Las clases antiguas (`Keyboard`, `KeyboardButton`, `KeyboardControlator`) **siguen existiendo** hasta Task 9; sus tests PlayMode antiguos se borran aquí (`KeyboardControlatorTests`, `KeyboardButtonTests`), porque sus casos ya están portados (Tasks 2 y 5, y abajo).

**Interfaces:**
- Consumes: `KeyboardSession`, `TextBuffer`, `LayoutData`; `CaretMapper`, `RtlShaper`.
- Produces:
  - `public class KeyboardController : MonoBehaviour`:
    - `public TextBuffer Buffer { get; }` · `public KeyboardSession Session { get; }` (creados en `Awake`, con `isRtl = RTLTMPro.TextUtils.IsRTLInput`)
    - `public int Caret => Buffer.Caret` · `public void SetCaret(int i) => Session.SetCaret(i)`
    - `public event Action CaretChanged` (reenvía `Buffer.Changed`)
    - `public bool ActiveKeyboardIsRtl => Session.Layout != null && Session.Layout.isRtl`
    - Métodos para eventos de escena: `public void OnShift()`, `OnAltGr()`, `OnSpace()`, `OnBackspace()`, `OnLeft()`, `OnRight()` (`OnShift` pasa `Time.unscaledTimeAsDouble`)
  - `public class InputFieldView : MonoBehaviour`:
    - `public TMP_InputField field; public KeyboardController controller;`
    - Escribe `field.text = Buffer.Text` en `Buffer.Changed`, con un guard.
    - En `field.onValueChanged` externo llama a `Session.ExternalTextChanged(field.text)`.
    - Mantiene en `RTLTextMeshPro` `Farsi = false` y `PreserveNumbers = true`.
  - `CaretView`: `public InputFieldView input; public RectTransform caret; public float blinkPeriod = 0.5f;` (la lógica actual, leyendo `input.controller`)
  - `FieldPointer`: `public InputFieldView input;`
  - `public class PreeditView : MonoBehaviour { public InputFieldView input; public RectTransform underline; }`: dibuja la línea entre `CaretMapper.CaretX(start)` y `CaretX(start + preedit.Length)` sobre `Buffer.Text`; se oculta si no hay preedit.
  - `public class KeyView : MonoBehaviour`:
    - `public int scanCode; public TextMeshProUGUI label; public event Action<int> Pressed;`
    - Engancha `ButtonVR.onRelease` o `CanvasButton.onRelease` en `Start`.
    - `public void SetLabel(string logical)`: vacío → `gameObject.SetActive(false)`; una sola marca combinante (`UnicodeCategory.NonSpacingMark`) → `"◌" + texto`; RTL → `RtlShaper.Shape` + `isRightToLeftText`.
  - `public class KeyboardView : MonoBehaviour`:
    - `public KeyboardLayout layout; public KeyboardController controller;`
    - `OnEnable`: se suscribe a `State.Changed` y a `KeyView.Pressed` de sus hijos, y refresca las etiquetas con la capa que da `State.Resolve(key)`. Las teclas muertas muestran su `deadChar`.
    - `OnDisable`: se desuscribe.
  - `public class LanguageSelector : MonoBehaviour`:
    - `[Serializable] public class Entry { public KeyboardLayout layout; public GameObject rigid; public GameObject canvas; }`
    - `public TMP_Dropdown dropdown; public KeyboardController controller; public List<Entry> entries;`
    - `Start` rellena las opciones con `displayName` por `order` y elige según `Application.systemLanguage.ToString() == systemLanguage` (si no, índice 0).
    - `public void Select(int index)`: activa el par rígido/canvas y llama a `Session.SetLayout`.
  - `public class KeyboardModeSwitcher : MonoBehaviour { public GameObject rigidRoot, canvasRoot; public void ChangeToCanvas(); public void ChangeToRigid(); }` (la corrutina de 1 s actual)

- [ ] **Step 1: Tests.** Rig: `KeyboardController` + `InputFieldView` + campo `RTLTextMeshPro` + `KeyboardView` con un `KeyboardLayout` creado en memoria (`ScriptableObject.CreateInstance`) a partir de `TestLayouts.SpanishLike()`. Copia `TestLayouts` a `Assets/Tests/PlayMode/TestLayouts.cs` (`Teclado.Tests.Core` es solo de editor y PlayMode no puede referenciarlo).
  - `InputFieldViewTests`:
    - `Preedit_EnCampoNoEnCommitted`: tecla muerta → `field.text == "´"`, `CommittedText == ""`.
    - `CambioExterno_SinPreeditFantasma` (Review Focus 4): tecla muerta, `field.text = ""` → `Buffer.Preedit == ""`; tecla `e` → `field.text == "e"`.
    - `SetTextWithoutNotify_NoRompe` (portado).
    - `PreeditView_SubrayaRango`: tras una tecla muerta, el ancho de `underline` > 0, y vale 0 (oculto) tras confirmar.
  - `KeyViewTests`:
    - `Etiqueta_SegunCapa`: Shift → etiqueta `E`.
    - `Muerta_MuestraAcento`.
    - `Marca_SobreCirculo`: texto `َ` → etiqueta `"◌َ"`.
    - `Arabe_LamAlefUnida`: comparación ordinal, contiene `ﻻ`; pulsar inserta `لا`.
    - `Vacia_Desactiva`.
    - `Pulsar_Escribe`: `CanvasButton.onRelease.Invoke()` → `CommittedText == "e"`. Recuerda crear los `UnityEvent` a mano en el rig.
  - `LanguageSelectorTests`: `Select(1)` activa el par 1 y desactiva el resto; `Session.Layout == entries[1].layout.data`; las opciones del dropdown siguen el `order`.
  - `CaretViewTests` y `FieldPointerTests`: los 15 casos actuales, adaptados al rig, en verde.
- [ ] **Step 2:** FAIL. **Step 3:** Implementa. Borra `KeyboardControlatorTests.cs` y `KeyboardButtonTests.cs`.
- [ ] **Step 4:** `ut.sh test PlayMode` y `ut.sh test EditMode` → PASS. Compila sin `error CS`; la escena aún usa las clases antiguas.
- [ ] **Step 5:** Commit `Vistas y presentador sobre el núcleo nuevo`.

### Task 8: Plantillas físicas e informe de paridad

**Files:**
- Create: `Assets/Editor/TemplateBuilder.cs`, `Assets/Editor/ParityReport.cs`
- Create (generados): `Assets/Prefabs/Templates/{ISO,ANSI,ANSI14}_{Rigid,Canvas}.prefab`, `docs/superpowers/reports/2026-11-17-paridad-teclados.md`
- Test: `Assets/Tests/EditMode/TemplateBuilderTests.cs`

**Interfaces:**
- Consumes: `TemplateScancodes` (creado en Task 6):
  - `public static class TemplateScancodes { public static int[][] Rows(PhysicalTemplate t); }`: devuelve 4 filas (Numbers, Row 1, Row 2, Row 3) con los scancodes de izquierda a derecha:
    - Numbers: `29 02 03 04 05 06 07 08 09 0A 0B 0C 0D` (+`2B` al final en ANSI14).
    - Row 1: `10 11 12 13 14 15 16 17 18 19 1A 1B` (+`2B` al final en ANSI).
    - Row 2: `1E 1F 20 21 22 23 24 25 26 27 28` (+`2B` al final en ISO).
    - Row 3: `2C 2D 2E 2F 30 31 32 33 34 35` (+`56` al principio en ISO).
- Produces:
  - `public static class TemplateBuilder`:
    - `[MenuItem("Teclado/Crear plantillas")] public static void BuildAll()`
    - `public static GameObject Build(GameObject sourceKeyboard, PhysicalTemplate t, string prefabPath)`
    - Fuentes: ISO = `QWERTY ESP`, ANSI = `QWERTY US`, ANSI14 = `JCUKEN`, de `ByRigidBodies` y `ByCanvas`.
    - Pasos sobre un **clon** (no toca el original):
      - Quita `Keyboard` y pone `KeyboardView`.
      - En cada tecla, quita `KeyboardButton` y pone `KeyView` con su scancode y su etiqueta.
      - Borra las teclas de tilde (`closed open diaeresis circumflex circunflex virgulilla dieresis_accented`).
      - Recablea las especiales: reemplaza los listeners persistentes de `ButtonVR`/`CanvasButton` (`CapsPressed`→`OnShift`, `AlterPressed`→`OnAltGr`, `insertSpace`→`OnSpace`, `deleteChar`→`OnBackspace`, `LeftPressed`→`OnLeft`, `RightPressed`→`OnRight`). El target queda sin asignar en el prefab; lo pone el generador.
      - Guarda con `PrefabUtility.SaveAsPrefabAsset` y loguea `[Tpl]`.
    - Si el número de teclas de una fila no coincide con `TemplateScancodes`, lanza excepción.
  - `ParityReport.Write()`: para cada teclado antiguo vivo, con su plantilla y su layout de la tabla de Task 6, compara por fila e índice `normalForms[0]`/`CapForms[0]` frente a base/shift del layout. Escribe una tabla Markdown por idioma, solo con las diferencias, y el total.

- [ ] **Step 1: Tests** (abren la escena, construyen en memoria en una ruta temporal `Assets/Tests/Tmp/` y la borran en `TearDown`):
  - `ISO_ScancodesEnOrden`: Row 3 índice 0 → `0x56`; Row 2 índice 11 → `0x2B`.
  - `ANSI14_BarraEnNumeros`: Numbers índice 13 → `0x2B`.
  - `SinTeclasDeTilde`.
  - `Especiales_Recableadas`: el `Caps` de la plantilla ISO tiene un listener persistente con método `OnShift`.
  - `NoTocaElOriginal`: tras `Build`, el teclado de escena sigue teniendo `Keyboard`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementa.
- [ ] **Step 4:** PASS. Ejecuta `ut.sh exec TemplateBuilder.BuildAll` → 6 líneas `[Tpl]`. Ejecuta `ut.sh exec ParityReport.Write` → genera el informe.
- [ ] **Step 5:** Commit `Plantillas físicas e informe de paridad` (incluye los 6 prefabs y el informe).

### Task 9: `KeyboardGenerator` y migración de la escena

**Files:**
- Create: `Assets/Editor/KeyboardGenerator.cs`
- Test: `Assets/Tests/EditMode/KeyboardGeneratorTests.cs` (sustituye a `ArabicKeyboardSetupTests.cs`)
- Delete (en el Step 6): `Assets/scripts/Keyboard.cs`, `KeyboardButton.cs`, `KeyboardControlator.cs`, `Assets/Editor/ArabicKeyboardSetup.cs`, `Assets/Editor/Layouts/Arabic102.json` y `Assets/Tests/EditMode/ArabicKeyboardSetupTests.cs`, con sus `.meta` (`git rm`)

**Interfaces:**
- Consumes: plantillas (Task 8), layouts (Task 6), vistas (Task 7).
- Produces: `public static class KeyboardGenerator`:
  - `[MenuItem("Teclado/Generar teclados")] public static void Run()`: confirma guardar, abre la escena, aplica y guarda.
  - `public static void Apply(Scene scene)`, idempotente. Pasos:
    1. Si no existen, crea en el GameObject de `KeyboardControlator` los componentes `KeyboardController`, `KeyboardModeSwitcher` y `LanguageSelector`. En el campo crea `InputFieldView`, `CaretView`, `FieldPointer` y `PreeditView` (con su `Image` `Underline`). Copia las referencias: field, dropdown y las raíces `ByRigidBodies`/`ByCanvas`.
    2. Recablea los eventos de escena:
       - `ChangeToCanvas`/`ChangeToRigid` → `KeyboardModeSwitcher`.
       - El `onValueChanged` del dropdown → `LanguageSelector.Select`.
       - Elimina los listeners de `ResetKoreanSyllable` y `ChangeLanguage`.
    3. Para cada layout ordenado por `order`:
       - Busca en cada raíz un hijo con `KeyboardView.layout == layout`. Si no existe, instancia el prefab de su plantilla (`PrefabUtility.InstantiatePrefab`) con el `localPosition`/`localRotation`/`localScale` del teclado antiguo equivalente (índice `order` de los arrays de `KeyboardControlator`, si existe) y lo nombra `displayName`.
       - Asigna `layout` y `controller`, y el target `controller` de los listeners de las especiales.
       - Para cada `KeyView`: si el layout no define su scancode, la desactiva y loguea `[Gen] aviso`. Si la define, le pone como nombre la base.
       - Las teclas del layout sin hueco en la plantilla se loguean como aviso.
    4. Rellena `LanguageSelector.entries` en orden.
    5. Borra los 16 teclados antiguos (los que tienen el componente `Keyboard`) y los componentes `KeyboardControlator`.
  - Cada paso comprueba antes de actuar.

- [ ] **Step 1: Tests** (abren la escena, `Apply`, asserts, reabren sin guardar):
  - `OchoIdiomas_DosModos`: `ByRigidBodies` y `ByCanvas` tienen 8 `KeyboardView` cada uno, con los layouts en orden, y ningún componente `Keyboard`.
  - `PlantillaCorrecta`: Español usa una tecla con scancode `0x56` activa; English no tiene ninguna tecla `0x56`.
  - `SinTeclasDeTilde`.
  - `Dropdown_8EnOrden`: las opciones son los `displayName` por `order`.
  - `CampoMontado`: `InputFieldView`, `CaretView`, `FieldPointer` y `PreeditView` con referencias no nulas (usa `TryGetComponent`, no `??`).
  - `EspecialesConTarget`: el `Caps` del teclado Español tiene un listener `OnShift` cuyo target es el `KeyboardController`.
  - `HuecosSinDefinir_Desactivados` (Review Focus 5): en Русский (ANSI14 + KBDRU), cualquier `KeyView` sin definición está inactiva, sin excepción.
  - `Idempotente`: `Apply` dos veces → mismos recuentos (8+8 teclados, 8 opciones, un `Underline`).
- [ ] **Step 2:** FAIL. **Step 3:** Implementa.
- [ ] **Step 4:** PASS. Ejecuta `ut.sh exec KeyboardGenerator.Run`. Esperado: `[Gen]` sin `Exception`; `git status` muestra `Pruebas VR.unity` modificado.
- [ ] **Step 5:** Borra los ficheros listados en **Delete**. Compila y ejecuta EditMode y PlayMode completos. Esperado: todo PASS.
- [ ] **Step 6:** Commit `Teclados generados desde plantillas; fuera las clases antiguas`.

### Task 10: Auditoría de glifos sobre distribuciones y verificación final

**Files:**
- Modify: `Assets/Editor/GlyphAudit.cs`: recorre todos los `KeyboardLayout`, sus 4 capas y todos los `to` de las tablas de teclas muertas. Comprueba la fuente de las etiquetas de su plantilla (rígida y canvas) y la del campo, más los fallbacks globales. Para RTL, también las formas unidas (como ahora). Usa `KeyboardView`/`KeyView` en lugar de las clases borradas.
- Test: `Assets/Tests/EditMode/GlyphAuditTests.cs`: `TodasLasDistribuciones_SinGlifosFaltantes` llama a un `public static List<string> GlyphAudit.Missing()` y espera la lista vacía.

- [ ] **Step 1:** Escribe el test y comprueba que falla (no existe `Missing()`). **Step 2:** Implementa. **Step 3:** PASS. Si falta algún glifo, añade la fuente de respaldo necesaria con licencia OFL y documenta cuál.
- [ ] **Step 4:** Compila y ejecuta EditMode y PlayMode completos. Pega los totales.
- [ ] **Step 5:** Pasa al usuario:
  - El informe `docs/superpowers/reports/2026-11-17-paridad-teclados.md`, para que revise las diferencias.
  - La checklist para el visor:
    - Cada idioma.
    - Español `´e` `¨u` `^a`; francés `^e`; griego `΄α` `΅ι`.
    - Coreano `한글`, `가가`, `닭`+`ㅏ`.
    - Shift de un solo uso y doble toque.
    - Cambiar de idioma con un acento pendiente.
    - Árabe como antes.
- [ ] **Step 6:** Commit `Auditoría de glifos sobre distribuciones`.
